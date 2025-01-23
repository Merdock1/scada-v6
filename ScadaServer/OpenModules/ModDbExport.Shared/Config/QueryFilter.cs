﻿// Copyright (c) Rapid Software LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Xml;

namespace Scada.Server.Modules.ModDbExport.Config
{
    /// <summary>
    /// Represents a query filter.
    /// <para>Представляет фильтр запроса.</para>
    /// </summary>
    [Serializable]
    internal class QueryFilter
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public QueryFilter()
        {
            CnlNums = [];
            ObjNums = [];
            DeviceNums = [];
        }


        /// <summary>
        /// Gets the channel numbers.
        /// </summary>
        public List<int> CnlNums { get; }

        /// <summary>
        /// Gets the object numbers.
        /// </summary>
        public List<int> ObjNums { get; }

        /// <summary>
        /// Gets the device numbers.
        /// </summary>
        public List<int> DeviceNums { get; }

        /// <summary>
        /// Gets a value indicating whether the filter is not set.
        /// </summary>
        public bool IsEmpty =>
            CnlNums.Count == 0 && 
            ObjNums.Count == 0 && 
            DeviceNums.Count == 0;


        /// <summary>
        /// Loads the options from the XML node.
        /// </summary>
        public void LoadFromXml(XmlNode xmlNode)
        {
            ArgumentNullException.ThrowIfNull(xmlNode, nameof(xmlNode));
            CnlNums.AddRange(ScadaUtils.ParseRange(xmlNode.GetChildAsString("CnlNums"), true, true));
            ObjNums.AddRange(ScadaUtils.ParseRange(xmlNode.GetChildAsString("ObjNums"), true, true));
            DeviceNums.AddRange(ScadaUtils.ParseRange(xmlNode.GetChildAsString("DeviceNums"), true, true));
        }

        /// <summary>
        /// Saves the options into the XML node.
        /// </summary>
        public void SaveToXml(XmlElement xmlElem)
        {
            ArgumentNullException.ThrowIfNull(xmlElem, nameof(xmlElem));

            if (CnlNums.Count > 0)
                xmlElem.AppendElem("CnlNums", CnlNums.ToRangeString());

            if (ObjNums.Count > 0)
                xmlElem.AppendElem("ObjNums", ObjNums.ToRangeString());

            if (DeviceNums.Count > 0)
                xmlElem.AppendElem("DeviceNums", DeviceNums.ToRangeString());
        }
    }
}
