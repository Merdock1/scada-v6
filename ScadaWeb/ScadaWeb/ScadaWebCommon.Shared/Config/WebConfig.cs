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
 * Module   : ScadaWebCommon
 * Summary  : The phrases used by the web application and its plugins
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2021
 * Modified : 2021
 */

using Scada.Client;
using Scada.Config;
using Scada.Lang;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Scada.Web.Config
{
    /// <summary>
    /// Represents a web application configuration.
    /// <para>Представляет конфигурацию веб-приложения.</para>
    /// </summary>
    public class WebConfig : IConfig
    {
        /// <summary>
        /// The default configuration file name.
        /// </summary>
        public const string DefaultFileName = "ScadaWebConfig.xml";


        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WebConfig()
        {
            SetToDefault();
        }


        /// <summary>
        /// Gets the general options.
        /// </summary>
        public GeneralOptions GeneralOptions { get; private set; }

        /// <summary>
        /// Gets the connection options.
        /// </summary>
        public ConnectionOptions ConnectionOptions { get; private set; }

        /// <summary>
        /// Gets the login options.
        /// </summary>
        public LoginOptions LoginOptions { get; private set; }

        /// <summary>
        /// Gets the display options.
        /// </summary>
        public DisplayOptions DisplayOptions { get; private set; }

        /// <summary>
        /// Gets the codes of the enabled plugins.
        /// </summary>
        public List<string> PluginCodes { get; private set; }

        /// <summary>
        /// Gets the plugin assignment.
        /// </summary>
        public PluginAssignment PluginAssignment { get; private set; }

        /// <summary>
        /// Gets the groups of custom options.
        /// </summary>
        public SortedList<string, OptionList> CustomOptions { get; private set; }



        /// <summary>
        /// Sets the default values.
        /// </summary>
        private void SetToDefault()
        {
            GeneralOptions = new GeneralOptions();
            ConnectionOptions = new ConnectionOptions();
            LoginOptions = new LoginOptions();
            DisplayOptions = new DisplayOptions();
            PluginCodes = new List<string>();
            PluginAssignment = new PluginAssignment();
            CustomOptions = new SortedList<string, OptionList>();
        }

        /// <summary>
        /// Loads the configuration from the specified file.
        /// </summary>
        public bool Load(string fileName, out string errMsg)
        {
            try
            {
                SetToDefault();

                if (!File.Exists(fileName))
                    throw new FileNotFoundException(string.Format(CommonPhrases.NamedFileNotFound, fileName));

                XmlDocument xmlDoc = new();
                xmlDoc.Load(fileName);
                XmlElement rootElem = xmlDoc.DocumentElement;

                if (rootElem.SelectSingleNode("GeneralOptions") is XmlNode generalOptionsNode)
                    GeneralOptions.LoadFromXml(generalOptionsNode);

                if (rootElem.SelectSingleNode("ConnectionOptions") is XmlNode connectionOptionsNode)
                    ConnectionOptions.LoadFromXml(connectionOptionsNode);

                if (rootElem.SelectSingleNode("LoginOptions") is XmlNode loginOptionsNode)
                    LoginOptions.LoadFromXml(loginOptionsNode);

                if (rootElem.SelectSingleNode("DisplayOptions") is XmlNode displayOptionsNode)
                    DisplayOptions.LoadFromXml(displayOptionsNode);

                HashSet<string> pluginCodes = new();

                if (rootElem.SelectSingleNode("Plugins") is XmlNode pluginsNode)
                {
                    foreach (XmlElement pluginElem in pluginsNode.SelectNodes("Plugin"))
                    {
                        string pluginCode = ScadaUtils.RemoveFileNameSuffixes(pluginElem.GetAttribute("code"));

                        if (pluginCodes.Add(pluginCode.ToLowerInvariant())) // check uniqueness
                            PluginCodes.Add(pluginCode);
                    }
                }


                if (rootElem.SelectSingleNode("PluginAssignment") is XmlNode pluginAssignmentNode)
                    PluginAssignment.LoadFromXml(pluginAssignmentNode);

                if (rootElem.SelectSingleNode("CustomOptions") is XmlNode customOptionsNode)
                {
                    foreach (XmlElement optionGroupElem in customOptionsNode.SelectNodes("OptionGroup"))
                    {
                        OptionList optionList = new();
                        optionList.LoadFromXml(optionGroupElem);
                        CustomOptions[optionGroupElem.GetAttrAsString("name")] = optionList;
                    }
                }

                errMsg = "";
                return true;
            }
            catch (Exception ex)
            {
                errMsg = ScadaUtils.BuildErrorMessage(ex, CommonPhrases.LoadAppConfigError);
                return false;
            }
        }

        /// <summary>
        /// Saves the configuration to the specified file.
        /// </summary>
        public bool Save(string fileName, out string errMsg)
        {
            try
            {
                XmlDocument xmlDoc = new();
                XmlDeclaration xmlDecl = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);
                xmlDoc.AppendChild(xmlDecl);

                XmlElement rootElem = xmlDoc.CreateElement("ScadaWebConfig");
                xmlDoc.AppendChild(rootElem);

                GeneralOptions.SaveToXml(rootElem.AppendElem("GeneralOptions"));
                ConnectionOptions.SaveToXml(rootElem.AppendElem("ConnectionOptions"));
                LoginOptions.SaveToXml(rootElem.AppendElem("LoginOptions"));
                DisplayOptions.SaveToXml(rootElem.AppendElem("DisplayOptions"));

                XmlElement pluginsElem = rootElem.AppendElem("Plugins");
                foreach (string pluginCode in PluginCodes)
                {
                    pluginsElem.AppendElem("Plugin").SetAttribute("code", pluginCode);
                }

                PluginAssignment.SaveToXml(rootElem.AppendElem("PluginAssignment"));

                XmlElement customOptionsElem = rootElem.AppendElem("CustomOptions");
                foreach (KeyValuePair<string, OptionList> pair in CustomOptions)
                {
                    XmlElement optionGroupElem = customOptionsElem.AppendElem("OptionGroup");
                    optionGroupElem.SetAttribute("name", pair.Key);
                    pair.Value.SaveToXml(optionGroupElem);
                }

                xmlDoc.Save(fileName);
                errMsg = "";
                return true;
            }
            catch (Exception ex)
            {
                errMsg = ScadaUtils.BuildErrorMessage(ex, CommonPhrases.SaveAppConfigError);
                return false;
            }
        }

        /// <summary>
        /// Gets the list of options by the specified group name, or an empty list if the group is not found.
        /// </summary>
        public OptionList GetOptions(string groupName)
        {
            return CustomOptions.TryGetValue(groupName, out OptionList options) ? options : new OptionList();
        }
    }
}