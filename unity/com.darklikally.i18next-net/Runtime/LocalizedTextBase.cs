using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

using I18Next.Net.Backends;
using I18Next.Net.Internal;

using UnityEngine;

namespace I18Next.Net.Unity
{
    /// <summary>
    ///     A component which translates a key with <see cref="I18NextUnity.Instance" /> and translates it again when the
    ///     language or the translations change. Derive from it to show the translation in other components.
    /// </summary>
    public abstract class LocalizedTextBase : MonoBehaviour
    {
        [SerializeField]
        private string _key;

        [SerializeField]
        private string _namespace;

        [SerializeField]
        private List<LocalizedTextArgument> _arguments = new List<LocalizedTextArgument>();

        private object _args;
        private object _count;
        private bool _started;

        /// <summary>
        ///     The key to be translated, optionally prefixed with a namespace like <c>common:save</c>.
        /// </summary>
        public string Key
        {
            get => _key;
            set
            {
                _key = value;
                Refresh();
            }
        }

        /// <summary>
        ///     The namespace to translate the key from instead of the default namespace.
        /// </summary>
        public string Namespace
        {
            get => _namespace;
            set
            {
                _namespace = value;
                Refresh();
            }
        }

        /// <summary>
        ///     The named arguments set in the inspector. Call <see cref="Refresh" /> after changing them.
        /// </summary>
        public List<LocalizedTextArgument> Arguments => _arguments;

        /// <summary>
        ///     The arguments set from code, added to <see cref="Arguments" />. Values like strings, numbers and dates are
        ///     available as <c>{{value}}</c>, objects and dictionaries provide their properties. Prefer dictionaries in
        ///     IL2CPP builds, the properties of objects are read with reflection.
        /// </summary>
        public object Args
        {
            get => _args;
            set
            {
                _args = value;
                Refresh();
            }
        }

        /// <summary>
        ///     The count used to resolve plurals. Replaces a <c>count</c> of <see cref="Arguments" /> and
        ///     <see cref="Args" />, strings are converted to numbers.
        /// </summary>
        public object Count
        {
            get => _count;
            set
            {
                _count = value;
                Refresh();
            }
        }

        /// <summary>
        ///     Translates the key and shows the translation. Does nothing while the component is disabled or before it has
        ///     started.
        /// </summary>
        public void Refresh()
        {
            if (!_started || !isActiveAndEnabled || string.IsNullOrEmpty(_key))
                return;

            var i18Next = I18NextUnity.Instance;
            var args = CreateArguments(_arguments, _args, _count);

            SetText(string.IsNullOrEmpty(_namespace) ? i18Next.T(_key, args) : i18Next.T(i18Next.Language, _namespace, _key, args));
        }

        /// <summary>
        ///     Shows the translation.
        /// </summary>
        /// <param name="text">The translated text.</param>
        protected abstract void SetText(string text);

        /// <summary>
        ///     Shows the translation for the first time. Call the base implementation when overriding it.
        /// </summary>
        protected virtual void Start()
        {
            _started = true;
            Refresh();
        }

        /// <summary>
        ///     Subscribes to the changes of the language and the translations and shows the translation again. Call the base
        ///     implementation when overriding it.
        /// </summary>
        protected virtual void OnEnable()
        {
            I18NextUnity.LanguageChanged += OnLanguageChanged;
            I18NextUnity.TranslationsChanged += OnTranslationsChanged;

            Refresh();
        }

        /// <summary>
        ///     Unsubscribes from the changes of the language and the translations. Call the base implementation when
        ///     overriding it.
        /// </summary>
        protected virtual void OnDisable()
        {
            I18NextUnity.LanguageChanged -= OnLanguageChanged;
            I18NextUnity.TranslationsChanged -= OnTranslationsChanged;
        }

        private static object CreateArguments(List<LocalizedTextArgument> arguments, object args, object count)
        {
            if (args is IConvertible or IFormattable or (IEnumerable and not IDictionary<string, object>))
                args = new Dictionary<string, object> { ["value"] = args };

            if ((arguments == null || arguments.Count == 0) && count == null)
                return args;

            var result = new Dictionary<string, object>();

            if (arguments != null)
            {
                foreach (var argument in arguments)
                {
                    if (argument != null && !string.IsNullOrEmpty(argument.Name))
                        result[argument.Name] = argument.Value;
                }
            }

            if (args != null)
            {
                foreach (var entry in ObjectExtensions.ObjectToDictionary(args))
                    result[entry.Key] = entry.Value;
            }

            if (count != null)
                result["count"] = count;

            if (result.TryGetValue("count", out var value))
                result["count"] = ParseCount(value);

            return result;
        }

        private static object ParseCount(object count)
        {
            if (count is not string text)
                return count;

            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
                return integer;

            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : count;
        }

        private void OnLanguageChanged(object sender, LanguageChangedEventArgs e)
        {
            Refresh();
        }

        private void OnTranslationsChanged(object sender, TranslationsChangedEventArgs e)
        {
            Refresh();
        }
    }
}
