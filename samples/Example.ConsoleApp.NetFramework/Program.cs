using System;
using I18Next.Net;
using I18Next.Net.Backends;
using I18Next.Net.Plugins;

namespace Example.ConsoleApp.NetFramework
{
    internal class Program
    {
        private static ITranslationBackend _backend;

        public static void Main(string[] args)
        {
            SetupBackend();
            
            var translator = new DefaultTranslator(_backend);
            
            var i18Next = new I18NextNet(_backend, translator);

            Console.WriteLine("English translation:");
            i18Next.Language = "en";
            Console.WriteLine(i18Next.T("exampleKey"));
            
            Console.WriteLine("German translation:");
            i18Next.Language = "de";
            Console.WriteLine(i18Next.T("exampleKey"));

            i18Next.SetFallbackLanguages("en");
            Console.WriteLine(i18Next.T("exampleKey2")); // should output "My English text." because of fallback language

            Console.ReadKey();
        }

        private static void SetupBackend()
        {
            var backend = new InMemoryBackend();
            
            backend.AddTranslation("en", "translation", "exampleKey", "My English text.");
            backend.AddTranslation("en", "translation", "exampleKey2", "My English fallback.");
            backend.AddTranslation("de", "translation", "exampleKey", "Mein deutscher text.");

            _backend = backend;
        }
    }
}
