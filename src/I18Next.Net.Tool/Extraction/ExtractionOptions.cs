using System;
using System.Collections.Generic;

namespace I18Next.Net.Tool.Extraction;

internal sealed class ExtractionOptions
{
    public static readonly string[] DefaultFunctionNames = ["T", "Ta", "TObject", "TaObject", "Exists", "ExistsAsync"];

    public string DefaultNamespace { get; set; } = "translation";

    public string NamespaceSeparator { get; set; } = ":";

    /// <summary>
    ///     The names of the translation methods whose first string argument is a key.
    /// </summary>
    public ISet<string> FunctionNames { get; } = new HashSet<string>(DefaultFunctionNames, StringComparer.Ordinal);

    /// <summary>
    ///     Additional names of variables with an indexer taking the key, besides names containing "localizer".
    /// </summary>
    public ISet<string> LocalizerNames { get; } = new HashSet<string>(StringComparer.Ordinal);
}
