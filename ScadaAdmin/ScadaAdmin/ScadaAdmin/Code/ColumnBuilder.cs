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
 * Module   : Administrator
 * Summary  : Creates columns for a DataGridView control
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2019
 */

using Scada.Admin.Project;
using Scada.Data.Const;
using Scada.Data.Entities;
using Scada.Data.Tables;
using Scada.Lang;
using System;
using System.Data;
using System.Windows.Forms;

namespace Scada.Admin.App.Code
{
    /// <summary>
    /// Creates columns for a DataGridView control.
    /// <para>Создает столбцы для элемента управления DataGridView.</para>
    /// </summary>
    internal static class ColumnBuilder
    {
        /// <summary>
        /// Creates a new column that hosts text cells.
        /// </summary>
        private static DataGridViewColumn NewTextBoxColumn(string dataPropertyName, ColumnOptions options = null)
        {
            DataGridViewTextBoxColumn column = new()
            {
                Name = dataPropertyName,
                HeaderText = dataPropertyName,
                DataPropertyName = dataPropertyName,
                Tag = options
            };

            if (options != null && options.MaxLength > 0)
                column.MaxInputLength = options.MaxLength;

            return column;
        }

        /// <summary>
        /// Creates a new column that hosts cells with checkboxes.
        /// </summary>
        private static DataGridViewColumn NewCheckBoxColumn(string dataPropertyName, ColumnOptions options = null)
        {
            return new DataGridViewCheckBoxColumn
            {
                Name = dataPropertyName,
                HeaderText = dataPropertyName,
                DataPropertyName = dataPropertyName,
                Tag = options,
                SortMode = DataGridViewColumnSortMode.Automatic
            };
        }

        /// <summary>
        /// Creates a new column that hosts cells with buttons.
        /// </summary>
        private static DataGridViewColumn NewButtonColumn(string dataPropertyName, ColumnOptions options = null)
        {
            return new DataGridViewButtonColumn
            {
                Name = dataPropertyName + (options == null ? ColumnKind.Button : options.Kind),
                HeaderText = dataPropertyName,
                DataPropertyName = dataPropertyName,
                Tag = options,
                Text = dataPropertyName,
                UseColumnTextForButtonValue = true
            };
        }

        /// <summary>
        /// Creates a new column that hosts cells which values are selected from a combo box.
        /// </summary>
        private static DataGridViewColumn NewComboBoxColumn(string dataPropertyName, string valueMember, 
            string displayMember, object dataSource, bool addEmptyRow = false, bool prependID = false,
            ColumnOptions options = null)
        {
            if (dataSource is IBaseTable baseTable)
                dataSource = CreateComboBoxSource(baseTable, valueMember, ref displayMember, addEmptyRow, prependID);

            return new DataGridViewComboBoxColumn
            {
                Name = dataPropertyName,
                HeaderText = dataPropertyName,
                DataPropertyName = dataPropertyName,
                ValueMember = valueMember,
                DisplayMember = displayMember,
                DataSource = dataSource,
                Tag = options,
                SortMode = DataGridViewColumnSortMode.Automatic,
                DisplayStyleForCurrentCellOnly = true
            };
        }

        /// <summary>
        /// Creates a new column that hosts cells which values are selected from a combo box.
        /// </summary>
        private static DataGridViewColumn NewComboBoxColumn(string dataPropertyName, 
            string displayMember, IBaseTable dataSource, bool addEmptyRow = false, bool prependID = false, 
            ColumnOptions options = null)
        {
            return NewComboBoxColumn(dataPropertyName, dataPropertyName, 
                displayMember, dataSource, addEmptyRow, prependID, options);
        }

        /// <summary>
        /// Creates a data table for using as a data source of a combo box.
        /// </summary>
        private static DataTable CreateComboBoxSource(
            IBaseTable baseTable, string valueMember, ref string displayMember, bool addEmptyRow, bool prependID)
        {
            DataTable dataTable = baseTable.ToDataTable(true);

            if (prependID)
            {
                // display ID and name
                string columnName = valueMember + "_" + displayMember;
                dataTable.Columns.Add(columnName, typeof(string), 
                    string.Format("'[' + {0} + '] ' + {1}", valueMember, displayMember));
                displayMember = columnName;
                dataTable.DefaultView.Sort = valueMember;
            }
            else
            {
                dataTable.DefaultView.Sort = displayMember;
            }

            if (addEmptyRow)
            {
                DataRow emptyRow = dataTable.NewRow();
                emptyRow[valueMember] = DBNull.Value;
                emptyRow[displayMember] = " ";
                dataTable.Rows.Add(emptyRow);
            }

            return dataTable;
        }

        /// <summary>
        /// Translates the column headers.
        /// </summary>
        private static DataGridViewColumn[] TranslateHeaders(string tableName, DataGridViewColumn[] columns)
        {
            if (Locale.Dictionaries.TryGetValue(typeof(ColumnBuilder).FullName + "." + tableName, 
                out LocaleDict localeDict))
            {
                foreach (DataGridViewColumn col in columns)
                {
                    if (localeDict.Phrases.TryGetValue(col.Name, out string header))
                    {
                        if (col is DataGridViewButtonColumn buttonColumn)
                            buttonColumn.Text = header;
                        col.HeaderText = header;
                    }
                }
            }

            return columns;
        }


        /// <summary>
        /// Creates columns for the archive table.
        /// </summary>
        private static DataGridViewColumn[] CreateArchiveTableColumns()
        {
            return TranslateHeaders("ArchiveTable", new DataGridViewColumn[]
            {
                NewTextBoxColumn("ArchiveID", new ColumnOptions(ConfigBase.MinID, ConfigBase.MaxID)),
                NewTextBoxColumn("Name", new ColumnOptions(ColumnLength.Name)),
                NewTextBoxColumn("Code", new ColumnOptions(ColumnLength.Code)),
                NewCheckBoxColumn("IsDefault"),
                NewTextBoxColumn("Bit", new ColumnOptions(0, 31)),
                NewTextBoxColumn("Descr", new ColumnOptions(ColumnLength.Description))
            });
        }

        /// <summary>
        /// Creates columns for the command type table.
        /// </summary>
        private static DataGridViewColumn[] CreateCmdTypeTableColumns()
        {
            return TranslateHeaders("CmdTypeTable", new DataGridViewColumn[]
            {
                NewTextBoxColumn("CmdTypeID", new ColumnOptions(ConfigBase.MinID, ConfigBase.MaxID)),
                NewTextBoxColumn("Name", new ColumnOptions(ColumnLength.Name)),
                NewTextBoxColumn("Descr", new ColumnOptions(ColumnLength.Description))
            });
        }

        /// <summary>
        /// Creates columns for the channel status table.
        /// </summary>
        private static DataGridViewColumn[] CreateCnlStatusTableColumns()
        {
            return TranslateHeaders("CnlStatusTable", new DataGridViewColumn[]
            {
                NewTextBoxColumn("CnlStatusID", new ColumnOptions(0, ConfigBase.MaxID)),
                NewTextBoxColumn("Name", new ColumnOptions(ColumnLength.Name)),
                NewTextBoxColumn("MainColor", new ColumnOptions(ColumnKind.Color, ColumnLength.Default)),
                NewButtonColumn("MainColor"),
                NewTextBoxColumn("SecondColor", new ColumnOptions(ColumnKind.Color, ColumnLength.Default)),
                NewButtonColumn("SecondColor"),
                NewTextBoxColumn("BackColor", new ColumnOptions(ColumnKind.Color, ColumnLength.Default)),
                NewButtonColumn("BackColor"),
                NewTextBoxColumn("Severity", new ColumnOptions(Severity.Min, Severity.Max)),
                NewTextBoxColumn("Descr", new ColumnOptions(ColumnLength.Description))
            });
        }

        /// <summary>
        /// Creates columns for the channel type table.
        /// </summary>
        private static DataGridViewColumn[] CreateCnlTypeTableColumns()
        {
            return TranslateHeaders("CnlTypeTable", new DataGridViewColumn[]
            {
                NewTextBoxColumn("CnlTypeID", new ColumnOptions(ConfigBase.MinID, ConfigBase.MaxID)),
                NewTextBoxColumn("Name", new ColumnOptions(ColumnLength.Name)),
                NewTextBoxColumn("Descr", new ColumnOptions(ColumnLength.Description))
            });
        }

        /// <summary>
        /// Creates columns for the communication line table.
        /// </summary>
        private static DataGridViewColumn[] CreateCommLineTableColumns()
        {
            return TranslateHeaders("CommLineTable", new DataGridViewColumn[]
            {
                NewTextBoxColumn("CommLineNum", new ColumnOptions(ConfigBase.MinID, ConfigBase.MaxID)),
                NewTextBoxColumn("Name", new ColumnOptions(ColumnLength.Name)),
                NewTextBoxColumn("Descr", new ColumnOptions(ColumnLength.Description))
            });
        }

        /// <summary>
        /// Creates columns for the data type table.
        /// </summary>
        private static DataGridViewColumn[] CreateDataTypeTableColumns()
        {
            return TranslateHeaders("DataTypeTable", new DataGridViewColumn[]
            {
                NewTextBoxColumn("DataTypeID", new ColumnOptions(ConfigBase.MinID, ConfigBase.MaxID)),
                NewTextBoxColumn("Name", new ColumnOptions(ColumnLength.Name)),
                NewTextBoxColumn("Descr", new ColumnOptions(ColumnLength.Description))
            });
        }

        /// <summary>
        /// Creates columns for the devices table.
        /// </summary>
        private static DataGridViewColumn[] CreateDeviceTableColumns(ConfigBase configBase)
        {
            return TranslateHeaders("DeviceTable", new DataGridViewColumn[]
            {
                NewTextBoxColumn("DeviceNum", new ColumnOptions(ConfigBase.MinID, ConfigBase.MaxID)),
                NewTextBoxColumn("Name", new ColumnOptions(ColumnLength.Name)),
                NewTextBoxColumn("Code", new ColumnOptions(ColumnLength.Code)),
                NewComboBoxColumn("DevTypeID", "Name", configBase.DevTypeTable, true),
                NewTextBoxColumn("NumAddress"),
                NewTextBoxColumn("StrAddress", new ColumnOptions(ColumnLength.Default)),
                NewComboBoxColumn("CommLineNum", "Name", configBase.CommLineTable, true),
                NewTextBoxColumn("Descr", new ColumnOptions(ColumnLength.Description))
            });
        }

        /// <summary>
        /// Creates columns for the device type table.
        /// </summary>
        private static DataGridViewColumn[] CreateDevTypeTableColumns()
        {
            return TranslateHeaders("DevTypeTable", new DataGridViewColumn[]
            {
                NewTextBoxColumn("DevTypeID", new ColumnOptions(ConfigBase.MinID, ConfigBase.MaxID)),
                NewTextBoxColumn("Name", new ColumnOptions(ColumnLength.Name)),
                NewTextBoxColumn("Driver", new ColumnOptions(ColumnLength.Default)),
                NewTextBoxColumn("Descr", new ColumnOptions(ColumnLength.Description))
            });
        }

        /// <summary>
        /// Creates columns for the format table.
        /// </summary>
        private static DataGridViewColumn[] CreateFormatTableColumns()
        {
            return TranslateHeaders("FormatTable", new DataGridViewColumn[]
            {
                NewTextBoxColumn("FormatID", new ColumnOptions(ConfigBase.MinID, ConfigBase.MaxID)),
                NewTextBoxColumn("Name", new ColumnOptions(ColumnLength.Name)),
                NewTextBoxColumn("Code", new ColumnOptions(ColumnLength.Code)),
                NewCheckBoxColumn("IsNumber"),
                NewCheckBoxColumn("IsEnum"),
                NewCheckBoxColumn("IsDate"),
                NewCheckBoxColumn("IsString"),
                NewTextBoxColumn("Frmt", new ColumnOptions(ColumnLength.Enumeration)),
                NewTextBoxColumn("Descr", new ColumnOptions(ColumnLength.Description))
            });
        }

        /// <summary>
        /// Creates columns for the input channel table.
        /// </summary>
        private static DataGridViewColumn[] CreateInCnlTableColumns(ConfigBase configBase)
        {
            return TranslateHeaders("InCnlTable", new DataGridViewColumn[]
            {
                NewTextBoxColumn("CnlNum", new ColumnOptions(ConfigBase.MinID, ConfigBase.MaxID)),
                NewCheckBoxColumn("Active", new ColumnOptions { DefaultValue = true }),
                NewTextBoxColumn("Name", new ColumnOptions(ColumnLength.Name)),
                NewComboBoxColumn("CnlTypeID", "Name", configBase.CnlTypeTable, false, false,
                    new ColumnOptions { DefaultValue = CnlTypeID.Measured }),
                NewComboBoxColumn("ObjNum","Name", configBase.ObjTable, true),
                NewComboBoxColumn("DeviceNum", "Name", configBase.DeviceTable, true),
                NewTextBoxColumn("DataLen"),
                NewTextBoxColumn("TagNum"),
                NewTextBoxColumn("TagCode", new ColumnOptions(ColumnLength.Code)),
                NewCheckBoxColumn("FormulaEnabled"),
                NewTextBoxColumn("Formula", new ColumnOptions(ColumnLength.Default)),
                NewTextBoxColumn("ArchiveMask", new ColumnOptions(ColumnKind.BitMask)),
                NewTextBoxColumn("EventMask", new ColumnOptions(ColumnKind.BitMask)),
                NewComboBoxColumn("DataTypeID", "Name", configBase.DataTypeTable, true),
                NewComboBoxColumn("FormatID", "Name", configBase.FormatTable, true),
                NewComboBoxColumn("QuantityID", "Name", configBase.QuantityTable, true),
                NewComboBoxColumn("UnitID", "Name", configBase.UnitTable, true),
                NewComboBoxColumn("LimID", "Name", configBase.LimTable, true),
            });
        }

        /// <summary>
        /// Creates columns for the limit table.
        /// </summary>
        private static DataGridViewColumn[] CreateLimTableColumns()
        {
            return TranslateHeaders("LimTable", new DataGridViewColumn[]
            {
                NewTextBoxColumn("LimID", new ColumnOptions(ConfigBase.MinID, ConfigBase.MaxID)),
                NewTextBoxColumn("Name", new ColumnOptions(ColumnLength.Name)),
                NewCheckBoxColumn("IsBoundToCnl"),
                NewCheckBoxColumn("IsShared"),
                NewTextBoxColumn("LoLo"),
                NewTextBoxColumn("Low"),
                NewTextBoxColumn("High"),
                NewTextBoxColumn("HiHi"),
                NewTextBoxColumn("Deadband")
            });
        }

        /// <summary>
        /// Creates columns for the object table.
        /// </summary>
        private static DataGridViewColumn[] CreateObjTableColumns(ConfigBase configBase)
        {
            return TranslateHeaders("ObjTable", new DataGridViewColumn[]
            {
                NewTextBoxColumn("ObjNum", new ColumnOptions(ConfigBase.MinID, ConfigBase.MaxID)),
                NewTextBoxColumn("Code", new ColumnOptions(ColumnLength.Code)),
                NewTextBoxColumn("Name", new ColumnOptions(ColumnLength.Name)),
                NewComboBoxColumn("ParentObjNum", "ObjNum", "Name", configBase.ObjTable, true),
                NewTextBoxColumn("Descr", new ColumnOptions(ColumnLength.Description))
            });
        }

        /// <summary>
        /// Creates columns for the object right table.
        /// </summary>
        private static DataGridViewColumn[] CreateObjRightTableColumns(ConfigBase configBase)
        {
            return TranslateHeaders("ObjRightTable", new DataGridViewColumn[]
            {
                NewTextBoxColumn("ObjRightID", new ColumnOptions(ConfigBase.MinID, ConfigBase.MaxID)),
                NewComboBoxColumn("ObjNum","Name", configBase.ObjTable),
                NewComboBoxColumn("RoleID", "Name", configBase.RoleTable),
                NewCheckBoxColumn("View"),
                NewCheckBoxColumn("Control")
            });
        }

        /// <summary>
        /// Creates columns for the output channel table.
        /// </summary>
        private static DataGridViewColumn[] CreateOutCnlTableColumns(ConfigBase configBase)
        {
            return TranslateHeaders("OutCnlTable", new DataGridViewColumn[]
            {
                NewTextBoxColumn("OutCnlNum", new ColumnOptions(ConfigBase.MinID, ConfigBase.MaxID)),
                NewCheckBoxColumn("Active", new ColumnOptions { DefaultValue = true }),
                NewTextBoxColumn("Name", new ColumnOptions(ColumnLength.Name)),
                NewComboBoxColumn("CmdTypeID", "Name", configBase.CmdTypeTable, false, false,
                    new ColumnOptions { DefaultValue = CmdTypeID.Standard }),
                NewComboBoxColumn("ObjNum", "Name", configBase.ObjTable, true),
                NewComboBoxColumn("DeviceNum", "Name", configBase.DeviceTable, true),
                NewTextBoxColumn("CmdNum"),
                NewTextBoxColumn("CmdCode", new ColumnOptions(ColumnLength.Code)),
                NewCheckBoxColumn("FormulaEnabled"),
                NewTextBoxColumn("Formula", new ColumnOptions(ColumnLength.Default)),
                NewCheckBoxColumn("EventEnabled")
            });
        }

        /// <summary>
        /// Creates columns for the quantity table.
        /// </summary>
        private static DataGridViewColumn[] CreateQuantityTableColumns()
        {
            return TranslateHeaders("QuantityTable", new DataGridViewColumn[]
            {
                NewTextBoxColumn("QuantityID", new ColumnOptions(ConfigBase.MinID, ConfigBase.MaxID)),
                NewTextBoxColumn("Name", new ColumnOptions(ColumnLength.Name)),
                NewTextBoxColumn("Code", new ColumnOptions(ColumnLength.Code)),
                NewTextBoxColumn("Icon", new ColumnOptions(ColumnLength.Default)),
                NewTextBoxColumn("Descr", new ColumnOptions(ColumnLength.Description))
            });
        }

        /// <summary>
        /// Creates columns for the role table.
        /// </summary>
        private static DataGridViewColumn[] CreateRoleTableColumns()
        {
            return TranslateHeaders("RoleTable", new DataGridViewColumn[]
            {
                NewTextBoxColumn("RoleID", new ColumnOptions(ConfigBase.MinID, ConfigBase.MaxID)),
                NewTextBoxColumn("Name", new ColumnOptions(ColumnLength.Name)),
                NewTextBoxColumn("Code", new ColumnOptions(ColumnLength.Code)),
                NewTextBoxColumn("Descr", new ColumnOptions(ColumnLength.Description))
            });
        }

        /// <summary>
        /// Creates columns for the role inheritance table.
        /// </summary>
        private static DataGridViewColumn[] CreateRoleRefTableColumns(ConfigBase configBase)
        {
            return TranslateHeaders("RoleRefTable", new DataGridViewColumn[]
            {
                NewTextBoxColumn("RoleRefID", new ColumnOptions(ConfigBase.MinID, ConfigBase.MaxID)),
                NewComboBoxColumn("ParentRoleID", "RoleID", "Name", configBase.RoleTable),
                NewComboBoxColumn("ChildRoleID", "RoleID", "Name", configBase.RoleTable)
            });
        }

        /// <summary>
        /// Creates columns for the script table.
        /// </summary>
        private static DataGridViewColumn[] CreateScriptTableColumns()
        {
            return TranslateHeaders("ScriptTable", new DataGridViewColumn[]
            {
                NewTextBoxColumn("ScriptID", new ColumnOptions(ConfigBase.MinID, ConfigBase.MaxID)),
                NewTextBoxColumn("Name", new ColumnOptions(ColumnLength.Name)),
                NewTextBoxColumn("Source", new ColumnOptions(ColumnKind.SourceCode, ColumnLength.SourceCode)),
                NewButtonColumn("Source"),
                NewTextBoxColumn("Descr", new ColumnOptions(ColumnLength.Description))
            });
        }

        /// <summary>
        /// Creates columns for the unit table.
        /// </summary>
        private static DataGridViewColumn[] CreateUnitTableColumns()
        {
            return TranslateHeaders("UnitTable", new DataGridViewColumn[]
            {
                NewTextBoxColumn("UnitID", new ColumnOptions(ConfigBase.MinID, ConfigBase.MaxID)),
                NewTextBoxColumn("Name", new ColumnOptions(ColumnLength.Name)),
                NewTextBoxColumn("Code", new ColumnOptions(ColumnLength.Code)),
                NewTextBoxColumn("Descr", new ColumnOptions(ColumnLength.Description))
            });
        }

        /// <summary>
        /// Creates columns for the user table.
        /// </summary>
        private static DataGridViewColumn[] CreateUserTableColumns(ConfigBase configBase)
        {
            return TranslateHeaders("UserTable", new DataGridViewColumn[]
            {
                NewTextBoxColumn("UserID", new ColumnOptions(ConfigBase.MinID, ConfigBase.MaxID)),
                NewCheckBoxColumn("Enabled", new ColumnOptions { DefaultValue = true }),
                NewTextBoxColumn("Name", new ColumnOptions(ColumnLength.Name)),
                NewTextBoxColumn("Password", new ColumnOptions(ColumnKind.Password, ColumnLength.Password)),
                NewComboBoxColumn("RoleID", "Name", configBase.RoleTable),
                NewTextBoxColumn("Descr", new ColumnOptions(ColumnLength.Description))
            });
        }

        /// <summary>
        /// Creates columns for the view table.
        /// </summary>
        private static DataGridViewColumn[] CreateViewTableColumns(ConfigBase configBase)
        {
            return TranslateHeaders("ViewTable", new DataGridViewColumn[]
            {
                NewTextBoxColumn("ViewID", new ColumnOptions(ConfigBase.MinID, ConfigBase.MaxID)),
                NewTextBoxColumn("Path", new ColumnOptions(ColumnKind.Path, ColumnLength.Long)),
                NewButtonColumn("Path", new ColumnOptions(ColumnKind.SelectFileButton)),
                NewButtonColumn("Path", new ColumnOptions(ColumnKind.SelectFolderButton)),
                NewComboBoxColumn("ViewTypeID", "Name", configBase.ViewTypeTable, true),
                NewComboBoxColumn("ObjNum","Name", configBase.ObjTable, true),
                NewTextBoxColumn("Args", new ColumnOptions(ColumnLength.Default)),
                NewTextBoxColumn("Title", new ColumnOptions(ColumnLength.Long)),
                NewTextBoxColumn("Ord"),
                NewCheckBoxColumn("Hidden"),
            });
        }

        /// <summary>
        /// Creates columns for the view type table.
        /// </summary>
        private static DataGridViewColumn[] CreateViewTypeTableColumns()
        {
            return TranslateHeaders("ViewTypeTable", new DataGridViewColumn[]
            {
                NewTextBoxColumn("ViewTypeID", new ColumnOptions(ConfigBase.MinID, ConfigBase.MaxID)),
                NewTextBoxColumn("Name", new ColumnOptions(ColumnLength.Name)),
                NewTextBoxColumn("Code", new ColumnOptions(ColumnLength.Code)),
                NewTextBoxColumn("FileExt", new ColumnOptions(ColumnLength.Default)),
                NewTextBoxColumn("Descr", new ColumnOptions(ColumnLength.Description))
            });
        }


        /// <summary>
        /// Creates columns for the specified table
        /// </summary>
        public static DataGridViewColumn[] CreateColumns(ConfigBase configBase, Type itemType)
        {
            if (configBase == null)
                throw new ArgumentNullException(nameof(configBase));

            if (itemType == typeof(Archive))
                return CreateArchiveTableColumns();
            else if (itemType == typeof(CmdType))
                return CreateCmdTypeTableColumns();
            else if (itemType == typeof(CnlStatus))
                return CreateCnlStatusTableColumns();
            else if (itemType == typeof(CnlType))
                return CreateCnlTypeTableColumns();
            else if (itemType == typeof(CommLine))
                return CreateCommLineTableColumns();
            else if (itemType == typeof(DataType))
                return CreateDataTypeTableColumns();
            else if (itemType == typeof(Device))
                return CreateDeviceTableColumns(configBase);
            else if (itemType == typeof(DevType))
                return CreateDevTypeTableColumns();
            else if (itemType == typeof(Format))
                return CreateFormatTableColumns();
            else if (itemType == typeof(InCnl))
                return CreateInCnlTableColumns(configBase);
            else if (itemType == typeof(Lim))
                return CreateLimTableColumns();
            else if (itemType == typeof(Obj))
                return CreateObjTableColumns(configBase);
            else if (itemType == typeof(ObjRight))
                return CreateObjRightTableColumns(configBase);
            else if (itemType == typeof(OutCnl))
                return CreateOutCnlTableColumns(configBase);
            else if (itemType == typeof(Quantity))
                return CreateQuantityTableColumns();
            else if (itemType == typeof(Role))
                return CreateRoleTableColumns();
            else if (itemType == typeof(RoleRef))
                return CreateRoleRefTableColumns(configBase);
            else if (itemType == typeof(Script))
                return CreateScriptTableColumns();
            else if (itemType == typeof(Unit))
                return CreateUnitTableColumns();
            else if (itemType == typeof(User))
                return CreateUserTableColumns(configBase);
            else if (itemType == typeof(Data.Entities.View))
                return CreateViewTableColumns(configBase);
            else if (itemType == typeof(ViewType))
                return CreateViewTypeTableColumns();
            else
                return Array.Empty<DataGridViewColumn>();
        }
    }
}
