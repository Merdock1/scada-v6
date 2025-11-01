// Copyright (c) Rapid Software LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Scada.Lang;

namespace Scada.Web.Pages.Errors
{
    /// <summary>
    /// Represents a page containing an error message corresponding to the status code.
    /// <para>Представляет страницу, которая содержит сообщение об ошибке, соответствующее коду статуса.</para>
    /// </summary>
    [AllowAnonymous]
    public class StatusCodeModel : PageModel
    {
        private readonly dynamic dict = Locale.GetDictionary("Scada.Web.Pages.Errors.StatusCode");

        [BindProperty(SupportsGet = true)]
        public int Code { get; set; }


        public string Title => string.Format(dict.PageTitle, Code);

        public string Header => Code switch
        {
            401 => dict.Header401,
            403 => dict.Header403,
            404 => dict.Header404,
            _ => string.Format(dict.PageTitle, Code)
        };

        public string ErrorMessage => Code switch
        {
            401 => dict.ErrorMessage401,
            403 => dict.ErrorMessage403,
            404 => dict.ErrorMessage404,
            _ => dict.ErrorMessage
        };
    }
}
