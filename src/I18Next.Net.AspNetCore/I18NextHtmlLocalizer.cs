using System;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;

namespace I18Next.Net.AspNetCore
{
    /// <inheritdoc />
    public class I18NextHtmlLocalizer : HtmlLocalizer
    {
        private readonly IStringLocalizer _localizer;

        /// <inheritdoc />
        public I18NextHtmlLocalizer(IStringLocalizer localizer)
            : base(localizer)
        {
            _localizer = localizer;
        }

        /// <inheritdoc />
        public override LocalizedHtmlString this[string name, params object[] arguments]
        {
            get
            {
                if (name == null)
                    throw new ArgumentNullException(nameof (name));
                
                return ToHtmlString(_localizer[name, arguments]);
            }
        }
    }
}
