using System;
using System.Collections.Generic;

namespace I18Next.Net.Plugins;

/// <summary>
///     The options of the <see cref="PseudoLocalizationPostProcessor" />.
/// </summary>
public class PseudoLocalizationOptions
{
    /// <summary>
    ///     The languages whose translations are pseudo-localized, compared case-sensitively. Empty by default, so nothing is
    ///     pseudo-localized until a language is added.
    /// </summary>
    public ICollection<string> LanguagesToPseudo { get; } = new HashSet<string>();

    /// <summary>
    ///     The letters repeated <see cref="LetterMultiplier" /> times to simulate longer translations. Defaults to the vowels
    ///     including <c>y</c>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is set to null.</exception>
    public char[] RepeatedLetters
    {
        get;
        set => field = value ?? throw new ArgumentNullException(nameof(value));
    } = [
        'a', 'e', 'i', 'o', 'u', 'y', 'A', 'E', 'I', 'O', 'U', 'Y'
    ];

    /// <summary>
    ///     How often the <see cref="RepeatedLetters" /> are repeated. Defaults to 2.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is set to less than 1 or more than 100.</exception>
    public int LetterMultiplier
    {
        get;
        set
        {
            if (value < 1 || value > 100)
                throw new ArgumentOutOfRangeException();

            field = value;
        }
    } = 2;

    /// <summary>
    ///     The replacements of the letters, by default accented look-alikes of the ASCII letters, e.g. <c>ḛ</c> for <c>e</c>.
    ///     Letters without a replacement are kept.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is set to null.</exception>
    public IDictionary<char, char> Letters
    {
        get;
        set => field = value ?? throw new ArgumentNullException(nameof(value));
    } = new Dictionary<char, char>
    {
        { 'a', 'α' },
        { 'b', 'ḅ' },
        { 'c', 'ͼ' },
        { 'd', 'ḍ' },
        { 'e', 'ḛ' },
        { 'f', 'ϝ' },
        { 'g', 'ḡ' },
        { 'h', 'ḥ' },
        { 'i', 'ḭ' },
        { 'j', 'ĵ' },
        { 'k', 'ḳ' },
        { 'l', 'ḽ' },
        { 'm', 'ṃ' },
        { 'n', 'ṇ' },
        { 'o', 'ṓ' },
        { 'p', 'ṗ' },
        { 'q', 'ʠ' },
        { 'r', 'ṛ' },
        { 's', 'ṡ' },
        { 't', 'ṭ' },
        { 'u', 'ṵ' },
        { 'v', 'ṽ' },
        { 'w', 'ẁ' },
        { 'x', 'ẋ' },
        { 'y', 'ẏ' },
        { 'z', 'ẓ' },
        { 'A', 'Ḁ' },
        { 'B', 'Ḃ' },
        { 'C', 'Ḉ' },
        { 'D', 'Ḍ' },
        { 'E', 'Ḛ' },
        { 'F', 'Ḟ' },
        { 'G', 'Ḡ' },
        { 'H', 'Ḥ' },
        { 'I', 'Ḭ' },
        { 'J', 'Ĵ' },
        { 'K', 'Ḱ' },
        { 'L', 'Ḻ' },
        { 'M', 'Ṁ' },
        { 'N', 'Ṅ' },
        { 'O', 'Ṏ' },
        { 'P', 'Ṕ' },
        { 'Q', 'Ǫ' },
        { 'R', 'Ṛ' },
        { 'S', 'Ṣ' },
        { 'T', 'Ṫ' },
        { 'U', 'Ṳ' },
        { 'V', 'V' },
        { 'W', 'Ŵ' },
        { 'X', 'Ẋ' },
        { 'Y', 'Ŷ' },
        { 'Z', 'Ż' },
    };

    /// <summary>
    ///     Whether the translations are wrapped in brackets, e.g. <c>[Ḥḛḛḽḽṓṓ]</c>, to reveal truncated or concatenated texts.
    ///     Disabled by default.
    /// </summary>
    public bool WrapStrings { get; set; }
}
