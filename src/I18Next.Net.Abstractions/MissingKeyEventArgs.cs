using System;

namespace I18Next.Net
{
    public class MissingKeyEventArgs : EventArgs
    {
        public MissingKeyEventArgs(string language, string ns, string key, string[] possibleKeys)
        {
            Language = language;
            Namespace = ns;
            Key = key;
            PossibleKeys = possibleKeys;
        }

        public string Language { get; }

        public string Namespace { get; }

        public string Key { get; }

        public string[] PossibleKeys { get; }
    }
}
