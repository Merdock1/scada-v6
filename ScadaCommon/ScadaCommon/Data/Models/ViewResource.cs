﻿/*
 * Copyright 2025 Rapid Software LLC
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
 * Module   : ScadaCommon
 * Summary  : Represents a view resource
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2021
 * Modified : 2021
 */

namespace Scada.Data.Models
{
    /// <summary>
    /// Represents a view resource.
    /// <para>Представляет ресурс представления.</para>
    /// </summary>
    public class ViewResource
    {
        /// <summary>
        /// Gets or sets the resource name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the view-dependent type code.
        /// </summary>
        public string TypeCode { get; set; }

        /// <summary>
        /// Gets or sets the path relative to the view directory.
        /// </summary>
        public string Path { get; set; }
    }
}
