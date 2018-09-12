using System;
using Microsoft.Extensions.Localization;

namespace I18Next.Net.Extensions
{
    public class I18NextStringLocalizerFactory : IStringLocalizerFactory
    {
        private readonly I18NextNet _i18NextNet;

        public I18NextStringLocalizerFactory(I18NextNet i18NextNet)
        {
            _i18NextNet = i18NextNet;
        }
        
        public IStringLocalizer Create(Type resourceSource) => new I18NextStringLocalizer(_i18NextNet);

        public IStringLocalizer Create(string baseName, string location) => new I18NextStringLocalizer(_i18NextNet);
    }
}
