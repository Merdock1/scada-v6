﻿/*
 * Copyright 2021 Rapid Software LLC
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 * 
 * Product  : Rapid SCADA
 * Module   : ScadaAgentEngine
 * Summary  : Controls an instance that includes of one or more applications
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2021
 */

using Scada.Agent.Config;
using Scada.Lang;
using Scada.Log;
using Scada.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;

namespace Scada.Agent.Engine
{
    /// <summary>
    /// Controls an instance that includes of one or more applications.
    /// <para>Управляет экземпляром, включающим из одно или несколько приложений.</para>
    /// </summary>
    internal class ScadaInstance
    {
        /// <summary>
        /// The number of milliseconds to wait for lock.
        /// </summary>
        private const int LockTimeout = 1000;

        private readonly ILog log;                        // the application log
        private readonly InstanceOptions instanceOptions; // the instance options
        private readonly ReaderWriterLockSlim configLock; // synchronizes access to the instance configuration


        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public ScadaInstance(ILog log, InstanceOptions instanceOptions)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.instanceOptions = instanceOptions ?? throw new ArgumentNullException(nameof(instanceOptions));
            configLock = new ReaderWriterLockSlim();
            PathBuilder = new PathBuilder(instanceOptions.Directory);
        }


        /// <summary>
        /// Gets the instance name.
        /// </summary>
        public string Name => instanceOptions.Name;

        /// <summary>
        /// Gets a value indicating whether the instance is deployed on another host.
        /// </summary>
        public bool ProxyMode => instanceOptions.ProxyMode;

        /// <summary>
        /// Gets the path builder.
        /// </summary>
        public PathBuilder PathBuilder { get; }


        /// <summary>
        /// Gets the file name that contains the service status.
        /// </summary>
        private string GetStatusFileName(ServiceApp serviceApp)
        {
            switch (serviceApp)
            {
                case ServiceApp.Server:
                    return "ScadaServer.txt";
                case ServiceApp.Comm:
                    return "ScadaComm.txt";
                default:
                    throw new ArgumentException("Service not supported.");
            }
        }

        /// <summary>
        /// Gets the file name of the service command.
        /// </summary>
        private string GetCommandFileName(ServiceCommand command)
        {
            string ext = ScadaUtils.IsRunningOnWin ? ".bat" : ".sh";

            switch (command)
            {
                case ServiceCommand.Start:
                    return "svc_start" + ext;
                case ServiceCommand.Stop:
                    return "svc_stop" + ext;
                case ServiceCommand.Restart:
                    return "svc_restart" + ext;
                default:
                    throw new ArgumentException("Command not supported.");
            }
        }


        /// <summary>
        /// Validates the username and password.
        /// </summary>
        public bool ValidateUser(string username, string password, out int userID, out int roleID, out string errMsg)
        {
            userID = 0;
            roleID = AgentRoleID.Disabled;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                errMsg = Locale.IsRussian ?
                    "Имя пользователя или пароль не может быть пустым" :
                    "Username or password can not be empty";
                return false;
            }

            if (string.Equals(instanceOptions.AdminUser.Username, username, StringComparison.OrdinalIgnoreCase))
            {
                if (instanceOptions.AdminUser.Password == password)
                {
                    roleID = AgentRoleID.Administrator;
                    errMsg = "";
                    return true;
                }
            }

            if (instanceOptions.ProxyMode && 
                string.Equals(instanceOptions.AgentUser.Username, username, StringComparison.OrdinalIgnoreCase))
            {
                if (instanceOptions.AgentUser.Password == password)
                {
                    roleID = AgentRoleID.Agent;
                    errMsg = "";
                    return true;
                }
            }

            errMsg = Locale.IsRussian ?
                "Неверное имя пользователя или пароль" :
                "Invalid username or password";
            return false;
        }

        /// <summary>
        /// Gets the current status of the specified service.
        /// </summary>
        public bool GetServiceStatus(ServiceApp serviceApp, out ServiceStatus serviceStatus)
        {
            try
            {
                string statusFileName = PathBuilder.GetAbsolutePath(
                    new RelativePath(serviceApp, AppFolder.Log, GetStatusFileName(serviceApp)));

                if (File.Exists(statusFileName))
                {
                    using (FileStream stream = 
                        new FileStream(statusFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            const int MaxLineCount = 10;
                            int lineCount = 0;

                            while (!reader.EndOfStream && lineCount < MaxLineCount)
                            {
                                string line = reader.ReadLine();
                                lineCount++;

                                if (line.StartsWith("Status", StringComparison.Ordinal) ||
                                    line.StartsWith("Статус", StringComparison.Ordinal))
                                {
                                    int colonIdx = line.IndexOf(':');

                                    if (colonIdx >= 0)
                                    {
                                        string s = line.Substring(colonIdx + 1).Trim();
                                        serviceStatus = ScadaUtils.ParseServiceStatus(s);
                                        return true;
                                    }

                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.WriteError(ex, Locale.IsRussian ?
                   "Ошибка при получении статуса службы" :
                   "Error getting service status");
            }

            serviceStatus = ServiceStatus.Undefined;
            return false;
        }

        /// <summary>
        /// Sends the command to the service.
        /// </summary>
        public bool ControlService(ServiceApp serviceApp, ServiceCommand cmd, int timeout)
        {
            try
            {
                if (configLock.TryEnterReadLock(LockTimeout))
                {
                    try
                    {
                        string batchFileName = PathBuilder.GetAbsolutePath(
                            new RelativePath(serviceApp, AppFolder.Root, GetCommandFileName(cmd)));

                        if (File.Exists(batchFileName))
                        {
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                FileName = batchFileName,
                                UseShellExecute = false
                            };

                            using (Process process = Process.Start(startInfo))
                            {
                                if (timeout <= 0)
                                {
                                    return true;
                                }
                                else if (!process.WaitForExit(timeout))
                                {
                                    log.WriteError(Locale.IsRussian ?
                                        "Процесс не завершился за {0} мс. Файл {0}" :
                                        "Process did not complete in {0} ms. File {0}", timeout, batchFileName);
                                }
                                else if (process.ExitCode == 0)
                                {
                                    return true;
                                }
                                else
                                {
                                    log.WriteError(Locale.IsRussian ?
                                        "Процесс завершён с ошибкой. Файл {0}" :
                                        "Process completed with an error. File {0}", batchFileName);
                                }
                            }
                        }
                        else
                        {
                            log.WriteError(Locale.IsRussian ?
                                "Не найден файл команды управления службой {0}" :
                                "Service control command file not found {0}", batchFileName);
                        }
                    }
                    finally
                    {
                        configLock.ExitReadLock();
                    }
                }
                else
                {
                    log.WriteError(EnginePhrases.InstanceLocked, nameof(ControlService), Name);
                }
            }
            catch (Exception ex)
            {
                log.WriteError(ex, Locale.IsRussian ?
                   "Ошибка при отправке команды управления службой" :
                   "Error sending service control command");
            }

            return false;
        }

        /// <summary>
        /// Packs the configuration to the archive.
        /// </summary>
        public bool PackConfig(string destFileName, RelativePath searchPath)
        {
            return true;
        }

        /// <summary>
        /// Unpacks the configuration from the archive.
        /// </summary>
        public bool UnpackConfig(string srcFileName)
        {
            try
            {
                /*
                // delete the existing configuration
                List<RelPath> configPaths = GetConfigPaths(configOptions.ConfigParts);
                PathDict pathDict = PrepareIgnoredPaths(configOptions.IgnoredPaths);

                foreach (RelPath relPath in configPaths)
                {
                    ClearDir(relPath, pathDict);
                }

                // delete a project information file
                string instanceDir = instanceOptions.Directory;
                string projectInfoFileName = Path.Combine(instanceDir, ProjectInfoEntryName);
                File.Delete(projectInfoFileName);

                // define allowed directories to unpack
                ConfigParts configParts = configOptions.ConfigParts;
                List<string> allowedEntries = new List<string> { ProjectInfoEntryName };

                foreach (ConfigParts configPart in AllConfigParts)
                {
                    if (configParts.HasFlag(configPart))
                        allowedEntries.Add(DirectoryBuilder.GetDirectory(configPart, '/'));
                }
                */

                using (FileStream fileStream =
                    new FileStream(srcFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (ZipArchive zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read))
                    {
                        // get upload options
                        TransferOptions uploadOptions = new TransferOptions();
                        ZipArchiveEntry optionsEntry = zipArchive.GetEntry(AgentConst.UploadOptionsEntry);

                        if (optionsEntry == null)
                        {
                            throw new ScadaException(Locale.IsRussian ?
                                "Параметры передачи не найдены." :
                                "Upload options not found.");
                        }

                        using (Stream optionsStream = optionsEntry.Open())
                        {
                            uploadOptions.Load(optionsStream);
                        }

                        // delete existing configuration

                        // unpack configuration
                        string instanceDir = instanceOptions.Directory;
                        List<string> allowedEntries = new List<string> { AgentConst.ProjectInfoEntry };

                        foreach (ZipArchiveEntry entry in zipArchive.Entries)
                        {
                            if (allowedEntries.Any(s => entry.FullName.StartsWith(s, StringComparison.Ordinal)))
                            {
                                string relPath = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                                string destFileName = Path.Combine(instanceDir, relPath);
                                Directory.CreateDirectory(Path.GetDirectoryName(destFileName));
                                entry.ExtractToFile(destFileName, true);
                            }
                        }

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                log.WriteError(ex, Locale.IsRussian ?
                    "Ошибка при распаковке конфигурации из архива" :
                    "Error unpacking configuration from archive");
                return false;
            }
        }
    }
}