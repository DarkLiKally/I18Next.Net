using System.Collections.Generic;

namespace I18Next.Net.MachineTranslation.Internal;

internal sealed class ProtectedText(string text, IReadOnlyList<string> tokens)
{
    public string Text { get; } = text;

    public IReadOnlyList<string> Tokens { get; } = tokens;
}
