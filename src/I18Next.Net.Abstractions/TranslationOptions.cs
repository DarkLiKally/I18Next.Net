using System;

namespace I18Next.Net
{
    public class TranslationOptions
    {
        private string _defaultNamespace;

        public string DefaultNamespace
        {
            get => _defaultNamespace;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException(nameof(value));

                _defaultNamespace = value;
            }
        }

        public string[] FallbackLanguages { get; set; }
    }
}
