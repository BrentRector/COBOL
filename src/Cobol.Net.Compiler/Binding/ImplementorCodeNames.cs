// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolNet.Binding;

/// <summary>
/// ONE ROW of the implementor code-name table — one name this implementation supports in the
/// <c>code-name-1</c> / <c>code-name-2</c> position of an ALPHABET clause, together with everything ISO
/// §12.3.7.4 GR7 i/j obliges the implementor to specify for it.
/// <para>GR7 i, in full: "<i>When code-name-1 is specified, the alphanumeric coded character set and collating
/// sequence referenced are defined by the implementor. The implementor shall specify the ordinal number of each
/// character for use when code-name-1 references a coded character set and the collating position of each
/// character for use when code-name-1 references a collating sequence. The implementor also shall specify the
/// correspondence between characters of the alphanumeric coded character set specified by code-name-1 and the
/// characters of the native alphanumeric coded character set.</i>" GR7 j says the same of code-name-2 over the
/// national sets. §12.3.7.4 Table 6 gives BOTH code-name rows a Y in the coded-character-set column AND a Y in
/// the collating-sequence column, so one row supplies both halves: <see cref="OrdinalCount"/> +
/// <see cref="MediumCorrespondence"/> are the SET, <see cref="Table"/> is the SEQUENCE.</para>
/// <para>"<i>The coded character set referenced by code-name-1 is statically defined</i>" (GR7 i, last sentence)
/// — hence a compile-time table with no runtime resolution step, unlike the LOCALE arm.</para>
/// </summary>
/// <param name="Name">The code-name's spelling, matched case-insensitively in the ALPHABET clause's bare-word
/// position. It is NOT a reserved word and means a code-name ONLY there (§12.3.7.3 SR15 defines the names for
/// "the ALPHABET clause"), so a program may still use the spelling as a user-defined word elsewhere.</param>
/// <param name="National">The class this row serves: false = code-name-1, the ALPHANUMERIC branch of the
/// §12.3.7.2 general format; true = code-name-2, the FOR NATIONAL branch. A row serves exactly one, because
/// GR7 a/b make the referenced set and sequence take the class of the branch they are written in.</param>
/// <param name="OrdinalCount">GR7 i/j's "<i>the ordinal number of each character</i>" — how many characters the
/// coded character set HAS, which is the §12.3.7.3 SR16 e/f (SYMBOLIC CHARACTERS … IN) and SR17 b2 (CLASS … IN)
/// range bound. Ordinal n names the character at <see cref="MediumCorrespondence"/>[n−1], or the native
/// character U+(n−1) when the correspondence is the identity.</param>
/// <param name="Table">GR7 i/j's "<i>the collating position of each character</i>", as the ONE sparse
/// <see cref="CollatingTable"/> every non-identity sequence in this compiler uses — or NULL when the collating
/// sequence IS the native order, in which case no runtime carrier is emitted at all.</param>
/// <param name="MediumCorrespondence">GR7 i/j's "<i>the correspondence between characters of the … coded
/// character set specified by code-name-1 and the characters of the native … coded character set</i>", as the
/// native character each code unit 0…<see cref="OrdinalCount"/>−1 of THIS set represents — or NULL when that
/// correspondence is the IDENTITY (the set's code unit n IS native character U+00nn). It is what
/// §13.18.13.4 GR6 replaces on each side of a CODE-SET conversion; an identity row with fewer characters than
/// the one-byte channel (ASCII) still converts, through the identity table
/// <see cref="CodedCharacterSet.MediumCorrespondence"/> derives from <see cref="OrdinalCount"/> (kb/Work PB1542).</param>
public sealed record ImplementorCodeName(string Name, bool National, int OrdinalCount,
    CollatingTable? Table, char[]? MediumCorrespondence);

/// <summary>
/// ⛔ THE ONE IMPLEMENTOR CODE-NAME TABLE — this implementation's whole answer to ISO §12.3.7.3 SR15,
/// "<i>The implementor shall specify the names supported for code-name-1 and code-name-2 in the ALPHABET clause,
/// if any.</i>" Adding the next supported code-name is A ROW HERE and nothing else: the binder looks the word up
/// (<c>DataBinder.AlphabetCodeName</c>), the coded-character-set reference sites read the row's ordinal count and
/// correspondence through <see cref="CodedCharacterSet"/>, the collating-sequence reference sites read its
/// <see cref="ImplementorCodeName.Table"/> through <see cref="AlphabetDef"/>/<see cref="NationalAlphabetDef"/>,
/// and the CODE-SET clause reads its <see cref="ImplementorCodeName.MediumCorrespondence"/>. There is no second
/// place, and no <c>if (word == "EBCDIC")</c> anywhere (kb/Work PB793; CLAUDE.md rule 5).
/// <para><b>The set — owner decision, kb/Work PB793 (2026-09-06):</b> <c>ASCII</c> and <c>EBCDIC</c> as
/// code-name-1; NO code-name-2 names. The standard leaves the set open ("if any"), the vendors split, and the
/// standing latitude protocol (owner decision <c>follow_gnucobol_on_split_latitude</c>) is to follow GnuCOBOL,
/// which accepts both spellings — its LATITUDE only: none of its sources or translation tables are used here.
/// The national set stays empty because no national code-name has a coded character set to name: the native
/// national set is the UTF-16 repertoire (determination D-N1) and the standard's own UCS-4 / UTF-8 / UTF-16
/// keywords already name the ISO/IEC 10646 forms.</para>
/// <para><b>ASCII</b> — the ISO/IEC 646 International Reference Version, 128 characters, ordinal n = native
/// character U+(n−1), collating sequence = the native order. That is the SAME set and sequence §12.3.7.4 GR7 c
/// defines for the STANDARD-1 keyword, and deliberately so: "ASCII" names a 7-bit code (ANSI X3.4 / ISO/IEC 646
/// IRV), not a 256-character one, and the first 128 positions of this compiler's native alphanumeric character
/// set ARE that code (implementor item 188 / determination D-N1), so GR7 i's required correspondence is the
/// identity and its required collating positions are the native ones. A code-name is not obliged to name a set
/// no keyword already reaches — SR15 asks only which NAMES are supported — and giving ASCII a 256-character
/// reading would be inventing a set ("extended ASCII") that no standard defines.</para>
/// <para><b>EBCDIC</b> — <b>IBM CCSID 37 (code page 037, "EBCDIC US/Canada")</b>, 256 characters, taken from
/// .NET's own <c>Encoding.GetEncoding(37)</c>: ordinal n is the native character that page maps code unit n−1 to,
/// the collating position of a native character IS its code unit in that page, and the correspondence with the
/// native set is the page itself. ⚖ <b>Why CCSID 37</b>, surveyed rather than assumed
/// (<c>survey_compilers_on_latitude</c>): it is the canonical, registered, COMPLETE single-byte EBCDIC page —
/// a bijection of all 256 code units onto 256 distinct native characters, exactly the Latin-1 repertoire, which
/// round-trips byte-exactly — and it is the default code page of IBM Enterprise COBOL on z/OS, the compiler
/// whose users write <c>ALPHABET … IS EBCDIC</c> in the first place. GnuCOBOL's OWN default (its
/// <c>-febcdic-table=default</c>) is a historical restricted table that corresponds to no registered CCSID; its
/// named alternates are CCSID 500 variants. Following its latitude does not mean adopting a table that is both
/// GPL and unregistered — ⛔ none of its tables or sources are read, copied or derived from here
/// (<c>feedback_gnucobol_differential</c>). CCSID 500 differs from 37 in five punctuation positions and is the
/// obvious second row for whoever needs it.</para>
/// <para><b>Characters outside the page.</b> The native alphanumeric character set is the 65 536 UTF-16 code
/// units (determination D-N1) and CCSID 37 spells only 256 of them, so 65 280 native characters have NO EBCDIC
/// ordinal and NO EBCDIC code unit. Two consequences, both already the standard's own answer rather than a new
/// rule: (1) as a COLLATING SEQUENCE they are the "<i>characters of the native collating sequence that are not
/// specified</i>" of §12.3.7.4 GR7 k3 — they "<i>assume a position … greater than that of the highest character
/// specified</i>", with "<i>the relative order within the set of these unspecified characters … unchanged from
/// the native collating sequence</i>" — which is precisely the tail every sparse <see cref="CollatingTable"/>
/// already computes, so GR7 i's obligation to give EVERY character a collating position is discharged by
/// adopting GR7 k3's own rule as this implementor's specification; (2) as a CODED CHARACTER SET they are simply
/// not members, so no SYMBOLIC CHARACTERS or CLASS ordinal reaches them and a CODE-SET record area holding one
/// cannot be represented on the medium (§13.18.13.4 GR6 b) — reported by the runtime, never silently mangled.
/// The page's own 256 native characters are exactly U+0000…U+00FF, so an alphanumeric record area — one byte per
/// character on this substrate — never holds one of the 65 280 in practice.</para>
/// </summary>
public static class ImplementorCodeNames
{
    /// <summary>The IBM CCSID this implementation means by <c>EBCDIC</c> — see the type remarks for the survey
    /// that chose it. The ONE place the number appears.</summary>
    public const int EbcdicCodePage = 37;

    /// <summary>The number of characters in the ISO/IEC 646 International Reference Version — the set
    /// <c>ASCII</c> names, and the same count §12.3.7.4 GR7 c gives STANDARD-1.</summary>
    private const int AsciiCharacters = 128;

    static ImplementorCodeNames()
    {
        // .NET Core carries no legacy code pages in-box; the provider is the supported way to reach them and
        // registering twice is harmless (the framework de-duplicates by encoding).
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Ascii = new ImplementorCodeName("ASCII", National: false, AsciiCharacters, Table: null,
            MediumCorrespondence: null);
        Ebcdic = SingleByteCodePage("EBCDIC", EbcdicCodePage);
        All = [Ascii, Ebcdic];
    }

    /// <summary>ISO/IEC 646 IRV under the spelling other compilers use for it (see the type remarks).</summary>
    public static ImplementorCodeName Ascii { get; }

    /// <summary>IBM CCSID <see cref="EbcdicCodePage"/> (see the type remarks).</summary>
    public static ImplementorCodeName Ebcdic { get; }

    /// <summary>⛔ THE SUPPORTED SET — §12.3.7.3 SR15's answer, in both classes at once. Everything else reads
    /// this list; nothing else enumerates code-names.</summary>
    public static IReadOnlyList<ImplementorCodeName> All { get; }

    /// <summary>The row a bare ALPHABET word names in <paramref name="national"/>'s class, or null when the word
    /// is not a supported code-name of that class (§12.3.7.3 SR15 — the caller's diagnostic). Matched
    /// case-insensitively: a code-name is written in COBOL basic letters, and §8.1.3.2 GR3 a says "<i>COBOL basic
    /// letters appearing elsewhere within the compilation group are treated in a case-insensitive manner</i>"
    /// (elsewhere = outside a non-hexadecimal literal, which this position is).</summary>
    public static ImplementorCodeName? Find(string word, bool national)
    {
        foreach (var row in All)
            if (row.National == national && string.Equals(row.Name, word, StringComparison.OrdinalIgnoreCase))
                return row;
        return null;
    }

    /// <summary>The supported spellings of one class, for a diagnostic — "<c>ASCII, EBCDIC</c>" or, for the
    /// empty class, "<c>none</c>". Generated from <see cref="All"/> so a new row cannot leave a stale message
    /// behind (kb/Work PB793; the SR15 refusal used to name the empty set in prose).</summary>
    public static string Spellings(bool national)
    {
        var names = All.Where(r => r.National == national).Select(r => r.Name).ToList();
        return names.Count == 0 ? "none" : string.Join(", ", names);
    }

    /// <summary>Build the row for a code-name whose coded character set IS a registered single-byte code page:
    /// the page's 256 code units are the set's ordinals 1…256 (GR7 i "the ordinal number of each character"), the
    /// collating position of each native character the page spells is that character's code unit in the page
    /// (GR7 i "the collating position of each character"), and the page itself is the correspondence with the
    /// native set (GR7 i's third sentence). ⛔ DERIVED, never transcribed — the whole table comes out of
    /// <see cref="Encoding.GetEncoding(int)"/>, so there is no 256-entry literal here to mistype or to drift.
    /// <para>Every native character the page does NOT spell takes the §12.3.7.4 GR7 k3 tail this implementation
    /// adopts as its GR7 i specification — see the type remarks — which is exactly what the sparse table's
    /// unspecified region already means, so nothing is stored for it.</para></summary>
    private static ImplementorCodeName SingleByteCodePage(string name, int codePage)
    {
        var page = Encoding.GetEncoding(codePage);
        char[] toNative = page.GetChars([.. Enumerable.Range(0, 256).Select(b => (byte)b)]);
        var pos = new Dictionary<char, int>(256);
        var order = new List<char>(256);
        var repByPos = new List<char>(256);
        for (int unit = 0; unit < toNative.Length; unit++)
        {
            // A registered single-byte page is a bijection, so no code unit can collide; the guard states the
            // requirement rather than trusting it (a page that is not one cannot define a collating sequence,
            // because two native characters would share a position without an ALSO phrase saying so).
            if (!pos.TryAdd(toNative[unit], unit))
                throw new InvalidOperationException(
                    $"code page {codePage} maps two code units to U+{(int)toNative[unit]:X4}: it cannot define "
                    + "a collating sequence (ISO §12.3.7.4 GR7 i)");
            order.Add(toNative[unit]);
            repByPos.Add(toNative[unit]);
        }
        return new ImplementorCodeName(name, National: false, toNative.Length,
            CollatingTable.Build(pos, order, repByPos, toNative.Length, national: false), toNative);
    }
}
