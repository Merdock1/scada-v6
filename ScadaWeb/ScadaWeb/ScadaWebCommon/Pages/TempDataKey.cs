// Copyright (c) Rapid Software LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Scada.Web.Pages
{
    /// <summary>
    /// Specifies the common keys to pass temporary data between HTTP requests when performing a redirect.
    /// <para>Задаёт общие ключи для передачи временных данных между HTTP-запросами при перенаправлении.</para>
    /// </summary>
    public static class TempDataKey
    {
        /// <summary>
        /// Error message displayed to a user.
        /// </summary>
        public const string ErrorMessage = nameof(ErrorMessage);
    }
}
