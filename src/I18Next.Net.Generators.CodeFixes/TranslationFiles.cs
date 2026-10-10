using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

namespace I18Next.Net.Generators.CodeFixes;

internal static class TranslationFiles
{
    public static TextDocument Find(Project project, string path)
    {
        return path == null ? null : project.AdditionalDocuments.FirstOrDefault(d => d.FilePath == path);
    }

    /// <summary>
    ///     Returns the solution with the keys added to the translation file, or <c>null</c> when none of them could be added.
    /// </summary>
    public static async Task<Solution> AddKeysAsync(Solution solution, DocumentId documentId, IEnumerable<KeyValuePair<string, string>> entries,
        string referenceText, CancellationToken cancellationToken)
    {
        var text = await solution.GetAdditionalDocument(documentId).GetTextAsync(cancellationToken).ConfigureAwait(false);
        var changed = text;

        foreach (var entry in entries)
        {
            var change = JsonKeyEditor.AddKey(changed.ToString(), entry.Key, entry.Value, referenceText);

            if (change != null)
                changed = changed.WithChanges(change.Value);
        }

        return changed == text ? null : solution.WithAdditionalDocumentText(documentId, changed);
    }
}
