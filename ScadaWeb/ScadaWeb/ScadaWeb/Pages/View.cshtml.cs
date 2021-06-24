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
 * Module   : Webstation Application
 * Summary  : Represents a page of a view
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2021
 * Modified : 2021
 */

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Scada.Lang;
using Scada.Web.Plugins;
using Scada.Web.Services;
using System.Text;
using System.Web;

namespace Scada.Web.Pages
{
    /// <summary>
    /// Represents a page of a view.
    /// <para>Представляет страницу представления.</para>
    /// </summary>
    public class ViewModel : PageModel
    {
        private readonly IWebContext webContext;
        private readonly IUserContext userContext;
        private readonly IMemoryCache memoryCache;

        public bool ViewError => !string.IsNullOrEmpty(ErrorMessage);
        public string ErrorMessage { get; set; }
        public int ViewID { get; set; }
        public string ViewFrameUrl { get; set; }

        public ViewModel(IWebContext webContext, IUserContext userContext, IMemoryCache memoryCache)
        {
            this.webContext = webContext;
            this.userContext = userContext;
            this.memoryCache = memoryCache;

            ErrorMessage = "";
            ViewID = 0;
            ViewFrameUrl = "";
        }

        public void OnGet(int? id)
        {
            ViewID = id ?? userContext.Views.GetFirstViewID() ?? 0;
            dynamic dict = Locale.GetDictionary("Scada.Web.Pages.View");

            if (ViewID <= 0)
            {
                ErrorMessage = dict.ViewNotSpecified;
                return;
            }

            // find view
            Data.Entities.View viewEntity = webContext.BaseDataSet.ViewTable.GetItem(ViewID);
            
            if (viewEntity == null)
            {
                ErrorMessage = dict.ViewNotExists;
                return;
            }

            // check access rights
            if (!userContext.Rights.GetRightByObj(viewEntity.ObjNum ?? 0).View)
            {
                ErrorMessage = dict.InsufficientRights;
                return;
            }

            // get view specification
            ViewSpec viewSpec = memoryCache.GetOrCreate(WebUtils.GetViewSpecCacheKey(ViewID), entry =>
            {
                entry.SetDefaultOptions(webContext);
                return webContext.GetViewSpec(viewEntity);
            });

            if (viewSpec == null)
            {
                ErrorMessage = dict.UnableResolveSpec;
                return;
            }

            ViewData["SelectedViewID"] = ViewID; // used by _MainLayout
            ViewFrameUrl = Url.Content(viewSpec.GetFrameUrl(ViewID));
        }

        public HtmlString RenderBottomTabs()
        {
            StringBuilder sbHtml = new();

            foreach (DataWindowSpec spec in webContext.PluginHolder.AllDataWindowSpecs())
            {
                sbHtml.AppendFormat("<div class='bottom-pnl-tab' data-url='{0}'>{1}</div>",
                    Url.Content(spec.Url), HttpUtility.HtmlEncode(spec.Title));
            }

            return new HtmlString(sbHtml.ToString());
        }
    }
}
