using System;
using System.Collections.Generic;

namespace I18Next.Net.Plugins;

public class PseudoLocalizationOptions
{
    public ICollection<string> LanguagesToPseudo { get; } = new HashSet<string>();

    public char[] RepeatedLetters
    {
        get;
        set => field = value ?? throw new ArgumentNullException(nameof(value));
    } = [
        'a', 'e', 'i', 'o', 'u', 'y', 'A', 'E', 'I', 'O', 'U', 'Y'
    ];

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

    public bool WrapStrings { get; set; }
}
