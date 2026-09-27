// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CodeSetConversion = CobolNet.Runtime.IO.CodeSetConversion;

namespace CobolNet.Binding;

/// <summary>
/// ⛔ THE ONE literal-phrase collating-sequence / coded-character-set model (ISO §12.3.6 PROGRAM COLLATING
/// SEQUENCE; §12.3.7.2 ALPHABET; §12.3.7.4 GR7 k) — for the ALPHANUMERIC arm and the <c>FOR NATIONAL</c> arm
/// ALIKE. GR7 k states ONE set of six sub-rules "where the native coded character set is the type of coded
/// character set or collating sequence being defined, either alphanumeric or national", and both native sets are
/// the 65,536 UTF-16 code units on this substrate (implementor item 188 / D-N1), so one table shape serves both.
/// <para>SPARSE by design: only the SPECIFIED characters are tabulated; every unspecified character takes a
/// DISTINCT ascending position above the highest specified one, in native relative order (GR7 k3), which the
/// runtime computes arithmetically (<c>CobolNet.Runtime.LiteralPhraseCollation</c>). ⛔ The alphanumeric arm was
/// DENSE over a 256-entry Latin-1 block until kb/Work PB770 and masked every operand with <c>&amp; 0xFF</c>, so
/// <c>ALPHABET A IS 305 THRU 300</c> — legal source naming U+012B..U+0130 — silently reversed <c>'+'</c>…
/// <c>'0'</c> instead. The table width IS the repertoire; the check was never the thing that was wrong.</para>
/// <para>Built once per alphabet by the SPECIAL-NAMES binder; an identity sequence (NATIVE / STANDARD-1 /
/// STANDARD-2 / UCS-4 / the ASCII code-name) normalizes to "no table" at the PCS resolution so the native fast
/// path costs nothing.</para>
/// <para>⛔ AND IT IS NOT ONLY THE LITERAL PHRASE'S SHAPE. An implementor code-name (§12.3.7.4 GR7 i/j —
/// <c>ImplementorCodeNames</c>) uses the SAME record and the same <see cref="Build"/>: the EBCDIC row tabulates
/// the 256 native characters its code page spells at the positions the page gives them, and every native
/// character the page does NOT spell takes GR7 k3's tail position, which this implementation adopts as its GR7 i
/// specification. That is why the sparse shape earns its keep — a dense table would have been 65 536 wide for a
/// 256-character code (kb/Work PB793).</para>
/// </summary>
/// <param name="Codes">The specified characters, sorted ASCENDING BY CODE (the runtime's lookup key).</param>
/// <param name="Positions">Parallel to <paramref name="Codes"/>: each specified character's 0-based collating
/// position. ALSO members share one position (§12.3.7.4 GR7 k6).</param>
/// <param name="RepByPos">Indexed by position 0..<paramref name="NextFree"/>−1: the FIRST character DEFINED at
/// that position, in SOURCE order — literal-1 of an ALSO group (§15.15.4 r2 / §15.16.4 r2's "first character
/// defined" / GR7 k6, the CHAR representative; fix-queue PB59 — <c>AlphabetBind</c> computed this into
/// <c>specOrder</c> and discarded it, so CHAR scanned for the LOWEST-coded member instead).</param>
/// <param name="NextFree">The first position AFTER the specified block (= the number of DISTINCT positions it
/// occupies; ALSO groups make it smaller than <c>Codes.Length</c>). Unspecified character <c>c</c> takes position
/// <c>NextFree + (c − |specified codes &lt; c|)</c> (GR7 k3), giving the sequence
/// <c>NextFree + (0x10000 − Codes.Length)</c> positions — the §15.15.3 r2 domain bound.</param>
/// <param name="HighValue">The runtime HIGH-VALUE character under this sequence (§12.3.7.4 GR8 + §8.3.3.6 GR6/7):
/// the character at the HIGHEST position; a tie (an ALSO group at the top) takes the LAST character specified.
/// ⚠ The ALPHANUMERIC arm deliberately still computes it over the Latin-1 block — the documented byte-stability
/// pin (the flagged §8.3.3.6 divergence recorded in PHASE4_RECONCILIATION), not an oversight of GR7 k3's
/// tail.</param>
/// <param name="LowValue">The runtime LOW-VALUE character (§12.3.7.4 GR9): lowest position; tie takes the FIRST
/// character specified.</param>
public sealed record CollatingTable(ushort[] Codes, ushort[] Positions, ushort[] RepByPos, int NextFree,
    char HighValue, char LowValue)
{
    /// <summary>The number of characters in the native character set of either class (D-N1: the 65,536 UTF-16
    /// code units) — §12.3.7.3 SR14 b1/c1's ordinal bound, SR14 b4/c4's count bound, and GR7 k3's tail size.
    /// The runtime twin is <c>CobolNet.Runtime.LiteralPhraseCollation.Repertoire</c>.</summary>
    public const int Repertoire = 0x10000;

    /// <summary>⛔ THE ONE builder of a sparse table from a finished position assignment — the array shaping and
    /// the §12.3.7.4 GR8/GR9 extremes written down ONCE, for the literal-phrase arm (GR7 k, the SPECIAL-NAMES
    /// binder walking the clause's operands) and the implementor code-name arm (GR7 i/j,
    /// <c>ImplementorCodeNames</c> walking a code page) alike. Neither caller re-derives HIGH-VALUE or
    /// LOW-VALUE.</summary>
    /// <param name="pos">Every SPECIFIED character and its 0-based collating position.</param>
    /// <param name="specOrder">The specified characters in the order the definition gives them — GR8's "<i>the
    /// last character specified</i>" and GR9's "<i>the first character specified</i>" tie rules read this.</param>
    /// <param name="repByPos">Per position 0…<paramref name="nextFree"/>−1, the FIRST character DEFINED there
    /// (§12.3.7.4 GR7 k6 / §15.15.4 r2 — an ALSO group's literal-1).</param>
    /// <param name="nextFree">The first position after the specified block.</param>
    /// <param name="national">Which native repertoire the unspecified tail sits in — it decides only where
    /// <see cref="Extremes"/> starts looking for GR8's character.</param>
    /// <remarks>⛔ POSITIONS AND COUNTS ARE <see cref="int"/> HERE, AND ONLY THE STORED ARRAYS ARE 16-BIT (kb/Work
    /// PB1557). A position is 0 … <see cref="Repertoire"/> − 1, which a <see cref="ushort"/> holds; the COUNT of
    /// positions is 0 … <see cref="Repertoire"/>, which it does not. An alphabet that specifies every native
    /// character — legal, since §12.3.7.3 SR14 b4/c4 bound the count by the native set's size, "<i>shall not
    /// exceed</i>" — has <paramref name="nextFree"/> = 65,536, and the builders used to count it in a
    /// <see cref="ushort"/> that wrapped to 0: the table then claimed no specified block at all, so CHAR/ORD read
    /// the wrong characters and every ordinal of its coded character set "did not exist". The invariants are
    /// asserted, not assumed, so a builder that miscounts fails HERE rather than in a user's program.</remarks>
    public static CollatingTable Build(Dictionary<char, int> pos, List<char> specOrder, List<char> repByPos,
        int nextFree, bool national)
    {
        if (nextFree is < 0 or > Repertoire || repByPos.Count != nextFree)
            throw new ArgumentOutOfRangeException(nameof(nextFree), nextFree,
                $"a collating table has 0 … {Repertoire} specified positions, one representative per position "
                + $"({repByPos.Count} representatives given)");
        char[] codes = [.. pos.Keys.Order()];
        var positions = new ushort[codes.Length];
        for (int i = 0; i < codes.Length; i++)
        {
            int p = pos[codes[i]];
            if (p < 0 || p >= nextFree)
                throw new ArgumentOutOfRangeException(nameof(pos), p,
                    $"U+{(int)codes[i]:X4} is at position {p}, outside the specified block 0 … {nextFree - 1}");
            positions[i] = (ushort)p;
        }
        var (high, low) = Extremes(pos, specOrder, positions, national);
        return new CollatingTable([.. codes.Select(c => (ushort)c)], positions,
            [.. repByPos.Select(c => (ushort)c)], nextFree, high, low);
    }

    /// <summary>⛔ THIS TABLE AS A LIVE <see cref="CobolNet.Runtime.CobolCollation"/> — the SAME runtime carrier
    /// class the generated program gets (<c>CollationEmit.New</c> renders its constructor as text; this
    /// materializes it in the compiler's own process), so a COMPILE-TIME question about the ordering this
    /// sequence gives is answered by the ONE §12.3.7.4 GR7 k weighting and the ONE §8.8.4.2.7 comparison, never
    /// by a second copy of either.
    /// <para>Asked by the §13.18.63.3 SR27 b) screen (kb/Work PB555): "<i>when literal-2 is of class alphanumeric
    /// or national, and the runtime collating sequence is known, the value of literal-4 shall not be equal to the
    /// value of any literal-2 or any value in the range of any occurrence of literal-2 through literal-3,
    /// inclusive</i>" — a rule the compiler can only enforce by ordering the literals itself.</para>
    /// <para>⚠ NOT memoized, and deliberately: <see cref="CollatingTable"/> is a record, so a cache field would
    /// join its synthesized value equality and make two identical tables unequal. The carrier's constructor
    /// builds a weight block, so the ONE caller hoists it into a local for the whole screen rather than asking
    /// per comparison.</para>
    /// <para>⚠ A LOCALE sequence has no table and is deliberately NOT reachable here: §13.18.63.3 SR26's NOTE
    /// puts it on the other side of the rule ("<i>The runtime collating sequence is unknown when the collating
    /// sequence is defined by a locale or the collating sequence is otherwise determined at runtime</i>"), so the
    /// caller drops the screen rather than inventing an ordering.</para></summary>
    /// <param name="national">Which runtime arm — the two differ only in identity and their GR8/GR9 defaults,
    /// both of which this table supplies explicitly, so the ordering is the same either way.</param>
    public CobolNet.Runtime.CobolCollation Collation(bool national) =>
        national
            ? new CobolNet.Runtime.NationalCollation(Codes, Positions, RepByPos, NextFree, HighValue, LowValue)
            : new CobolNet.Runtime.AlphanumericCollation(Codes, Positions, RepByPos, NextFree, HighValue, LowValue);

    /// <summary>The §12.3.7.4 GR8/GR9 extremes of a non-identity sequence — the characters at the HIGHEST and
    /// LOWEST positions, a tie going to the LAST (GR8) / FIRST (GR9) character specified.
    /// <para>The LOW end is common to both classes: position 0 belongs to the first character specified, and a
    /// position-0 ALSO tie resolves to that same character.</para>
    /// <para>The HIGH end differs, and the difference is a DOCUMENTED PIN, not an asymmetry of the rule.
    /// Unspecified characters sit above every specified one (GR7 k3), so GR8's character is the largest
    /// UNSPECIFIED code unit — U+FFFF for the national arm. The ALPHANUMERIC arm deliberately keeps computing it
    /// over the Latin-1 block (U+00FF unless specified): the flagged §8.3.3.6 byte-stability divergence recorded
    /// in PHASE4_RECONCILIATION, which HIGH-VALUE's single-byte alphanumeric width depends on. ⚠ When the whole
    /// block IS specified — which every 256-entry single-byte code-name page does — the walk falls through to
    /// GR8's own tie rule over the specified positions, giving the native character the code page puts at its
    /// HIGHEST code unit. That is the answer an EBCDIC programmer expects (HIGH-VALUE is X'FF' on the
    /// medium).</para></summary>
    private static (char High, char Low) Extremes(Dictionary<char, int> pos, List<char> specOrder,
        ushort[] positions, bool national)
    {
        char low = specOrder[0];
        char high = national ? (char)0xFFFF : (char)0xFF;
        while (pos.ContainsKey(high) && high > (char)0) high--;
        if (pos.ContainsKey(high))               // the whole block is specified — GR8's tie rule over it
        {
            ushort maxPos = positions.Max();
            foreach (char c in specOrder) if (pos[c] == maxPos) high = c;      // tie → LAST specified (GR8)
        }
        return (high, low);
    }
}

/// <summary>
/// A LOCALE-based collating sequence (ISO §12.3.7.2 <c>ALPHABET … IS LOCALE [locale-name-2]</c>; §8.8.4.2.11;
/// DESIGN-locale-facility §4.4.2 / kb/Work PB101): the runtime <c>LocaleCollation</c> arm of the ONE
/// <c>CobolCollation</c> carrier — the derived CLDR/UCA engine for the locale NAMED here or, when
/// <paramref name="LocaleName"/> is null, for the locale CURRENT at each use (§12.3.7.4 GR7e: "*otherwise by the
/// locale that is current at the time the collating sequence is used at runtime*").
/// </summary>
/// <param name="Locale">The ONE "which locale" operand (<see cref="LocaleRef"/>): <see cref="LocaleRef.Current"/> for the
/// phrase without a locale-name; the SPECIAL-NAMES locale-name (§12.3.7.3 SR24) whose external identification binds
/// the sequence for <c>IS LOCALE locale-name-2</c> — its normalized tag is what the emitted carrier holds, and its
/// availability is decided at use (EC-LOCALE-MISSING; L1 item 4).</param>
public sealed record LocaleCollatingSpec(LocaleRef Locale)
{
    /// <summary>The phrase without a locale-name — the locale current at each use.</summary>
    public static LocaleCollatingSpec CurrentLocale { get; } = new(LocaleRef.Current);
}

/// <summary>
/// One SPECIAL-NAMES ALPHANUMERIC alphabet (an <c>ALPHABET</c> clause without FOR NATIONAL, ISO §12.3.7): what the
/// name references — the compile-time twin of the runtime's ONE <c>CobolCollation</c> carrier. Exactly one of the
/// arms is set for a NON-identity sequence: <paramref name="Table"/> for a literal phrase, <paramref name="Locale"/>
/// for the LOCALE phrase; both null for the identity sequences NATIVE / STANDARD-1 / STANDARD-2 (ISO/IEC 646
/// order IS the native order here — no carrier is emitted and the native fast path costs nothing).
/// <see cref="HighValue"/>/<see cref="LowValue"/> are the sequence's §12.3.7.4 GR8/GR9 extremes: a table's are
/// computed by the binder; a LOCALE sequence's are U+FFFF / U+0000 under EVERY CLDR/UCA table (U+FFFF carries the
/// maximum primary, U+0000 is completely ignorable — the runtime's <c>LocaleCollation</c> materializes the same
/// answer, DESIGN-locale-facility L7), so the figurative constants can still fold at compile time.
/// </summary>
/// <param name="Table">The non-identity collating table, or null for a native-order (identity) sequence.</param>
/// <param name="Locale">The LOCALE phrase's arm, or null.</param>
/// <param name="Phrase">The defining phrase, for diagnostics ("NATIVE", "STANDARD-1", "STANDARD-2",
/// "literal-phrase", "LOCALE", or a supported code-name's own spelling).</param>
/// <param name="CodeName">The <c>ImplementorCodeNames</c> row when the definition is a code-name-1 (ISO
/// §12.3.7.3 SR15 / §12.3.7.4 GR7 i), else null. It is what carries GR7 i's OTHER two obligations — the set's
/// ordinal count and its correspondence with the native set — which <see cref="CodedCharacterSet"/> reads;
/// the collating half is already in <paramref name="Table"/>.</param>
public sealed record AlphabetDef(CollatingTable? Table, LocaleCollatingSpec? Locale, string Phrase,
    ImplementorCodeName? CodeName = null)
{
    /// <summary>The identity (native-order) alphabet: NATIVE, STANDARD-1, STANDARD-2 (their <see cref="Phrase"/>
    /// differs — the coded character SETS differ per Table 6 even though the sequences are all the native order).</summary>
    public static AlphabetDef Native { get; } = new(null, null, "NATIVE");

    /// <summary>True when the alphabet IS the native order — no runtime carrier is emitted for it.</summary>
    public bool IsIdentity => Table is null && Locale is null;

    /// <summary>The sequence's HIGH-VALUE character (§12.3.7.4 GR8 / §8.3.3.6.4 GR6).</summary>
    public char HighValue => Table?.HighValue ?? (Locale is not null ? (char)0xFFFF : (char)0xFF);

    /// <summary>The sequence's LOW-VALUE character (§12.3.7.4 GR9 / §8.3.3.6.4 GR7).</summary>
    public char LowValue => Table?.LowValue ?? (char)0;

    /// <summary>The CODED CHARACTER SET this alphabet defines (ISO §12.3.7.4 GR7 Table 6; kb/Work PB110), or null
    /// for a LOCALE alphabet — Table 6's one blank coded-character-set row (the reference sites raise COBOLNET1669
    /// through <c>DataBinder.CodedCharacterSetOf</c>, the ONE resolver).</summary>
    public CodedCharacterSet? CodedSet => Locale is not null ? null : new CodedCharacterSet(Phrase, National: false, Table, CodeName);
}

/// <summary>
/// One SPECIAL-NAMES national alphabet (an <c>ALPHABET … FOR NATIONAL</c> clause, ISO §12.3.7): what the name
/// references per Table 6 — a coded character set, a collating sequence, or both.
/// <list type="bullet">
/// <item><c>NATIVE</c> — the native national coded character set AND collating sequence (GR7 d2): identity,
/// no table.</item>
/// <item><c>UCS-4</c> — the ISO/IEC 10646 UTF-32 coded character set AND the ISO/IEC 10646 appearance-order
/// collating sequence (GR7 f; Table 6 row UCS-4: both). On the D-N1 substrate the collating sequence is the
/// IDENTITY: the implementor correspondence (implementor item 188) maps UTF-32 character U+0000..U+FFFF to the
/// SAME native code unit, and §8.5.1.4 makes each UTF-16 code element its OWN character position with "no
/// special handling or recognition of surrogate pairs" — so the codepoint-vs-code-unit divergence above
/// U+FFFF (a surrogate PAIR weighed as one supplementary codepoint) is UNREACHABLE in COBOL's per-position
/// comparison model, and ISO 10646 order over the 65,536 single-position characters IS code-unit order (D-N3).</item>
/// <item><c>UTF-16</c> / <c>UTF-8</c> — coded character sets ONLY (Table 6: their collating-sequence column is
/// EMPTY): legal where an alphabet references a coded character set (CODE-SET, SYMBOLIC … IN, CLASS … IN), an
/// SR violation where a collating sequence is required (§12.3.6 SR2, §12.4.5.7, SORT/MERGE). UTF-16 as a coded
/// set is the D-N1 native identity; UTF-8's external form matters only at a codec boundary (none exists yet —
/// the CODE-SET clause has no compiler surface, so declaring it is well-formed but inert).</item>
/// <item>literal-phrase — a user collating sequence AND coded character set (Table 6 last row): the sparse
/// <see cref="CollatingTable"/> - the same record the alphanumeric arm builds (GR7 k governs both).</item>
/// <item><c>LOCALE [locale-name-2]</c> — a locale-based national collating sequence (Table 6 row LOCALE: a
/// collating sequence, NOT a coded character set — §12.3.7.3 SR16g/SR17d forbid it in CODE-SET / SYMBOLIC / CLASS):
/// the <see cref="LocaleCollatingSpec"/> arm; DETERMINATION L5 — one locale sequence serves both classes on the
/// D-N1 substrate, so the same runtime <c>LocaleCollation</c> is emitted.</item>
/// </list>
/// </summary>
/// <param name="Table">The non-identity collating table (literal phrase), or null (NATIVE/UCS-4/UTF-16/UTF-8/LOCALE
/// — identity, not-a-collating-sequence, or the locale arm).</param>
/// <param name="Locale">The LOCALE phrase's arm, or null.</param>
/// <param name="HasCollatingSequence">Table 6's collating-sequence column: false for UTF-8/UTF-16 (coded
/// character set only) — referencing such an alphabet as a collating sequence is the SR violation.</param>
/// <param name="Phrase">The defining phrase, for diagnostics ("NATIVE", "UCS-4", "UTF-8", "UTF-16",
/// "literal-phrase", "LOCALE").</param>
/// <param name="CodeName">The <c>ImplementorCodeNames</c> row when the definition is a code-name-2 (ISO
/// §12.3.7.3 SR15 / §12.3.7.4 GR7 j), else null. ⚠ This implementation supports no code-name-2 spelling today,
/// so the component is always null in practice — it exists because SR15 states ONE rule for both classes and the
/// binder asks the SAME table for both, so a national row would work the moment one is added (CLAUDE.md rule 5).</param>
public sealed record NationalAlphabetDef(CollatingTable? Table, LocaleCollatingSpec? Locale, bool HasCollatingSequence, string Phrase,
    ImplementorCodeName? CodeName = null)
{
    /// <summary>A national alphabet without a locale arm (the pre-PB101 constructor shape).</summary>
    public NationalAlphabetDef(CollatingTable? Table, bool HasCollatingSequence, string Phrase)
        : this(Table, null, HasCollatingSequence, Phrase) { }

    /// <summary>True when the alphabet is an identity sequence or no sequence at all — no runtime carrier.</summary>
    public bool IsIdentity => Table is null && Locale is null;

    /// <summary>The national HIGH-VALUE character (§12.3.7.4 GR8): the table's, else U+FFFF (a LOCALE sequence's
    /// maximum and the native national pin alike).</summary>
    public char HighValue => Table?.HighValue ?? (char)0xFFFF;

    /// <summary>The national LOW-VALUE character (§12.3.7.4 GR9): the table's, else U+0000.</summary>
    public char LowValue => Table?.LowValue ?? (char)0;

    /// <summary>The CODED CHARACTER SET this alphabet defines (Table 6; null for LOCALE — see the alphanumeric twin).</summary>
    public CodedCharacterSet? CodedSet => Locale is not null ? null : new CodedCharacterSet(Phrase, National: true, Table, CodeName);
}

/// <summary>
/// The CODED CHARACTER SET an alphabet-name references (ISO §12.3.7.4 GR7 + Table 6; kb/Work PB110 — the model
/// the four coded-set reference sites share: the class condition's alphabet-name-1, §8.8.4.4.4 GR3 a; SYMBOLIC
/// CHARACTERS … IN, GR11 b/c; CLASS … IN, GR12 a; CODE-SET, §13.18.13). A set associates each of its characters
/// with an ORDINAL POSITION (GR6), and for a literal-phrase alphabet the ordinals ARE the collating positions + 1
/// (GR7 k4's implementor determination — the unspecified characters follow the highest specified position in
/// native relative order, exactly as the collating sequence places them; documented in CONFORMANCE.md).
/// <b>Determinations:</b> the native alphanumeric set is the UTF-16 repertoire in code-unit order (ordinal n =
/// code unit n−1 — the same correspondence FUNCTION CHAR / ORD use, §15.15.4 r1 / §15.70.4 r1); STANDARD-1 /
/// STANDARD-2 are ISO/IEC 646 IRV, 128 characters, the identity correspondence on U+0000–U+007F (GR7 c);
/// NATIVE / UTF-16 national sets are the UTF-16 code units (GR7 d2/h); UCS-4 / UTF-8 are the ISO/IEC 10646
/// scalar values (GR7 f/g — an unpaired surrogate is not a character of these sets).
/// </summary>
/// <param name="CodeName">The <c>ImplementorCodeNames</c> row when this set is the one an implementor code-name
/// references (§12.3.7.4 GR7 i/j), else null — it supplies the two obligations a keyword set answers from the
/// standard instead: how many characters the set HAS and how they correspond to the native ones.</param>
public sealed record CodedCharacterSet(string Phrase, bool National, CollatingTable? Table,
    ImplementorCodeName? CodeName = null)
{
    /// <summary>The number of ordinal positions in the set (the §12.3.7.3 SR16 e/f and SR17 b2 range bound):
    /// a code-name's own count (§12.3.7.4 GR7 i/j — "<i>the implementor shall specify the ordinal number of each
    /// character</i>"); 128 for STANDARD-1/2; 65 536 for the native / UTF-16 / literal-phrase sets (a literal
    /// phrase's set contains every native character — GR7 k4); 0x110000 scalar values for UCS-4 / UTF-8.</summary>
    public int OrdinalCount => CodeName?.OrdinalCount ?? Phrase switch
    {
        "STANDARD-1" or "STANDARD-2" => 128,
        "UCS-4" or "UTF-8" => 0x110000 - 0x800,   // the scalar values (surrogate code points are not characters)
        _ => 65536,
    };

    /// <summary>⛔ THE ONE ANSWER to "what does §13.18.13.4 GR6 have to DO to a record described with this coded
    /// character set on the storage medium?" — asked by the CODE-SET binder and by nothing else, so the A.3
    /// item 27 refusal is a PROPERTY OF THE SET rather than a list of phrase names in the file-description
    /// binder (kb/Work PB770 wrote it as <c>Table is not null || Phrase is "UTF-8" or "UCS-4"</c>, which would
    /// have refused every code-name set that ever gained a table).</summary>
    public CodeSetMedium Medium => MediumNotProvided ? CodeSetMedium.NotProvided
        : MediumCorrespondence is null ? CodeSetMedium.Identity : CodeSetMedium.Translated;

    /// <summary>The documented A.3 item 27 non-support: a literal-phrase alphabet's remapped ordinals (GR7 k4) and
    /// UTF-8 / UCS-4 as variable-width MEDIUM encodings. A code-name's set is never one — its row carries its
    /// own correspondence (GR7 i).</summary>
    private bool MediumNotProvided => CodeName is null && (Table is not null || Phrase is "UTF-8" or "UCS-4");

    /// <summary>§13.18.13.4 GR6's correspondence, as the native character each medium code unit represents —
    /// non-null exactly when <see cref="Medium"/> is <see cref="CodeSetMedium.Translated"/>: a code-name's own
    /// translated correspondence (EBCDIC, GR7 i), OR the identity over the set's characters when the set is the
    /// identity but SMALLER than the one-byte record channel — STANDARD-1 / STANDARD-2 (GR7 c, ISO/IEC 646 IRV,
    /// this native set's first 128 positions, implementor item 188) and the <c>ASCII</c> code-name (the same
    /// set). ⛔ Those carry the identity table rather than nothing because GR6 b replaces a native character
    /// only "<i>with its associated coded character as defined in the alphabet being used</i>", so a record
    /// character above U+007F has none and must be REFUSED by the output statement — which the runtime can do
    /// only if it knows where the set ends (kb/Work PB1542: with no table, <c>é</c> was written as X'E9' with
    /// status '00'). The whole-channel identity sets — NATIVE (GR7 d) and UTF-16 (GR7 h on the D-N1 substrate) —
    /// need no conversion: the file character set's own screen covers them (<c>FileCharacterSet</c>).</summary>
    public char[]? MediumCorrespondence => MediumNotProvided ? null
        : CodeName?.MediumCorrespondence
          ?? (OrdinalCount < CodeSetConversion.ChannelUnits
              ? Enumerable.Range(0, OrdinalCount).Select(unit => (char)unit).ToArray()
              : null);

    /// <summary>The character at 1-based <paramref name="ordinal"/> (GR11 b/c — SYMBOLIC CHARACTERS; GR12 a —
    /// a numeric CLASS literal under IN), as a native STRING (a UCS-4/UTF-8 supplementary character is its UTF-16
    /// surrogate pair — one character, two code units). Null when the ordinal is outside the set (the caller's
    /// range diagnostic).</summary>
    public string? CharAt(int ordinal)
    {
        if (ordinal < 1 || ordinal > OrdinalCount) return null;
        // ⛔ ONE literal-phrase arm for both classes (kb/Work PB770): the alphanumeric arm used to carry its own
        // copy that hard-coded the Latin-1 block width — `256 + (ordinal - 1) - NextFree` — so an ordinal above the
        // block named the wrong character the moment the table stopped being 256 wide. The sparse walk below is the
        // general §12.3.7.4 GR7 k3 inverse and has no block width in it at all.
        if (Table is { } t)
        {
            if (ordinal <= t.NextFree) return ((char)t.RepByPos[ordinal - 1]).ToString();
            // position p ≥ NextFree belongs to the unspecified character c with c − |specified < c| = p − NextFree
            int want = ordinal - 1 - t.NextFree;
            int specifiedBelow = 0;
            for (int c = 0; c < CollatingTable.Repertoire; c++)
            {
                if (specifiedBelow < t.Codes.Length && t.Codes[specifiedBelow] == c) { specifiedBelow++; continue; }
                if (c - specifiedBelow == want) return ((char)c).ToString();
                if (c - specifiedBelow > want) break;
            }
            return null;
        }
        if (Phrase is "UCS-4" or "UTF-8")
        {
            int scalar = ordinal - 1;
            if (scalar >= 0xD800) scalar += 0x800;                                 // skip the surrogate block — not scalar values
            return char.ConvertFromUtf32(scalar);
        }
        return ((char)(ordinal - 1)).ToString();                                   // the native / STANDARD / UTF-16 identity
    }
}

/// <summary>What ISO §13.18.13.4 GR1/GR6 conversion a coded character set asks of the storage medium — the
/// three states a CODE-SET clause can be in, decided once by <see cref="CodedCharacterSet.Medium"/>.</summary>
public enum CodeSetMedium
{
    /// <summary>The set's correspondence with the native character set is the IDENTITY over the WHOLE one-byte
    /// record channel (NATIVE, UTF-16), so GR6's replacement is a no-op and the record crosses the boundary
    /// byte-exactly. No conversion is emitted; the file character set's own screen refuses what the channel
    /// cannot carry.</summary>
    Identity,

    /// <summary>The set is a single-byte code whose correspondence with the native set this processor realizes:
    /// GR6 a replaces each medium code unit with its native character on input, GR6 b the reverse on output,
    /// through <see cref="CodedCharacterSet.MediumCorrespondence"/> — a translated code (EBCDIC), or the identity
    /// over FEWER characters than the channel holds (ISO/IEC 646: STANDARD-1, STANDARD-2, ASCII), which the
    /// connector needs so an output statement can refuse a character outside the set (kb/Work PB1542).</summary>
    Translated,

    /// <summary>The set names a medium representation this processor does not provide — Annex A §A.3 item 27,
    /// "<i>The CODE-SET clause is dependent upon a device capable of supporting the specified code</i>". Refused
    /// loudly (COBOLNET1672), never silently treated as the identity.</summary>
    NotProvided,
}

/// <summary>
/// ⛔ THE COMPARISON CLASS that selects WHICH collating sequence applies to an operand — the ONE place the rule is
/// written down. ISO §8.8.4.2 gives one comparison clause per class and only two of them consult a sequence:
/// §8.8.4.2.7 (alphanumeric — the alphanumeric sequence), §8.8.4.2.9 (national — "the collating sequence of
/// characters specified for the current national program collating sequence"), §8.8.4.2.8 (boolean — "a comparison
/// of their boolean value, regardless of their usage", never collated) and §8.8.4.2.4 (numeric — algebraic, never
/// collated). §14.9.40.4 GR5 / §14.9.24.4 GR5 say the same thing for SORT/MERGE keys in one sentence: "The
/// alphanumeric collating sequence that applies to the comparison of key data items of class alphabetic and class
/// alphanumeric, and the national collating sequence that applies to the comparison of key data items of class
/// national, are each separately determined …".
/// <para>Every consumer picks its own RENDERING from this one classification — the relation-condition renderer's
/// trailing <c>__COLLATE</c>/<c>__COLLATE_NAT</c> argument, the table sort's comparer argument, the file sort's
/// runtime key descriptor — but none of them re-decides which class an operand IS (kb/Work PB678: the SORT key
/// comparator had no national arm at all, and its boolean keys took the alphanumeric weights).</para>
/// </summary>
public enum CollatingClass
{
    /// <summary>Class alphabetic / alphanumeric (and the edited categories, which compare as alphanumeric), plus
    /// every ordinary GROUP operand — §8.8.4.2.1 "For comparison, an alphanumeric group item shall be treated as an
    /// elementary alphanumeric data item. A class alphabetic operand shall be treated as though it were an operand
    /// of class alphanumeric." (⛔ NOT §8.8.4.2.3 SR2, which this used to cite: SR2 is the identifier-CLASS syntax
    /// rule and says nothing about groups — kb/Work PB741, feedback_a_real_clause_can_answer_a_different_question.)
    /// </summary>
    Alphanumeric,

    /// <summary>Class national (§8.8.4.2.9) — including a national GROUP, which operates as an elementary item of
    /// PICTURE N(m) (§13.18.29.4 GR2b).</summary>
    National,

    /// <summary>Class boolean (§8.8.4.2.8) — including a bit GROUP (§13.18.29.4 GR1b).</summary>
    Boolean,

    /// <summary>Class numeric (§8.8.4.2.4) — algebraic comparison by value, regardless of usage.</summary>
    Numeric,
}

/// <summary>The classifiers for <see cref="CollatingClass"/>, asked once so no consumer re-decides them.
/// <para>⛔ THERE ARE TWO QUESTIONS HERE AND THEY ARE NOT THE SAME QUESTION. <see cref="Of(PicCategory?)"/> answers
/// the ONE-OPERAND question — "what class is this SORT/MERGE KEY, and which of §14.9.40.4 GR5 / §14.9.24.4 GR5's
/// two separately-determined sequences does it take?", where a key of class numeric takes NEITHER.
/// <see cref="ForComparison"/> answers the TWO-OPERAND question — "which of §8.8.4.2's per-class comparison rules
/// does this relation select?", where §8.8.4.2.5 makes a numeric integer operand compared with an ALPHANUMERIC
/// operand an alphanumeric comparison that §8.8.4.2.7 collates. Folding the comparison question onto the SORT-key
/// classifier dropped the program collating sequence from every <c>numeric-item &lt; SPACE</c> relation and turned
/// NIST NC215A's SEQ-TEST-GF-6/-7 from PASS to FAIL* (kb/Work PB741; the fold was kb/Work PB678's
/// <c>f798397f</c>).</para></summary>
public static class CollatingSelection
{
    /// <summary>⛔ THE ONE data-CATEGORY reading of a bound relation operand — literals carry their own tag
    /// (<see cref="Bound.BoundStringLiteral.Category"/>); a reference-modified field is the unique item of its
    /// inner's class view (alphanumeric for the classic categories, ISO §8.4.3.3 GR6 — but national/boolean ref-mod
    /// stays national/boolean, GR1/GR5a); a plain field asks <c>DataItem.OperandPic</c>, never raw <c>Pic</c>, so a
    /// bit / national GROUP answers as the elementary item §13.18.29.4 GR1b/GR2b makes it; null only for the
    /// genuinely context-dependent shapes (a figurative, whose category §8.3.3.6.4 GR1 takes from its context;
    /// an error operand).
    /// <para>It is named here rather than in the renderer because the BINDER asks it too: §14.9.13.3 SR3 admits a
    /// range's <c>IN alphabet-name-1</c> phrase only "when the literals or identifiers specified in the THROUGH
    /// phrase are of class alphabetic, alphanumeric, or national", and chooses the alphabet's own class from the
    /// same reading. Two copies of a category reader is precisely the shape that handed a GROUP-USAGE NATIONAL
    /// operand the alphanumeric weight table twice already (kb/Work PB728 arm 15, PB741).</para>
    /// <para>⛔ AND IT WAS TWO COPIES ANYWAY, HERE (kb/Work PB401). This name used to carry its OWN switch — the
    /// same literal / ref-mod / field arms as <see cref="IntrinsicResultType.OperandCategory"/>, plus a
    /// STRING-ONLY arm for a function result — and every shape it did not carry answered null, which
    /// <see cref="ForComparison"/> reads as the ALPHANUMERIC arm. That is harmless for a RELATION (a numeric
    /// operand against an alphanumeric one IS an alphanumeric comparison, §8.8.4.2.5, and two numeric operands
    /// never reach the string branch) and NOT harmless for the question a BINDER asks of a THROUGH range, where
    /// §14.7.8's rule 1 / rule 2 split IS the range's class: an arithmetic-expression-ended range (numeric by
    /// §8.8.1.1) answered ALPHANUMERIC, so <c>EVALUATE N WHEN A + 1 THRU B + 2 IN AL</c> sailed past SR3 and was
    /// evaluated as a STRING range (measured on d80cb676). It is now the OTHER reader's body — the one that was
    /// already total and already declared itself the single definition — so a shape added there is seen
    /// here.</para></summary>
    public static PicCategory? OperandCategory(Bound.BoundOperand o) => IntrinsicResultType.OperandCategory(o);

    /// <summary>⛔ THE §14.7.8 CLASS OF A THROUGH RANGE, asked of BOTH ends at once and written down ONCE for the
    /// two clauses that print the phrase — "This specification applies to THROUGH phrases specified in the VALUE
    /// clause and the EVALUATE statement" (§14.7.8 opening sentence), so one sentence governs both and one
    /// function answers for both.
    /// <para>The pair has ONE class to have: §14.9.13.3 SR4 — "The two operands in a range-expression shall be of
    /// the same class and shall not be of class boolean, message-tag, object, or pointer" — and the same-class
    /// rule is §13.18.63.3's in the VALUE clause, where both ends are literals of the conditional variable's own
    /// category. Asking <see cref="ForComparison"/> over both ends rather than classifying one of them is the
    /// kb/Work PB741 rule: a comparison class is a property of the PAIR.</para></summary>
    public static CollatingClass ThroughRangeClass(Bound.BoundOperand low, Bound.BoundOperand high) =>
        ForComparison(OperandCategory(low), OperandCategory(high));

    /// <summary>⛔ WHICH OF §14.7.8'S TWO RULES GOVERNS A THROUGH RANGE, in ONE place for every site that asks —
    /// true when the range is rule 2's ("When the range of values is defined by alphanumeric or national
    /// literals, the range of values depends on the collating sequence used for evaluation of the range"), false
    /// when it is rule 1's ("When the range of values is defined by numeric literals, the range of values
    /// includes literal-1, literal-2, and all algebraic values between literal-1 and literal-2" — algebraic, no
    /// sequence, and NO exception condition).
    /// <para>⛔ THE ANSWER IS THE RANGE'S CLASS, NOT ITS OPERANDS' WRITTEN FORM (kb/Work PB401). Rule 2 names
    /// "literal-1"/"literal-2" because §14.7.8 introduces the phrase with "A THROUGH phrase specifies a range of
    /// values, literal-1 through literal-2"; EVALUATE's range-expression has no literal-1 or literal-2 at all
    /// (§14.9.13.2 prints literal-3/identifier-3/arithmetic-expression-3 THROUGH literal-4/identifier-4/
    /// arithmetic-expression-4), and §14.9.13.3 SR3 — a rule about THIS phrase — says "the literals <b>or
    /// identifiers</b> specified in the THROUGH phrase are of class alphabetic, alphanumeric, or national".
    /// Reading rule 2's "literals" as a restriction on the OPERAND FORM would therefore make the whole of rule 2
    /// — its collating sequence, its IN alphabet-name sentence and its exception alike — inapplicable to every
    /// EVALUATE range, which is not a reading anything in the standard supports and is not the reading the
    /// alphabet arm of this compiler already takes.</para>
    /// <para>THREE CALLERS, ONE RULE: the EVALUATE range's EC gate, the level-88 VALUE range's EC gate, and
    /// <c>DataBinder.TryResolveRangeAlphabet</c>'s §14.9.13.3 SR3 / §13.18.63.3 SR31 screen. Before this they
    /// were three separately-written tests and two of them disagreed — the EVALUATE gate demanded a
    /// string-LITERAL pair while the level-88 gate asked the class, so <c>88 X VALUE "M" THRU "A"</c> raised
    /// EC-RANGE-INVALID and <c>EVALUATE X WHEN WS-M THRU WS-A</c> over the identical values raised nothing
    /// (measured on d80cb676).</para></summary>
    public static bool IsCollatedThroughRange(CollatingClass rangeClass) =>
        rangeClass is CollatingClass.Alphanumeric or CollatingClass.National;

    /// <summary>The SORT/MERGE KEY class of an operand described by <paramref name="operandPic"/> — which shall be
    /// the item's OPERAND picture (<c>DataItem.OperandPic</c>: its own PICTURE for an elementary item, the
    /// §13.18.29.4 GR1b/GR2b as-if PICTURE for a bit / national group), never <c>Pic</c> guarded by
    /// <c>IsGroup</c>. A null picture is an ordinary (alphanumeric) group — §8.8.4.2.1 "For comparison, an
    /// alphanumeric group item shall be treated as an elementary alphanumeric data item".</summary>
    public static CollatingClass Of(PicInfo? operandPic) => Of(operandPic?.Category);

    /// <summary>The SORT/MERGE KEY class keyed directly on a category. ⛔ NOT the comparison-class rule: a relation
    /// condition has TWO operands and its rule is chosen from BOTH — call <see cref="ForComparison"/> there.
    /// §14.9.40.4 GR5 / §14.9.24.4 GR5 name a sequence for keys of class alphabetic/alphanumeric and for keys of
    /// class national only, so a numeric key answers <see cref="CollatingClass.Numeric"/> = no sequence.</summary>
    public static CollatingClass Of(PicCategory? category) => category switch
    {
        PicCategory.National => CollatingClass.National,
        PicCategory.Boolean => CollatingClass.Boolean,
        PicCategory.Numeric => CollatingClass.Numeric,
        _ => CollatingClass.Alphanumeric,
    };

    /// <summary>⛔ THE COMPARISON-CLASS RULE, written down ONCE: which of ISO §8.8.4.2's per-class comparison
    /// clauses a relation condition selects, given the categories of BOTH operands (null = an ordinary alphanumeric
    /// group or a category-less operand). Every relation-condition surface — the direct relation, the figurative
    /// relation, the level-88 membership test and the EVALUATE THRU range — asks THIS, so none of them can decide
    /// half a comparison from one operand.
    /// <para>Both arguments shall be OPERAND categories (<c>DataItem.OperandPic</c>'s, so a bit / national GROUP
    /// answers as the elementary item §13.18.29.4 GR1b/GR2b makes it); a figurative constant has no category of its
    /// own and takes its context's (§8.3.3.6.4 GR1).</para>
    /// <para>The categories §8.8.4.2.14/.15/.16 govern — message-tag, object and pointer — never reach here: those
    /// relations are reference-IDENTITY comparisons with no sequence of any kind, and the relation renderer returns
    /// them before any category is classified (pinned by <c>CollatingComparisonClassDriftTests</c>).</para></summary>
    /// <param name="left">The subject operand's category (§8.8.4.2.1 — "The first operand is called the subject").</param>
    /// <param name="right">The object operand's category. The rule is symmetric; both orders answer alike.</param>
    public static CollatingClass ForComparison(PicCategory? left, PicCategory? right)
    {
        // §8.8.4.2.8 — a boolean comparison is "a comparison of their boolean value, regardless of their usage":
        // NO sequence, and the shorter operand is boolean-zero extended. §8.8.4.2.1 item 4 defines the comparison
        // only for TWO class-boolean operands and a class mix is bind-rejected (COBOLNET0844), so either side
        // answering boolean settles it. First, because an alphanumeric weight table that reorders '0' and '1'
        // would otherwise invert a boolean test over the D-B1 character substrate.
        if (left is PicCategory.Boolean || right is PicCategory.Boolean) return CollatingClass.Boolean;
        // §8.8.4.2.9 — a national comparison is made "with respect to the collating sequence of characters
        // specified for the current national program collating sequence". BOTH mixed forms land here: §8.8.4.2.6
        // converts a class-alphanumeric operand to national and then compares "by the rules for comparison of two
        // operands of class national", and §8.8.4.2.5 moves a numeric integer operand to an item "of the same
        // class and usage as the alphanumeric or national operand". So one national operand makes the comparison
        // national (§8.8.4.2.1 items 5, 6 and 7).
        if (left is PicCategory.National || right is PicCategory.National) return CollatingClass.National;
        // §8.8.4.2.4 — ONLY when BOTH operands are class numeric is the comparison algebraic ("a comparison is
        // made with respect to the algebraic value of the operands regardless of the manner in which their usage
        // is described") and therefore sequence-free. ⛔ ONE numeric operand does NOT make a numeric comparison:
        // that is §8.8.4.2.5's case and it falls through to the alphanumeric arm below.
        if (left is PicCategory.Numeric && right is PicCategory.Numeric) return CollatingClass.Numeric;
        // §8.8.4.2.7 — everything else is an alphanumeric comparison, "made with respect to the collating sequence
        // of characters specified for the current alphanumeric program collating sequence": two alphanumeric
        // operands (§8.8.4.2.1 item 3), the §8.8.4.2.1 normalizations (an alphanumeric GROUP and a class
        // alphabetic operand are "treated as" elementary alphanumeric — a null category is that group), the edited
        // categories (§8.8.4.2.1 NOTE — "All comparisons involving numeric-edited data items are alphanumeric or
        // national comparisons"), and §8.8.4.2.5's numeric integer against an alphanumeric operand, which is
        // "treated as though it were moved … to an elementary data item … of the same class and usage as the
        // alphanumeric … operand" and then compared "by the rules for comparison of that alphanumeric … item".
        return CollatingClass.Alphanumeric;
    }
}

/// <summary>
/// The PAIR of collating sequences one SORT/MERGE statement resolves (ISO §14.9.40.4 GR5 / §14.9.24.4 GR5 — the two
/// are "each separately determined", in this order of precedence: a) the statement's COLLATING SEQUENCE phrase,
/// alphabet-name-1 for keys of class alphabetic and alphanumeric and alphabet-name-2 for keys of class national;
/// b) the program collating sequences). Either half is null for the native order of its class, and the two are
/// independent — a statement may name one, both or neither.
/// </summary>
/// <param name="Alphanumeric">The GR5-resolved sequence for keys of class alphabetic and alphanumeric (and for
/// ordinary group keys, §8.8.4.2.1 — "an alphanumeric group item shall be treated as an elementary alphanumeric
/// data item").</param>
/// <param name="National">The GR5-resolved sequence for keys of class national.</param>
public sealed record SortCollation(AlphabetDef? Alphanumeric, NationalAlphabetDef? National)
{
    /// <summary>Both halves native — no carrier is emitted for either.</summary>
    public static SortCollation Native { get; } = new(null, null);
}

/// <summary>
/// One per-runtime-module carrier for a collating sequence a THROUGH range's <c>IN alphabet-name-1</c> phrase names
/// (ISO §14.7.8 rule 2). The <see cref="Field"/> is allocated by <c>DataBinder.RegisterRangeCollation</c> and
/// declared by <c>ObjectComputerEmit</c>; the bound tree carries the COBOL alphabet-NAME and the renderer maps it
/// through <c>DataBinder.RangeCollations</c>, so no C# identifier ever travels in a bound node.
/// </summary>
/// <param name="Field">The <c>CobolCollation</c> field to pass, or NULL when the alphabet is the native order —
/// an identity sequence has no carrier and the native two-argument comparison IS it (§14.7.8 rule 2's "the
/// collating sequence defined by that alphabet", where that sequence is the native one).</param>
/// <param name="Name">alphabet-name-1 as written, for the declaration's comment.</param>
/// <param name="Table">The non-identity collating table, or null for the LOCALE arm / an identity alphabet.</param>
/// <param name="Locale">The LOCALE arm, or null for a table / an identity alphabet.</param>
/// <param name="National">True when the alphabet is an <c>ALPHABET … FOR NATIONAL</c> one — it becomes a
/// <c>NationalCollation</c> rather than an <c>AlphanumericCollation</c>.</param>
/// <param name="Declare">True when THIS entry owns the field's declaration. False for an identity alphabet (no
/// field) and for one that IS the program collating sequence, whose <c>__COLLATE</c> / <c>__COLLATE_NAT</c> is
/// already declared — the entry still exists so the renderer finds a field for the name, which is the whole point
/// of keying the map on the NAME rather than on the definition.</param>
public sealed record RangeCollationCarrier(string? Field, string Name, CollatingTable? Table,
    LocaleCollatingSpec? Locale, bool National, bool Declare);
