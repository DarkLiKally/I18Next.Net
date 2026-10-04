using System;

namespace I18Next.Net;

/// <summary>
///     Generates key constants and typed translation accessors into the annotated partial class from the JSON translation
///     files of the source language. The files have to be added as <c>AdditionalFiles</c> and the
///     <c>I18Next.Net.Generators</c> package has to be referenced.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class I18NextResourcesAttribute(string path) : Attribute
{
    /// <summary>
    ///     The folder containing the <c>{language}/{namespace}.json</c> files, relative to the project.
    /// </summary>
    public string Path { get; } = path;

    /// <summary>
    ///     The language whose keys and placeholders are used for the generated code. The other languages are checked
    ///     against it.
    /// </summary>
    public string SourceLanguage { get; set; } = "en";

    /// <summary>
    ///     The namespace used for keys without a namespace prefix.
    /// </summary>
    public string DefaultNamespace { get; set; } = "translation";

    public string NamespaceSeparator { get; set; } = ":";

    /// <summary>
    ///     The i18next JSON format version of the plural keys (1 to 4).
    /// </summary>
    public int JsonFormatVersion { get; set; } = 4;
}
