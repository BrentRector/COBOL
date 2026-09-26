// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

/// <summary>
/// The Table-10 ROLE a picture symbol OCCURRENCE plays (ISO/IEC 1989:2023 §13.18.40.6, Table 10 — Format 1
/// picture symbol order of precedence). The values ARE the table's column indexes, in the printed order, so a
/// role indexes <see cref="PictureComposition.MayPrecede"/> directly.
/// <para>Eight symbols occupy TWO columns and two rows each, because their precedence depends on WHERE they
/// stand, and the standard says exactly which is which: the currency symbol used as a FIXED insertion symbol
/// ("the leftmost column and the uppermost row … represent its use as the first or second symbol in
/// character-string-1"; the rightmost/lowermost, "the last or penultimate symbol"); '+' and '−' used as a
/// NON-FLOATING insertion symbol (uppermost = "the first symbol", lowermost = "the last symbol"); and 'P', the
/// floating currency symbol, the zero-suppression pair 'Z'/'*' and the floating sign pair (uppermost = "their
/// use to the LEFT of the decimal point position", lowermost = "to the RIGHT"). Assigning each occurrence its
/// role IS the composition problem; the matrix then answers it.</para>
/// </summary>
internal enum PicRole
{
    /// <summary>B 0 / — simple insertion (§13.18.40.5 rule 3).</summary>
    SimpleInsertion = 0,
    /// <summary>The GROUPING separator: ',' — or, under DECIMAL-POINT IS COMMA, '.' (§13.18.40.3 SR13).</summary>
    Grouping = 1,
    /// <summary>The DECIMAL separator: '.' — or, under DECIMAL-POINT IS COMMA, ',' (SR13).</summary>
    DecimalSeparator = 2,
    /// <summary>The exponent's '+' — "the symbol '+' that appears in a column and in a row by itself, represents
    /// its use in the exponent part of character-string-1 for a floating-point numeric-edited item".</summary>
    ExponentSign = 3,
    /// <summary>'+' / '−' as a non-floating insertion symbol, FIRST in character-string-1.</summary>
    SignLeading = 4,
    /// <summary>'+' / '−' as a non-floating insertion symbol, LAST in character-string-1.</summary>
    SignTrailing = 5,
    /// <summary>CR / DB — each two-character pair is ONE symbol (§13.18.40.3 SR12 NOTE 2).</summary>
    CrDb = 6,
    /// <summary>The currency symbol as FIXED insertion, first or second symbol.</summary>
    CurrencyLeading = 7,
    /// <summary>The currency symbol as FIXED insertion, last or penultimate symbol.</summary>
    CurrencyTrailing = 8,
    /// <summary>'Z' / '*' left of the decimal point position.</summary>
    SuppressLeft = 9,
    /// <summary>'Z' / '*' right of the decimal point position.</summary>
    SuppressRight = 10,
    /// <summary>'+' / '−' as a FLOATING insertion symbol, left of the decimal point position.</summary>
    FloatSignLeft = 11,
    /// <summary>'+' / '−' as a FLOATING insertion symbol, right of the decimal point position.</summary>
    FloatSignRight = 12,
    /// <summary>The currency symbol as a FLOATING insertion symbol, left of the decimal point position.</summary>
    FloatCurrencyLeft = 13,
    /// <summary>The currency symbol as a FLOATING insertion symbol, right of the decimal point position.</summary>
    FloatCurrencyRight = 14,
    /// <summary>9.</summary>
    Digit = 15,
    /// <summary>A / X.</summary>
    AlphaNumeric = 16,
    /// <summary>S.</summary>
    OperationalSign = 17,
    /// <summary>V.</summary>
    ImpliedPoint = 18,
    /// <summary>'P' left of the decimal point position — i.e. a TRAILING 'P' string, whose implied point is
    /// "to the right of the string of 'P's" (§13.18.40.4 GR14).</summary>
    ScaleLeft = 19,
    /// <summary>'P' right of the decimal point position — a LEADING 'P' string, implied point to its left.</summary>
    ScaleRight = 20,
    /// <summary>1 — boolean (§8.5.2.5).</summary>
    Boolean = 21,
    /// <summary>N — national (§8.5.2.10).</summary>
    National = 22,
    /// <summary>E — the floating-point exponent separator (§13.18.40.4 GR13 b).</summary>
    Exponent = 23,
}

/// <summary>
/// The COMPOSITION validator for a Format-1 PICTURE character-string: ISO/IEC 1989:2023 §13.18.40.3's
/// composition syntax rules plus §13.18.40.6's Table 10 precedence, as ONE pass over the repeat-expanded symbol
/// string. It answers the second obligation of SR2 — "Character-string-1 shall consist of an ALLOWABLE
/// COMBINATION of characters used as picture symbols. The allowable combinations of symbols for a PICTURE clause
/// are specified in 13.18.40.6, Precedence rules" — which <see cref="PictureAnalyzer"/>'s symbol loop, testing
/// MEMBERSHIP only, never reached (kb/Work PB528).
/// <para>⛔ THE SHAPE IS DELIBERATE (kb/Work PB528; data-model design D24). Table 10 is DATA — the 24×24 matrix
/// transcribed cell-for-cell in the standard's own printed order, drift-tested against
/// <c>specs/ISO_COBOL.md</c> — and the composition rules are a fixed, cited sequence of declarative checks.
/// Symbol ORDER is decided by ASSIGNING each occurrence its Table-10 role and asking the matrix about every
/// ordered pair, never by hand-written positional <c>if</c>s: that is why SR25 ('+'/'−' leftmost or rightmost)
/// and SR26 (the currency symbol's placement) need no code of their own — the leading-sign ROW is empty, so
/// nothing may precede a leading sign; the trailing-sign and CR/DB COLUMNS are empty, so nothing may follow one;
/// and the leading-currency row admits only a leading sign. A rule the matrix already carries is not written
/// down twice.</para>
/// <para>The comma/period ROLES are a PARAMETER (SR13: "The rules for the symbol period apply to the symbol
/// comma, and the rules for the symbol comma apply to the symbol period"; §13.18.40.6: "When the DECIMAL-POINT
/// IS COMMA clause is specified, the precedence rules for the symbols comma and period are interchanged"), so
/// the whole rule set transfers with one flag rather than growing a comma copy of every period rule.</para>
/// </summary>
internal static class PictureComposition
{
    /// <summary>
    /// Test seam (kb/Work PB1600): when set, every Table-10 walk (<see cref="Precedence"/>) reports how many role
    /// tests it made — one per live symbol per candidate assignment, plus the culprit scan on a refusal. The scale
    /// gate asserts that COUNT grows linearly with the symbol count; it replaced a stopwatch ratio that a loaded
    /// runner turned red with no code change. <see cref="AsyncLocal{T}"/>, so only the observing test's own
    /// analysis reports here while the Unit assembly runs in parallel.
    /// </summary>
    internal static readonly AsyncLocal<Action<long>?> WalkObserver = new();

    // ── ISO §13.18.40.6, Table 10 — Format 1 picture symbol order of precedence ──────────────────────────────
    // One entry per SECOND symbol (the table's ROW, in printed order = the PicRole order); within an entry, one
    // cell per FIRST symbol (the table's COLUMN, same order). 'x' == "the symbol at the top of the column MAY
    // PRECEDE (but not necessarily immediately) in character-string-1 the symbol at the left of the row"; '.' is
    // the printed BLANK, which is therefore a PROHIBITION.
    //
    // Transcribed from the canonical PDF (printed folios 459-460) by GEOMETRY — every 'x' glyph bucketed into a
    // cell by the page's own ruling lines — and verified identical to the specs/ISO_COBOL.md transcription, all
    // 576 cells. `PictureTable10DriftTests` re-derives it from the markdown on every run, so this array can
    // never silently drift from the standard.
    //
    //                                                  f  f  f  f
    //                              +  +        c  c  Z  Z  +  +  c  c        A        P  P
    //                     B  ,  .  e  -  -  C  s  s  *  *  -  -  s  s  9  X  S  V  L  R  1  N  E
    //                     0        x  L  R  R  L  R  L  R  L  R  L  R
    //                     /                 D
    internal static readonly string[] Table10Rows =
    [
        /* B 0 /  */ "x x x . x . . x . x x x x x x x x . x . x . x .",
        /* ,      */ "x x x . x . . x . x x x x x x x . . x . x . . .",
        /* .      */ "x x . . x . . x . x . x . x . x . . . . . . . .",
        /* +  exp */ ". . . . . . . . . . . . . . . . . . . . . . . x",
        /* + - L  */ ". . . . . . . . . . . . . . . . . . . . . . . .",
        /* + - R  */ "x x x . . . . x x x x . . x x x . . x x x . . .",
        /* CR DB  */ "x x x . . . . x x x x . . x x x . . x x x . . .",
        /* cs L   */ ". . . . x . . . . . . . . . . . . . . . . . . .",
        /* cs R   */ "x x x . x . . . . x x . . . . x . . x x x . . .",
        /* Z * L  */ "x x . . x . . x . x . . . . . . . . . . . . . .",
        /* Z * R  */ "x x x . x . . x . x x . . . . . . . x . x . . .",
        /* f+- L  */ "x x . . . . . x . . . x . . . . . . . . . . . .",
        /* f+- R  */ "x x x . . . . x . . . x x . . . . . x . . . . .",
        /* fcs L  */ "x x . . x . . . . . . . . x . . . . . . . . . .",
        /* fcs R  */ "x x x . x . . . . . . . . x x . . . x . . . . .",
        /* 9      */ "x x x x x . . x . x . x . x . x x x x . x . . x",
        /* A X    */ "x . . . . . . . . . . . . . . x x . . . . . . .",
        /* S      */ ". . . . . . . . . . . . . . . . . . . . . . . .",
        /* V      */ "x x . . x . . x . x . x . x . x . x . x . . . .",
        /* P L    */ "x x . . x . . x . x . x . x . x . x . x . . . .",
        /* P R    */ ". . . . x . . x . . . . . . . . . x x . x . . .",
        /* 1      */ ". . . . . . . . . . . . . . . . . . . . . x . .",
        /* N      */ "x . . . . . . . . . . . . . . . . . . . . . x .",
        /* E      */ "x x x . x . . . . . . . . . . x . . . . . . . .",
    ];

    /// <summary>Row r's admitted FIRST symbols, as a bit set over <see cref="PicRole"/> — bit k is set when the
    /// role-k symbol may precede the role-r symbol. Packed once; the walk is then one shift and one test.</summary>
    internal static readonly int[] MayPrecede = PackTable();

    private static int[] PackTable()
    {
        var packed = new int[Table10Rows.Length];
        for (int r = 0; r < Table10Rows.Length; r++)
        {
            string row = Table10Rows[r];
            int col = 0, mask = 0;
            for (int i = 0; i < row.Length; i++)
            {
                if (row[i] == ' ') continue;
                if (row[i] == 'x') mask |= 1 << col;
                col++;
            }
            if (col != Table10Rows.Length)
                throw new InvalidOperationException($"Table 10 row {r} has {col} cells, expected {Table10Rows.Length}");
            packed[r] = mask;
        }
        return packed;
    }

    /// <summary>One symbol of the repeat-expanded character-string. <see cref="Text"/> is the symbol as the
    /// standard names it — one character, or the two-character <c>CR</c>/<c>DB</c>, which SR12 NOTE 2 makes ONE
    /// symbol. <see cref="At"/> is its 0-based SYMBOL ordinal (not a character offset), which is the position
    /// every rule below counts in.</summary>
    private readonly record struct Sym(string Text, char Kind, int At);

    /// <summary>
    /// Validate the composition of one Format-1 character-string. Returns <see langword="true"/> when it is an
    /// allowable combination; otherwise emits ONE diagnostic naming the exact rule and returns
    /// <see langword="false"/>. <paramref name="expanded"/> is the repeat-expanded, uppercased symbol string;
    /// <paramref name="cs"/> the picture's currency symbol; <paramref name="char1"/> the declared PICTURE EDITING
    /// character-1 letters; <paramref name="decimalPointIsComma"/> the SPECIAL-NAMES switch that interchanges the
    /// comma and period ROLES (SR13).
    /// </summary>
    public static bool Validate(string picture, string expanded, char cs, IReadOnlySet<char> char1,
        IReadOnlySet<char> char1Extended, bool blankWhenZero, bool decimalPointIsComma, EditionContext edition,
        string where)
    {
        char decimalSep = decimalPointIsComma ? ',' : '.';
        char grouping = decimalPointIsComma ? '.' : ',';
        var syms = Tokenize(expanded);
        int n = syms.Count;
        if (n == 0) return true;   // the empty string is already COBOLNET0808 at the caller

        bool Fail(DiagnosticDescriptor code, string why)
        {
            string shown = expanded.Equals(picture.ToUpperInvariant().Replace(" ", ""), StringComparison.Ordinal)
                ? $"PICTURE {picture}" : $"PICTURE {picture} (expanded: {expanded})";
            edition.Error(code, $"invalid {shown} — {where}: {why}");
            return false;
        }

        int Count(string sym)
        {
            int k = 0;
            foreach (var s in syms) if (s.Text == sym) k++;
            return k;
        }

        var floating = MarkFloating(syms, cs, char1, char1Extended, decimalSep, grouping);
        bool IsFloating(int i) => floating[i];

        // The DIGIT POSITIONS (§13.18.40.4 GR14): 9, the zero-suppression symbols, every 'P' ("not counted in
        // the size of the item, but each symbol 'P' is counted in the maximum number of digit positions") and
        // each occurrence of a floating string's symbol.
        var digitPositions = new List<int>(n);
        for (int i = 0; i < n; i++)
            if (syms[i].Kind is '9' or 'Z' or '*' or 'P'
                || ((syms[i].Kind == '+' || syms[i].Kind == '-' || syms[i].Kind == cs) && IsFloating(i)))
                digitPositions.Add(i);

        // ── §13.18.40.3's composition syntax rules, each cited, in a fixed order so the message names the most
        // specific rule the string breaks. Every one of them is a PROHIBITION on a combination; the precedence
        // walk below then decides the ORDER question SR2 delegates to §13.18.40.6.

        // SR12 b) "Each of the symbols from the set 'CR', 'DB', 'E', 'S', 'V' '.' may appear only once in
        // character-string-1." (SR13 puts the comma in the period's place under DECIMAL-POINT IS COMMA.)
        foreach (string once in new[] { "CR", "DB", "E", "S", "V", decimalSep.ToString() })
            if (Count(once) is var k and > 1)
                return Fail(DiagnosticCatalog.PictureComposition,
                    $"the symbol '{once}' appears {k} times — each of the symbols 'CR', 'DB', 'E', 'S', 'V', "
                    + $"'{decimalSep}' may appear only once in character-string-1 (ISO §13.18.40.3 SR12 b"
                    + (decimalPointIsComma ? "; SR13 — the rules for the symbol period apply to the symbol comma)" : ")"));

        // SR20 "The symbol 'V' and the symbol '.' are mutually exclusive in character-string-1."
        if (Count("V") > 0 && Count(decimalSep.ToString()) > 0)
            return Fail(DiagnosticCatalog.PictureComposition,
                $"the symbol 'V' and the symbol '{decimalSep}' are mutually exclusive in character-string-1 — both "
                + "indicate the decimal point position (ISO §13.18.40.3 SR20"
                + (decimalPointIsComma ? "; SR13)" : ")"));

        // SR17 "The symbol 'P' and the symbol '.' are mutually exclusive in character-string-1."
        if (Count("P") > 0 && Count(decimalSep.ToString()) > 0)
            return Fail(DiagnosticCatalog.PictureComposition,
                $"the symbol 'P' and the symbol '{decimalSep}' are mutually exclusive in character-string-1 — 'P' "
                + "already implies the assumed decimal point (ISO §13.18.40.3 SR17"
                + (decimalPointIsComma ? "; SR13)" : ")"));

        // SR21 "The symbol 'Z' and the symbol '*' are mutually exclusive in character-string-1."
        if (Count("Z") > 0 && Count("*") > 0)
            return Fail(DiagnosticCatalog.PictureComposition,
                "the symbol 'Z' and the symbol '*' are mutually exclusive in character-string-1 — one item has one "
                + "replacement character, space for 'Z' and asterisk for '*' (ISO §13.18.40.3 SR21; §13.18.40.5 rule 7)");

        // SR23 "The editing sign control symbols '+', '-', 'CR', and 'DB' are mutually exclusive in
        // character-string-1 with the exception of a numeric-edited data item for a floating-point edited
        // result" — and a floating-point picture reaches this walk one PART at a time (the significand alone),
        // in which NOTE 3's sanctioned pair is never both present.
        var signKinds = new List<string>(2);
        foreach (string k in new[] { "+", "-", "CR", "DB" }) if (Count(k) > 0) signKinds.Add(k);
        if (signKinds.Count > 1)
            return Fail(DiagnosticCatalog.PictureComposition,
                $"the editing sign control symbols '{signKinds[0]}' and '{signKinds[1]}' are mutually exclusive in "
                + "character-string-1 (ISO §13.18.40.3 SR23)");

        // SR18 "The symbol 'S', if present, shall be the first symbol in character-string-1."
        if (Count("S") > 0 && syms[0].Kind != 'S')
            return Fail(DiagnosticCatalog.PictureComposition,
                "the symbol 'S' shall be the FIRST symbol in character-string-1 (ISO §13.18.40.3 SR18)");

        // SR16 "The symbol 'P' may appear only as a continuous string of 'P's in the leftmost or rightmost digit
        // positions in character-string-1." Both halves are measured: one contiguous run, and that run standing
        // at an END of the DIGIT-POSITION sequence (GR14's digit positions, so `PIC $$$$PP` counts the floating
        // currency positions and its 'P's are rightmost — kb/Work PB155).
        var ps = new List<int>();
        for (int i = 0; i < n; i++) if (syms[i].Kind == 'P') ps.Add(i);
        bool leadingP = false, trailingP = false;
        if (ps.Count > 0)
        {
            for (int i = 1; i < ps.Count; i++)
                if (ps[i] != ps[i - 1] + 1)
                    return Fail(DiagnosticCatalog.PictureComposition,
                        "the symbols 'P' do not form a CONTINUOUS string — 'P' may appear only as a continuous "
                        + "string of 'P's in the leftmost or rightmost digit positions (ISO §13.18.40.3 SR16)");
            leadingP = ps[0] == digitPositions[0];
            trailingP = ps[^1] == digitPositions[^1];
            if (!leadingP && !trailingP)
                return Fail(DiagnosticCatalog.PictureComposition,
                    "the string of 'P's stands at neither the leftmost nor the rightmost digit positions of "
                    + "character-string-1 (ISO §13.18.40.3 SR16)");
        }

        // SR19 "When the symbol 'V' and one or more symbols 'P' are used in character-string-1, the symbol 'V'
        // shall either immediately precede the first symbol 'P' or immediately follow the last symbol 'P'."
        if (ps.Count > 0 && Count("V") > 0)
        {
            int v = -1;
            for (int i = 0; i < n; i++) if (syms[i].Kind == 'V') { v = i; break; }
            if (v != ps[0] - 1 && v != ps[^1] + 1)
                return Fail(DiagnosticCatalog.PictureComposition,
                    "the symbol 'V' neither immediately precedes the first symbol 'P' nor immediately follows the "
                    + "last (ISO §13.18.40.3 SR19)");
        }

        // SR22 "Neither the symbol 'S' nor the symbol '*' shall be specified in character-string-1 when the
        // BLANK WHEN ZERO clause is specified for the subject of the entry."
        if (blankWhenZero && (Count("S") > 0 || Count("*") > 0))
            return Fail(DiagnosticCatalog.PictureComposition,
                $"the symbol '{(Count("S") > 0 ? 'S' : '*')}' is specified beside a BLANK WHEN ZERO clause — neither "
                + "'S' nor '*' may be (ISO §13.18.40.3 SR22)");

        // SR24 "For fixed insertion with editing sign control symbols, only one currency symbol and only one
        // editing sign control symbol may be used in character-string-1."
        // ⛔ A PICTURE EDITING character-1 is NOT counted, and that is a reading, not an oversight. SR12 calls
        // the IS form "a fixed editing sign control symbol", which would put it under this sentence and bound it
        // to ONE - but SR12 a) names "at least two occurrences of one of the symbols from the set character-1 ..."
        // as a way to BE a valid character-string, and SR24's own second sentence, "For extended editing sign
        // control symbols, either one or two extended editing sign control symbols may be used in
        // character-string-1", sanctions two. Counting character-1 here would reject `PIC LL EDITING "L" IS ":"`
        // and `PIC L999F`. Its placement is SR8-SR12 / SR25 / SR26, in PictureAnalyzer.ValidateEditing.
        int fixedCurrency = 0, fixedSign = 0;
        for (int i = 0; i < n; i++)
        {
            if (IsFloating(i)) continue;
            if (syms[i].Kind == cs) fixedCurrency++;
            else if (syms[i].Text is "+" or "-" or "CR" or "DB") fixedSign++;
        }
        if (fixedCurrency > 1)
            return Fail(DiagnosticCatalog.PictureComposition,
                $"character-string-1 uses {fixedCurrency} currency symbols as FIXED insertion — only one currency "
                + "symbol may be used (ISO §13.18.40.3 SR24). Two or more ADJACENT occurrences would be a floating "
                + "insertion string (§13.18.40.5 rule 6); these are separated by other symbols");
        if (fixedSign > 1)
            return Fail(DiagnosticCatalog.PictureComposition,
                $"character-string-1 uses {fixedSign} editing sign control symbols as FIXED insertion — only one may "
                + "be used (ISO §13.18.40.3 SR24)");

        // SR12 a) "Character-string-1 shall contain: — at least on[e] one of the symbols from the set 'A', 'N',
        // 'X', 'Z', '1', '9', *', or — at least two occurrences of one of the symbols from the set character-1,
        // 'x', '+', '-', and the currency symbol." (The two printed defects — "at least on one" and the
        // lowercase "'x'", which is the symbol 'X' and is already covered by the first bullet — are the PRINTED
        // standard's, verified against the canonical PDF; the transcription is faithful.) Without one of these a
        // character-string describes no digit position and no character position, so it defines no category at
        // all (§13.18.40.4 GR5-GR13).
        bool anyContent = false;
        foreach (var s in syms) if (s.Kind is 'A' or 'N' or 'X' or 'Z' or '1' or '9' or '*') { anyContent = true; break; }
        if (!anyContent)
        {
            foreach (char c in Doubled(char1, cs))
                if (Count(c.ToString()) >= 2) { anyContent = true; break; }
        }
        if (!anyContent)
            return Fail(DiagnosticCatalog.PictureComposition,
                "character-string-1 describes no digit position and no character position — it shall contain at "
                + "least one of the symbols 'A', 'N', 'X', 'Z', '1', '9', '*', or at least two occurrences of one "
                + $"of character-1, 'X', '+', '-', '{cs}' (ISO §13.18.40.3 SR12 a)");

        // The DECIMAL POINT POSITION — derived ONCE, here, because two rules count from it: SR29 just below and
        // the Table-10 role assignment (which of the eight two-row symbols' halves an occurrence takes).
        int dp = DecimalPointPosition(syms, n, decimalSep, leadingP, ps);

        // SR29 "For floating insertion, at least one insertion symbol shall be specified to the left of the
        // decimal point position." A floating string is anchored left of the point BY DEFINITION: §13.18.40.5
        // rule 6 a) represents "any or all of the leading numeric character positions TO THE LEFT of the decimal
        // point position by the same insertion symbol", and rule 6 b) "ALL of the numeric character positions",
        // whose result "is the same as if the floating insertion editing were defined only to the left of the
        // decimal point position". A string lying WHOLLY to the right of the point matches neither, so the
        // standard defines no image for it — `PIC .$$` and `PIC V--` are not merely unrejected today, they
        // render one the rules do not derive (kb/Work PB530). Table 10 refuses most of these shapes already,
        // because a '9' may not precede a right-of-point floating symbol; what it cannot refuse is the string
        // that has NOTHING to its left but the point itself (`PIC .$$`, `PIC V$$`, `PIC B.++`), which is exactly
        // the residue this rule names.
        int firstFloat = -1;
        for (int i = 0; i < n; i++) if (IsFloating(i)) { firstFloat = i; break; }
        if (firstFloat >= 0 && firstFloat >= dp)
        {
            string point = dp < n && syms[dp].Kind is 'V'
                ? $"the symbol 'V' at symbol position {dp + 1}"
                : dp < n && syms[dp].Kind == decimalSep
                    ? $"the symbol '{decimalSep}' at symbol position {dp + 1}"
                    : $"the assumed point to the left of the 'P' string at symbol position {dp + 1}";
            return Fail(DiagnosticCatalog.PictureComposition,
                $"the floating insertion string of '{syms[firstFloat].Text}' lies wholly to the RIGHT of the "
                + $"decimal point position ({point}) — for floating insertion, at least one insertion symbol "
                + "shall be specified to the LEFT of the decimal point position (ISO §13.18.40.3 SR29"
                + (decimalPointIsComma ? "; SR13 — the rules for the symbol period apply to the symbol comma" : "")
                + "). §13.18.40.5 rule 6 defines the image only for a string that starts left of the point");
        }

        // ── §13.18.40.6 Table 10 — the ORDER question SR2 delegates ("The allowable combinations of symbols for
        // a PICTURE clause are specified in 13.18.40.6, Precedence rules"). Assign every occurrence its role,
        // then require an 'x' for EVERY ordered pair: an 'x' means the column symbol "may precede (BUT NOT
        // NECESSARILY IMMEDIATELY)" the row symbol, so the relation binds non-adjacent pairs too.
        return Precedence(syms, n, cs, char1, dp, decimalSep, grouping, IsFloating, leadingP, Fail);
    }

    /// <summary>
    /// The DECIMAL POINT POSITION of a Format-1 character-string, as the SYMBOL ORDINAL the point stands at — so
    /// a symbol at a LOWER ordinal is "to the left of the decimal point position" and one at a higher ordinal is
    /// to its right. 'V' and the decimal separator each state it outright (SR20 has already made them mutually
    /// exclusive); with neither, a LEADING 'P' string puts it immediately to that string's left (§13.18.40.4
    /// GR14 — "The symbol 'P' implies an assumed decimal point that is either — to the left of the string of
    /// 'P's if they indicate the leftmost digit positions in character-string-1; or — to the right of the string
    /// of 'P's if they indicate the rightmost digit positions"), and otherwise it lies past the last symbol,
    /// which makes every symbol left of it.
    /// <para>⛔ ONE DEFINITION for the whole file: SR29 and the Table-10 role assignment both count from it, and
    /// they must agree about which half of a two-row symbol an occurrence takes (kb/Work PB530).</para>
    /// </summary>
    private static int DecimalPointPosition(List<Sym> syms, int n, char decimalSep, bool leadingP, List<int> ps)
    {
        for (int i = 0; i < n; i++)
            if (syms[i].Kind == 'V' || syms[i].Kind == decimalSep) return i;
        return leadingP ? ps[0] : n;
    }

    /// <summary>
    /// ⛔ THE ONE DEFINITION OF A FLOATING INSERTION STRING (ISO §13.18.40.5 rule 6), read by the composition
    /// rules here AND by <see cref="PictureAnalyzer"/>'s geometry derivation, so the two can never disagree.
    /// "Floating insertion editing is indicated by specifying a string of at least two IDENTICAL floating
    /// insertion editing symbols. Any of the simple insertion editing symbols embedded in this string or to the
    /// immediate right of this string are part of the string" — and rule 6 b) lets the string span the decimal
    /// point ("The second way is to represent ALL of the numeric character positions by the same insertion
    /// symbol"), so the decimal separator continues a run as well. Floating-ness is therefore an ADJACENCY
    /// property. An occurrence outside such a run is FIXED insertion (rule 5) and SR24 bounds it to one.
    /// </summary>
    /// <returns>The floating string's symbol and how many times it occurs, or <c>('\0', 0)</c> when the
    /// character-string has none. §13.18.40.3 SR27 admits at most one such string, and the Table-10 walk rejects
    /// a second, so the first one found is the only one.</returns>
    internal static (char Symbol, int Occurrences) FloatingString(string expanded, char cs,
        IReadOnlySet<char> char1, IReadOnlySet<char> char1Extended, char decimalSep, char grouping)
    {
        var syms = Tokenize(expanded);
        var floating = MarkFloating(syms, cs, char1, char1Extended, decimalSep, grouping);
        for (int i = 0; i < syms.Count; i++)
        {
            if (!floating[i]) continue;
            char sym = syms[i].Kind;
            int occ = 0;
            foreach (var s in syms) if (s.Kind == sym) occ++;
            return (sym, occ);
        }
        return ('\0', 0);
    }

    /// <summary>Mark every symbol occurrence that belongs to a floating insertion string — see
    /// <see cref="FloatingString"/> for the rule this implements.</summary>
    private static bool[] MarkFloating(List<Sym> syms, char cs, IReadOnlySet<char> char1,
        IReadOnlySet<char> char1Extended, char decimalSep, char grouping)
    {
        int n = syms.Count;
        var floating = new bool[n];
        // "Any of the SIMPLE INSERTION editing symbols embedded in this string or to the immediate right of this
        // string are part of the string" (ISO §13.18.40.5 rule 6), plus — by rule 6 b), which represents ALL the
        // numeric character positions — the decimal point. The simple-insertion SET is CobolEdit's, read and not
        // re-spelled (kb/Work PB490: it was written out at four sites and two of the copies drifted to {',', 'B'}).
        // A PICTURE EDITING character-1 stays transparent to this walk in EITHER form, for the reason the role map
        // below records: it constrains no neighbour, and breaking a floating run on one would answer a Table-10
        // question the standard does not ask here (kb/Work PB528).
        bool Embedded(char c) => CobolNet.Runtime.CobolEdit.IsSimpleInsertionSymbol(c, grouping)
            || c == decimalSep || char1.Contains(c);
        // ⛔ THE FLOATING INSERTION SYMBOLS are §13.18.40.5 rule 6's own list, all four kinds: "The currency
        // symbol, the EXTENDED EDITING SIGN CONTROL SYMBOLS, if specified, and the fixed editing sign control
        // symbols '+' and '-' are used as the floating insertion symbols." The extended ones are exactly the
        // character-1 letters declared with a FOR phrase (SR12), which is why they arrive as their own set
        // rather than being guessed from the character-string: an IS-form character-1 is a SIMPLE insertion
        // symbol (rule 3) and stays transparent to this walk, an extended one is a candidate for the ≥2 test
        // below. Leaving them out is what made `PIC LLLL9.99F` a render GAP (kb/Work PB491) — its LLLL is a
        // floating string that no detector saw.
        var floatSymbols = new List<char>(3 + char1Extended.Count) { '+', '-', cs };
        floatSymbols.AddRange(char1Extended);
        foreach (char sym in floatSymbols)
        {
            for (int i = 0; i < n;)
            {
                if (syms[i].Kind != sym) { i++; continue; }
                int last = i, occ = 0;
                for (int j = i; j < n && (syms[j].Kind == sym || Embedded(syms[j].Kind)); j++)
                    if (syms[j].Kind == sym) { last = j; occ++; }
                if (occ >= 2)
                    for (int j = i; j <= last; j++) if (syms[j].Kind == sym) floating[j] = true;
                i = last + 1;
            }
        }
        return floating;
    }

    private static IEnumerable<char> Doubled(IReadOnlySet<char> char1, char cs)
    {
        foreach (char c in char1) yield return c;
        yield return 'X';
        yield return '+';
        yield return '-';
        yield return cs;
    }

    /// <summary>The Table-10 walk. Two symbol kinds are POSITION-AMBIGUOUS — a non-floating '+'/'−' is either
    /// the leading or the trailing sign, and a non-floating currency symbol either the leading or the trailing
    /// one — so the assignment is SEARCHED rather than guessed: the string is an allowable combination when SOME
    /// assignment satisfies the matrix. SR24 has already bounded each to one occurrence, so the search is at most
    /// four assignments. Reporting picks the assignment that got FURTHEST, so the message names the pair a reader
    /// would name.
    /// <para><paramref name="dp"/> is the decimal point position from <see cref="DecimalPointPosition"/> — the
    /// ONE derivation of it (SR29 counts from the same one), and what splits the eight two-row symbols into
    /// their left-of-point and right-of-point halves.</para></summary>
    private static bool Precedence(List<Sym> syms, int n, char cs, IReadOnlySet<char> char1, int dp,
        char decimalSep, char grouping, Func<int, bool> isFloating, bool leadingP,
        Func<DiagnosticDescriptor, string, bool> fail)
    {
        var roleA = new PicRole[n];      // the first candidate role
        var roleB = new PicRole[n];      // the second, where the occurrence is position-ambiguous
        var live = new bool[n];
        var ambiguous = new List<int>(4);
        for (int i = 0; i < n; i++)
        {
            char c = syms[i].Kind;
            bool left = i < dp;
            // ⛔ A PICTURE EDITING character-1 is DELIBERATELY TRANSPARENT to Table 10. The standard gives a
            // Table-10 precedence to 'es' ALONE — "If the EDITING phrase is specified, the precedence of 'es' as
            // related to Table 10 … has the same precedence as the 'cs' symbol in the column and row of
            // non-floating insertion symbols" — where 'es' is the EXTENDED editing sign control symbol, the FOR
            // form (SR12: "If literal-1 is specified, character-1 is a fixed editing sign control symbol. If the
            // FOR phrase is specified, character-1 is an extended editing sign control symbol"). Even for 'es'
            // that mapping contradicts the rules that place it: the leading-currency-before-trailing-currency
            // cell is BLANK, while SR24 and SR25 expressly sanction TWO extended symbols, "the first occurrence
            // … for the leftmost symbol in character-string-1 and the second occurrence … for the rightmost".
            // For the IS form the standard assigns no Table-10 precedence at all, and §13.18.40.5 rule 3 makes it
            // SIMPLE insertion, which would take the B 0 / row. Applying the 'cs' mapping to either would REJECT
            // LEGAL SOURCE (`PIC L999F` with two FOR phrases; `PIC LL EDITING "L" IS ":"`, the very shape SR12 a)
            // names as sufficient), so character-1 constrains no neighbour here. Its own rules are SR8-SR12,
            // SR25 and SR26, screened by PictureAnalyzer.ValidateEditing. (kb/Work PB528; data-model design D24.)
            if (char1.Contains(c)) { live[i] = false; continue; }
            live[i] = true;
            switch (c)
            {
                case 'B' or '0' or '/': roleA[i] = PicRole.SimpleInsertion; break;
                case '9': roleA[i] = PicRole.Digit; break;
                case 'A' or 'X': roleA[i] = PicRole.AlphaNumeric; break;
                case 'S': roleA[i] = PicRole.OperationalSign; break;
                case 'V': roleA[i] = PicRole.ImpliedPoint; break;
                case '1': roleA[i] = PicRole.Boolean; break;
                case 'N': roleA[i] = PicRole.National; break;
                case 'E': roleA[i] = PicRole.Exponent; break;
                case 'Z' or '*': roleA[i] = left ? PicRole.SuppressLeft : PicRole.SuppressRight; break;
                case 'P': roleA[i] = leadingP ? PicRole.ScaleRight : PicRole.ScaleLeft; break;
                case 'C' or 'D': roleA[i] = PicRole.CrDb; break;          // the CR / DB pair
                default:
                    if (c == decimalSep) { roleA[i] = PicRole.DecimalSeparator; break; }
                    if (c == grouping) { roleA[i] = PicRole.Grouping; break; }
                    if (c is '+' or '-')
                    {
                        if (isFloating(i)) { roleA[i] = left ? PicRole.FloatSignLeft : PicRole.FloatSignRight; break; }
                        roleA[i] = PicRole.SignLeading; roleB[i] = PicRole.SignTrailing; ambiguous.Add(i); break;
                    }
                    if (c == cs)
                    {
                        if (isFloating(i)) { roleA[i] = left ? PicRole.FloatCurrencyLeft : PicRole.FloatCurrencyRight; break; }
                        roleA[i] = PicRole.CurrencyLeading; roleB[i] = PicRole.CurrencyTrailing; ambiguous.Add(i); break;
                    }
                    live[i] = false; break;   // unreachable: the SR2 whitelist admitted nothing else
            }
        }

        // ⛔ THE WALK IS OVER ROLES, NOT OVER PAIRS. The precedence relation's operands are the 24 Table-10
        // roles, so the whole prefix of a character-string is summarised by the SET of roles it has used: a
        // symbol is admissible exactly when every role seen so far is in its row. Carrying that set as a bit
        // mask answers every one of the O(n^2) ordered pairs in one test per symbol. This is not a
        // micro-optimization — `ExpandRepeats` turns `PIC X(30000)` into a 30 000-symbol string (SR4's
        // 63-character limit is on character-string-1 AS WRITTEN, which `X(30000)` obeys), and a pair loop over
        // that is 9x10^8 comparisons inside the binder.
        var role = new PicRole[n];
        int combos = 1 << ambiguous.Count;
        (int First, int Second) worst = (-1, -1);
        long tests = 0;   // the work the PB1600 scale gate counts (WalkObserver)
        for (int pick = 0; pick < combos; pick++)
        {
            for (int i = 0; i < n; i++) role[i] = roleA[i];
            for (int k = 0; k < ambiguous.Count; k++)
                if ((pick & (1 << k)) != 0) role[ambiguous[k]] = roleB[ambiguous[k]];
            (int First, int Second) bad = (-1, -1);
            int seen = 0;
            for (int j = 0; j < n; j++)
            {
                if (!live[j]) continue;
                tests++;
                int refused = seen & ~MayPrecede[(int)role[j]];
                if (refused != 0)
                {
                    // Report the LOWEST refused role's FIRST occurrence — deterministic, and the symbol a
                    // reader would name (the earliest one the row forbids).
                    var culprit = (PicRole)System.Numerics.BitOperations.TrailingZeroCount(refused);
                    for (int i = 0; i < j; i++)
                    {
                        tests++;
                        if (live[i] && role[i] == culprit) { bad = (i, j); break; }
                    }
                    break;
                }
                seen |= 1 << (int)role[j];
            }
            if (bad.First < 0) { WalkObserver.Value?.Invoke(tests); return true; }
            if (bad.Second > worst.Second || (bad.Second == worst.Second && bad.First > worst.First)) worst = bad;
        }

        WalkObserver.Value?.Invoke(tests);
        return fail(DiagnosticCatalog.PicturePrecedence,
            $"the symbol '{syms[worst.Second].Text}' at symbol position {worst.Second + 1} may not follow the symbol "
            + $"'{syms[worst.First].Text}' at symbol position {worst.First + 1}. Character-string-1 shall consist of "
            + "an allowable COMBINATION of characters used as picture symbols (ISO §13.18.40.3 SR2), and the "
            + "allowable combinations are §13.18.40.6, Table 10 — Format 1 picture symbol order of precedence, "
            + "whose blank cell here is a prohibition");
    }

    /// <summary>Split the repeat-expanded string into SYMBOLS: one character each, except <c>CR</c> and
    /// <c>DB</c>, which SR12 NOTE 2 makes one symbol apiece ("The symbols 'CR' and 'DB' although consisting of
    /// two characters, are each considered to be a symbol by itself"). The caller's SR2 whitelist has already
    /// guaranteed that a 'C' or 'D' outside such a pair is impossible.</summary>
    private static List<Sym> Tokenize(string expanded)
    {
        var syms = new List<Sym>(expanded.Length);
        for (int i = 0; i < expanded.Length; i++)
        {
            if (expanded[i] == 'C' && i + 1 < expanded.Length && expanded[i + 1] == 'R')
            { syms.Add(new Sym("CR", 'C', syms.Count)); i++; continue; }
            if (expanded[i] == 'D' && i + 1 < expanded.Length && expanded[i + 1] == 'B')
            { syms.Add(new Sym("DB", 'D', syms.Count)); i++; continue; }
            syms.Add(new Sym(expanded[i].ToString(), expanded[i], syms.Count));
        }
        return syms;
    }
}
