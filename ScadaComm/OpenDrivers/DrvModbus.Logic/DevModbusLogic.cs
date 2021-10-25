﻿// Copyright (c) Rapid Software LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Scada.Comm.Config;
using Scada.Comm.Devices;
using Scada.Comm.Drivers.DrvModbus.Config;
using Scada.Comm.Drivers.DrvModbus.Protocol;
using Scada.Comm.Lang;
using Scada.Data.Models;
using Scada.Lang;
using System.Collections.Generic;
using System.Threading;

namespace Scada.Comm.Drivers.DrvModbus.Logic
{
    /// <summary>
    /// Implements the device logic.
    /// <para>Реализует логику КП.</para>
    /// </summary>
    public class DevModbusLogic : DeviceLogic
    {
        /// <summary>
        /// Represents a template dictionary.
        /// </summary>
        protected class TemplateDict : Dictionary<string, DeviceTemplate>
        {
            public override string ToString()
            {
                return Locale.IsRussian ?
                    $"Словарь из {Count} шаблонов" :
                    $"Dictionary of {Count} templates";
            }
        }

        private TransMode transMode;       // the data transfer mode of the communication line
        protected DeviceModel deviceModel; // the device model
        private ModbusPoll modbusPoll;     // implements device polling


        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public DevModbusLogic(ICommContext commContext, ILineContext lineContext, DeviceConfig deviceConfig)
            : base(commContext, lineContext, deviceConfig)
        {
            transMode = TransMode.RTU;
            deviceModel = null;
            modbusPoll = null;
        }


        /// <summary>
        /// Gets the device tag format depending on the Modbus element type.
        /// </summary>
        private TagFormat GetTagFormat(ElemType elemType)
        {
            if (elemType == ElemType.Bool)
                return TagFormat.OffOn;
            else if (elemType == ElemType.Float || elemType == ElemType.Double)
                return TagFormat.FloatNumber;
            else
                return TagFormat.IntNumber;
        }

        /// <summary>
        /// Initializes an object for polling the device.
        /// </summary>
        private void InitModbusPoll()
        {
            if (deviceModel != null)
            {
                // calculate input buffer size
                int bufferSize = 0;

                foreach (ElemGroup elemGroup in deviceModel.ElemGroups)
                {
                    if (bufferSize < elemGroup.RespAduLen)
                        bufferSize = elemGroup.RespAduLen;
                }

                foreach (ModbusCmd cmd in deviceModel.Cmds)
                {
                    if (bufferSize < cmd.RespAduLen)
                        bufferSize = cmd.RespAduLen;
                }

                // create polling object
                modbusPoll = new ModbusPoll(transMode, bufferSize)
                {
                    Timeout = PollingOptions.Timeout,
                    Connection = Connection,
                    Log = Log
                };
            }
        }

        /// <summary>
        /// Sets the data of the element group tags.
        /// </summary>
        private void SetTagData(ElemGroup elemGroup)
        {
            for (int elemIdx = 0, tagIdx = elemGroup.StartTagIdx + elemIdx, cnt = elemGroup.Elems.Count; 
                elemIdx < cnt; elemIdx++, tagIdx++)
            {
                DeviceData.Set(tagIdx, elemGroup.GetElemVal(elemIdx));
            }
        }


        /// <summary>
        /// Gets a template dictionary from the shared data of the communication line, or creates a new one.
        /// </summary>
        protected virtual TemplateDict GetTemplateDict()
        {
            const string TemplateDictKey = "Modbus.Templates";
            TemplateDict templateDict = LineContext.SharedData.ContainsKey(TemplateDictKey) ?
                LineContext.SharedData[TemplateDictKey] as TemplateDict : null;

            if (templateDict == null)
            {
                templateDict = new TemplateDict();
                LineContext.SharedData.Add(TemplateDictKey, templateDict);
            }

            return templateDict;
        }

        /// <summary>
        /// Gets the device template from the shared dictionary.
        /// </summary>
        protected virtual DeviceTemplate GetDeviceTemplate()
        {
            DeviceTemplate deviceTemplate = null;
            string fileName = PollingOptions.CmdLine.Trim();

            if (string.IsNullOrEmpty(fileName))
            {
                Log.WriteLine(string.Format(Locale.IsRussian ?
                    "Ошибка: Не задан шаблон устройства для {1}" :
                    "Error: Device template is undefined for {1}", Title));
            }
            else
            {
                TemplateDict templateDict = GetTemplateDict();

                if (templateDict.TryGetValue(fileName, out DeviceTemplate existingTemplate))
                {
                    deviceTemplate = existingTemplate;
                }
                else
                {
                    Log.WriteLine(string.Format(Locale.IsRussian ?
                        "Загрузка шаблона устройства из файла {1}" :
                        "Load device template from file {1}", fileName));

                    DeviceTemplate newTemplate = CreateDeviceTemplate();
                    templateDict.Add(fileName, newTemplate);

                    if (newTemplate.Load(Storage, fileName, out string errMsg))
                        deviceTemplate = newTemplate;
                    else
                        Log.WriteLine(errMsg);
                }
            }

            return deviceTemplate;
        }

        /// <summary>
        /// Create a new device template.
        /// </summary>
        protected virtual DeviceTemplate CreateDeviceTemplate()
        {
            return new DeviceTemplate();
        }

        /// <summary>
        /// Create a new device model.
        /// </summary>
        protected virtual DeviceModel CreateDeviceModel()
        {
            return new DeviceModel();
        }


        /// <summary>
        /// Performs actions when starting a communication line.
        /// </summary>
        public override void OnCommLineStart()
        {
            transMode = PollingOptions.CustomOptions.GetValueAsEnum("TransMode", TransMode.RTU);
        }

        /// <summary>
        /// Performs actions after setting the connection.
        /// </summary>
        public override void OnConnectionSet()
        {
            // set new line in the ASCII mode
            if (transMode == TransMode.ASCII && Connection != null)
                Connection.NewLine = ModbusUtils.CRLF;

            // update connection reference
            if (modbusPoll != null)
                modbusPoll.Connection = Connection;
        }

        /// <summary>
        /// Initializes the device tags.
        /// </summary>
        public override void InitDeviceTags()
        {
            DeviceTemplate deviceTemplate = GetDeviceTemplate();

            if (deviceTemplate == null)
                return;

            // create device model
            deviceModel = CreateDeviceModel();
            deviceModel.Addr = (byte)NumAddress;

            // add model elements and device tags
            foreach (ElemGroupConfig elemGroupConfig in deviceTemplate.ElemGroups)
            {
                bool groupActive = elemGroupConfig.Active;
                ElemGroup elemGroup = null;
                TagGroup tagGroup = new TagGroup(elemGroupConfig.Name) { Hidden = groupActive };

                if (groupActive)
                {
                    elemGroup = deviceModel.CreateElemGroup(elemGroupConfig.DataBlock);
                    elemGroup.Name = elemGroupConfig.Name;
                    elemGroup.Address = (ushort)elemGroupConfig.Address;
                }

                foreach (ElemConfig elemConfig in elemGroupConfig.Elems)
                {
                    if (groupActive)
                    {
                        Elem elem = elemGroup.CreateElem();
                        elem.Name = elemConfig.Name;
                        elem.ElemType = elemConfig.ElemType;
                        elem.ByteOrder = ModbusUtils.ParseByteOrder(elemConfig.ByteOrder) ??
                            deviceTemplate.Options.GetDefaultByteOrder(ModbusUtils.GetDataLength(elemConfig.ElemType));
                        elemGroup.Elems.Add(elem);
                    }

                    tagGroup.AddTag("", elemConfig.Name).Format = GetTagFormat(elemConfig.ElemType);
                }

                if (groupActive)
                {
                    elemGroup.InitReqPDU();
                    elemGroup.InitReqADU(deviceModel.Addr, transMode);
                    deviceModel.ElemGroups.Add(elemGroup);
                }

                DeviceTags.AddGroup(tagGroup);
            }

            // add model commands
            foreach (CmdConfig cmdConfig in deviceTemplate.Cmds)
            {
                ModbusCmd modbusCmd = deviceModel.CreateModbusCmd(cmdConfig.DataBlock, cmdConfig.Multiple);
                modbusCmd.Name = cmdConfig.Name;
                modbusCmd.Address = (ushort)cmdConfig.Address;
                modbusCmd.ElemType = cmdConfig.ElemType;
                modbusCmd.ElemCnt = cmdConfig.ElemCnt;
                modbusCmd.ByteOrder = ModbusUtils.ParseByteOrder(cmdConfig.ByteOrder) ?? 
                    deviceTemplate.Options.GetDefaultByteOrder(
                        ModbusUtils.GetDataLength(cmdConfig.ElemType) * cmdConfig.ElemCnt);
                modbusCmd.CmdNum = cmdConfig.CmdNum;

                modbusCmd.InitReqPDU();
                modbusCmd.InitReqADU(deviceModel.Addr, transMode);
                deviceModel.Cmds.Add(modbusCmd);
            }

            deviceModel.InitCmdMap();
            CanSendCommands = deviceModel.Cmds.Count > 0;
            InitModbusPoll();
        }

        /// <summary>
        /// Performs a communication session.
        /// </summary>
        public override void Session()
        {
            base.Session();

            if (deviceModel == null)
            {
                Log.WriteLine(Locale.IsRussian ?
                    "Невозможно опросить устройство, потому что модель устройства не определена" :
                    "Unable to poll the device because device model is undefined");
                Thread.Sleep(PollingOptions.Delay);
                LastRequestOK = false;
            }
            else if (deviceModel.ElemGroups.Count > 0)
            {
                // request element groups
                int elemGroupCnt = deviceModel.ElemGroups.Count;
                int elemGroupIdx = 0;

                while (elemGroupIdx < elemGroupCnt && LastRequestOK)
                {
                    ElemGroup elemGroup = deviceModel.ElemGroups[elemGroupIdx];
                    LastRequestOK = false;
                    int tryNum = 0;

                    while (RequestNeeded(ref tryNum))
                    {
                        // perform request
                        if (modbusPoll.DoRequest(elemGroup))
                        {
                            LastRequestOK = true;
                            SetTagData(elemGroup);
                        }

                        FinishRequest();
                        tryNum++;
                    }

                    if (LastRequestOK)
                    {
                        // next element group
                        elemGroupIdx++;
                    }
                    else if (tryNum > 0)
                    {
                        // set tag data as undefined for the current and the next element groups
                        while (elemGroupIdx < elemGroupCnt)
                        {
                            elemGroup = deviceModel.ElemGroups[elemGroupIdx];
                            DeviceData.Invalidate(elemGroup.StartTagIdx, elemGroup.Elems.Count);
                            elemGroupIdx++;
                        }
                    }
                }
            }
            else
            {
                Log.WriteLine(Locale.IsRussian ?
                    "Отсутствуют элементы для запроса" :
                    "No elements to request");
                Thread.Sleep(PollingOptions.Delay);
            }

            FinishSession();
        }

        /// <summary>
        /// Sends the telecontrol command.
        /// </summary>
        public override void SendCommand(TeleCommand cmd)
        {
            base.SendCommand(cmd);

            if (deviceModel.GetCmd(cmd.CmdNum) is ModbusCmd modbusCmd)
            {
                // prepare Modbus command
                if (modbusCmd.Multiple)
                {
                    modbusCmd.Value = 0;

                    if (cmd.CmdData == null)
                        modbusCmd.SetCmdData(cmd.CmdVal);
                    else
                        modbusCmd.Data = cmd.CmdData;
                }
                else
                {
                    modbusCmd.Value = modbusCmd.DataBlock == DataBlock.HoldingRegisters 
                        ? (ushort)cmd.CmdVal 
                        : (ushort)(cmd.CmdVal > 0 ? 1 : 0);
                    modbusCmd.SetCmdData(cmd.CmdVal);
                }

                modbusCmd.InitReqPDU();
                modbusCmd.InitReqADU(deviceModel.Addr, transMode);

                // send command to device
                LastRequestOK = false;
                int tryNum = 0;

                while (RequestNeeded(ref tryNum))
                {
                    if (modbusPoll.DoRequest(modbusCmd))
                        LastRequestOK = true;

                    FinishRequest();
                    tryNum++;
                }
            }
            else
            {
                LastRequestOK = false;
                Log.WriteLine(CommPhrases.InvalidCommand);
            }

            FinishCommand();
        }
    }
}