// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>
/// Numeric-edited PICTURE formatting (ISO §13.18.40.4 editing rules): simple insertion (<c>B 0 / ,</c>), special
/// insertion (<c>.</c>), fixed insertion (<c>cs</c>, leading <c>+ -</c>, trailing <c>+ - CR DB</c>), floating
/// insertion (<c>$$… ++… --…</c>), and zero suppression/replacement (<c>Z *</c>). The ONE numeric→edited
/// conversion — used by MOVE to a numeric-edited receiver and by arithmetic GIVING/COMPUTE stores (§14.7.7: the
/// result is stored per the MOVE editing rules). The algorithm is proven over the NIST-85
/// corpus and hosted on the typed-native substrate (unscaled <see cref="long"/> + scale; no byte areas, no
/// <c>decimal</c>).
///
/// SPECIAL-NAMES threading (ISO §12.3.7): <c>currency</c> is the mask's currency PICTURE SYMBOL (GR13; the
/// compiler CANONICALIZES every mask's currency symbol to <c>$</c> at bind — <c>PictureAnalyzer</c> — so
/// generated code always passes the default) and <c>currencyString</c> is the currency STRING that symbol expands
/// to when it is not the single character <c>$</c> (§12.3.7.3 r23: "Literal-7 may have any length"; §13.18.40.4
/// GR14: "The first occurrence of the currency symbol adds the number of characters in the currency string to the
/// size of the item. Each subsequent occurrence of the currency symbol adds one" — kb/Work PB60 / AR-15.68.3-3).
/// The core edits on the LOGICAL one-character-per-symbol image and <see cref="Materialize"/> maps that image to
/// the physical one — the ONE place a symbol wider than one character position is written out, shared with the
/// PICTURE EDITING literal of §13.18.40.4 GR14 'es' (kb/Work PB491) — so neither multi-character string touches
/// an editing rule; <c>commaMode</c> is DECIMAL-POINT IS COMMA (GR14 — the decimal and grouping
/// separators EXCHANGE functionality; §13.18.40.2 SR13: "the rules for the symbol period apply to the symbol
/// comma, and the rules for the symbol comma apply to the symbol period", and §13.18.40.6: "the precedence rules
/// … are interchanged"). The public entries CANONICALIZE: under comma-mode the mask's <c>.</c>/<c>,</c> are
/// swapped so the core logic always sees dot-as-decimal, and the rendered output swaps back — which realizes
/// GR14b exactly (the inserted decimal separator IS the comma, the inserted grouping separator IS the period),
/// including zero suppression absorbing GROUPING periods and stopping at the COMMA decimal position (§13.18.40.5
/// — a dead-flag bug, fixed here per spec).
/// </summary>
public static partial class CobolEdit
{
    /// <summary>One resolved PICTURE EDITING phrase (ISO §13.18.40.5): the user editing <paramref name="Char1"/>
    /// renders as <paramref name="Neg"/> when the value is negative and <paramref name="Pos"/> otherwise
    /// (Table 8 for fixed insertion, Table 9 for floating). The simple-insertion (IS) form is sign-independent
    /// (<c>Neg == Pos</c> = literal-1); the extended sign-control (FOR) form selects by sign, the unspecified
    /// side defaulting to "the space character repeated for the number of characters in" the specified one
    /// (SR12c). character-1 is NEVER a digit position while it renders its literal (SR8 excludes every
    /// digit/edit symbol); a FLOATING character-1's other occurrences ARE digit positions, exactly as a floating
    /// currency symbol's are.
    /// <para>⛔ <paramref name="Neg"/> and <paramref name="Pos"/> are STRINGS, not characters, because
    /// §13.18.40.4 GR14's 'es' entry sizes the item from the literal: "If character-1 is a simple insertion
    /// symbol or a fixed insertion symbol, the size of literal-1 is counted in the size of the item. … For
    /// floating inserting, one occurrence of literal-2 or literal-3 is counted in the size of the item plus one
    /// character for each repetition of character-1." A literal of up to 50 characters is legal (SR9), so the
    /// render is VARIABLE-WIDTH and the 1:1 mask model cannot carry it — which is why both shapes used to be
    /// staged loud at bind (COBOLNET0899, kb/Work PB491) although Annex D.24 demonstrates both. They are now
    /// rendered by the SAME logical-image-then-materialize mechanism the multi-character currency string uses
    /// (<see cref="Materialize"/>): the editing rules still edit one position per symbol, and the one pass that
    /// knows a symbol's physical width expands it.</para>
    /// <para><paramref name="SimpleInsertion"/> is the phrase's FORM, and it is what decides whether this
    /// character-1 belongs to a zero-suppression or floating string (§13.18.40.5 rules 6 and 7: "Any of the
    /// simple insertion editing symbols embedded in this string or to the immediate right of this string are part
    /// of the string"). It is the IS form: rule 3 — "the symbols 'B', '0', '/', ',' and, if literal=1 is
    /// specified, character-1 are used as the simple insertion editing symbols"; the FOR form is FIXED insertion
    /// — rule 5 — "When character-1 is used, and is not a simple insertion character, it represents literal-2, or
    /// literal-3 as the insertion characters". ⛔ It is NOT derivable from <c>Neg == Pos</c>: a FOR phrase may
    /// name the same literal on both sides (<c>PIC ZT9 EDITING "T" FOR NEGATIVE IS ":" POSITIVE IS ":"</c>),
    /// which is fixed insertion and must survive the suppression walk that eats an IS-form ':' beside it.</para>
    /// <para><paramref name="Floating"/> is rule 6's own test applied to THIS character-1 — "Floating insertion
    /// editing is indicated by specifying a string of at least two identical floating insertion editing symbols",
    /// and rule 6's first sentence lists "the extended editing sign control symbols, if specified" among the
    /// floating insertion symbols. It is decided at bind, where the character-string is in hand.</para></summary>
    public readonly record struct EditRule(char Char1, string Neg, string Pos, bool SimpleInsertion, bool Floating)
    {
        /// <summary>The character positions this character-1 renders into — SR12a ("Literal-2 and literal-3 shall
        /// occupy the same number of character positions") and SR12c (the defaulted side is that many spaces)
        /// make the two sides agree, so either answers.</summary>
        public int Width => Neg.Length;
    }

    /// <summary>⛔ THE SIMPLE INSERTION EDITING SYMBOLS — ISO §13.18.40.5 rule 3, written down ONCE: "the symbols
    /// 'B', '0', '/', ',' and, if literal=1 is specified, character-1 are used as the simple insertion editing
    /// symbols". Every consumer of the set reads <see cref="IsSimpleInsertionSymbol"/> or
    /// <see cref="TrySimpleInsertion"/> — <see cref="Format"/>'s pass 1 (place the insertion character), its
    /// pass 2 (a symbol inside a suppression string takes the replacement character),
    /// <see cref="FindFloatingPlacement"/> (a symbol inside a floating string is a landing position for the
    /// floating character), <see cref="FormatAlphanumeric"/> and the floating-point significand renderer
    /// (<c>RenderFloat</c>) — because the set was previously spelled out at
    /// each site and the copies drifted to the pair {',', 'B'}, so <c>PIC ZZ/ZZ</c> and <c>PIC ZZ0ZZ</c> kept
    /// their insertion character where no significant numeric character stood to its left and <c>PIC ++/++9</c>
    /// landed its sign four positions early (kb/Work PB490). <c>CobolEditSimpleInsertionDriftTests</c> fails if
    /// the consumers disagree again.</summary>
    public static ReadOnlySpan<char> SimpleInsertionSymbols => "B0/,";

    /// <summary>Is <paramref name="maskChar"/> one of the picture's SIMPLE INSERTION editing symbols
    /// (§13.18.40.5 rule 3)? <paramref name="grouping"/> is the character playing the GROUPING separator's part —
    /// ',' in the dot-canonical core, and the caller's own choice on a RAW picture, because DECIMAL-POINT IS
    /// COMMA exchanges the two (§13.18.40.2 SR13: "the rules for the symbol period apply to the symbol comma, and
    /// the rules for the symbol comma apply to the symbol period"). Whichever of the pair is NOT the grouping
    /// separator is the decimal point, whose editing is SPECIAL insertion (rule 4), never simple.</summary>
    public static bool IsSimpleInsertionSymbol(char maskChar, char grouping = ',')
    {
        char u = char.ToUpperInvariant(maskChar);
        return u is ',' or '.' ? maskChar == grouping : SimpleInsertionSymbols.IndexOf(u) >= 0;
    }

    /// <summary>⛔ THE SYMBOLS THAT DEFINE AN EDITED CHARACTER CATEGORY — the one set ISO §13.18.40.4 GR7 and
    /// GR10 BOTH name, word for word: "at least one instance of character-1 or one of the symbols from the set
    /// 'B', '0', '/'". GR7 makes an X/A picture holding one ALPHANUMERIC-EDITED (§8.5.2.4); GR10 makes an N
    /// picture holding one NATIONAL-EDITED (§8.5.2.11); §13.18.40.5 Table 7 then gives both categories SIMPLE
    /// INSERTION and nothing else, which is why one predicate serves both.
    /// <para>It is <see cref="SimpleInsertionSymbols"/> minus the grouping separator: rule 3's set carries ','
    /// as well, but §13.18.40.6 Table 10 admits a ',' only beside the NUMERIC rows — the 'A X' and 'N' rows
    /// carry no ',' cell — so a comma can never stand in a character picture and GR7/GR10 leave it out. Deriving
    /// the set rather than spelling it a third time is the point: it was written out as a literal list at the
    /// alphanumeric arm and, missing character-1 entirely, at the national arm, which is how GR10's character-1
    /// leg came to be refused as an INVALID PICTURE (kb/Work PB492; the same shape as PB490 one level up).</para>
    /// </summary>
    /// <param name="maskChar">A symbol of the repeat-expanded picture character-string.</param>
    /// <param name="char1">The picture's declared PICTURE EDITING character-1 letters (§13.18.40.3 SR8),
    /// uppercased; null or empty when the clause carries no EDITING phrase.</param>
    public static bool IsEditedCategorySymbol(char maskChar, IReadOnlySet<char>? char1 = null)
    {
        char u = char.ToUpperInvariant(maskChar);
        if (char1 is not null && char1.Contains(u)) return true;
        return u is not (',' or '.') && SimpleInsertionSymbols.IndexOf(u) >= 0;
    }

    /// <summary>The picture's simple insertion symbols INCLUDING every IS-form PICTURE EDITING character-1, with
    /// the character each one inserts: §13.18.40.5 rule 3 — "Simple insertion editing results in the insertion
    /// character occupying the same character position in the edited item as the associated symbol occupies in
    /// character-string-1" — over §13.18.40.4 GR14's per-symbol text ('B' "a character position into which the
    /// character space will be inserted", '0' "the character zero", '/' "the character slant", ',' "the character
    /// comma"). The mask given here is the DOT-CANONICAL one <see cref="Format"/> works on, so the grouping
    /// separator is ','.</summary>
    private static bool TrySimpleInsertion(char maskChar, EditRule[]? edits, out char inserted)
    {
        char u = char.ToUpperInvariant(maskChar);
        if (edits is not null)
            foreach (var e in edits)
                if (e.SimpleInsertion && u == char.ToUpperInvariant(e.Char1))
                {
                    // Rule 3 is sign-independent: the IS form carries Neg == Pos == literal-1. The LOGICAL
                    // image holds one character per symbol, so a literal wider than one character stands here
                    // as its first character and Materialize writes the whole of it (GR14 'es').
                    inserted = e.Pos.Length > 0 ? e.Pos[0] : ' ';
                    return true;
                }
        if (!IsSimpleInsertionSymbol(u)) { inserted = '\0'; return false; }
        inserted = u == 'B' ? ' ' : maskChar;   // B inserts a space; 0 / and , insert themselves
        return true;
    }

    /// <summary>The EditRule whose character-1 is <paramref name="maskChar"/>, or null.</summary>
    private static EditRule? RuleFor(char maskChar, EditRule[]? edits)
    {
        if (edits is null) return null;
        char u = char.ToUpperInvariant(maskChar);
        foreach (var e in edits)
            if (u == char.ToUpperInvariant(e.Char1)) return e;
        return null;
    }

    /// <summary>Swap <c>.</c>↔<c>,</c> — the §13.18.40.2 SR13 role exchange, applied to a mask entering the
    /// dot-canonical core and to the rendered output leaving it (the swap is its own inverse).</summary>
    private static string SwapSeparators(string s)
    {
        var a = s.ToCharArray();
        for (int i = 0; i < a.Length; i++)
            a[i] = a[i] switch { '.' => ',', ',' => '.', _ => a[i] };
        return new string(a);
    }

    /// <summary>Format <paramref name="value"/> (unscaled, with <paramref name="valueScale"/> fraction digits)
    /// into <paramref name="picture"/> — the EXPANDED edited picture (repeats unrolled, uppercased, the implied
    /// decimal point <c>V</c> retained; <c>V</c> occupies no output position).</summary>
    public static string Format(Int128 value, int valueScale, string picture, bool blankWhenZero = false,
        char currency = '$', bool commaMode = false, EditRule[]? edits = null, string? currencyString = null)
    {
        // DECIMAL-POINT IS COMMA: canonicalize the mask (dot = decimal), render, swap the rendered separators
        // back (GR14b). ⛔ The swap is applied to the LOGICAL image and only at the positions whose SYMBOL is a
        // separator, never to the physical one: a currency string or an editing literal may itself hold a '.'
        // or a ',' (§12.3.7.3 r23 lets literal-7 be any length; SR9 lets a literal be 50 characters), and
        // §13.18.40.2 SR13 exchanges the roles of the two SYMBOLS, not the characters of a literal.
        string mask = commaMode ? SwapSeparators(picture) : picture;
        // The output pattern: V marks the implied point but holds no character position (ISO §13.18.40.3).
        string pattern = mask.Replace("V", "").Replace("P", "");   // V and P hold no output position (§13.18.40.3)
        // The currency STRING replaces the canonical '$' the mask carries whenever it is not that single
        // character — a one-character '#' (§12.3.7.4 GR13) as much as a multi-character "USD" (§12.3.7.3 r23).
        bool wideCurrency = currencyString is not null && currencyString != currency.ToString();
        var (logical, placed) = Render(value, valueScale, mask, pattern, blankWhenZero, currency, edits, wideCurrency);
        if (commaMode)
            for (int i = 0; i < pattern.Length; i++)
                if (pattern[i] is '.' or ',')
                    logical[i] = logical[i] switch { '.' => ',', ',' => '.', _ => logical[i] };
        return Materialize(pattern, logical, placed, edits, currency, currencyString, value < 0);
    }

    /// <summary>⭐ THE LOGICAL RENDER — one character per picture SYMBOL, which is the image every editing rule
    /// of §13.18.40.5 is written about. <see cref="Materialize"/> turns it into the physical image by giving the
    /// two variable-width symbols of §13.18.40.4 GR14 — the currency symbol ('cs', whose currency string "may
    /// have any length") and a PICTURE EDITING character-1 ('es', whose literal may be 50 characters) — the
    /// width GR14 gives them. <paramref name="placed"/> is null (and never read) unless some symbol IS wider
    /// than one character, so an ordinary program's render allocates and walks exactly what it did before.</summary>
    private static (char[] Logical, bool[]? Placed) Render(Int128 value, int valueScale, string picture,
        string pattern, bool blankWhenZero, char currency, EditRule[]? edits, bool wideCurrency)
    {
        bool negative = value < 0;

        // ⛔ THE VARIABLE-WIDTH MARKER. `placed[i]` says the symbol at position i renders its whole STRING here
        // — the currency string, or literal-2/literal-3/literal-1 — as opposed to holding a digit, a space or a
        // replacement character. It is the ONE fact Materialize cannot re-derive, because where a FLOATING
        // symbol lands is decided by the suppression walk (§13.18.40.5 rule 6 a) and not by the mask.
        bool[]? placed = wideCurrency || HasWideLiteral(edits) ? new bool[pattern.Length] : null;

        // Pre-scan (on the full picture): fixed vs floating sign/currency — ONE occurrence is a fixed-insertion
        // character; TWO OR MORE form a floating string whose members are also digit positions (§13.18.40.4).
        // The currency symbol is the program's CURRENCY SIGN PICTURE SYMBOL (ISO §12.3.7 GR13; '$' per SR25).
        char currencyChar = char.ToUpperInvariant(currency);
        int plusCount = 0, minusCount = 0, currencyCount = 0;
        foreach (char raw in pattern)
        {
            char p = char.ToUpperInvariant(raw);
            if (p == '+') plusCount++;
            else if (p == '-') minusCount++;
            else if (p == currencyChar) currencyCount++;
        }
        bool isFixedPlus = plusCount == 1 && minusCount == 0;
        bool isFixedMinus = minusCount == 1 && plusCount == 0;
        bool isFixedCurrency = currencyCount == 1;

        // Digit capacity: 9/Z/* always; floating $/+/- and a floating EXTENDED editing sign control character-1
        // are digit positions, but the floating string reserves ONE position for the symbol itself.
        // ⛔ §13.18.40.5 rule 6, first sentence: "The currency symbol, the extended editing sign control
        // symbols, if specified, and the fixed editing sign control symbols '+' and '-' are used as the floating
        // insertion symbols" — character-1 under a FOR phrase is one of them, and rule 6's fourth paragraph
        // ("The second floating symbol represents the leftmost limit of the numeric data that may be stored in
        // the item") is what makes its repetitions digit positions. The rule that decided FLOATING was asked at
        // bind, where the character-string is in hand, and travels on EditRule.Floating (kb/Work PB491).
        int trueDigitCount = 0;
        bool floatingChar1 = false;
        foreach (char raw in pattern)
        {
            char p = char.ToUpperInvariant(raw);
            if (p is '9' or 'Z' or '*') trueDigitCount++;
            else if (p == currencyChar && !isFixedCurrency) trueDigitCount++;
            else if (p == '+' && !isFixedPlus) trueDigitCount++;
            else if (p == '-' && !isFixedMinus) trueDigitCount++;
            else if (RuleFor(raw, edits) is { Floating: true }) { trueDigitCount++; floatingChar1 = true; }
        }
        bool hasFloating = currencyCount > 1 || plusCount > 1 || minusCount > 1 || floatingChar1;
        int effectiveDigitCount = hasFloating ? trueDigitCount - 1 : trueDigitCount;

        // The mask's fraction scale: digit positions after the point (V in the picture, or the '.' insertion).
        int fracDigits = FractionDigits(picture, currencyChar, isFixedCurrency, isFixedPlus, isFixedMinus);

        // Align the operand to the mask's scale (truncation — §14.9.25 GR: excess fraction digits truncate) and
        // render the absolute digit string at the mask's capacity (excess INTEGER digits truncate high-order).
        // ⛔ The widening is the STORE-CAP form (kb/Work PB639): the digits this mask could never show are
        // dropped decimally BEFORE the multiply, so the high-order truncation two lines below is the LOW-ORDER
        // digits of the true value (§14.6.8.2 r4 "zero fill or truncation on either end"), not the low-order
        // digits of an Int128 wrap. MOVE 10^30 TO PIC Z(8)9.9(9) showed 123822295.304634368 for 0.000000000.
        Int128 scaled = CobolNum.RescaleStoreCap(value, valueScale, fracDigits, CobolRounding.Truncation);
        string digits = Int128.Abs(scaled).ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (digits.Length < effectiveDigitCount) digits = digits.PadLeft(effectiveDigitCount, '0');
        else if (digits.Length > effectiveDigitCount) digits = digits[^effectiveDigitCount..];

        // ⛔ BLANK WHEN ZERO (ISO §13.18.8.4 GR1) — "the content of the data item is set to all spaces when the
        // item is a receiving operand and THE VALUE BEING STORED is zero", which is the value the item ENDS UP
        // HOLDING: exactly the digit string above. §14.9.25.4 GR6 d) closes "Alignment of the numeric value by
        // decimal point, any necessary zero filling, any truncation of digits, and transfer of the algebraic data
        // into the receiving data item, take place as defined in 14.6.8", and §14.6.8.2 r4 is "zero fill or
        // truncation on either end as required" — so a store that truncates to zero at EITHER end (0.4 into
        // PIC 9(3), 100 into PIC 99) stores zero and blanks. §13.18.8.1 says the same in one line: the clause
        // "causes the blanking of an item when a value of zero is being stored in it".
        // ⛔ THE GUARD BELONGS HERE, AFTER THE STORE RESCALE, and nowhere else: every arm reaches this one
        // formatter, but only the arithmetic emitters hand it a value already at the resultant's scale, so a
        // guard on the raw (value, valueScale) pair blanked for COMPUTE and not for MOVE on identical items
        // (kb/Work PB566 — MOVE 0.4 TO PIC ZZZ9 BLANK WHEN ZERO rendered '   0'). It is the same "value being
        // stored" that rule 7 b)'s all-suppressed test below already reads.
        if (blankWhenZero && digits.AsSpan().IndexOfAnyExcept('0') < 0)
            return (Filled(' ', pattern.Length), placed);

        // Pass 1 — right-to-left: fill digit positions, place insertion and fixed characters.
        var output = new char[pattern.Length];
        int digitIdx = digits.Length - 1;
        for (int i = pattern.Length - 1; i >= 0; i--)
        {
            char p = char.ToUpperInvariant(pattern[i]);
            // SIMPLE INSERTION (ISO §13.18.40.5 rule 3) — 'B', '0', '/', ',' and every IS-form PICTURE EDITING
            // character-1, from the ONE set (TrySimpleInsertion), resolved BEFORE the metacharacter switch so a
            // literal that happens to be a mask symbol (':', '/', '(', …) is never re-interpreted. Not a digit
            // position (SR8 for character-1; GR14 for the four symbols) — the digit index is untouched.
            if (TrySimpleInsertion(pattern[i], edits, out char simpleIns))
            {
                output[i] = simpleIns;
                if (placed is not null && RuleFor(pattern[i], edits) is not null) placed[i] = true;
                continue;
            }
            // FIXED INSERTION with an extended editing sign control symbol (rule 5, Table 8): the FOR form selects
            // Neg on a negative value, Pos otherwise (SR12c default = space). A FLOATING one (rule 6) is skipped
            // here and treated below exactly as a floating currency symbol is: its occurrences are DIGIT
            // positions and the literal lands once, at the placement pass (kb/Work PB491).
            if (RuleFor(pattern[i], edits) is { } rule)
            {
                if (!rule.Floating)
                {
                    string lit = negative ? rule.Neg : rule.Pos;
                    output[i] = lit.Length > 0 ? lit[0] : ' ';
                    if (placed is not null) placed[i] = true;
                    continue;
                }
                output[i] = digitIdx >= 0 ? digits[digitIdx--] : '0';
                continue;
            }
            if (p == currencyChar)
            {
                output[i] = isFixedCurrency ? currencyChar : digitIdx >= 0 ? digits[digitIdx--] : '0';
                if (placed is not null && isFixedCurrency) placed[i] = true;
                continue;
            }
            switch (p)
            {
                case '9' or 'Z' or '*':
                    output[i] = digitIdx >= 0 ? digits[digitIdx--] : '0';
                    break;
                case '+':
                    output[i] = isFixedPlus ? (negative ? '-' : '+') : digitIdx >= 0 ? digits[digitIdx--] : '0';
                    break;
                case '-':
                    output[i] = isFixedMinus ? (negative ? '-' : ' ') : digitIdx >= 0 ? digits[digitIdx--] : '0';
                    break;
                case '.': output[i] = '.'; break;   // SPECIAL insertion (§13.18.40.5 rule 4)
                case 'C':   // CR — spaces when the value is not negative (§13.18.40.4 fixed insertion)
                    if (i + 1 < pattern.Length && char.ToUpperInvariant(pattern[i + 1]) == 'R')
                    {
                        output[i] = negative ? 'C' : ' ';
                        output[i + 1] = negative ? 'R' : ' ';
                    }
                    else output[i] = pattern[i];
                    break;
                case 'R':   // second char of CR — placed by the 'C' case
                    if (!(i > 0 && char.ToUpperInvariant(pattern[i - 1]) == 'C')) output[i] = pattern[i];
                    break;
                case 'D':   // DB
                    if (i + 1 < pattern.Length && char.ToUpperInvariant(pattern[i + 1]) == 'B')
                    {
                        output[i] = negative ? 'D' : ' ';
                        output[i + 1] = negative ? 'B' : ' ';
                    }
                    else output[i] = pattern[i];
                    break;
                default: output[i] = pattern[i]; break;
            }
        }

        // Pass 2 — left-to-right zero suppression/replacement (Z → space, * → asterisk; a floating symbol's zone
        // suppresses like Z). §13.18.40.5 rule 7 a): "the corresponding replacement character is placed into any
        // character position immediately preceding whichever of the following is encountered first" — the first
        // nonzero numeric character, the first character position for which no zero suppression is specified, or
        // the decimal point position (rule 6 a) is the same walk for a floating string, its landing placed below).
        // ⛔ THE POSITIONS ARE THE STRING'S. Rules 6 and 7 both say "Any of the simple insertion editing symbols
        // embedded in this string or to the immediate right of this string are part of the string" — EMBEDDED or
        // RIGHT, never left, so a simple insertion symbol takes the replacement character only once a member of
        // the string (Z, * or a floating symbol) has been seen. `PIC 0ZZ9` MOVE 1 keeps its leading zero ("0  1")
        // while `PIC ZZ0ZZ` MOVE 12 suppresses the embedded one ("   12"); §13.18.40.4 GR15 says the same from
        // the VALIDATE side — "the character zero if the symbol '0' is neither part of floating insertion editing
        // nor of zero suppression with replacement editing; otherwise, the character zero or, if no significant
        // numeric character appears to its left, the corresponding floating insertion character or replacement
        // character respectively" (kb/Work PB490: the set was the hand-written pair {',', 'B'} and the membership
        // test was missing altogether, so a leading ',' was eaten and an embedded '0' or '/' survived).
        bool suppressing = true;
        bool inString = false;   // a Z / * / floating member has been seen — only then is an insertion "part of the string"
        bool asteriskFill = pattern.Contains('*');
        bool allIntegerSuppressed = true;
        for (int i = 0; i < pattern.Length && suppressing; i++)
        {
            char p = char.ToUpperInvariant(pattern[i]);
            if ((p == currencyChar && !isFixedCurrency) || RuleFor(pattern[i], edits) is { Floating: true })
            {
                inString = true;
                if (output[i] == '0') output[i] = ' ';
                else { suppressing = false; allIntegerSuppressed = false; }
                continue;
            }
            switch (p)
            {
                case 'Z':
                    inString = true;
                    if (output[i] == '0') output[i] = ' ';
                    else { suppressing = false; allIntegerSuppressed = false; }
                    break;
                case '*':
                    inString = true;
                    if (output[i] == '0') output[i] = '*';
                    else { suppressing = false; allIntegerSuppressed = false; }
                    break;
                case '+' when !isFixedPlus:
                case '-' when !isFixedMinus:
                    inString = true;
                    if (output[i] == '0') output[i] = ' ';
                    else { suppressing = false; allIntegerSuppressed = false; }
                    break;
                case '.':
                    suppressing = false;
                    break;
                case '9':
                    suppressing = false;
                    allIntegerSuppressed = false;
                    break;
                default:
                    // Inside the string: a simple insertion symbol is part of it and takes the replacement
                    // character; anything else is "the first character position for which no zero suppression
                    // with replacement is specified" and ends the walk. Outside it (a leading insertion, a
                    // leading fixed sign or currency) nothing is replaced and the string has yet to start.
                    if (!inString) break;
                    if (TrySimpleInsertion(pattern[i], edits, out _))
                    {
                        output[i] = asteriskFill ? '*' : ' ';
                        // The literal is no longer AT this position — the replacement character is (rule 7 a),
                        // and GR14 'es' still counts the literal's width, so Materialize repeats the
                        // replacement character across it rather than writing literal-1.
                        if (placed is not null) placed[i] = false;
                    }
                    else suppressing = false;
                    break;
            }
        }

        // Zero + every position suppressible (no fixed 9 anywhere) → the whole field blanks (spaces, or asterisk
        // fill keeping the actual decimal point — §13.18.40.4 zero-suppression rules).
        bool fullFieldBlanked = false;
        if (allIntegerSuppressed && scaled == 0 && !pattern.Contains('9'))
        {
            fullFieldBlanked = true;
            for (int i = 0; i < output.Length; i++)
            {
                char p = char.ToUpperInvariant(pattern[i]);
                output[i] = asteriskFill ? (p == '.' ? '.' : '*') : ' ';
                // "all character positions of the item will contain the character space / asterisk" (rule 7 b)
                // — every position, so no symbol renders its string; Materialize still gives each the width
                // GR14 gives it, so the physical image stays the item's own size.
                if (placed is not null) placed[i] = false;
            }
        }
        if (fullFieldBlanked && !asteriskFill) return (output, placed);

        // Floating symbol placement: the symbol lands at the rightmost suppressed position of its floating zone.
        if (plusCount > 0 && plusCount + minusCount > 1)
        {
            int pos = FindFloatingPlacement(pattern, output, '+', edits);
            if (pos >= 0) output[pos] = negative ? '-' : '+';
        }
        else if (minusCount > 0 && plusCount + minusCount > 1)
        {
            int pos = FindFloatingPlacement(pattern, output, '-', edits);
            if (pos >= 0) output[pos] = negative ? '-' : ' ';
        }
        if (currencyCount > 1)
        {
            int pos = FindFloatingPlacement(pattern, output, currencyChar, edits);
            if (pos >= 0) { output[pos] = currencyChar; if (placed is not null) placed[pos] = true; }
        }
        // A FLOATING extended editing sign control symbol lands by the SAME rule 6 a) walk — Table 9 selects
        // literal-2 on a negative value and literal-3 otherwise, the unspecified side being SR12c's spaces
        // (kb/Work PB491). Its other occurrences stay the digits pass 1 wrote.
        if (floatingChar1 && edits is not null)
            foreach (var e in edits)
            {
                if (!e.Floating) continue;
                int pos = FindFloatingPlacement(pattern, output, e.Char1, edits);
                if (pos < 0) continue;
                string lit = negative ? e.Neg : e.Pos;
                output[pos] = lit.Length > 0 ? lit[0] : ' ';
                if (placed is not null) placed[pos] = true;
            }

        return (output, placed);
    }

    /// <summary>⛔ ISO §13.18.8.4 GR3's CONTENT TEST — "the content of the sending data item is all spaces",
    /// after which "the value of the sending data item is considered to be zero" wherever the item is a sending
    /// item and the object of the operation is a numeric or numeric-edited data item. The de-editing arm needs
    /// no separate answer: every digit position of an all-spaces image contributes zero, so <see cref="DeEdit"/>
    /// already yields exactly that zero — this predicate exists for the operations that do NOT de-edit by
    /// default, today the relation condition (kb/Work PB509). An EMPTY image answers true vacuously: it has no
    /// character position that is not a space.</summary>
    public static bool IsBlanked(string? image) => image is null || image.AsSpan().IndexOfAnyExcept(' ') < 0;

    /// <summary>DE-EDIT a numeric-edited item's image back to its numeric value (ISO §14.9.25.4 GR5 — a
    /// numeric-edited SENDER moved to a numeric receiver, the COBOL-85 de-editing move): every digit POSITION of
    /// the mask contributes its image digit (a suppressed/replaced position — space, <c>*</c>, a floating symbol's
    /// landing spot — contributes zero), yielding the unscaled value at the mask's fraction scale
    /// (<see cref="MaskScale"/>); the value is negative when the image carries the mask's negative sign
    /// (<c>-</c> anywhere a sign can land, or a non-blank CR/DB). Under EC-DATA-INCOMPATIBLE checking (ISO
    /// §14.6.13.2 rule 4: content "not a possible result for any editing operation in that data item" → the
    /// exception, the result undefined) the image is verified to be an editing result — <see cref="Format"/> of the
    /// de-edited value at the mask's scale (with the item's BLANK WHEN ZERO, <paramref name="blankWhenZero"/>) must
    /// reproduce it: Format IS the one editor, so the round trip is the exact test (kb/Work PB66 sweep — the
    /// floating-point form's <c>DeEditFloat</c> validates position by position); the fatal exception is raised
    /// before any receiver is written. With checking off the digit positions are read as before.</summary>
    public static Int128 DeEdit(string image, string picture, char currency = '$', bool commaMode = false,
        EditRule[]? edits = null, string? currencyString = null, bool blankWhenZero = false)
    {
        string physical = image;
        string rawPicture = picture;
        if (commaMode) picture = SwapSeparators(picture);   // canonicalize (§13.18.40.2 SR13) — digit POSITIONS are unchanged
        string pattern = picture.Replace("V", "").Replace("P", "");   // V and P hold no output position (§13.18.40.3)
        char currencyChar = char.ToUpperInvariant(currency);
        int plus = 0, minus = 0, cs = 0;
        foreach (char raw in pattern)
        {
            char p = char.ToUpperInvariant(raw);
            if (p == '+') plus++;
            else if (p == '-') minus++;
            else if (p == currencyChar) cs++;
        }
        bool fixedPlus = plus == 1 && minus == 0, fixedMinus = minus == 1 && plus == 0, fixedCs = cs == 1;
        int csWidth = currencyString is { Length: > 1 } ? currencyString.Length : 1;

        Int128 value = 0;
        bool negative = false;
        // ⛔ THE PHYSICAL CURSOR. The mask's positions and the image's characters are 1:1 only while every symbol
        // is one character wide; §13.18.40.4 GR14 gives TWO symbols a width of their own — the currency symbol
        // ('cs', whose currency string "may have any length") and a PICTURE EDITING character-1 ('es', whose
        // literal may be 50 characters, SR9) — so the walk advances the image by each symbol's own width. A
        // symbol used with FIXED insertion is unconditionally that wide; a FLOATING one renders its string at
        // exactly one occurrence, so its occurrences are tested against the string and the one that matches is
        // the landing (the others are digit positions, rule 6). This replaces the search-and-collapse pass that
        // served the currency string alone and could not have served the editing literal at all (kb/Work PB491).
        int phys = 0;
        for (int i = 0; i < pattern.Length && phys < image.Length; i++)
        {
            char p = char.ToUpperInvariant(pattern[i]);
            char c = image[phys];
            // A user EDITING character-1 holds no digit while it renders its literal; a sign-control (FOR)
            // character-1 whose image text is literal-2 recovers the sign (ISO §13.18.40.5 Table 8 / Table 9 —
            // the de-editing MOVE, §14.9.25.4 GR5).
            if (RuleFor(pattern[i], edits) is { } e)
            {
                int w = Math.Max(1, e.Width);
                if (!e.Floating)
                {
                    if (e.Neg != e.Pos && Matches(image, phys, e.Neg)) negative = true;
                    phys += w;
                    continue;
                }
                if (Matches(image, phys, e.Neg)) { if (e.Neg != e.Pos) negative = true; phys += w; continue; }
                if (Matches(image, phys, e.Pos)) { phys += w; continue; }
                value = value * 10 + (c is >= '0' and <= '9' ? c - '0' : 0);   // a floating repetition — a digit position
                phys++;
                continue;
            }
            if (p == currencyChar)
            {
                if (fixedCs) { phys += csWidth; continue; }
                if (csWidth > 1 && Matches(image, phys, currencyString!)) { phys += csWidth; continue; }
                value = value * 10 + (c is >= '0' and <= '9' ? c - '0' : 0);
                phys++;
                continue;
            }
            phys++;
            bool digitPos = p is '9' or 'Z' or '*'
                || (p == '+' && !fixedPlus)
                || (p == '-' && !fixedMinus);
            if (digitPos) { value = value * 10 + (c is >= '0' and <= '9' ? c - '0' : 0); continue; }
            if (c == '-') negative = true;                      // a fixed sign position holding minus
            else if (p == 'C' && c == 'C') negative = true;     // CR rendered (negative value)
            else if (p == 'D' && c == 'D') negative = true;     // DB rendered
        }
        // A floating minus landed inside its zone. ⛔ Gated on the mask actually HAVING a floating '-' — an
        // ungated scan reads a '-' that belongs to a currency string or an EDITING literal as the item's sign.
        if (minus > 1 && image.Contains('-')) negative = true;
        Int128 result = negative ? -value : value;
        if (ExceptionState.DataIncompatibleChecking)
        {
            // Re-edit through the PUBLIC entry with the ORIGINAL mask and flag: the comma swap belongs to the
            // logical image (a currency string or an editing literal may itself hold a separator), so it is
            // Format's to apply, never a swap over the physical result.
            string expected = Format(result, MaskScale(picture, currency, commaMode: false, edits), rawPicture,
                blankWhenZero, currency, commaMode, edits, currencyString);
            if (expected != physical)
                ExceptionState.DataIncompatibleError(
                    $"the content '{physical}' is not a possible result of editing into {picture} (a de-editing MOVE, ISO 14.6.13.2 rule 4)");
        }
        return result;
    }

    /// <summary>Checked numeric→edited conversion for ARITHMETIC stores under ON SIZE ERROR (ISO §14.7.5 — size
    /// error condition case 3: after decimal-point alignment, |value| exceeds the mask's digit positions; storing
    /// rule 2: that resultant is left UNCHANGED and only the flag is set, the statement's other receivers still
    /// store). MOVE keeps the unchecked <see cref="Format"/> — high-order truncation IS the defined MOVE behavior
    /// (§14.9.25).</summary>
    public static bool TryFormat(Int128 value, int valueScale, string picture, out string image,
        bool blankWhenZero = false, char currency = '$', bool commaMode = false, EditRule[]? edits = null,
        string? currencyString = null)
    {
        var (capacity, fracDigits) = MaskCapacity(picture, currency, commaMode);
        // ⛔ The capacity test may never look at a WRAPPED alignment (kb/Work PB639): a widening the Int128
        // carrier cannot form is further from zero than any mask can show (§14.7.5 case 3), and the wrap can
        // land back inside `capacity` — a guard that silently passes exactly the magnitudes it exists to catch.
        if (!CobolNum.WideningFits(value, fracDigits - valueScale)) { image = string.Empty; return false; }
        Int128 scaled = CobolNum.Rescale(value, valueScale, fracDigits, CobolRounding.Truncation);
        if (Int128.Abs(scaled) >= CobolNum.Pow10Wide(capacity)) { image = string.Empty; return false; }
        image = Format(value, valueScale, picture, blankWhenZero, currency, commaMode, edits, currencyString);
        return true;
    }

    // ── THE VARIABLE-WIDTH SYMBOLS (§13.18.40.4 GR14; kb/Work PB60 / AR-15.68.3-3, PB491) ─────────────────────
    // The editing rules of §13.18.40.5 are written for symbols that each occupy ONE character position, and two
    // symbols do not: the currency symbol, whose currency string "may have any length" (§12.3.7.3 r23), and a
    // PICTURE EDITING character-1, whose literal may be 50 characters (§13.18.40.3 SR9). Both are handled the
    // same way and in one place — edit the LOGICAL image (one position per symbol, so every editing rule reads
    // as written), then let Materialize give each symbol the width GR14 gives it. DeEdit walks the same widths
    // in reverse. Nothing else in this file knows that a symbol can be wider than one character.

    /// <summary>⭐ LOGICAL → PHYSICAL, the ONE place a picture symbol wider than one character position is
    /// written out. <paramref name="placed"/> is null when no symbol is wide, and then the logical image IS the
    /// physical one — an ordinary program pays nothing for this.
    /// <para>The widths are ISO §13.18.40.4 GR14's own:</para>
    /// <list type="bullet">
    ///   <item><b>cs</b> — "The first occurrence of the currency symbol adds the number of characters in the
    ///     currency string to the size of the item. Each subsequent occurrence of the currency symbol adds one."
    ///     Fixed insertion renders the string once, at the symbol's position; floating insertion lands it once,
    ///     wherever rule 6 a) put it, and the other occurrences are digit positions one character wide.</item>
    ///   <item><b>es</b> — "If character-1 is a simple insertion symbol or a fixed insertion symbol, the size of
    ///     literal-1 is counted in the size of the item. … For extended editing sign control symbols with fixed
    ///     insertion, each occurrence of the character(s) specified in the associated literal are counted in the
    ///     size of the item. For floating inserting, one occurrence of literal-2 or literal-3 is counted in the
    ///     size of the item plus one character for each repetition of character-1." So a NON-floating
    ///     character-1 is literal-wide at EVERY occurrence — including one the zero-suppression walk overwrote,
    ///     which then carries that many replacement characters — and a floating one is literal-wide at exactly
    ///     the occurrence it landed on.</item>
    /// </list>
    /// <para>When a floating symbol never landed — §13.18.40.5 rule 6 b)'s zero value, or a BLANK WHEN ZERO
    /// image — "all character positions will contain the space character", so the item's own width is still
    /// owed: the FIRST occurrence carries it, and every position is a space either way. This is the shape the
    /// currency string had alone (kb/Work PB60), generalized so the EDITING literal of §13.18.40.3 SR9 — up to
    /// 50 characters, and Annex D.24's own examples — renders by the same mechanism (kb/Work PB491).</para>
    /// </summary>
    private static string Materialize(string pattern, char[] logical, bool[]? placed, EditRule[]? edits,
        char currency, string? currencyString, bool negative)
    {
        if (placed is null) return new string(logical);
        char currencyChar = char.ToUpperInvariant(currency);
        bool wideCurrency = currencyString is not null && currencyString != currency.ToString();
        // One pre-pass over the mask: each wide symbol's FIRST occurrence, and whether any occurrence of it
        // rendered its string — the two facts the per-position decision needs, so the walk stays linear.
        int firstCurrency = -1;
        bool anyCurrencyPlaced = false;
        var firstChar1 = new Dictionary<char, int>(2);
        var char1Placed = new HashSet<char>(2);
        for (int i = 0; i < pattern.Length; i++)
        {
            char p = char.ToUpperInvariant(pattern[i]);
            if (wideCurrency && p == currencyChar)
            {
                if (firstCurrency < 0) firstCurrency = i;
                if (placed[i]) anyCurrencyPlaced = true;
            }
            else if (RuleFor(pattern[i], edits) is { Width: > 1 })
            {
                firstChar1.TryAdd(p, i);
                if (placed[i]) char1Placed.Add(p);
            }
        }
        var sb = new System.Text.StringBuilder(logical.Length + 16);
        for (int i = 0; i < pattern.Length; i++)
        {
            char p = char.ToUpperInvariant(pattern[i]);
            if (wideCurrency && p == currencyChar)
            {
                if (placed[i]) sb.Append(currencyString);
                else if (i == firstCurrency && !anyCurrencyPlaced) sb.Append(logical[i], currencyString!.Length);
                else sb.Append(logical[i]);
                continue;
            }
            if (RuleFor(pattern[i], edits) is { Width: > 1 } e)
            {
                if (placed[i]) sb.Append(negative ? e.Neg : e.Pos);
                else if (!e.Floating || (i == firstChar1[p] && !char1Placed.Contains(p))) sb.Append(logical[i], e.Width);
                else sb.Append(logical[i]);
                continue;
            }
            sb.Append(logical[i]);
        }
        return sb.ToString();
    }

    /// <summary>Does the image carry <paramref name="text"/> at <paramref name="at"/>? An empty literal matches
    /// nothing, so a degenerate EDITING literal cannot swallow the walk.</summary>
    private static bool Matches(string image, int at, string text)
        => text.Length > 0 && at + text.Length <= image.Length
           && string.CompareOrdinal(image, at, text, 0, text.Length) == 0;

    /// <summary>Is any EDITING literal wider than the one character position the logical image gives it
    /// (§13.18.40.4 GR14 'es')? The whole variable-width path is skipped when none is.</summary>
    private static bool HasWideLiteral(EditRule[]? edits)
    {
        if (edits is null) return false;
        foreach (var e in edits) if (e.Width > 1) return true;
        return false;
    }

    private static char[] Filled(char c, int n)
    {
        var a = new char[n];
        Array.Fill(a, c);
        return a;
    }

    /// <summary>The mask's total digit-position capacity (9/Z/* plus floating-string members, less the ONE
    /// position the floating symbol itself occupies) and its fraction scale — the §14.7.5 size-error bound.
    /// Mirrors <see cref="Format"/>'s prologue exactly. Public so the compiler can reuse the ONE canonical
    /// edited digit-position count for the HIGHEST/LOWEST-ALGEBRAIC PICTURE fold (§15.43/§15.58; singular-pattern).</summary>
    public static (int Capacity, int FracDigits) MaskCapacity(string picture, char currency = '$', bool commaMode = false,
        EditRule[]? edits = null)
    {
        if (commaMode) picture = SwapSeparators(picture);   // canonicalize (§13.18.40.2 SR13)
        string pattern = picture.Replace("V", "").Replace("P", "");   // V and P hold no output position (§13.18.40.3)
        char currencyChar = char.ToUpperInvariant(currency);
        int plus = 0, minus = 0, cs = 0;
        foreach (char raw in pattern)
        {
            char p = char.ToUpperInvariant(raw);
            if (p == '+') plus++;
            else if (p == '-') minus++;
            else if (p == currencyChar) cs++;
        }
        bool fixedPlus = plus == 1 && minus == 0, fixedMinus = minus == 1 && plus == 0, fixedCs = cs == 1;
        int digits = 0;
        bool floatingChar1 = false;
        foreach (char raw in pattern)
        {
            char p = char.ToUpperInvariant(raw);
            if (p is '9' or 'Z' or '*') digits++;
            else if (p == currencyChar && !fixedCs) digits++;
            else if (p == '+' && !fixedPlus) digits++;
            else if (p == '-' && !fixedMinus) digits++;
            // A FLOATING extended editing sign control symbol's repetitions are digit positions (§13.18.40.5
            // rule 6) — the same footing as a floating currency symbol's, and the same footing the analyzer's
            // own DigitPositions count gives them (kb/Work PB491).
            else if (RuleFor(raw, edits) is { Floating: true }) { digits++; floatingChar1 = true; }
        }
        bool hasFloating = cs > 1 || plus > 1 || minus > 1 || floatingChar1;
        return (hasFloating ? digits - 1 : digits,
                FractionDigits(picture, currencyChar, fixedCs, fixedPlus, fixedMinus, edits));
    }

    /// <summary>The mask's fraction scale — digit positions right of the point (<c>V</c> or <c>.</c>). Public so
    /// the compiler can fold the working scale of an edited RECEIVER at emit time (a quotient/ROUNDED result must
    /// be computed and rounded AT this scale before editing, ISO §14.7.4/§14.7.7).</summary>
    public static int MaskScale(string picture, char currency = '$', bool commaMode = false,
        EditRule[]? edits = null)
    {
        if (commaMode) picture = SwapSeparators(picture);   // canonicalize (§13.18.40.2 SR13) — the comma IS the decimal position
        char currencyChar = char.ToUpperInvariant(currency);
        int plus = 0, minus = 0, cs = 0;
        foreach (char raw in picture)
        {
            char p = char.ToUpperInvariant(raw);
            if (p == '+') plus++;
            else if (p == '-') minus++;
            else if (p == currencyChar) cs++;
        }
        return FractionDigits(picture, currencyChar, cs == 1, plus == 1 && minus == 0, minus == 1 && plus == 0);
    }

    /// <summary>Digit positions to the right of the point — the <c>V</c> in the picture, or the <c>.</c> insertion
    /// character (only one of the two may appear, ISO §13.18.40.3).</summary>
    private static int FractionDigits(string picture, char currencyChar, bool fixedCs, bool fixedPlus,
        bool fixedMinus, EditRule[]? edits = null)
    {
        // A FLOATING extended editing sign control symbol is a digit position on exactly the same footing as a
        // floating currency symbol (§13.18.40.5 rule 6 — the same sentence lists both among the floating
        // insertion symbols), and §13.18.40.3 SR29 allows a floating string to reach past the decimal point, so
        // it counts in both branches below (kb/Work PB491).
        bool FloatingChar1(char c) => RuleFor(c, edits) is { Floating: true };
        // PICTURE P scaling positions (§13.18.40.3): trailing P → a NEGATIVE mask scale (the value is a multiple
        // of 10^P — PIC ZZZPP aligns 900 to unscaled 9, NC124A PICTURE-TEST-30); leading P → every digit position
        // is fractional (scale = P-count + digit positions). P never coexists with V-fraction digits.
        // ⛔ The leading/trailing split anchors on EVERY digit position — 9/Z/* AND a FLOATING string's member
        // occurrences (§13.18.40.6 Table 10 puts 'P (left of decimal point)' beside floating cs and +/−) — never
        // on only the literal 9/Z/* (kb/Work PB155: `PIC $$$$PP` has no 9/Z/* at all, so its rightmost P run
        // read as LEADING and the mask scale came out +2 where the value is a multiple of 10^2, scale −2).
        // A FIXED single +/−/cs is not a digit position and must not anchor (a trailing fixed sign sits right
        // of a trailing P run: 99PPCR).
        int pCount = 0;
        foreach (char raw in picture) if (char.ToUpperInvariant(raw) == 'P') pCount++;
        if (pCount > 0)
        {
            string up = picture.ToUpperInvariant();
            bool IsDigitPos(char c) => c is '9' or 'Z' or '*'
                || (c == currencyChar && !fixedCs) || (c == '+' && !fixedPlus) || (c == '-' && !fixedMinus)
                || FloatingChar1(c);
            int lastDigitPos = -1, digitPositions = 0;
            bool sawFloating = false;
            for (int i = 0; i < up.Length; i++)
                if (IsDigitPos(up[i]))
                {
                    lastDigitPos = i;
                    // the LEFTMOST occurrence of a floating string is the sign/currency itself, not a digit
                    if (up[i] is not ('9' or 'Z' or '*') && !sawFloating) { sawFloating = true; continue; }
                    digitPositions++;
                }
            if (lastDigitPos >= 0 && up.IndexOf('P', lastDigitPos) > lastDigitPos) return -pCount;
            return pCount + digitPositions;
        }
        int point = picture.IndexOf('V');
        if (point < 0) point = picture.IndexOf('.');
        if (point < 0) return 0;
        int n = 0;
        for (int i = point + 1; i < picture.Length; i++)
        {
            char p = char.ToUpperInvariant(picture[i]);
            if (p is '9' or 'Z' or '*') n++;
            else if (p == currencyChar && !fixedCs) n++;
            else if (p == '+' && !fixedPlus) n++;
            else if (p == '-' && !fixedMinus) n++;
            else if (FloatingChar1(picture[i])) n++;
            else if (p is 'C' or 'D') break;   // CR/DB
        }
        return n;
    }

    /// <summary>⭐ THE EDITED CHARACTER CATEGORIES' formatter — ISO §13.18.40.5 <b>Table 7</b> gives category
    /// ALPHANUMERIC-EDITED and category NATIONAL-EDITED the SAME single type of editing, "Simple insertion", and
    /// nothing else, so ONE renderer serves both: every insertion position supplies its character from the ONE
    /// set (<see cref="TrySimpleInsertion"/>) — rule 3, "the insertion character occupying the same character
    /// position in the edited item as the associated symbol occupies in character-string-1" — and every OTHER
    /// position is a data position, filled from <paramref name="source"/> left-to-right and space-padded when the
    /// source is exhausted (§14.6.8.4/§14.6.8.5 alignment).
    /// <para>⛔ THE DATA POSITIONS ARE THE COMPLEMENT OF THE INSERTION POSITIONS, not a symbol list, and that is
    /// what makes this one method rather than two. The alphanumeric-edited mask holds only A X 9 B 0 / and
    /// character-1 (GR7 + Table 10's 'A X' row) and the national-edited mask only N B 0 / and character-1 (GR10 +
    /// the 'N' row), so "not an insertion symbol" IS "a data position" in both, while a symbol list has to name
    /// 'N' in a second arm — the shape that left <c>PIC NNBNN</c> with no renderer at all (kb/Work PB492).</para>
    /// <para>⛔ <paramref name="edits"/> is the same <c>EditRule[]</c> channel the numeric-edited path takes, and
    /// it is not optional in practice: GR7 and GR10 both name character-1 as a constituent, so
    /// <c>PIC XXTXX EDITING "T" IS ":"</c> renders <c>AB:CD</c> and <c>PIC NNTNN EDITING "T" IS N":"</c> renders
    /// <c>AB:CD</c> in national characters, never the mask LETTER (kb/Work PB490 — this arm of the dispatch had no
    /// rules parameter at all and every emit site dropped <c>PicInfo.EditingRules</c>; the compiler reaches this
    /// method only through <c>RuntimeApi.EditFormatSimpleInsertion(value, pic)</c>, which renders the mask and the
    /// rules together from the one <c>PicInfo</c>). A FOR-phrase character-1 cannot occur here — §13.18.40.3 SR12
    /// limits it to numeric and numeric-edited items and SR12 b) to the symbols 9 . cs P V Z — so every rule
    /// reaching this method is simple insertion.</para>
    /// <para>The national arm needs no separate character repertoire: §13.18.40.4 GR2 puts the insertion
    /// characters in "the national character representation" when the item's usage is national, and under the
    /// D-N1 model one national position IS one UTF-16 <c>char</c>, so the space / zero / slant an edit inserts
    /// are the very characters this method writes.</para></summary>
    public static string FormatSimpleInsertion(string source, string picture, EditRule[]? edits = null)
    {
        // The common case — every symbol one character position wide — writes straight into a char[].
        if (!HasWideLiteral(edits))
        {
            var output = new char[picture.Length];
            int n = 0;
            for (int i = 0; i < picture.Length; i++)
                output[i] = TrySimpleInsertion(picture[i], edits, out char ins) ? ins
                    : n < source.Length ? source[n++] : ' ';
            return new string(output);
        }
        // A multi-character literal-1: §13.18.40.4 GR14 'es' — "If character-1 is a simple insertion symbol …
        // the size of literal-1 is counted in the size of the item" — so the position is literal-1 wide, at
        // EVERY occurrence, and `PIC XXTXX EDITING "T" IS "::"` is a SIX-character item rendering `AB::CD`
        // (kb/Work PB491; the size half is the lead appended to that note from the PB492 report).
        var sb = new System.Text.StringBuilder(picture.Length + 16);
        int si = 0;
        for (int i = 0; i < picture.Length; i++)
        {
            if (RuleFor(picture[i], edits) is { } e) { sb.Append(e.Pos); continue; }   // rule 3 is sign-independent
            if (TrySimpleInsertion(picture[i], edits, out char ins)) { sb.Append(ins); continue; }
            sb.Append(si < source.Length ? source[si++] : ' ');
        }
        return sb.ToString();
    }

    /// <summary>The rightmost suppressed position within a floating symbol's string — its own occurrences plus the
    /// SIMPLE INSERTION symbols the string absorbs (ISO §13.18.40.5 rule 6: "Any of the simple insertion editing
    /// symbols embedded in this string or to the immediate right of this string are part of the string"), read
    /// from the ONE set (<see cref="TrySimpleInsertion"/>). Rule 6 a) then lands "a single occurrence of the
    /// replacement character(s) … into the character position(s) immediately preceding whichever of the following
    /// is encountered first" — the first nonzero numeric character, the first character position for which no
    /// floating insertion editing is specified, or the decimal point position — which is exactly the rightmost
    /// position of the string that pass 2 left suppressed; "Any character positions preceding this (these)
    /// insertion character(s) will contain the space character", which pass 2 has already done.</summary>
    private static int FindFloatingPlacement(string pattern, char[] output, char floatChar, EditRule[]? edits)
    {
        char target = char.ToUpperInvariant(floatChar);
        int lastSuppressed = -1;
        bool inZone = false;
        for (int i = 0; i < pattern.Length; i++)
        {
            char p = char.ToUpperInvariant(pattern[i]);
            if (p == target)
            {
                inZone = true;
                if (output[i] == ' ') lastSuppressed = i;
                else break;
            }
            else if (inZone && output[i] == ' ' && TrySimpleInsertion(pattern[i], edits, out _)) lastSuppressed = i;
            else if (inZone) break;
        }
        return lastSuppressed;
    }
}
