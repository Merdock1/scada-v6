// Copyright (c) Rapid Software LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Scada.Web.Pages
{
    /// <summary>
    /// Specifies the common keys to pass data from a page model to its view.
    /// <para>Задаёт общие ключи для передачи данных из модели страницы в её представление.</para>
    /// </summary>
    public static class ViewDataKey
    {
        /// <summary>
        /// Page title.
        /// </summary>
        public const string Title = nameof(Title);

        /// <summary>
        /// CSS class of a body HTML element.
        /// </summary>
        public const string BodyCssClass = nameof(BodyCssClass);

        /// <summary>
        /// View ID to select a menu item in the view explorer.
        /// </summary>
        public const string SelectedViewID = nameof(SelectedViewID);

        /// <summary>
        /// URL to select a menu item in the main menu.
        /// </summary>
        public const string SelectedURL = nameof(SelectedURL);
    }
}
