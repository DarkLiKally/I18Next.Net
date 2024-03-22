using System;
using System.Collections.Generic;

namespace I18Next.Net.Plugins;

public class PseudoLocalizationOptions
{
    private int _letterMultiplier = 2;
    private char[] _repeatedLetters =
    {
        'a', 'e', 'i', 'o', 'u', 'y', 'A', 'E', 'I', 'O', 'U', 'Y'
    };

    private IDictionary<char, char> _letters = new Dictionary<char, char>
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

    public ICollection<string> LanguagesToPseudo { get; } = new HashSet<string>();

    public char[] RepeatedLetters
    {
        get => _repeatedLetters;
        set => _repeatedLetters = value ?? throw new ArgumentNullException(nameof(value));
    }

    public int LetterMultiplier
    {
        get => _letterMultiplier;
        set
        {
            if (value < 1 || value > 100)
                throw new ArgumentOutOfRangeException();
            
            _letterMultiplier = value;
        }
    }

    public IDictionary<char, char> Letters
    {
        get => _letters;
        set => _letters = value ?? throw new ArgumentNullException(nameof(value));
    }

    public bool WrapStrings { get; set; }
}
