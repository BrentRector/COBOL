// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Editions;

/// <summary>Where Annex B.3 permits a character in a user-defined word.</summary>
public enum WordCharacterPosition
{
    /// <summary>Not in the edition's Annex B: the character may not appear in a COBOL word at all.</summary>
    NotPermitted,
    /// <summary>B.3 item 1 — anywhere (the XID_Start characters and the basic digits).</summary>
    Anywhere,
    /// <summary>B.3 item 2 — anywhere except as the start character (combining marks and other continuers).</summary>
    NotFirst,
    /// <summary>B.3 item 3 — neither first nor last (hyphen, underscore and, from 2023, KATAKANA MIDDLE DOT).</summary>
    Medial,
}

/// <summary>
/// ⛔ THE CHARACTERS OF A COBOL WORD — ISO §8.3.2.1 and §8.1.3.2 GR4 over Annex B, "Characters permitted in
/// user-defined words" (kb/Work PB1402). §8.3.2.1: "Each character of a COBOL word that is not a special character
/// word shall be selected from the set of basic letters, basic digits, extended letters, and the basic special
/// characters hyphen and underscore." §8.1.3.2 GR4: the extended letters are the Annex B repertoire "excluding any
/// character that is defined as a basic letter, basic digit, or basic special character", and GR4 a): "A character in
/// Annex B is included in the set of extended letters if it exists in the implementor-defined compile-time coded
/// character set" — WiseOwl COBOL's is UTF-16 (DOC-A.1-25), which holds every one, so the extended letters ARE the
/// rest of Annex B (Annex A.4.6, claimed).
/// <para><b>Two questions, two answers, never mixed.</b> <see cref="IsWordCharacter"/> is LEXICAL: which characters
/// a word is made of when source text is split into words — the lexer's <c>NAME_BODY</c> class and every text-stage
/// word scanner ask it, so they all agree where a word ends. It is a superset (every character outside the basic
/// repertoire except the internal noncharacters), because the character's LEGALITY is the second question,
/// <see cref="Violation"/>, asked of the finished word with the edition in hand — so a character outside Annex B is
/// named in a diagnostic of its own rather than ending the word with an unexplained "unexpected" (the superset-parse
/// / bind-narrow doctrine the underscore follows).</para>
/// <para><b>Editions.</b> The tables are COBOL 2023's Annex B.3; COBOL 2002 and 2014 hold the earlier repertoire,
/// derived by reversing Annex E: E.2 item 4 (U+037A deleted at 2023; U+30FB made medial — it was permitted anywhere),
/// E.3.3 item 4 (characters newly permitted FIRST at 2023 — before, anywhere but first) and E.3.3 item 5 (characters
/// added at 2023 — absent before). The standard records no change of Annex B between 2002 and 2014, so 2002 uses the
/// 2014 repertoire. COBOL-85 has no extended letters: the gate is the construct
/// <c>user-word-extended-letter-2002</c>, asked before this class.</para>
/// </summary>
public static partial class CobolCharacterRepertoire
{
    /// <summary>The LEXICAL word character: a basic letter, basic digit, hyphen or underscore, or ANY character
    /// outside the basic repertoire except the Unicode noncharacters U+FDD0–U+FDEF and U+FFFE–U+FFFF, which the
    /// text stages use as internal markers (the debugging-line carrier). A surrogate is a word character, so a
    /// supplementary-plane letter (Adlam, Osage, …) stays inside its word.</summary>
    public static bool IsWordCharacter(char c) =>
        c < 0x80 ? char.IsAsciiLetterOrDigit(c) || c is '-' or '_'
        : c is not ((>= '﷐' and <= '﷯') or '￾' or '￿');

    /// <summary>A basic letter or an extended letter — §8.3.2.2's "each user-defined word shall contain at least one
    /// basic letter or extended letter". Annex B.1: every character of the repertoire beyond the basic letters, basic
    /// digits, hyphen and underscore is an extended letter — a combining mark or a digit of another script included —
    /// so every character outside the basic repertoire counts; whether the edition's Annex B holds it is
    /// <see cref="Violation"/>'s question.</summary>
    public static bool IsLetter(char c) => IsBasicLetter(c) || (c >= 0x80 && IsWordCharacter(c));

    /// <summary>⛔ THE ONE BASIC-LETTER PREDICATE: the basic letters of §8.1.3.1 Table 1 are the Latin letters
    /// A–Z and a–z and nothing else — an extended letter (Annex B; §8.1.3.2 GR4) is a DISTINCT row of the same table,
    /// so "any basic letter in the COBOL character set" (§13.18.40.3 SR8, a PICTURE EDITING phrase's character-1)
    /// excludes every one of them (kb/Work PB533). <see cref="char.IsLetter(char)"/> is NOT this question: it admits
    /// every Unicode letter, which is the word-character question's superset
    /// (<see cref="IsLetter"/>/<see cref="IsWordCharacter"/>), and a rule that says "basic" shall not ask it.</summary>
    public static bool IsBasicLetter(char c) => char.IsAsciiLetter(c);

    /// <summary>The SHAPE of a COBOL word (§8.3.2.1), as every text-stage word reader asks it: one or more
    /// <see cref="IsWordCharacter"/> characters, "The hyphen or underscore shall not appear as the first or last
    /// character". The edition's Annex B question is <see cref="Violation"/>, asked of the word afterwards.</summary>
    public static bool IsWordShape(string word)
    {
        if (word.Length == 0 || word[0] is '-' or '_' || word[^1] is '-' or '_') return false;
        foreach (char c in word) if (!IsWordCharacter(c)) return false;
        return true;
    }

    /// <summary>True when <paramref name="word"/> holds a character outside the basic repertoire — an extended letter,
    /// or a character that will be refused as not one.</summary>
    public static bool HasExtendedCharacter(string word)
    {
        foreach (char c in word) if (c >= 0x80) return true;
        return false;
    }

    /// <summary>The length of a COBOL word in CHARACTERS (§8.3.2.1's 63): one per code point, so a supplementary-plane
    /// letter is one character, and §8.1.3.2 GR4 c) — "the base character and each combining character are treated as
    /// separate characters in determining the length of a user-defined word" — holds because a combining character is
    /// a code point of its own.</summary>
    public static int LengthInCharacters(string word)
    {
        int n = 0;
        for (int i = 0; i < word.Length; i++, n++)
            if (char.IsHighSurrogate(word[i]) && i + 1 < word.Length && char.IsLowSurrogate(word[i + 1])) i++;
        return n;
    }

    /// <summary>Where the edition <paramref name="dialectLevel"/>'s Annex B.3 permits <paramref name="codePoint"/>.
    /// Below 2002 only the basic characters exist (the construct gate names an extended one first).</summary>
    public static WordCharacterPosition PositionOf(int codePoint, int dialectLevel)
    {
        var table = dialectLevel >= 2023 ? Edition2023.Value : Edition2014.Value;
        foreach (var (classSet, position) in table)
            if (Contains(classSet, codePoint)) return position;
        return WordCharacterPosition.NotPermitted;
    }

    /// <summary>The first Annex B violation in <paramref name="word"/> at <paramref name="dialectLevel"/> — a
    /// character outside the repertoire, a B.3 item 2 character first, or a B.3 item 3 character first or last — as
    /// the diagnostic's message; null when every character is where Annex B permits it. Hyphen and underscore never
    /// reach the position arms: the lexer cannot end or start a word on them (§8.3.2.1, <c>NAME_TAIL</c>).</summary>
    public static string? Violation(string word, int dialectLevel)
    {
        int edition = dialectLevel >= 2023 ? 2023 : dialectLevel >= 2014 ? 2014 : 2002;
        for (int i = 0; i < word.Length;)
        {
            int start = i;
            char c = word[i];
            int cp = char.IsHighSurrogate(c) && i + 1 < word.Length && char.IsLowSurrogate(word[i + 1])
                ? char.ConvertToUtf32(c, word[++i]) : c;
            i++;
            if (cp < 0x80) continue;
            bool first = start == 0, last = i == word.Length;
            string what = $"the COBOL word '{word}' contains U+{cp:X4}";
            switch (PositionOf(cp, dialectLevel))
            {
                case WordCharacterPosition.NotPermitted:
                    return $"{what}, which is not a character of a COBOL word at COBOL-{edition}: a word is made of "
                        + "basic letters, basic digits, hyphen, underscore and the extended letters of Annex B "
                        + "(ISO §8.3.2.1; §8.1.3.2 GR4)";
                case WordCharacterPosition.NotFirst when first:
                    return $"{what} as its first character, which Annex B.3 item 2 permits in a word only after the "
                        + $"start character (COBOL-{edition}; ISO §8.3.2.1)";
                case WordCharacterPosition.Medial when first || last:
                    return $"{what} as its {(first ? "first" : "last")} character, which Annex B.3 item 3 permits only "
                        + $"inside a word (COBOL-{edition}; ISO §8.3.2.1)";
            }
        }
        return null;
    }

    private static bool Contains((int First, int Second)[] ranges, int codePoint)
    {
        int lo = 0, hi = ranges.Length - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >>> 1;
            if (codePoint < ranges[mid].First) hi = mid - 1;
            else if (codePoint > ranges[mid].Second) lo = mid + 1;
            else return true;
        }
        return false;
    }

    // Each edition's three B.3 classes, as sorted, merged ranges, asked in order (a character sits in one class).
    private static readonly Lazy<((int First, int Second)[] Set, WordCharacterPosition Position)[]> Edition2023 =
        new(() => [(B3Anywhere, WordCharacterPosition.Anywhere), (B3NotFirst, WordCharacterPosition.NotFirst),
                   (B3Medial, WordCharacterPosition.Medial)]);

    /// <summary>The COBOL 2002/2014 classes: 2023's with Annex E reversed.</summary>
    private static readonly Lazy<((int First, int Second)[] Set, WordCharacterPosition Position)[]> Edition2014 = new(() =>
    {
        var added = Expand(E33Item5Added);
        var madeFirst = Expand(E33Item4MadeFirst);
        var madeMedial = Expand(E2Item4MadeMedial);
        var anywhere = Expand(B3Anywhere);
        var notFirst = Expand(B3NotFirst);
        var medial = Expand(B3Medial);
        anywhere.ExceptWith(added);
        notFirst.ExceptWith(added);
        anywhere.ExceptWith(madeFirst);
        notFirst.UnionWith(madeFirst);
        medial.ExceptWith(madeMedial);
        anywhere.UnionWith(madeMedial);
        anywhere.UnionWith(Expand(E2Item4Deleted));
        return [(Ranges(anywhere), WordCharacterPosition.Anywhere), (Ranges(notFirst), WordCharacterPosition.NotFirst),
                (Ranges(medial), WordCharacterPosition.Medial)];
    });

    private static HashSet<int> Expand((int First, int Second)[] ranges)
    {
        var set = new HashSet<int>();
        foreach (var (first, last) in ranges) for (int cp = first; cp <= last; cp++) set.Add(cp);
        return set;
    }

    private static (int First, int Second)[] Ranges(HashSet<int> set)
    {
        var sorted = set.Order().ToArray();
        var ranges = new List<(int, int)>();
        for (int i = 0; i < sorted.Length;)
        {
            int j = i;
            while (j + 1 < sorted.Length && sorted[j + 1] == sorted[j] + 1) j++;
            ranges.Add((sorted[i], sorted[j]));
            i = j + 1;
        }
        return [.. ranges];
    }
}
