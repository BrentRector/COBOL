// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Binding.Model;
using CobolNet.Runtime;

namespace CobolNet.Binding;

/// <summary>One DECODED PICTURE EDITING literal — its content AND the literal CLASS the source wrote it in.
/// <para>⛔ The class is carried, not dropped, because ISO §13.18.40.3 SR9 is a rule ABOUT it: "If USAGE IS
/// NATIONAL is specified for the subject of the entry or if character-string-1 contains the symbol 'N',
/// literal-1, literal-2, and literal-3 shall be national literals. Otherwise, literal1, literal-2, and literal-3
/// shall be alphanumeric literals." Only the decoded TEXT used to reach the analyzer, so neither half of that
/// sentence could be asked and both were unenforced — `PIC NNTNN EDITING "T" IS ":"` and
/// `PIC XXTXX EDITING "T" IS N":"` both bound silently (kb/Work PB492).</para>
/// <para>A literal position admits more than a written literal (kb/Work PB778): a constant-name (§13.10.3 SR2)
/// and a symbolic-character arrive here AS the literal they stand for, and a keyword figurative constant
/// (§8.3.3.6.3 SR1) arrives as <see cref="EditLiteralClass.Figurative"/> — ONE character (§8.3.3.6.4 GR3b)
/// whose class is the context's (GR1), carried in both representations because only the analyzer knows
/// whether the subject is national.</para></summary>
/// <param name="Text">The literal's decoded content (quotes stripped, doubled quotes folded, hex decoded); for
/// a figurative, its ALPHANUMERIC character; for <see cref="EditLiteralClass.NotAlphanumericOrNational"/>, the
/// operand as written.</param>
/// <param name="Class">The literal class the operand was written in.</param>
/// <param name="NationalText">A figurative's NATIONAL character (HIGH-/LOW-VALUE read the national collating
/// sequence); null for every other class.</param>
public readonly record struct EditLiteral(string Text, EditLiteralClass Class, string? NationalText = null)
{
    /// <summary>The literal as the subject uses it: a figurative takes the subject's class (§8.3.3.6.4 GR1).</summary>
    public EditLiteral InContext(bool nationalSubject) =>
        Class is EditLiteralClass.Figurative
            ? new EditLiteral(nationalSubject ? NationalText ?? Text : Text,
                nationalSubject ? EditLiteralClass.National : EditLiteralClass.Alphanumeric)
            : this;
}

/// <summary>The class of a PICTURE EDITING literal operand, as §13.18.40.3 SR9 asks it.</summary>
public enum EditLiteralClass
{
    /// <summary>An alphanumeric literal (incl. the X"…" hexadecimal format, §8.3.3.2).</summary>
    Alphanumeric,
    /// <summary>A national literal (N"…" / NX"…").</summary>
    National,
    /// <summary>A keyword figurative constant — class from context (§8.3.3.6.4 GR1).</summary>
    Figurative,
    /// <summary>Any other literal — numeric, boolean, or the NULL figurative: never a literal SR9 admits.</summary>
    NotAlphanumericOrNational,
}

/// <summary>A parsed PICTURE EDITING phrase (ISO §13.18.40.2 Format 1, COBOL-2023) handed to
/// <see cref="PictureAnalyzer.Analyze"/>: the DECODED editing character-1 text and its DECODED literal(s).
/// <see cref="IsForForm"/> distinguishes the sign-control FOR form (<see cref="Neg"/>/<see cref="Pos"/>, either
/// side null when unspecified per SR12c) from the simple-insertion IS form (<see cref="Simple"/>).</summary>
public sealed record EditingPhraseSpec(string Char1Text, EditLiteral? Simple, EditLiteral? Neg, EditLiteral? Pos,
    bool IsForForm);

/// <summary>
/// The PICTURE character-string scanner and the USAGE-keyword mapper (ISO/IEC 1989:2023 §13.18.40 / §13.18.60) —
/// the ANALYSIS half split off <see cref="PicInfo"/> (P5.11c, DESIGN-data-model §2.7): <see cref="PicInfo"/> is
/// now a pure value record (the analyzed FACTS + the representation projections); the scanning, the SR2 symbol
/// whitelist, the category classification, and every entry-time diagnostic live here.
/// </summary>
public static class PictureAnalyzer
{
    /// <summary>
    /// Analyze a PICTURE string (already stripped of the <c>PIC</c> keyword) plus an optional usage keyword and the
    /// entry's own SIGN clause (<see langword="null"/> when the entry has none — a group-level SIGN may still apply,
    /// via the binder's post-build inheritance pass, ISO §13.18.52 GR1–3). <paramref name="currencies"/> is the
    /// unit's CURRENCY SIGN SET (ISO §12.3.7 — symbol → currency string; <c>DataBinder.CurrencySigns</c>, the
    /// r25-implied <c>'$' → "$"</c> included), of which a PICTURE uses at most ONE symbol (§13.18.40.3 r24/r28):
    /// a non-default symbol (e.g. NC107A's <c>W</c>, NC108M's <c>&lt;</c>) classifies exactly like <c>$</c> — its
    /// mask positions are fixed/floating currency insertion, making the item NUMERIC-EDITED (§13.18.40.4) — and
    /// the emitted <see cref="PicInfo.EditMask"/> carries it CANONICALIZED to <c>$</c> with the string it stands
    /// for on <see cref="PicInfo.CurrencyString"/>; a multi-character string widens the item by its extra length
    /// (§13.18.40.4 GR14 — "the first occurrence of the currency symbol adds the number of characters in the
    /// currency string to the size of the item"; kb/Work PB60 / AR-15.68.3-3). <paramref name="currency"/> is the
    /// legacy single-symbol form (tests / report items) — ignored when <paramref name="currencies"/> is given.
    /// <paramref name="edition"/>
    /// + <paramref name="where"/> carry the W2 loud-guard diagnostics: the symbol whitelist (§13.18.40.3 SR2) —
    /// a symbol outside the legal set is a COBOLNET0808 error, and the legal-but-unimplemented 2002+ symbols
    /// <c>N</c>/<c>1</c>/<c>E</c> route their introduction gates + a not-implemented error (never the historical
    /// silent fall-through to "pure numeric, zero digits"). Analyze sees the RAW picture — DECIMAL-POINT IS COMMA
    /// (ISO §13.18.40.3 SR13) swaps the ROLES of <c>,</c> and <c>.</c> at edit time (<c>CobolEdit.MaskScale</c>'s
    /// flag), not the symbols themselves, and both are whitelisted regardless; <paramref name="decimalPointIsComma"/>
    /// carries that same switch into the COMPOSITION validator (<see cref="PictureComposition"/>), where SR13's
    /// "the rules for the symbol period apply to the symbol comma, and the rules for the symbol comma apply to the
    /// symbol period" decides which of the two is the DECIMAL separator that SR12b / SR17 / SR20 and Table 10 speak
    /// of — without it `PIC 9.999.999,99` under DECIMAL-POINT IS COMMA (NIST NC107A) is rejected as legal source.
    /// <para>Every analyzed (non-recovery) profile carries <see cref="PicInfo.Clause"/> — the PICTURE clause's
    /// identity, attached HERE for every category arm at once because this is the one place the character-string,
    /// the unit's CURRENCY SIGN set and its DECIMAL-POINT IS COMMA state are all in hand (kb/Work PB1166).</para>
    /// </summary>
    public static PicInfo Analyze(string picture, Usage usage, EditionContext edition, string where,
        SignSpec? sign = null, char currency = '$', bool blankWhenZero = false, bool explicitUsage = false,
        IReadOnlyList<EditingPhraseSpec>? editing = null, IReadOnlyDictionary<char, CurrencyDefinition>? currencies = null,
        LocaleEditSpec? localeFormat2 = null, bool decimalPointIsComma = false)
    {
        var pic = AnalyzeCharacterString(picture, usage, edition, where, sign, currency, blankWhenZero, explicitUsage,
            editing, currencies, localeFormat2, decimalPointIsComma, out var clause);
        return pic.IsRecovery || clause is null ? pic : pic with { Clause = clause };
    }

    /// <summary>The body of <see cref="Analyze"/>; <paramref name="clause"/> is the PICTURE clause identity, set
    /// once the character-string is expanded and its currency symbol known, and completed with the EDITING
    /// phrase rules once those are resolved.</summary>
    private static PicInfo AnalyzeCharacterString(string picture, Usage usage, EditionContext edition, string where,
        SignSpec? sign, char currency, bool blankWhenZero, bool explicitUsage,
        IReadOnlyList<EditingPhraseSpec>? editing, IReadOnlyDictionary<char, CurrencyDefinition>? currencies,
        LocaleEditSpec? localeFormat2, bool decimalPointIsComma, out PictureClauseIdentity? clause)
    {
        clause = null;
        // A TRAILING ';' is the clause SEPARATOR (ISO §8.3.5 rule 2 — a semicolon immediately followed by a
        // space is a separator; ';' is never a PICTURE symbol). The REAL cure is the W3 lexer-mode trim
        // (DEVLOG 596; VCR Table 7 row 7.14): PIC_STRING trims a trailing ','/';' when LA(1) is whitespace —
        // the separator shape — so a legal SR7 trailing-',' mask (NC125A's `…9,.`) keeps its comma. This
        // single-';'-strip stays as DEFENSE-IN-DEPTH for the funnel's other callers; `PIC 99;;` and a bare
        // `PIC ;` remain invalid and fall to the 0808 whitelist below (the adversarial-review fix for the
        // strip-to-empty leak).
        if (picture.EndsWith(';')) picture = picture[..^1];
        if (picture.Length == 0)
        {
            edition.Error("COBOLNET0808", $"invalid PICTURE character-string — {where} "
                + "(ISO §13.18.40.3 SR2: empty after separating the trailing ';' separator)");
            return PicInfo.Recovery();
        }

        // ── ISO §13.18.40.3 SR4 ── "The maximum number of characters allowed in character-string-1 is 63."
        // The count is over character-string-1 AS WRITTEN — SR6's second sentence ("the length of the integer,
        // not the length of the constant-name, is counted toward the maximum number of characters in
        // character-string-1") fixes that reading, and it is already the form that reaches here:
        // DataBinder.Constants.ExpandPicConstants has rewritten every `(constant-name)` to `(integer)` in the
        // SOURCE string before Analyze sees it. Measured on the WRITTEN string, not the expansion, because the
        // expanded reading would outlaw `PIC X(30000)` — four characters long (kb/Work PB532). A space is not a
        // picture symbol (SR2), so the spaces ExpandRepeats skips are not characters OF character-string-1.
        int written = picture.Count(c => c != ' ');
        if (written > MaxPictureStringLength)
        {
            edition.Error(DiagnosticCatalog.PictureStringTooLong, $"{where}: character-string-1 is {written} "
                + $"characters long — the maximum is {MaxPictureStringLength} (ISO §13.18.40.3 SR4; the count is "
                + "over character-string-1 as WRITTEN, so a repetition factor counts as its own digits, not as "
                + "the symbols it expands to)");
            return PicInfo.Recovery();
        }

        // Expand (n) repetition into a flat symbol run, e.g. "X(4)" → "XXXX", "9(3)V99" → "999V99". Every
        // repetition factor is VALIDATED here (§13.18.40.3 SR6) and the expansion is bounded, so no unchecked
        // count reaches StringBuilder.Append (kb/Work PB531).
        if (!TryExpandRepeats(picture, edition, where, out string expanded)) return PicInfo.Recovery();

        // The picture's currency symbol — the ONE member of the unit's CURRENCY SIGN SET it uses (§13.18.40.3
        // r24 fixed / r28 floating: a picture carries one currency symbol kind), or the legacy single symbol.
        // Two DIFFERENT set members in one picture are an invalid PICTURE (0808). The mask is canonicalized to
        // '$' below and the symbol's STRING travels on PicInfo.CurrencyString.
        IReadOnlyDictionary<char, CurrencyDefinition> currencySet = currencies
            ?? new Dictionary<char, CurrencyDefinition>
            { [char.ToUpperInvariant(currency)] = new(currency.ToString(), LiteralClass.Alphanumeric) };
        char cs = '\0';
        foreach (char raw in expanded)
        {
            char up = char.ToUpperInvariant(raw);
            if (!currencySet.ContainsKey(up)) continue;
            if (cs == '\0') { cs = up; continue; }
            if (cs == up) continue;
            edition.Error("COBOLNET0808", $"invalid PICTURE character-string {picture} — {where}: it uses two "
                + $"different currency symbols ('{cs}' and '{up}'); a PICTURE character-string may contain one "
                + "currency symbol kind (ISO §13.18.40.3 r24 / r28)");
            return PicInfo.Recovery();
        }
        string currencyString = cs == '\0' ? "$" : currencySet[cs].Text;
        // §12.3.7.3 SR28's input — the class of the literal-7 behind the symbol THIS picture uses; null when it uses
        // none (the default below is only what the whitelist/editing checks see, not a symbol the picture wrote).
        LiteralClass? currencyClass = cs == '\0' ? null : currencySet[cs].LiteralClass;
        if (cs == '\0') cs = '$';   // no currency symbol in this picture: the whitelist/editing checks see the default
        // GR14: the first occurrence contributes the whole string; the mask below is canonical ('$').
        int currencyExtra = expanded.Any(c => char.ToUpperInvariant(c) == cs) ? currencyString.Length - 1 : 0;
        clause = PictureClauseIdentity.Of(expanded, cs, currencyString, decimalPointIsComma);

        // ── PICTURE format 2 (the LOCALE phrase, §13.18.40.2; kb/Work PB64 T6): its own analysis — the format-1
        // walker below reads none of its rules (Table 11 replaces Table 10; the item's size is the SIZE integer,
        // not the mask's width; the emitted separators/currency/sign are the locale's). The currency-symbol
        // classification above still applies: the picture's cs is the program's currency symbol (§12.3.7.3 SR22
        // — a CURRENCY SIGN clause's symbol classifies exactly like '$'), canonicalized to '$' below.
        if (localeFormat2 is { } locale2)
            return AnalyzeLocaleEdited(picture, expanded, cs, usage, explicitUsage, edition, where, blankWhenZero, locale2,
                hasEditingPhrase: editing is { Count: > 0 });

        // ── PICTURE EDITING phrases (ISO §13.18.40.2 Format 1, COBOL-2023): validate SR8–SR12 and build the
        // render rules. char1Set lets the SR2 whitelist admit the declared editing characters (else char-1
        // letters like 'L'/'T'/'G' would trip COBOLNET0808); char1Extended is the FOR-phrase subset, which
        // §13.18.40.5 rule 6 makes FLOATING insertion symbols. The introduction gate below 2023 is fired by
        // VersionConformancePass.ParseArm.VisitPictureClause.
        var editRules = ValidateEditing(editing, expanded, usage, edition, where, cs, decimalPointIsComma,
            out var char1Set, out var char1Extended, out var char1Declared);
        // §13.18.40.4 GR14 'es' — the character-1 positions' size beyond the one position each occupies in
        // character-string-1 (kb/Work PB491). Computed once here, beside the currency widening, and added at
        // every category arm a character-1 can reach.
        int editingExtra = EditingPositions(expanded, editRules);
        clause = clause with { Editing = PictureClauseIdentity.EditingText(editRules) };

        // ── The §13.18.40.3 SR2 symbol whitelist (the W2 loud guard). The legal ISO 2023 Format-1 symbols are
        // A B E N P S V X Z 0 1 9 / , . + - * CR DB and the program's currency symbol (§13.18.40.4 GR14;
        // ExpandRepeats has already uppercased, so the §8.1.3 GR3 case equivalence is folded). 'N' (national,
        // §8.5.2.10), '1' (boolean, §8.5.2.5) and 'E' (external float, §13.18.40.4 GR13b) are LEGAL 2002+
        // symbols with no implementation yet: each fires its ConstructRegistry introduction gate (0900 below
        // 2002) plus the not-implemented error at 2002+. Anything else is an invalid PICTURE. ──
        bool hasN = false, has1 = false, hasE = false;
        char? invalid = null;
        for (int i = 0; i < expanded.Length; i++)
        {
            char c = expanded[i];
            if (char1Declared.Contains(c)) continue;   // a declared PICTURE EDITING character-1 (ISO §13.18.40.3 SR8) — not an invalid symbol
            switch (c)
            {
                // '$' is a currency picture symbol iff it is a member of the unit's CURRENCY SIGN SET —
                // §12.3.7.3 r25 IMPLIES `CURRENCY SIGN '$' PICTURE SYMBOL '$'` unless a clause names '$' as
                // literal-7 or literal-8, so `PIC $$,$$9.99` stays legal beside a declared '#' (kb/Work PB60 /
                // AR-15.68.3-3 measured it as COBOLNET0808 under the former single-symbol model — a legal-source
                // rejection); only a clause that TAKES '$' for another string retires it as a symbol.
                case '$' when currencySet.ContainsKey('$'):
                    break;
                case 'A' or 'B' or 'P' or 'S' or 'V' or 'X' or 'Z'
                    or '0' or '9' or '/' or ',' or '.' or '+' or '-' or '*':
                    break;
                case 'C' when i + 1 < expanded.Length && expanded[i + 1] == 'R':
                case 'D' when i + 1 < expanded.Length && expanded[i + 1] == 'B':
                    i++; break;   // CR / DB — each two-character pair is ONE symbol (§13.18.40.3 SR12 NOTE 2)
                case 'N': hasN = true; break;
                case '1': has1 = true; break;
                case 'E': hasE = true; break;
                default:
                    if (c == cs) break;   // the program's currency symbol (ISO §12.3.7 GR13)
                    invalid ??= c;        // 'C'/'D' not opening CR/DB land here too — no legal lone use
                    break;
            }
        }
        // ── The FLOATING-POINT numeric-edited form (§13.18.40.4 GR13 b — data-model design D21, kb/Work PB66): the
        //    whole string is validated as significand 'E' exponent and returned as a numeric-edited PicInfo whose
        //    IsFloatEdited flag drives every store / read dispatch. The COBOL-2002 introduction gate keys on that flag
        //    in VersionConformancePass.UsageConstructId (no SkeletonGate: the category is no longer recovered). ──
        if (hasE && invalid is null && !hasN && !has1)
            return AnalyzeFloatEdited(picture, expanded, usage, explicitUsage, edition, where, editRules, char1Extended, editingExtra);
        if (invalid is { } bad)
        {
            // Wording is exact about what IS checked HERE: symbol MEMBERSHIP in the SR2 inventory. SR2's
            // OTHER obligation — the §13.18.40.6 allowable COMBINATION, i.e. symbol order and multiplicity
            // (`PIC 99.99.99`, `PIC 9ZZ`, `PIC N9`, `PIC NE`) — is PictureComposition, just below, and reports
            // COBOLNET1934 / COBOLNET1935 (kb/Work PB528; data-model design D24).
            edition.Error("COBOLNET0808", $"invalid PICTURE symbol '{bad}' in PICTURE {picture} — {where} "
                + "(ISO §13.18.40.3 SR2: not an allowable picture symbol)");
            // Recovery representation ONLY: the compile has already FAILED above — this shape merely keeps the
            // doomed emit pass crash-free (CompilerDriver reports bind diagnostics after Emit completes).
            return PicInfo.Recovery(expanded.Length);
        }

        // ── The §13.18.40.3 COMPOSITION rules + the §13.18.40.6 Table 10 precedence (kb/Work PB528; data-model
        // design D24). SR2 has TWO obligations and the loop above answered only the first: the symbols shall be
        // picture symbols, AND they shall form "an allowable combination", whose allowable combinations "are
        // specified in 13.18.40.6, Precedence rules". PictureComposition is that second half plus every
        // composition syntax rule of the clause; a violation is COBOLNET1934 / COBOLNET1935 and the item recovers
        // (the compile has already failed; the shape only keeps the doomed emit crash-free). It runs BEFORE the
        // geometry below because that derivation reads the symbol MULTISET and presumes a well-formed string —
        // e.g. the scale, the digit-position count and the floating-string detector all assume the P run, the
        // decimal point and the floating symbol are where the standard requires them.
        // ⛔ IT ALSO RUNS BEFORE THE CATEGORY ARMS, and that is the whole Table-10 question asked ONCE
        // (kb/Work PB492). The national and boolean arms each used to carry a hand-written copy of a Table-10
        // row — "'N' may be combined only with the insertion symbols B 0 /" — which the 24×24 matrix already
        // answers (row N admits only the 'B 0 /' and 'N' columns; row '1' only the '1' column; and only those
        // same rows admit their columns). Both copies were WRONG in the same way the copies PB490 found were:
        // they spelled a SET as a literal list and left out the declared EDITING character-1, so GR10's
        // character-1 leg — `PIC NNTNN EDITING "T" IS N":"` — was refused as an invalid PICTURE. The matrix is
        // transparent to character-1 by design (its own note says why), so routing the question here is what
        // makes the legal shape legal. A THIRD Table-10 copy, `hasN && has1`, was also deleted: N-with-1 is the
        // blank cell at (row N, column 1) and the walk names the offending pair better than the hand-written
        // message did — AND its guard was hiding a SILENT REJECTION, because an E-bearing picture that also
        // held N or '1' (`PIC NE`, `PIC 1E`) entered that block, matched neither message and returned Recovery
        // with NO diagnostic at all: `01 W PIC NE.` compiled clean as a 2-character item.
        if (!PictureComposition.Validate(picture, expanded, cs, char1Set, char1Extended, blankWhenZero, decimalPointIsComma,
                edition, where))
            return PicInfo.Recovery(expanded.Length);

        // ── Category national (§8.5.2.10) / national-edited (§8.5.2.11) / boolean (§8.5.2.5) — LIVE. The
        // introduction gate stays at every entry point (COBOLNET0900 below 2002; the registry rows are silent at
        // 2002+), exactly the BINARY-CHAR/POINTER pattern. Usage resolution per §13.18.60.4: SR13a — PIC N with
        // no USAGE clause implies NATIONAL; SR20 — PIC N admits ONLY usage NATIONAL; SR13b — PIC 1 with no usage
        // is DISPLAY; SR5 — usage BIT requires a boolean picture; SR12 national-form boolean (PIC 1 USAGE
        // NATIONAL) is spec-legal but STAGED (0899). ──
        if (hasN)
        {
            // §13.18.40.4 GR9 — "To define an item as national, character-string-1 shall contain only one or more
            // occurrences of the symbol 'N'" — and GR10 — "To define an item as national-edited, character-string-1
            // shall include — at least one symbol 'N', and — at least one instance of character-1 or one of the
            // symbols from the set 'B', '0', '/'". The GR10 insertion set is CobolEdit's ONE definition of it, the
            // same predicate the alphanumeric-edited arm below reads for the WORD-FOR-WORD IDENTICAL set GR7 names
            // (kb/Work PB492): two arms spelling one set out is how this one lost character-1.
            // NationalData2002 / NationalEdited2002 (the introduction gates) fire on the RESOLVED item in the
            // VersionConformancePass GateData/GateReports enumerator (Step 14g.1 / 14g.5).
            bool nationalEdited = expanded.Any(c => CobolEdit.IsEditedCategorySymbol(c, char1Set));
            if (expanded.All(c => c is 'N' || CobolEdit.IsEditedCategorySymbol(c, char1Set)))
            {
                ScreenUsageAgainstPicture(PicCategory.National, usage, explicitUsage, picture, edition, where);
                // GR4 through the ONE count (kb/Work PB535): every symbol of a national or national-edited
                // character-string is COUNTED in the size — each 'N' a national character position (GR1), each
                // 'B'/'0'/'/' an insertion position, and character-1 "the size of literal-1" (GR14's 'es'
                // entry), which the landable IS form fixes at one. None of 'P'/'S'/'V' may stand beside an 'N'
                // (Table 10), so the exclusion takes nothing away here — that agreement is the point of asking
                // ONE function rather than writing `expanded.Length` as a second reading of the same rule.
                return new PicInfo(PicCategory.National, Usage.National,
                    Length: CharacterPositions(expanded, editingExtra: editingExtra), Digits: 0, Scale: 0, Signed: false)
                { EditMask = nationalEdited ? expanded : null, EditingRules = nationalEdited ? editRules : null };
            }
            // Unreachable while Table 10 and GR9/GR10 agree — the matrix admits nothing else beside an 'N'. It
            // stands as the loud assertion of that agreement: a picture that reaches here holds an 'N' and some
            // symbol no category-defining rule admits, so it defines NO category and must never fall through to
            // the numeric geometry below.
            edition.Error("COBOLNET0808", $"invalid PICTURE {picture} — {where} (ISO §13.18.40.4 GR9/GR10: a "
                + "character-string containing the symbol 'N' defines an item only when it is all 'N' (national) "
                + "or 'N' with the insertion symbols character-1 / B 0 / (national-edited))");
            return PicInfo.Recovery(expanded.Length);
        }
        if (has1)
        {
            if (expanded.All(c => c is '1'))
            {
                // BooleanData2002 (the introduction gate) fires on the RESOLVED item in the VersionConformancePass
                // GateData enumerator (keyed on Pic.Category Boolean); Step 14g.1.
                usage = ScreenUsageAgainstPicture(PicCategory.Boolean, usage, explicitUsage, picture, edition, where);
                // GR4 through the ONE count: GR14's '1' entry — "Each symbol '1' represents a boolean position
                // … Each symbol '1' is counted in the size of the item" — is the "boolean positions" half of
                // GR4's own sentence.
                return new PicInfo(PicCategory.Boolean, usage,
                    Length: CharacterPositions(expanded), Digits: 0, Scale: 0, Signed: false);
            }
            // REACHABLE, and the citation is the one that governs: §13.18.40.4 GR8 admits ONLY the symbol '1',
            // while Table 10 is transparent to a declared EDITING character-1 — so `PIC 11T EDITING "T" IS ":"`
            // passes the matrix and still defines no category. There is no boolean-edited category (Table 7 gives
            // category boolean "None").
            edition.Error("COBOLNET0808", $"invalid PICTURE {picture} — {where} (ISO §13.18.40.4 GR8: to define "
                + "an item as boolean, character-string-1 shall contain only occurrences of the symbol '1'; "
                + "§13.18.40.5 Table 7 gives category boolean no editing)");
            return PicInfo.Recovery(expanded.Length);
        }

        bool signed = expanded.Contains('S');
        bool hasV = expanded.Contains('V');
        int digits = expanded.Count(c => c is '9');
        int afterV = hasV ? expanded[(expanded.IndexOf('V') + 1)..].Count(c => c is '9') : 0;

        // PICTURE 'P' scaling positions (ISO §13.18.40): each P holds no digit and no storage but shifts the implied
        // decimal point. TRAILING P (e.g. 99P) scales the stored digits UP → a NEGATIVE fraction scale (the value is
        // a multiple of 10^P). LEADING P (e.g. P(4)9) puts the point left of every digit → scale = leadingP + the
        // digit count (all 9s are fractional). The net SIGNED scale flows through the whole numeric pipeline; the
        // runtime Rescale handles a negative scale natively (Pow10 of the always-non-negative scale difference).
        // P positions classify against the DIGIT POSITIONS — 9, the suppression symbols Z/* (an EDITED
        // P-scaled mask like ZZZPP has no '9' at all, NC124A PICTURE-TEST-30), AND a FLOATING string's
        // occurrences (kb/Work PB155: `PIC $$$$PP` has no 9/Z/* at all, so its rightmost P run classified as
        // LEADING and the scale came out +2 where the value is a multiple of 10^2, scale −2; §13.18.40.6
        // Table 10 places left-of-point P beside the floating symbols). The floating symbol is determined
        // FIRST so the anchors can include it; §13.18.40.5 allows at most one floating string per picture.
        // ⛔ ONE floating-string detector, shared with the composition validator (kb/Work PB528). §13.18.40.5
        // rule 6 defines a floating string as "a string of at least two IDENTICAL floating insertion editing
        // symbols" with the simple insertion symbols (and, by rule 6 b, the decimal point) embedded — an
        // ADJACENCY property, never a bare occurrence count. The count this once read made `PIC +999+` and
        // `PIC $999$` look like floating strings and silently answered a question the source never asked; both
        // are now SR24 errors, and the two readings can no longer disagree because there is only one.
        var (floatChar, floatOcc) = PictureComposition.FloatingString(expanded, cs, char1Set, char1Extended,
            decimalPointIsComma ? ',' : '.', decimalPointIsComma ? '.' : ',');
        int floatingExtra = floatOcc >= 2 ? floatOcc - 1 : 0;
        bool IsDigitAnchor(char c) => c is '9' or 'Z' or '*' || (floatChar != '\0' && c == floatChar);
        int firstNine = -1, lastNine = -1;
        for (int i = 0; i < expanded.Length; i++)
            if (IsDigitAnchor(expanded[i])) { if (firstNine < 0) firstNine = i; lastNine = i; }
        int leadingP = 0, trailingP = 0;
        for (int i = 0; i < expanded.Length; i++)
            if (expanded[i] == 'P') { if (firstNine < 0 || i < firstNine) leadingP++; else if (i > lastNine) trailingP++; }
        int digitPositions = expanded.Count(c => c is '9' or 'Z' or '*');
        int scale = trailingP > 0 ? -trailingP : leadingP > 0 ? leadingP + digitPositions + floatingExtra : afterV;

        // §13.18.40.3 SR14 DIGIT-POSITION count for the 1–31 capacity cap: the 9/Z/* positions, every P (counted in the
        // maximum digit positions though it stores no digit, §13.18.40.4), and the floating '+'/'-'/currency digit
        // positions — a floating string of a symbol appearing k≥2 times contributes k−1 (the leftmost is the
        // sign/currency, not a digit). Counting SYMBOL occurrences (not run length) naturally excludes embedded simple
        // insertions ($$,$$9 → 4 '$' → 3 digit positions). For a pure-numeric picture (no Z/*, no floating) this equals
        // Digits + P. (CA33 — the cap must NOT undercount to only the '9's, which let Z(35)/Z(11)9(8) slip past.)
        int digitPos = digitPositions + leadingP + trailingP + floatingExtra;   // floatingExtra hoisted above the P split

        bool anyAlpha = expanded.Any(c => c is 'X' or 'A');
        // CR / DB are fixed-insertion editing symbols too (ISO §13.18.40.4) — `PIC 9(5)CR` is NUMERIC-EDITED
        // (NC104A MOVE-TEST-F1-14), not pure numeric with stray letters. The program's currency symbol (ISO
        // §12.3.7 GR13) is an editing symbol exactly like '$' — without it a `PIC WWWWW` would fall through to
        // "pure numeric, zero digits".
        bool anyEdit = expanded.Any(c => c is 'Z' or '*' or '+' or '-' or ',' or '.' or '$' or 'B' or '0' or '/' || c == cs)
            || expanded.Contains("CR", StringComparison.Ordinal) || expanded.Contains("DB", StringComparison.Ordinal)
            || char1Set.Count > 0;   // a PICTURE EDITING character-1 makes the item numeric-/alphanumeric-edited (ISO §13.18.40.5 Table 7)

        // ── THE §13.18.60.3 USAGE × PICTURE SCREEN over the picture's resolved CATEGORY (SR3 / SR5 / SR12) —
        // the SAME call the national and boolean early returns above make, and the same call the §13.18.60.4 GR1
        // inheritance pass makes for a usage an elementary item acquires from its group. The category-national
        // and category-boolean pictures never reach here (both returned above), so what remains is
        // alphabetic/alphanumeric (anyAlpha), numeric-edited (anyEdit) and pure numeric.
        usage = ScreenUsageAgainstPicture(
            anyAlpha ? PicCategory.Alphanumeric : anyEdit ? PicCategory.NumericEdited : PicCategory.Numeric,
            usage, explicitUsage, picture, edition, where, currencyClass);

        if (anyAlpha)
        {
            // ALPHANUMERIC-EDITED (ISO §13.18.40.4 GR7 — "at least one symbol 'A' or one symbol 'X', and at least
            // one instance of character-1 or one of the symbols from the set 'B', '0', '/'"): every position counts
            // in the length, and the mask drives MOVE editing. A plain alphanumeric has no insertion symbols and no
            // editing character-1. ⛔ The insertion SET is CobolEdit's one definition of it — the SAME predicate
            // the national arm above reads, because GR7 and GR10 name the same set word for word (kb/Work PB492).
            bool edited = expanded.Any(c => CobolEdit.IsEditedCategorySymbol(c, char1Set));
            // GR5 / GR6 / GR7's ANTECEDENT, stated at the arm that answers it — the same loud assertion the
            // national (GR9/GR10) and boolean (GR8) arms above carry, and for the same reason (kb/Work PB535).
            // GR6 admits "a combination of symbols from the set 'A', 'X', and '9'" and GR7 adds character-1 and
            // 'B'/'0'/'/' to it; GR5's alphabetic is the all-'A' case of the same alphabet. Unreachable while
            // Table 10 and GR5–GR7 agree — the matrix admits nothing else beside an 'A' or an 'X' — but the
            // predicate that GETS here is "contains an 'A' or an 'X'", which is not GR6's antecedent, and the
            // gap between the two is exactly what bound `PIC XX,XX`, `PIC AAZZ`, `PIC XXCR` and `PIC ZZ9AA` as
            // silently SHORT alphanumeric items for as long as no rule read symbol COMBINATION.
            if (!expanded.All(c => c is 'A' or 'X' or '9' || CobolEdit.IsEditedCategorySymbol(c, char1Set)))
            {
                edition.Error("COBOLNET0808", $"invalid PICTURE {picture} — {where} (ISO §13.18.40.4 GR6/GR7: a "
                    + "character-string containing the symbol 'A' or 'X' defines an item only when its symbols "
                    + "come from the set 'A', 'X', '9' (alphanumeric), optionally with character-1 or the "
                    + "insertion symbols B 0 / (alphanumeric-edited))");
                // A recovery width is NOT a GR4 size — the compile has already failed and this shape only keeps
                // the doomed emit crash-free — so it stays the neighbours' plain symbol count.
                return PicInfo.Recovery(expanded.Length);
            }
            return new PicInfo(PicCategory.Alphanumeric, usage,
                // GR4 through the ONE count (kb/Work PB535): this arm used to spell GR4 as a WHITELIST of the
                // symbols it expected, which answered every other symbol by dropping it from the size.
                Length: CharacterPositions(expanded, editingExtra: editingExtra),
                Digits: 0, Scale: 0, Signed: false)
            { EditMask = edited ? expanded : null, IsAlphabetic = expanded.All(c => c is 'A'),
              EditingRules = editRules };
        }

        string signKind = PicInfo.SignKindFor(usage, signed, sign);

        // BLANK WHEN ZERO on a category-numeric picture DEFINES the item as numeric-edited (ISO §13.18.8 GR2;
        // SR1 admits it only without 'S', SR2 only usage display/national) — NC108M's `PIC 9(9) BLANK ZERO`
        // holds SPACES after a zero store and compares as an alphanumeric item.
        // ⛔ BOTH USAGES SR2 NAMES, not just display (kb/Work PB646). GR2 is written about the PICTURE
        // CHARACTER-STRING ("If the subject of the entry is described by its picture character-string as
        // category numeric…") and SR2 names the usages the clause may be written with — "display or usage
        // national" — so the display-only arm silently DROPPED the clause on `PIC 9(3) USAGE NATIONAL BLANK
        // WHEN ZERO`: measured storing 000 where GR1 requires spaces, once SR12's staging stopped hiding the
        // shape. (SR2's own screen — refusing the clause on any OTHER usage — is missing for every usage
        // alike and is not this arm's; see the report's new-defect paragraph.)
        if (anyEdit || (blankWhenZero && digits > 0 && !signed && usage is Usage.Display or Usage.National))
            // Numeric-edited: the .NET storage is the formatted display image (string); width = edited symbol
            // count. NOTE no digits>0 requirement — an all-symbol mask (PIC ****, $$$$) is numeric-edited too,
            // its digit positions being the Z/*/floating symbols themselves (§13.18.40).
            return new PicInfo(PicCategory.NumericEdited, usage,
                Length: CharacterPositions(expanded, currencyExtra, editingExtra), Digits: digits, Scale: scale, Signed: signed)
            { SignKind = signKind, EditMask = CanonicalCurrencyMask(expanded, cs), EditingRules = editRules, DigitPositions = digitPos,
              CurrencyString = currencyString == "$" ? null : currencyString, CurrencyClass = currencyClass };

        // ⛔ GR11's ANTECEDENT, at the arm that answers it (kb/Work PB535). This arm is the FALL-THROUGH — it is
        // reached by exhaustion, by everything that is not national, boolean, alphabetic/alphanumeric or edited —
        // and for as long as no rule read symbol COMBINATION it had no precondition at all, so `PIC S`,
        // `PIC PPP`, `PIC SV` and `PIC SPPP` each bound a ZERO-LENGTH category-numeric item at every edition,
        // silently. §13.18.40.4 GR11: "To define an item as fixed-point numeric, character-string-1 — shall
        // include at least one symbol '9', and — may contain a combination of symbols from the set 'P', 'S',
        // and 'V'." §13.18.40.3 SR12 a says the same thing from the other side and PictureComposition now
        // rejects all four above, so this is the loud assertion of that agreement rather than a second gate —
        // the same shape the national and boolean arms carry. A character-string that defines NO category must
        // never reach a PicInfo.
        if (digits == 0)
        {
            edition.Error("COBOLNET0808", $"invalid PICTURE {picture} — {where} (ISO §13.18.40.4 GR11: to "
                + "define an item as fixed-point numeric, character-string-1 shall include at least one symbol "
                + "'9'; a character-string of 'S', 'V' and 'P' alone defines none of GR3's eight categories)");
            return PicInfo.Recovery(expanded.Length);
        }

        // Pure numeric. The stored-digit count (Digits) and DISPLAY width (Length) are the '9' count — which is
        // GR4's count too, GR14 excluding exactly the 'S', 'V' and 'P' that are all this arm's alphabet holds
        // beside the '9's. P holds no storage; the implied decimal position lives entirely in the signed Scale.
        return new PicInfo(PicCategory.Numeric, usage, Length: CharacterPositions(expanded), Digits: digits, Scale: scale, Signed: signed)
        { SignKind = signKind, DigitPositions = digitPos };
    }

    /// <summary>
    /// ⛔ THE §13.18.60.3 USAGE × PICTURE SCREEN — SR3, SR5, SR12 and SR20 — asked ONCE, of an elementary item's
    /// EFFECTIVE usage, for BOTH ways the standard lets an item acquire one:
    /// <list type="number">
    ///   <item>the item's OWN USAGE clause — <see cref="Analyze"/>, at entry bind; and</item>
    ///   <item>§13.18.60.4 GR1 GROUP INHERITANCE — "If the USAGE clause is specified or implied at a group level,
    ///     it applies only to each elementary item in the group" — <c>DataBinder.UsageInheritancePass</c>, once
    ///     the forest is complete.</item>
    /// </list>
    /// <para>The four rules are written against the item, not against the clause's PLACEMENT: SR3 governs "an
    /// elementary data item whose declaration contains, OR an elementary data item SUBORDINATE TO A GROUP ITEM
    /// whose declaration contains, a USAGE clause specifying BINARY, COMPUTATIONAL, or PACKED-DECIMAL"; SR5 and
    /// SR12 govern "an elementary data item WITH usage bit / national"; SR20 governs "a USAGE clause ASSOCIATED
    /// WITH an elementary data item". GR1 is what associates a group's clause with the item, so every one of
    /// them reaches the inherited spelling — and before kb/Work PB495 not one of them did, because this screen
    /// was reachable only from the written-clause path.</para>
    ///
    /// <para><paramref name="category"/> is what the PICTURE CHARACTER-STRING DESCRIBES (§13.18.40.6 Table 10 /
    /// §8.5.2 Table 2), which is what all four rules key on — not the item's final category, which a BLANK WHEN
    /// ZERO clause can move (§13.18.8 GR2) without changing the string.</para>
    ///
    /// <para>Returns the usage the item BINDS with: the screened one, or <see cref="Usage.Display"/> where a
    /// violation has been reported — the recovery discipline, since the compile has already failed and the value
    /// only keeps the doomed emit crash-free (DEVLOG 597).</para>
    /// </summary>
    internal static Usage ScreenUsageAgainstPicture(PicCategory category, Usage usage, bool explicitUsage,
        string picture, EditionContext edition, string where, LiteralClass? currencyClass = null)
    {
        // ── §12.3.7.3 SR28 ── "If literal-7 is of class alphanumeric, the associated currency symbol may be used
        // only to define a numeric-edited item with usage display. If literal-7 is of class national, the associated
        // currency symbol may be used only to define a numeric-edited item with usage national." The two classes pair
        // with the two usages crosswise-forbidden, so one comparison asks both sentences (kb/Work PB1089).
        // currencyClass is the class of the literal-7 behind the symbol the PICTURE uses, null when it uses none.
        // Asked HERE — the one USAGE × PICTURE screen — so the written clause and a usage acquired from the group
        // (§13.18.60.4 GR1) are screened by the same call; a usage other than display/national is SR3's.
        if (currencyClass is { } literal7Class && category is PicCategory.NumericEdited
            && (usage is Usage.Display or Usage.National)
            && (literal7Class is LiteralClass.National) != (usage is Usage.National))
            edition.Error(DiagnosticCatalog.CurrencySymbolClassUsage, $"{where}: the currency symbol of PICTURE {picture} "
                + $"is defined by a{(literal7Class is LiteralClass.National ? " national" : "n alphanumeric")} literal-7 "
                + "of a CURRENCY SIGN clause (the clause SR25 implies for '$' is alphanumeric), so it may be used only to "
                + $"define a numeric-edited item with usage {(literal7Class is LiteralClass.National ? "national" : "display")}"
                + $", not one with USAGE {UsageFamilies.UsageWord(usage)} (ISO §12.3.7.3 SR28)");

        // ── §13.18.60.3 SR20 ── "Only the NATIONAL phrase may be specified in a USAGE clause associated with an
        // elementary data item whose explicit or implicit picture character-string contains the symbol 'N'."
        // With no clause associated at all, SR13a supplies NATIONAL — so the rule bites only on a written one.
        if (category is PicCategory.National)
        {
            if (explicitUsage && usage is not Usage.National)
                edition.Error(DiagnosticCatalog.UsageClauseCompatibility, $"{where}: a national PICTURE (symbol N) admits only USAGE "
                    + $"NATIONAL, not {UsageFamilies.UsageWord(usage)} (ISO §13.18.60.3 SR20; SR13a implies NATIONAL "
                    + "when no USAGE clause is specified)");
            return Usage.National;
        }

        // ── A BOOLEAN picture ── SR5 admits usage bit, SR13b implies display, SR12 admits national.
        if (category is PicCategory.Boolean)
            switch (usage)
            {
                case Usage.Display or Usage.Bit or Usage.National:
                    // display-form (SR13b), bit-form (SR5) and NATIONAL-form (SR12) — one D-B1 '0'/'1'
                    // character per boolean position in every one of them. What usage NATIONAL changes is the
                    // STORAGE representation, not the carrier: §13.18.60.4 GR8 puts the item in "a national
                    // coded character set", so each of those characters occupies a national character position
                    // (two bytes under D-N1) — applied once, by the one national byte transform, at every byte
                    // boundary (D-N7; kb/Work PB646).
                    return usage;
                default:
                    edition.Error(DiagnosticCatalog.UsageClauseCompatibility, $"{where}: a boolean PICTURE (symbol 1) admits only USAGE "
                        + $"DISPLAY, BIT, or NATIONAL, not {UsageFamilies.UsageWord(usage)} "
                        + "(ISO §13.18.60.3 SR5/SR12/SR13b)");
                    return Usage.Display;
            }

        // ── §13.18.60.3 SR5 ── "An elementary data item with usage bit shall be specified only with a picture
        // character-string that describes a boolean data item" — and this one describes none.
        if (usage is Usage.Bit)
        {
            edition.Error(DiagnosticCatalog.UsageClauseCompatibility, $"{where}: USAGE BIT requires a boolean PICTURE (symbol 1 only) — "
                + $"PICTURE {picture} is not boolean (ISO §13.18.60.3 SR5)");
            return Usage.Display;
        }

        // ── §13.18.60.3 SR12 ── usage national "shall be described with a picture character-string that
        // describes a boolean, national, national-edited, numeric, or numeric-edited data item". FIVE shapes,
        // and all five are LIVE (kb/Work PB646): the two category-national forms returned above, the boolean
        // form in the switch above, and the NUMERIC / NUMERIC-EDITED forms here. The ONLY picture SR12 refuses
        // is an alphabetic/alphanumeric one, which §13.18.40.3 SR30 refuses from the other side as well.
        if (usage is Usage.National)
        {
            if (category is PicCategory.Alphanumeric)
            {
                edition.Error(DiagnosticCatalog.UsageClauseCompatibility, $"{where}: USAGE NATIONAL may not be specified with an "
                    + $"alphabetic or alphanumeric PICTURE ({picture}) — it admits boolean, national, "
                    + "national-edited, numeric, and numeric-edited pictures only (ISO §13.18.60.3 SR12; "
                    + "§13.18.40.3 SR30)");
                return Usage.Display;
            }
            // A national-form NUMERIC or NUMERIC-EDITED item. §13.18.40.4 GR1 — "When the usage of the subject
            // of the entry is national, each symbol representing a character position defines a national
            // character position" — so its digits, its sign and its insertion characters are NATIONAL
            // characters; §13.18.60.4 GR8 pins their storage size (two bytes, D-N1). Its VALUE is the same
            // number its DISPLAY twin holds and its CHARACTER IMAGE is the same digit run, so the carrier and
            // the numeric pipeline are shared verbatim and only the BYTE serialization differs — design D-N7.
            return Usage.National;
        }

        // ── §13.18.60.3 SR3 ── BINARY / COMPUTATIONAL / PACKED-DECIMAL "shall be specified only with a picture
        // character-string that describes a NUMERIC item". §8.5.2 Table 2 puts category numeric alone in class
        // numeric — numeric-EDITED is class alphanumeric (usage display) or national — so an edited mask is as
        // nonconforming here as an alphanumeric one, and silently accepting it produced an item whose declared
        // width disagreed with its own character image (`PIC ZZ9 USAGE COMP` measured 1 byte against a 3-character
        // image; kb/Work PB540). Without the screen a `PIC XX COMP` bound as category Alphanumeric with the
        // numeric usage DROPPED. The picture-LESS BINARY-CHAR/-SHORT/-LONG/-DOUBLE usages take no PICTURE at all
        // — that is §13.16.3 SR8, screened in DataBinder against UsageFamilies.IsPictureless.
        if (usage is Usage.Binary or Usage.Comp5 or Usage.Packed && category is not PicCategory.Numeric)
        {
            // ⛔ DataBinder.UsageWord is THE spelling table (it has the drift test) — this site used to carry its
            // own three-member copy whose default arm rendered the C# enum name.
            edition.Error(DiagnosticCatalog.UsageClauseCompatibility, $"{where}: USAGE {UsageFamilies.UsageWord(usage)} requires a PICTURE that describes a numeric "
                + $"item — PICTURE {picture} is "
                + (category is PicCategory.NumericEdited ? "numeric-edited" : "alphabetic/alphanumeric")
                + " (ISO §13.18.60.3 SR3)");
            return Usage.Display;
        }

        return usage;
    }

    /// <summary>The runtime mask: the picture's currency symbol <paramref name="cs"/> canonicalized to <c>$</c>
    /// (position-preserving, so <see cref="PicInfo.EditingRules"/>'s indexes still hold), so <c>CobolEdit</c>
    /// sees ONE currency symbol and the string rides beside it (<see cref="PicInfo.CurrencyString"/>).</summary>
    private static string CanonicalCurrencyMask(string expanded, char cs)
    {
        if (cs == '$') return expanded;
        var a = expanded.ToCharArray();
        for (int i = 0; i < a.Length; i++)
            if (char.ToUpperInvariant(a[i]) == cs) a[i] = '$';
        return new string(a);
    }

    /// <summary>
    /// ⛔ ISO §13.18.40.4 GR4 — THE SIZE OF A PICTURE-DESCRIBED ELEMENTARY ITEM, WRITTEN ONCE (kb/Work PB535).
    /// "The size in boolean positions or character positions of an elementary data item that has been defined
    /// with a PICTURE clause is determined by the number of symbols in character-string-1 that represent either
    /// boolean positions or character positions."
    /// <para>GR14 says which symbols represent none, and there are exactly three: <c>'P'</c> — "not counted in
    /// the size of the item, but each symbol 'P' is counted in the maximum number of digit positions";
    /// <c>'V'</c> — "not counted in the size of the item"; and <c>'S'</c> — "counted in the size of the item ONLY
    /// when the subject of the entry is described with a SIGN clause with the SEPARATE phrase" (that one
    /// character is added by the SIGN pass, not here). EVERY other symbol of every category — 'A' 'B' 'E' 'N' 'X'
    /// 'Z' '0' '1' '9' '/' ',' '.' '*', each character of 'CR'/'DB', each currency symbol, each EDITING
    /// character-1 — carries GR14's sentence "is counted in the size of the item".</para>
    /// <para>⛔ THE POINT OF THE EXTRACTION. This count was spelled FOUR ways across five arms: the national and
    /// boolean arms as <c>expanded.Length</c>, the float-edited arm as <c>expanded.Length</c> again, the
    /// numeric-edited arm as this exclusion, the pure-numeric arm as its '9' count, and the alphanumeric arm as
    /// a hard-coded WHITELIST of the symbols it expected (<c>X A 9</c> plus the insertion set). The whitelist
    /// is the dangerous spelling, because it answers a symbol it does not know about by DROPPING it: before the
    /// composition validator (kb/Work PB528) existed, <c>PIC XX,XX</c> bound LENGTH 4 and <c>PIC XXCR</c> bound
    /// 2 — an item SHORTER than its picture, which shifts every following member of a group image — where GR4
    /// gives 5 and 4. Stated as an exclusion the rule cannot do that: a symbol nobody anticipated is counted,
    /// because GR14 counts it. <see cref="PictureCategoryDriftTests"/> pins the property over the whole accepted
    /// set rather than over a list of cases.</para>
    /// </summary>
    /// <param name="expanded">The repeat-expanded character-string.</param>
    /// <param name="currencyExtra">GR14's currency widening: "the first occurrence of the currency symbol adds
    /// the number of characters in the currency string to the size of the item. Each subsequent occurrence of the
    /// currency symbol adds one" — i.e. the occurrences are already counted one apiece above, and this is the
    /// first one's string length minus that one. Zero for a one-character currency string and for every category
    /// whose symbols Table 10 will not let stand beside a currency symbol.</param>
    /// <param name="editingExtra">GR14's 'es' widening — <see cref="EditingPositions"/>.</param>
    internal static int CharacterPositions(string expanded, int currencyExtra = 0, int editingExtra = 0)
        => expanded.Count(c => c is not ('P' or 'S' or 'V')) + currencyExtra + editingExtra;

    /// <summary>⛔ ISO §13.18.40.4 GR14's 'es' entry — the PICTURE EDITING character-1's contribution to the
    /// item's SIZE, which is the literal's width and not the one character position the symbol occupies in
    /// character-string-1. <see cref="CharacterPositions"/> has already counted each occurrence once, so this is
    /// the REMAINDER, exactly as <c>currencyExtra</c> is for the currency string:
    /// <list type="bullet">
    ///   <item>"If character-1 is a simple insertion symbol or a fixed insertion symbol, the size of literal-1 is
    ///     counted in the size of the item" — the IS form, at EVERY occurrence;</item>
    ///   <item>"For extended editing sign control symbols with fixed insertion, each occurrence of the
    ///     character(s) specified in the associated literal are counted in the size of the item" — the FOR form
    ///     with a single occurrence;</item>
    ///   <item>"For floating inserting, one occurrence of literal-2 or literal-3 is counted in the size of the
    ///     item plus one character for each repetition of character-1" — the FOR form with two or more, where
    ///     exactly ONE occurrence is literal-wide and the rest are one character apiece.</item>
    /// </list>
    /// <para>Annex D.24 states the arithmetic for the floating case outright: <c>PIC LLLL9,88 EDITING "L" FOR
    /// NEGATIVE IS "DEBIT "</c> "would result in an item size of 13 characters: 6 for the first 'L', 3 for the
    /// next three, and 4 for the numbers". Before kb/Work PB491 the count was the bare occurrence count, which
    /// is why the appended PB492 lead measured <c>PIC NNTNN EDITING "T" IS N"::"</c> at LENGTH 5 where GR14
    /// gives 6.</para></summary>
    private static int EditingPositions(string expanded, IReadOnlyList<CobolEdit.EditRule>? rules)
    {
        if (rules is null) return 0;
        int extra = 0;
        foreach (var r in rules)
        {
            if (r.Width == 1) continue;
            char c1 = char.ToUpperInvariant(r.Char1);
            int occ = 0;
            foreach (char c in expanded) if (char.ToUpperInvariant(c) == c1) occ++;
            if (occ == 0) continue;
            extra += (r.SimpleInsertion ? occ : 1) * (r.Width - 1);
        }
        return extra;
    }

    /// <summary>ISO §13.18.40.3 SR4 — "The maximum number of characters allowed in character-string-1 is 63."
    /// Measured on character-string-1 AS WRITTEN (kb/Work PB532); see <see cref="Analyze"/>'s prologue.</summary>
    internal const int MaxPictureStringLength = 63;

    /// <summary>⚠ THE IMPLEMENTOR-DEFINED MAXIMUM number of character positions in one elementary item. The
    /// standard sets none: §13.18.40.3 SR4 bounds only the WRITTEN character-string, SR14 bounds only a numeric
    /// or fixed-point numeric-edited item's DIGIT positions (1 through 31), and Annex A.1 carries no
    /// maximum-item-size item, so an alphanumeric, alphabetic, national or boolean string is unbounded by the
    /// standard. 2^27 is the largest power of two whose UTF-16 image — two bytes per character position, for
    /// alphanumeric and national alike — stays inside .NET's single-object ceiling; past it the expansion used
    /// to die with an OutOfMemoryException instead of naming the source line (kb/Work PB531).</summary>
    internal const int MaxCharacterPositions = 1 << 27;

    /// <summary>
    /// Expand <c>symbol(n)</c> repetition factors into a flat symbol run (uppercased), VALIDATING every factor
    /// against ISO §13.18.40.3 SR6 — "An unsigned nonzero integer that is enclosed in parentheses indicates the
    /// number of consecutive occurrences of the symbol that immediately precedes the left parenthesis. The
    /// integer may be specified by a constant-name, in which case the length of the integer, not the length of
    /// the constant-name, is counted toward the maximum number of characters in character-string-1."
    /// <para>⛔ THIS IS THE ONE PLACE A REPETITION FACTOR IS READ, and it is why it validates rather than
    /// parses. Both spellings of the factor arrive here: the literal one, and the constant-name one that
    /// <c>DataBinder.Constants.ExpandPicConstants</c> rewrites to <c>(integer)</c> in the SOURCE string before
    /// calling <see cref="Analyze"/> — the two-arm shape (kb/Work PB531), where the constant arm was added
    /// without re-deriving the factor's own syntax rule, so both arms inherited the hole. The factor used to be
    /// read with a bare <c>int.TryParse</c> (which accepts a LEADING SIGN and zero, and whose FAILURE on an
    /// overflowing factor silently fell through to "these are ordinary picture characters", reporting SR2's
    /// invalid-symbol '(' instead of the rule that was broken) and fed straight to
    /// <c>StringBuilder.Append(char, int)</c>: <c>PIC X(-3)</c> left the binder as an unhandled
    /// <c>ArgumentOutOfRangeException</c> and <c>PIC X(2000000000)</c> as an <c>OutOfMemoryException</c> — a
    /// compiler crash with no diagnostic and no source location, by either arm.</para>
    /// <para>A '(' can never be anything BUT a repetition factor's left parenthesis: it is not a picture symbol
    /// (SR2), and §12.3.7.3 SR27 c) excludes '(' and ')' from literal-8, so no CURRENCY SIGN clause can make one
    /// a currency symbol either. That is what lets an unclosed or unparsable factor be reported as the SR6
    /// violation it is instead of being handed on as ordinary text.</para>
    /// </summary>
    /// <returns><see langword="false"/> when a factor violates SR6 or the expansion would exceed
    /// <see cref="MaxCharacterPositions"/>; the diagnostic has been reported and the caller recovers.</returns>
    internal static bool TryExpandRepeats(string picture, EditionContext edition, string where,
        out string expanded)
    {
        var sb = new System.Text.StringBuilder();
        string p = picture.ToUpperInvariant();
        expanded = "";
        for (int i = 0; i < p.Length; i++)
        {
            char c = p[i];
            if (c is ' ') continue;
            if (i + 1 < p.Length && p[i + 1] == '(')
            {
                int close = p.IndexOf(')', i + 2);
                string factor = close > 0 ? p[(i + 2)..close] : p[(i + 2)..];
                if (!TryRepetitionFactor(factor, close > 0, out int n))
                {
                    edition.Error(DiagnosticCatalog.PictureRepetitionFactor, $"{where}: the repetition factor "
                        + $"'({factor}{(close > 0 ? ")" : "")}' after the symbol '{c}' in PICTURE {picture} is not "
                        + "an unsigned nonzero integer (ISO §13.18.40.3 SR6)");
                    return false;
                }
                if ((long)sb.Length + n > MaxCharacterPositions)
                {
                    ReportTooLarge(edition, where, picture, (long)sb.Length + n);
                    return false;
                }
                sb.Append(c, n);
                i = close;
                continue;
            }
            if (sb.Length + 1 > MaxCharacterPositions) { ReportTooLarge(edition, where, picture, sb.Length + 1L); return false; }
            sb.Append(c);
        }
        expanded = sb.ToString();
        return true;
    }

    /// <summary>SR6's integer, read ONCE: the digits of the factor with no sign, no space and no other
    /// character, denoting a value of at least one. <paramref name="closed"/> is false for a '(' the string
    /// never closes.</summary>
    private static bool TryRepetitionFactor(string factor, bool closed, out int n)
    {
        n = 0;
        if (!closed || factor.Length == 0) return false;
        long value = 0;
        foreach (char d in factor)
        {
            if (d is < '0' or > '9') return false;                // a sign, a space or any non-digit — not "an unsigned … integer"
            value = value * 10 + (d - '0');
            // SATURATE rather than overflow: SR4 bounds the factor to 60-odd digits, which still overruns
            // Int64. One past the cap is all the caller needs to report the limit, and it keeps an
            // arbitrarily long run of digits from wrapping back INSIDE the cap (the failure shape kb/Work
            // PB639 named on the arithmetic side).
            if (value > MaxCharacterPositions) value = MaxCharacterPositions + 1L;
        }
        if (value == 0) return false;                             // "an unsigned NONZERO integer"
        n = (int)value;
        return true;
    }

    /// <summary>⛔ THE ONE "one instance of the picture symbol" PREDICATE (kb/Work PB1210) — asked by §13.18.2.3 SR1
    /// (ANY LENGTH: "shall be one instance of the picture symbol 'N', 'X', or '1'") and §13.18.19.3 SR1 (DYNAMIC
    /// LENGTH: 'N' or 'X'); the symbol set is the caller's, the COUNT rule is here. A character-string is one
    /// instance when it is a single symbol of <paramref name="symbols"/>, bare or followed by a repetition factor
    /// whose value is 1 — SR6 of §13.18.40.3: the parenthesized integer "indicates the number of consecutive
    /// occurrences of the symbol", so <c>X</c>, <c>X(1)</c>, <c>X(01)</c> and <c>N(001)</c> all denote one. The
    /// factor is read by <see cref="TryRepetitionFactor"/>, the ONE reader of a factor, so a constant-name factor
    /// (rewritten to its integer by <c>DataBinder.ExpandPicConstants</c> before any caller asks) is covered too.
    /// The two clauses used to carry two different spellings of this test — a closed list of six strings for ANY
    /// LENGTH, a count-1 regex for DYNAMIC LENGTH — so <c>PIC X(01) ANY LENGTH</c> was refused while its DYNAMIC
    /// LENGTH twin compiled.</summary>
    /// <param name="picture">The PICTURE character-string (constant-name factors already expanded), or null.</param>
    /// <param name="symbols">The admissible symbols, upper case (e.g. <c>"NX1"</c>).</param>
    internal static bool IsSingleInstance(string? picture, string symbols)
    {
        string p = picture?.Trim().ToUpperInvariant() ?? "";
        if (p.Length == 0 || !symbols.Contains(p[0])) return false;
        if (p.Length == 1) return true;
        return p.Length >= 4 && p[1] == '(' && p[^1] == ')'
            && TryRepetitionFactor(p[2..^1], closed: true, out int n) && n == 1;
    }

    private static void ReportTooLarge(EditionContext edition, string where, string picture, long positions)
        => edition.Error(DiagnosticCatalog.PictureItemTooLarge, $"{where}: PICTURE {picture} describes "
            + $"{(positions > MaxCharacterPositions ? "more than " : "")}{Math.Min(positions, (long)MaxCharacterPositions)} "
            + $"character positions — WiseOwl COBOL's maximum for one elementary item is {MaxCharacterPositions} "
            + "(⚠ implementor-defined: ISO §13.18.40.3 SR4 bounds only the WRITTEN character-string and SR14 "
            + "only a numeric item's digit positions, and Annex A.1 carries no maximum-item-size item)");

    /// <summary>Validate the PICTURE EDITING phrases (ISO §13.18.40.3 SR8–SR12; COBOL-2023) and build the render
    /// rules. Emits the SR diagnostics (COBOLNET1591–1596, COBOLNET1955). Every legal shape produces a rule: a
    /// literal of any width (SR9 allows 50 characters) and a floating character-1 (the same character-1 appearing
    /// ≥2 times under a FOR phrase, §13.18.40.5 rule 6) are rendered by <c>CobolEdit</c>'s variable-width
    /// materialization, not staged (kb/Work PB491). <paramref name="char1Set"/> (uppercased) collects every
    /// accepted character-1 so the SR2 whitelist admits them; <paramref name="char1Extended"/> is the subset
    /// declared with the FOR phrase, which SR12 makes EXTENDED editing sign control symbols and rule 6 therefore
    /// lists among the FLOATING insertion symbols — the one fact <c>PictureComposition</c>'s floating-string
    /// detector cannot derive from the character-string alone. Returns null when there are no phrases or an SR
    /// error.
    /// <para><paramref name="usage"/> is the subject's usage as the entry resolved it, and it is here for SR9's
    /// first condition alone — "If USAGE IS NATIONAL is specified for the subject of the entry OR if
    /// character-string-1 contains the symbol 'N' …".</para></summary>
    private static CobolEdit.EditRule[]? ValidateEditing(
        IReadOnlyList<EditingPhraseSpec>? editing, string expanded, Usage usage, EditionContext edition,
        string where, char cs, bool decimalPointIsComma, out HashSet<char> char1Set, out HashSet<char> char1Extended,
        out HashSet<char> char1Declared)
    {
        char1Set = [];
        char1Extended = [];
        char1Declared = [];
        if (editing is null || editing.Count == 0) return null;

        // ── ISO §13.18.40.3 SR9, first sentence — the LITERAL CLASS the phrase's literals shall be written in.
        // Both halves bind: a national subject takes national literals, any other subject takes alphanumeric
        // ones. The subject is national when the entry says USAGE IS NATIONAL or the character-string holds an
        // 'N' — which is also exactly the pair of ways §13.18.40.4 GR9/GR10 make a category-national item, so the
        // condition needs no category and can be asked here, before the category is decided.
        bool nationalSubject = usage is Usage.National || expanded.Contains('N');

        // Pre-scan every phrase's character-1 (SR25 admits up to two extended sign symbols — e.g. a leftmost 'L'
        // and a rightmost 'F') so SR12b's "only character-1 and 9 . cs P V Z" test admits ALL declared editing
        // characters, not just the phrase under validation.
        // The pre-scan records every single LETTER a phrase NAMES as character-1 — an extended letter included, and a
        // letter SR8 is about to refuse — in <paramref name="char1Declared"/>: SR8 (basic letters only) is asked ONCE,
        // in the loop below, and the SR2 symbol whitelist beside it reads the DECLARED set, so a refused letter does
        // not draw a second "not an allowable picture symbol" for the same fault (the PB960 shape). char1Set, by
        // contrast, holds only the character-1 that passed SR8-SR11.
        var allChar1 = char1Declared;
        foreach (var ph0 in editing)
            if ((ph0.Char1Text ?? "") is { Length: 1 } t0 && CobolCharacterRepertoire.IsLetter(t0[0])) allChar1.Add(char.ToUpperInvariant(t0[0]));

        var rules = new List<CobolEdit.EditRule>();
        bool error = false;
        // The character-1 of the EXTENDED (FOR-phrase) editing sign control symbols in PHRASE ORDER — the fact SR25's
        // second sentence is stated over. It is a property of the phrase LIST, not of any one phrase, so it is asked
        // once after this loop (kb/Work PB530).
        var extended = new List<char>(2);

        foreach (var ph in editing)
        {
            // character-1 (§13.18.40.3 SR8): a single basic letter, not a CURRENCY-SIGN letter and not one of
            // A B C D E N P R S V X Z. "Basic" is §8.1.3.1 Table 1's row — the Latin letters A-Z and a-z — and an
            // extended letter (Annex B) is a DIFFERENT row, so the question is CobolCharacterRepertoire.IsBasicLetter,
            // never char.IsLetter, which admits every Unicode letter (kb/Work PB533).
            string c1 = ph.Char1Text ?? "";
            if (c1.Length != 1 || !CobolCharacterRepertoire.IsBasicLetter(c1[0]))
            {
                edition.Error("COBOLNET1591", $"{where}: PICTURE EDITING character-1 must be a single basic letter "
                    + $"(A-Z or a-z; an extended letter is not one — ISO §13.18.40.3 SR8; ISO §8.1.3.1 Table 1; got \"{c1}\")");
                error = true; continue;
            }
            char char1 = char.ToUpperInvariant(c1[0]);
            if (char1 == cs || "ABCDENPRSVXZ".IndexOf(char1) >= 0)
            {
                edition.Error("COBOLNET1591", $"{where}: PICTURE EDITING character-1 '{c1}' must be a basic letter "
                    + "other than A B C D E N P R S V X Z or a CURRENCY-SIGN letter (ISO §13.18.40.3 SR8)");
                error = true; continue;
            }
            // SR11: distinct character-1 across phrases.
            if (!char1Set.Add(char1))
            {
                edition.Error("COBOLNET1592", $"{where}: PICTURE EDITING character-1 '{c1}' is specified in more than "
                    + "one EDITING phrase (ISO §13.18.40.3 SR11 — each character-1 shall be distinct)");
                error = true; continue;
            }
            // SR10: character-1 shall appear at least once in the character-string.
            int occ = 0;
            for (int i = 0; i < expanded.Length; i++)
                if (char.ToUpperInvariant(expanded[i]) == char1) occ++;
            if (occ == 0)
            {
                edition.Error("COBOLNET1593", $"{where}: PICTURE EDITING character-1 '{c1}' does not appear in the "
                    + "PICTURE character-string (ISO §13.18.40.3 SR10)");
                error = true; continue;
            }
            // SR9, second sentence: "The total number of characters in literal-1, literal-2, or literal-3 shall
            // not exceed 50." And SR9's FIRST sentence — the literal CLASS — screened against the subject in the
            // same walk, because they are one rule: a national subject (USAGE IS NATIONAL, or an 'N' in
            // character-string-1) takes NATIONAL literals and every other subject takes ALPHANUMERIC ones. Both
            // halves were unenforced while national-edited could not be defined at all (kb/Work PB492): the
            // decoded literal reached here as a bare string with its class discarded, so `PIC NNTNN EDITING "T"
            // IS ":"` and `PIC XXTXX EDITING "T" IS N":"` each bound silently.
            // A figurative operand takes the subject's class here (§8.3.3.6.4 GR1), before either sentence is asked.
            EditLiteral? simpleLit = ph.Simple?.InContext(nationalSubject);
            EditLiteral? negLitSpec = ph.Neg?.InContext(nationalSubject);
            EditLiteral? posLitSpec = ph.Pos?.InContext(nationalSubject);
            foreach (var (lit, name) in new (EditLiteral?, string)[] { (simpleLit, "literal-1"), (negLitSpec, "literal-2"), (posLitSpec, "literal-3") })
            {
                if (lit is not { } l) continue;
                if (l.Class is EditLiteralClass.NotAlphanumericOrNational)
                {
                    edition.Error(DiagnosticCatalog.PictureEditingLiteralClass, $"{where}: the PICTURE EDITING "
                        + $"{name} '{l.Text}' is not an alphanumeric or a national literal — "
                        + $"{(nationalSubject ? "USAGE IS NATIONAL is specified or character-string-1 contains the symbol 'N', so literal-1, literal-2 and literal-3 shall be national literals" : "literal-1, literal-2 and literal-3 shall be alphanumeric literals")} "
                        + "(ISO §13.18.40.3 SR9)");
                    error = true;
                    continue;
                }
                if (l.Text.Length > 50)
                {
                    edition.Error("COBOLNET1594", $"{where}: a PICTURE EDITING literal exceeds 50 characters "
                        + "(ISO §13.18.40.3 SR9)");
                    error = true;
                }
                bool national = l.Class is EditLiteralClass.National;
                if (national != nationalSubject)
                {
                    edition.Error(DiagnosticCatalog.PictureEditingLiteralClass, $"{where}: the PICTURE EDITING "
                        + $"{name} is {(national ? "a national literal (N\"…\")" : "an alphanumeric literal")} but the "
                        + $"subject of the entry is {(nationalSubject ? "national" : "not national")} — "
                        + $"{(nationalSubject ? "USAGE IS NATIONAL is specified or character-string-1 contains the symbol 'N', so literal-1, literal-2 and literal-3 shall be national literals" : "literal-1, literal-2 and literal-3 shall be alphanumeric literals")} "
                        + "(ISO §13.18.40.3 SR9)");
                    error = true;
                }
            }

            if (ph.IsForForm)
            {
                // SR12: "If the FOR phrase is specified, character-1 is an EXTENDED editing sign control symbol"
                // — so this phrase, and only this form, is one of the two SR25's second sentence orders.
                extended.Add(char1);
                char1Extended.Add(char1);
                // SR12b: a FOR (extended sign-control) picture may contain only character-1 and 9 . cs P V Z.
                foreach (char mc in expanded)
                {
                    char mu = char.ToUpperInvariant(mc);
                    if (allChar1.Contains(mu) || mu is '9' or '.' or 'P' or 'V' or 'Z' || mu == cs) continue;
                    edition.Error("COBOLNET1596", $"{where}: with a FOR (extended editing sign control) EDITING "
                        + $"phrase the PICTURE character-string may contain only character-1 and 9 . {cs} P V Z "
                        + $"(ISO §13.18.40.3 SR12b; found '{mc}')");
                    error = true; break;
                }
                // SR12a: the NEGATIVE and POSITIVE literals (when both present) occupy the same number of positions.
                if (negLitSpec is { } n2 && posLitSpec is { } p3 && n2.Text.Length != p3.Text.Length)
                {
                    edition.Error("COBOLNET1595", $"{where}: the NEGATIVE and POSITIVE literals of a FOR EDITING "
                        + "phrase shall occupy the same number of character positions (ISO §13.18.40.3 SR12a)");
                    error = true; continue;
                }
                // SR12c: "If only POSITIVE is specified, the default character for the unspecified phrase is the
                // space character repeated for the number of characters in literal-2. If only NEGATIVE is
                // specified, the default character for the unspecified phrase is the space character repeated
                // the number of characters in literal-3." SR12a has already made the two widths agree when both
                // are written, so the specified one's width is the item's either way.
                int width = (negLitSpec ?? posLitSpec)?.Text.Length ?? 0;
                string negLit = negLitSpec?.Text ?? new string(' ', width);
                string posLit = posLitSpec?.Text ?? new string(' ', width);
                // FOR = an EXTENDED editing sign control symbol. A FIXED occurrence is rule 5 (Table 8); a STRING of at
                // least two identical symbols is a FLOATING insertion string — rule 6's first sentence lists "the
                // extended editing sign control symbols, if specified" among the floating insertion symbols and its
                // second sentence is the ≥2 test, with Table 9 for the result. Which one a letter is depends on the
                // WHOLE character-string and the full set of character-1 (an IS-form letter embedded in the string is
                // part of it), so the flag is decided AFTER this loop, by the one floating-string detector
                // (PictureComposition.HasFloatingString). A literal wider than one character is no longer a GAP
                // either: §13.18.40.4 GR14's 'es' entry gives the item the literal's width and CobolEdit
                // materializes it (kb/Work PB491; Annex D.24 demonstrates both shapes, so both were legal source
                // the compiler refused).
                rules.Add(new CobolEdit.EditRule(char1, negLit, posLit, SimpleInsertion: false, Floating: false));
            }
            else
            {
                // IS (simple insertion) form — sign-independent (ISO §13.18.40.5 editing rule 3): character-1
                // inserts literal-1 at every occurrence, immune to sign, at literal-1's own width (GR14 'es').
                string lit = simpleLit?.Text ?? "";
                // IS = SIMPLE insertion (rule 3), so this character-1 joins any zero-suppression or floating
                // string it is embedded in or immediately right of (rules 6 and 7) — CobolEdit.TrySimpleInsertion.
                rules.Add(new CobolEdit.EditRule(char1, lit, lit, SimpleInsertion: true, Floating: false));
            }
        }

        // Rule 6's floating test, per extended letter, now that every character-1 is known (see the FOR arm above).
        for (int k = 0; k < rules.Count; k++)
            if (!rules[k].SimpleInsertion)
                rules[k] = rules[k] with
                {
                    Floating = PictureComposition.HasFloatingString(expanded, cs, char1Set, char1Extended,
                        decimalPointIsComma ? ',' : '.', decimalPointIsComma ? '.' : ',', rules[k].Char1),
                };

        // ── SR25's SECOND sentence over the EXTENDED editing sign control symbols as a SET (ISO §13.18.40.3). It is
        // not askable inside the phrase loop: it is stated over the WHOLE phrase list. (SR24's second sentence, the
        // bound of one or two symbols, is asked over the PICTURE by PictureComposition.Validate: a count of EDITING
        // phrases misses a repeated fixed occurrence, kb/Work PB855.)
        // SR25: "When extended editing sign control symbols are used and two are specified, the first occurrence of
        // the EDITING phrase shall be for the leftmost symbol in character-string-1 and the second occurrence shall
        // be for the rightmost symbol in character-string-1."
        // ⛔ DETERMINATION (kb/Work PB855, replacing PB530's): the sentence is read AS WRITTEN — the first phrase's
        // character-1 IS character-string-1's leftmost symbol and the second's IS its rightmost. PB530 read "the
        // leftmost symbol" as the leftmost OF THE TWO and constrained only the phrase ORDER, because §13.18.40.6's
        // sentence giving 'es' the precedence of 'cs' "cannot be applied literally": Table 10's blank
        // leading-currency-before-trailing-currency cell would refuse Annex D.24's `PIC IS L9999.99F`. That premise
        // is true of the cell and false of the reading — PictureComposition.Precedence applies the sentence to
        // every symbol BUT another currency-like one, whose order SR25 and SR26 own — so the stricter reading no
        // longer rejects the standard's own example, and the weaker one left `PIC LF$999` (two extended symbols
        // ahead of the currency symbol) and `PIC 9L9F` legal against the words of the rule.
        if (extended.Count == 2
            && (char.ToUpperInvariant(expanded[0]) != extended[0] || char.ToUpperInvariant(expanded[^1]) != extended[1]))
        {
            edition.Error(DiagnosticCatalog.PictureEditingPhraseOrder, $"{where}: with two extended editing sign "
                + "control symbols the FIRST EDITING phrase shall be for the LEFTMOST symbol of character-string-1 "
                + $"and the SECOND for the RIGHTMOST, but the first phrase is for '{extended[0]}' and the "
                + $"second for '{extended[1]}', while the string begins with '{expanded[0]}' and ends with "
                + $"'{expanded[^1]}' (ISO §13.18.40.3 SR25)");
            error = true;
        }

        // An SR error → no rule set is applied (the item still binds numeric-edited; under the doomed emit
        // character-1 renders verbatim — harmless, the compile has already failed). There is no longer a STAGED
        // arm: the two shapes that used to raise COBOLNET0899 here — a literal wider than one character, and a
        // floating (repeated) character-1 under a FOR phrase — are both rendered, so no legal EDITING phrase is
        // refused (kb/Work PB491).
        return error || rules.Count == 0 ? null : rules.ToArray();
    }

    /// <summary>
    /// Map a COBOL usage keyword (e.g. <c>COMP-3</c>) to a <see cref="Usage"/>. EVERY grammar-accepted keyword
    /// (the ISO §13.18.60 inventory in <c>CobolData.g4 usageClause/usageKeyword</c>) is recognized EXPLICITLY —
    /// the historical silent catch-all mapped the whole 2002 inventory (NATIONAL, BIT, POINTER, OBJECT
    /// REFERENCE, the FLOAT-x and BINARY-x families) to <see cref="Usage.Display"/>, a wrong-answer misbind (the
    /// W2 loud-guard sweep). An unrecognized keyword is a LOUD internal error — never Display. (The former
    /// <c>out bool skeleton</c> overload is DELETED, P5.11c: every keyword has been LIVE since the 14g.1
    /// introduction-gate migration and nothing ever set the flag — the parameter was constant-false dead code.)
    /// </summary>
    public static Usage ParseUsage(string? keyword, EditionContext edition, string where)
    {
        // ⛔ THE ONE PLACE A USAGE SPELLING IS RECOGNIZED, so it is also the one place a NONSTANDARD one is named
        // (ISO §4.2.10; kb/Work PB1525): COMP-1 … COMP-5 and their COMPUTATIONAL-n long spellings are vendor
        // spellings of usages ISO defines under other words (FLOAT-SHORT, FLOAT-LONG, PACKED-DECIMAL, BINARY, …).
        // The register row carries the spellings, so a new vendor usage word is flagged by adding it to its row.
        if (keyword is not null && NonstandardExtensionRegister.ForUsageSpelling(keyword) is { } extension)
            edition.Extension(extension, $"{where}: USAGE {keyword.ToUpperInvariant()}");
        switch (keyword?.ToUpperInvariant().Replace("COMPUTATIONAL", "COMP"))
        {
            case null or "DISPLAY": return Usage.Display;
            case "COMP" or "COMP-4" or "BINARY": return Usage.Binary;
            case "COMP-3" or "PACKED-DECIMAL": return Usage.Packed;
            case "COMP-5": return Usage.Comp5;
            case "COMP-1": return Usage.Float;
            case "COMP-2": return Usage.Double;
            case "INDEX": return Usage.Index;
            // USAGE NATIONAL / BIT — LIVE (Phase 4a track (a)): only the introduction gate remains (0900
            // below 2002; the registry rows are silent at 2002+), the POINTER/BINARY-CHAR pattern. Picture
            // conformance (SR5/SR12/SR13/SR20) is Analyze's job; a picture-LESS elementary entry is caught at
            // the group-fixup pass (DataBinder.ResolveIndexItems — a group header legally sheds the usage to
            // its subordinates per §13.18.60.4 GR1).
            case "NATIONAL":
                return Usage.National;
            case "BIT":
                return Usage.Bit;
            // USAGE POINTER — LIVE (Phase-4b increment 1): only the introduction gate remains (0900 below
            // 2002; the registry row is silent at 2002+), like OBJECT REFERENCE. The caller synthesizes
            // PicInfo.PointerItem (PICTURE-less, the IndexItem pattern).
            case "POINTER":
                return Usage.Pointer;
            // USAGE PROGRAM-POINTER — LIVE (P10 Step 7): the introduction gate (0900 below 2002) fires from
            // UsageConstructId; the caller synthesizes PicInfo.ProgramPointerItem (PICTURE-less, the
            // PointerItem pattern). The restricted TO-prototype form stages loud at the BindEntry site.
            case "PROGRAM-POINTER":
                return Usage.ProgramPointer;
            // USAGE FUNCTION-POINTER — LIVE (kb/Work PB452 + PB817): the introduction gate (0900 below 2014)
            // fires from UsageConstructId; the caller synthesizes PicInfo.FunctionPointerItem with the MANDATORY
            // TO function-prototype-name operand (PICTURE-less, the ProgramPointerItem pattern). It used to be
            // refused here with the 0899 staged-loud band, which also left the entry with no PicInfo at all —
            // so `01 FP USAGE FUNCTION-POINTER TO FPROTO.` drew a COBOLNET0881 "a PICTURE clause shall be
            // specified" AND a COBOLNET0844 calling the item "of category alphanumeric" on top of the stage.
            case "FUNCTION-POINTER":
                return Usage.FunctionPointer;
            // USAGE MESSAGE-TAG — the DATA half of the declined asynchronous-messaging facility (Annex A.3
            // item 4; docs/CONFORMANCE.md §4 item 1), refused BY NAME at every edition, the FLOAT-BINARY-128
            // posture. ⛔ There is no accept-inert reading: §13.18.60.4 GR9 makes the class AND category of a
            // message-tag data item message-tag, so an accepted item would have to bind as some OTHER class and
            // every reference to it would silently answer wrong. The member flows through so the caller can
            // synthesize a recovery Pic — a PICTURE-less entry (§13.16.3 SR8 exempts message-tag) would
            // otherwise reach the emitter with a null Pic, which is the crash kb/Work PB487 measured. ⛔ BOTH
            // spellings land here: §13.18.60.2 prints [ USAGE IS ] as optional, so bare `01 M MESSAGE-TAG.` is
            // this same clause, and before PB487 it was swallowed by the §13.16.2 vendor catch-all instead.
            case "MESSAGE-TAG":
                edition.Declined(DiagnosticCatalog.MessageTagUsageUnsupported, $"{where}: USAGE MESSAGE-TAG");
                return Usage.MessageTag;
            // LIVE as of the Phase-3 OO spine: only the introduction gate remains (0900 below 2002 — the
            // registry row is silent at 2002+); the caller synthesizes PicInfo.ObjectReferenceItem with the
            // declared class name (PICTURE-less per §13.18.60.4, the IndexItem pattern).
            case "OBJECT REFERENCE":
                return Usage.ObjectReference;
            // The fixed-width binary usages — LIVE (Phase 4 M2-DATA-1): only the introduction gate remains
            // (0900 below 2002; the registry row is silent at 2002+, like POINTER / OBJECT REFERENCE). The
            // caller synthesizes PicInfo.BinaryItem (PICTURE-less per §13.16.3 SR8; the IndexItem pattern).
            case "BINARY-CHAR":
                return Usage.BinaryChar;
            case "BINARY-SHORT":
                return Usage.BinaryShort;
            case "BINARY-LONG":
                return Usage.BinaryLong;
            case "BINARY-DOUBLE":
                return Usage.BinaryDouble;
            case "FLOAT-SHORT":   // the implementor-defined float trio (§13.18.60.4 GR13) — LIVE (Phase 6a, D16)
                return Usage.FloatShort;
            case "FLOAT-LONG":
                return Usage.FloatLong;
            case "FLOAT-EXTENDED":
                return Usage.FloatExtended;
            // The COBOL-2014 IEEE-754 interchange float family (§13.18.60.4 GR14-18). binary32/64 map EXACTLY to
            // native float/double (the pinned ISO/IEC 60559:2020 formats are conforming) — LIVE (P12 wave 3). The
            // introduction gate (0900 below 2014) fires from UsageConstructId.
            case "FLOAT-BINARY-32":
                return Usage.FloatBinary32;
            case "FLOAT-BINARY-64":
                return Usage.FloatBinary64;
            // FLOAT-BINARY-128 (binary128, GR16) and FLOAT-DECIMAL-16/34 (decimal64/128, GR17-18) are
            // PROCESSOR-DEPENDENT language elements (Annex A.3 items 17/19): .NET has no IEEE binary128 or IEEE
            // decimal64/128 type, and GR16-18 PIN the formats (a double/System.Decimal approximation would be
            // NON-conforming — the P12 re-scout catch). Documented non-support — rejected LOUD (never a silent wrong
            // representation). The member flows through so the 2014 introduction gate still fires below 2014.
            case "FLOAT-BINARY-128":
                edition.Error("COBOLNET1564", $"{where}: USAGE FLOAT-BINARY-128 (ISO/IEC 60559:2020 binary128, "
                    + "ISO §13.18.60.4 GR16) is a processor-dependent language element not supported by WiseOwl COBOL "
                    + "(Annex A.3 item 17): .NET provides no IEEE 754 binary128 type, and GR16 pins the format so a "
                    + "double-backed approximation would be non-conforming");
                return Usage.FloatBinary128;
            case "FLOAT-DECIMAL-16":
                edition.Error("COBOLNET1564", $"{where}: USAGE FLOAT-DECIMAL-16 (ISO/IEC 60559:2020 decimal64, "
                    + "ISO §13.18.60.4 GR17) is a processor-dependent language element not supported by WiseOwl COBOL "
                    + "(Annex A.3 item 19): .NET provides no IEEE 754 decimal64 type (System.Decimal is a different "
                    + "format)");
                return Usage.FloatDecimal16;
            case "FLOAT-DECIMAL-34":
                edition.Error("COBOLNET1564", $"{where}: USAGE FLOAT-DECIMAL-34 (ISO/IEC 60559:2020 decimal128, "
                    + "ISO §13.18.60.4 GR18) is a processor-dependent language element not supported by WiseOwl COBOL "
                    + "(Annex A.3 item 19): .NET provides no IEEE 754 decimal128 type");
                return Usage.FloatDecimal34;
            case { } other:
                // The grammar admits nothing else — reaching here is a compiler defect (a new grammar
                // alternative without its ParseUsage arm). LOUD, never a silent Display misbind.
                edition.Error(DiagnosticCatalog.UsageKeywordUnmappedInternal,
                    $"internal: unrecognized USAGE keyword '{other}' — {where} (ISO §13.18.60; every "
                    + "grammar-accepted usage keyword must have an explicit ParseUsage mapping)");
                return Usage.Display;
        }
    }

    /// <summary>The ≥edition half of the W2 skeleton gate for a recognized-but-unimplemented PICTURE construct
    /// (external-float symbol E / national-edited): at or above the row's introducing edition — where the
    /// introduction <c>Check</c> is silent — a COBOLNET0899 "recognized but not yet implemented" naming the owning
    /// roadmap phase. Below the edition it is a NO-OP: the COBOLNET0900 introduction gate is fired instead by the
    /// post-bind <c>VersionConformancePass</c> GateData enumerator over <c>PicInfo.SkeletonGate</c> (Step 14g.5 — the
    /// category is recovered to Alphanumeric, erasing the parse identity, so the flag carries the gate forward). Either
    /// way the compile FAILS below its edition (the 0900) or above (the 0899) — never a silent misbind.</summary>
    /// <summary>The floating-point numeric-edited PICTURE (ISO §13.18.40.4 GR13 b; data-model design D21 / kb/Work PB66):
    /// <c>significand E exponent</c>. Validated per string (§13.18.40.6 — Table 10 applies to the two strings
    /// separately): the exponent is exactly <c>+9</c>…<c>+9999</c>; the significand an optional leading fixed
    /// sign (SR23 + NOTE 3 sanction the significand's <c>−</c> beside the exponent's <c>+</c>) and then only the
    /// symbols Table 10 row E admits before E — <c>9 B 0 / , .</c> (no floating insertion, no zero suppression with
    /// replacement, no S/V/P/CR/DB/currency; an IS-form EDITING character-1 is simple insertion and admitted, a FOR-form
    /// one is barred by SR12's FOR-phrase rules, kb/Work PB866) — with one point at most (SR12 b)
    /// and 1..36 digit positions (SR15 — SR14's 31 does not apply). Every symbol of both parts is a character position
    /// (GR14: E, the point, the insertion symbols and the signs are all counted). Returns a numeric-edited PicInfo
    /// with <see cref="PicInfo.IsFloatEdited"/>; Scale and DigitPositions are 0 (a floating-point item's scale is a
    /// runtime property of its value); the recovery shape on a violation (the compile has failed).</summary>
    private static PicInfo AnalyzeFloatEdited(string picture, string expanded, Usage usage, bool explicitUsage,
        EditionContext edition, string where, IReadOnlyList<CobolEdit.EditRule>? editRules, HashSet<char> char1Extended,
        int editingExtra)
    {
        void Bad(string why) => edition.Error(DiagnosticCatalog.PictureFloatEdited,
            $"invalid floating-point numeric-edited PICTURE {picture} — {where}: {why}");
        int eCount = expanded.Count(c => c == 'E');
        if (eCount > 1) { Bad("the symbol E may appear only once (ISO §13.18.40.3 SR12 b)"); return PicInfo.Recovery(expanded.Length); }
        // ⛔ §13.18.40.3 SR12 bars only the FOR form here: "Extended editing sign control symbols shall not be
        // specified for a floating-point edited item" is a rule of SR12's FOR-phrase list (printed as that list's
        // SECOND 'a)', after 'c)' — the standard's own lettering, p.442; kb/Work PB866). An IS-form character-1 is
        // a SIMPLE INSERTION symbol (§13.18.40.5 rule 3), and Table 7 gives this category "Simple insertion, special
        // insertion, and fixed insertion for the significand part" — so it is admitted in the significand below.
        if (char1Extended.Count > 0)
        {
            Bad($"EDITING {char1Extended.First()} FOR … makes character-1 an extended editing sign control symbol, and "
                + "extended editing sign control symbols shall not be specified for a floating-point edited item (ISO "
                + "§13.18.40.3 SR12, the FOR-phrase rules; an IS-form EDITING phrase is permitted)");
            return PicInfo.Recovery(expanded.Length);
        }
        bool IsChar1(char c) => editRules is not null && editRules.Any(r => r.SimpleInsertion && char.ToUpperInvariant(r.Char1) == c);
        int e = expanded.IndexOf('E');
        string sig = expanded[..e], exp = expanded[(e + 1)..];
        // the exponent: '+' then 1..4 '9's, nothing else (§13.18.40.4 GR13 b)
        if (exp.Any(IsChar1))
        {
            Bad("a PICTURE EDITING character-1 may not appear in the exponent — Table 7 gives the exponent part no "
                + "editing (ISO §13.18.40.5 Table 7: \"None for the exponent part\")");
            return PicInfo.Recovery(expanded.Length);
        }
        if (exp.Length < 2 || exp[0] != '+' || exp[1..].Any(c => c != '9') || exp.Length - 1 > 4)
        {
            Bad("the exponent shall be +9, +99, +999 or +9999 (ISO §13.18.40.4 GR13 b)");
            return PicInfo.Recovery(expanded.Length);
        }
        // the significand: an optional leading fixed sign, then Table 10 row E's symbols only
        int start = sig.Length > 0 && sig[0] is '+' or '-' ? 1 : 0;
        bool signed = start == 1;
        int digits = 0, points = 0;
        for (int i = start; i < sig.Length; i++)
        {
            char c = sig[i];
            switch (c)
            {
                case '9': digits++; break;
                case '.': points++; break;
                case 'B': case '0': case '/': case ',': break;
                case var c1 when IsChar1(c1): break;   // an IS-form character-1: simple insertion (§13.18.40.5 rule 3)
                case '+': case '-':
                    Bad("a second sign symbol in the significand — the significand admits one leading + or − (ISO §13.18.40.6 Table 10 row E; SR25 applies per string)");
                    return PicInfo.Recovery(expanded.Length);
                case 'Z': case '*':
                    Bad($"the symbol '{c}' — zero suppression with replacement shall not be specified for the significand (ISO §13.18.40.4 GR13 b)");
                    return PicInfo.Recovery(expanded.Length);
                case 'V': case 'P': case 'S':
                    Bad($"the symbol '{c}' may not appear in the significand — its point is the real '.' (ISO §13.18.40.6 Table 10 row E; SR17/SR18/SR20)");
                    return PicInfo.Recovery(expanded.Length);
                case 'C': case 'D':
                    Bad("CR / DB may not appear in the significand of a floating-point edited item (ISO §13.18.40.6 Table 10 row E; SR23)");
                    return PicInfo.Recovery(expanded.Length);
                default:
                    Bad($"the symbol '{c}' may not appear in the significand — a currency symbol or floating insertion is not permitted (ISO §13.18.40.4 GR13 b; §13.18.40.6 Table 10 row E)");
                    return PicInfo.Recovery(expanded.Length);
            }
        }
        if (points > 1) { Bad("the decimal point may appear only once (ISO §13.18.40.3 SR12 b)"); return PicInfo.Recovery(expanded.Length); }
        if (digits < 1 || digits > 36) { Bad($"the significand carries {digits} digit position(s) — it shall carry 1 to 36 (ISO §13.18.40.3 SR15)"); return PicInfo.Recovery(expanded.Length); }
        // ⛔ THE §13.18.60.3 USAGE × PICTURE SCREEN, asked through the ONE function (kb/Work PB646). This arm
        // used to carry its own SR12 half — a hand-written staging of the national form — and NOTHING else, so
        // a floating-point edited picture under USAGE BINARY/COMP/PACKED slipped past SR3 entirely.
        usage = ScreenUsageAgainstPicture(PicCategory.NumericEdited, usage, explicitUsage, picture, edition, where);
        // GR4 through the ONE count (kb/Work PB535). The loop above has already refused 'S', 'V' and 'P' in the
        // significand and the exponent is '+' and '9's, so nothing is excluded here — asking the one function is
        // what keeps that agreement from becoming a fourth reading of GR4 that nobody re-derives.
        // GR14 'es': an IS-form character-1 counts literal-1's size at every occurrence (EditingPositions).
        return new PicInfo(PicCategory.NumericEdited, usage, Length: CharacterPositions(expanded, editingExtra: editingExtra),
            Digits: digits, Scale: 0, Signed: signed)
        { EditMask = expanded, EditingRules = editRules, DigitPositions = 0, IsFloatEdited = true };
    }

    /// <summary>The format-2 (LOCALE) PICTURE analysis (ISO §13.18.40.3 SR33–SR36 + §13.18.40.6 Table 11;
    /// kb/Work PB64 T6). The symbol alphabet is EXACTLY <c>{ '+', cs, 'Z', '9', '.' }</c> (§13.18.40.4 GR18
    /// defines meanings for exactly those five) and Table 11's non-adjacent precedence closes to the shape
    /// <c>[+] [cs] Z{a} 9{b} [ '.' Z{c} 9{d} ]</c> with fraction Z's only when no 9 precedes (row Z's 9 column is
    /// BLANK: no '9' may precede ANY 'Z', so all Z's form one leading run over the digit sequence). ⛔ DECIMAL-POINT
    /// IS COMMA IS INERT HERE — §12.3.7.4 GR14: "The DECIMAL-POINT IS COMMA clause has no effect on the editing or
    /// de-editing of a data item described with the locale format of the PICTURE clause" (its NOTE 3: the character
    /// written for the decimal separator "is always the period"); SR13's swap is printed under FORMAT 1. ',' is not
    /// a format-2 symbol under any setting. ⛔ NO compile-time relation between integer-1 and the digit count is
    /// checked — the hypothetical item's width is a RUNTIME quantity (the currency string's length is the locale's,
    /// §13.18.40.5 r9) and §13.18.40.5 r14 reconciles the two at each edit. Violations are COBOLNET1673 with the
    /// sub-rule named; the item recovers at the SIZE length (the compile has failed). The returned PicInfo: category
    /// numeric-edited (GR16), Length = integer-1 (GR17 — never the mask width), Digits/Scale/DigitPositions from
    /// character-string-1, EditMask NULL, <see cref="PicInfo.LocaleEdit"/> carrying the canonical picture.</summary>
    private static PicInfo AnalyzeLocaleEdited(string picture, string expanded, char cs, Usage usage,
        bool explicitUsage, EditionContext edition, string where, bool blankWhenZero, LocaleEditSpec locale2,
        bool hasEditingPhrase)
    {
        PicInfo Bad(string why)
        {
            edition.Error(DiagnosticCatalog.PictureLocaleFormat2Violation,
                $"invalid format 2 (LOCALE) PICTURE {picture} — {where}: {why}");
            return PicInfo.Recovery(Math.Max(1, locale2.Size));
        }
        if (hasEditingPhrase)
            return Bad("an EDITING phrase is specified beside the LOCALE phrase; format 2 has no EDITING phrase "
                + "(ISO §13.18.40.2 — the EDITING phrase belongs to format 1)");
        // The SAME implementor maximum the format-1 expansion obeys, asked at the ONE place the limit lives
        // (kb/Work PB531's sibling sweep): §13.18.40.4 GR17 — "The number of character positions in the item is
        // specified by integer-1" — so format 2's SIZE is a character-position count exactly like format 1's
        // expansion, and `SIZE IS 2000000000` used to bind an item whose emitted initializer allocates two
        // billion characters at run time.
        if (locale2.Size > MaxCharacterPositions)
        {
            ReportTooLarge(edition, where, picture, locale2.Size);
            return PicInfo.Recovery(1);
        }
        // Canonicalize the program's currency symbol to '$' (the ONE symbol kind the picture may use — the set
        // membership was classified by the caller) and uppercase is already folded by ExpandRepeats.
        string canonical = string.Concat(expanded.Select(c => char.ToUpperInvariant(c) == cs ? '$' : c));
        int plus = 0, dots = 0, csCount = 0, digitsLeft = 0, digitsRight = 0, zRun = 0;
        bool zOpen = true, sawDot = false;
        for (int i = 0; i < canonical.Length; i++)
        {
            char c = canonical[i];
            switch (c)
            {
                case '+':
                    plus++;
                    if (i != 0) return Bad("the symbol '+' shall be the first symbol (ISO §13.18.40.6 Table 11 — no symbol may precede it)");
                    break;
                case '$':
                    csCount++;
                    if (i > (canonical[0] == '+' ? 1 : 0))
                        return Bad("the currency symbol may follow only a leading '+' (ISO §13.18.40.6 Table 11)");
                    break;
                case '.':
                    dots++;
                    sawDot = true;
                    break;
                case 'Z':
                    if (!zOpen) return Bad("a 'Z' follows a '9' — no '9' may precede any 'Z', so every 'Z' precedes every '9' (ISO §13.18.40.6 Table 11, row Z)");
                    zRun++;
                    if (sawDot) digitsRight++; else digitsLeft++;
                    break;
                case '9':
                    zOpen = false;
                    if (sawDot) digitsRight++; else digitsLeft++;
                    break;
                default:
                    return Bad($"the symbol '{c}' is not a format-2 picture symbol — character-string-1 may contain "
                        + "only '+', the currency symbol, 'Z', '9' and '.' (ISO §13.18.40.4 GR18 defines exactly those; "
                        + "§13.18.40.6 Table 11)");
            }
            // §13.18.40.3 SR36 (cs and '+' only left of the decimal point position) is subsumed by the Table 11
            // position checks above — a cs or '+' past position 0/1 is rejected there, dot or no dot.
        }
        if (plus > 1 || dots > 1 || csCount > 1)
            return Bad("each of the symbols '+', '.', the currency symbol may appear only once in character-string-1 (ISO §13.18.40.3 SR34)");
        int digits = digitsLeft + digitsRight;
        if (digits == 0)
            return Bad("character-string-1 shall contain at least one of the symbols 'Z' or '9' (ISO §13.18.40.3 SR33)");
        if (digits > 31)
            return Bad($"character-string-1 describes {digits} digit positions — the number shall range from 1 through 31 (ISO §13.18.40.3 SR35)");
        // ⛔ THE §13.18.60.3 USAGE × PICTURE SCREEN, asked through the ONE function (kb/Work PB646) — §13.18.40.4
        // GR1 admits USAGE NATIONAL for format 2 (each position a national character position) and SR12 admits a
        // numeric-edited picture, so there is ONE national posture, never a locale-specific fork. This arm too
        // used to carry only a hand-written SR12 staging, leaving SR3 unasked of a format-2 picture.
        usage = ScreenUsageAgainstPicture(PicCategory.NumericEdited, usage, explicitUsage, picture, edition, where);
        return new PicInfo(PicCategory.NumericEdited, usage, Length: locale2.Size, Digits: digits,
            Scale: digitsRight, Signed: plus > 0)
        { DigitPositions = digits, LocaleEdit = locale2 with { Picture = canonical } };
    }

}
