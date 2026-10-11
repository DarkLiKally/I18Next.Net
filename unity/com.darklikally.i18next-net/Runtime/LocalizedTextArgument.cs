using System;

using UnityEngine;

namespace I18Next.Net.Unity
{
    /// <summary>
    ///     A named argument of a <see cref="LocalizedTextBase" /> set in the inspector, e.g. <c>name</c> for
    ///     <c>{{name}}</c>.
    /// </summary>
    [Serializable]
    public class LocalizedTextArgument
    {
        [SerializeField]
        private string _name;

        [SerializeField]
        private string _value;

        /// <summary>
        ///     Creates an argument without name and value, e.g. for the inspector.
        /// </summary>
        public LocalizedTextArgument()
        {
        }

        /// <summary>
        ///     Creates an argument with the given name and value.
        /// </summary>
        /// <param name="name">The name of the argument, e.g. <c>name</c> for <c>{{name}}</c>.</param>
        /// <param name="value">The value of the argument.</param>
        public LocalizedTextArgument(string name, string value)
        {
            _name = name;
            _value = value;
        }

        /// <summary>
        ///     The name of the argument.
        /// </summary>
        public string Name
        {
            get => _name;
            set => _name = value;
        }

        /// <summary>
        ///     The value of the argument. The value of <c>count</c> is converted to a number to resolve plurals.
        /// </summary>
        public string Value
        {
            get => _value;
            set => _value = value;
        }
    }
}
