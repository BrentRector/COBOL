// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Editions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE TWO CLOSURE PROPERTIES OF <see cref="PictureAnalyzer"/>, MEASURED OVER THE WHOLE FORMAT-1 SYMBOL
/// ALPHABET RATHER THAN OVER A LIST OF CASES (kb/Work PB535).
/// <para>Every picture character-string the compiler ACCEPTS shall satisfy both:</para>
/// <list type="number">
///   <item><b>ISO §13.18.40.4 GR3 — it defines a category</b>, and the category the analyzer assigned is the one
///     whose defining general rule (GR5 alphabetic, GR6 alphanumeric, GR7 alphanumeric-edited, GR8 boolean,
///     GR9 national, GR10 national-edited, GR11 fixed-point numeric, GR13 numeric-edited) the character-string
///     actually satisfies. The defect this pins is the FALL-THROUGH: <c>Analyze</c> reaches "pure numeric" by
///     exhaustion, so before the composition validator existed <c>PIC S</c>, <c>PIC PPP</c>, <c>PIC SV</c> and
///     <c>PIC SPPP</c> each bound a ZERO-LENGTH category-numeric item at every edition, silently.</item>
///   <item><b>ISO §13.18.40.4 GR4 — its size is the symbol count</b>: "the number of symbols in character-string-1
///     that represent either boolean positions or character positions". GR14 names the three symbols that
///     represent none — 'P' ("not counted in the size of the item"), 'S' (counted "only when the subject of the
///     entry is described with a SIGN clause with the SEPARATE phrase") and 'V' ("not counted in the size of the
///     item") — so with no SIGN SEPARATE and a one-character currency string the size is exactly the count of
///     symbols outside {'P','S','V'}, for EVERY category. The defect this pins is a category arm spelling that
///     count as its own whitelist: the alphanumeric arm counted <c>X A 9</c> plus the insertion symbols, so any
///     symbol outside that list vanished from the size instead of the picture being rejected — <c>PIC XX,XX</c>
///     bound LENGTH 4, <c>PIC XXCR</c> bound 2 — and an item SHORTER than its picture shifts every following
///     member of a group image.</item>
/// </list>
/// <para>⛔ Both are stated as properties of the ACCEPTED set, which is what makes the next symbol automatic: a
/// prohibition relaxed in <see cref="PictureComposition"/>, or a category arm widened here, changes the accepted
/// set and is measured against the standard's own rules rather than against a list someone remembered to
/// extend. Rejection is not asserted — that is <see cref="PictureCompositionTests"/>'s job; this says only that
/// whatever survives is well-defined.</para>
/// </summary>
public sealed class PictureCategoryDriftTests
{
    /// <summary>The Format-1 picture symbols of ISO §13.18.40.3 SR2 / §13.18.40.6 Table 10, one representative of
    /// each row: the two-character symbols 'CR' and 'DB' enter through <see cref="LongerShapes"/> because an
    /// enumeration over single characters cannot spell them.</summary>
    private const string Alphabet = "AXN91ZP SV0B/,.+-*$";

    /// <summary>Shapes an exhaustive 1..3 walk cannot reach: the two-character editing sign control symbols, the
    /// floating strings (§13.18.40.5 rule 6 needs two adjacent occurrences), the repeat form, the currency-string
    /// widening of GR14 and the floating-point form of GR13 b.</summary>
    private static readonly string[] LongerShapes =
    [
        "999CR", "999DB", "$$,$$9.99", "++++9", "----9", "**,**9.99", "ZZ,ZZ9.99-",
        "X(12)", "9(18)", "S9(9)V9(9)", "PPP999", "999PPP", "N(4)", "1(8)", "AAAABAAAA",
        "XXBXX/XX", "NNBNN", "9(5)E+99", "ZZZ.ZZ", "$999.99CR", "+$$$,$$9.99", "9,999.99",
    ];

    private static (PicInfo Pic, string[] Diagnostics) Analyze(string picture, bool blankWhenZero = false)
    {
        var ed = new EditionContext(2023);
        PicInfo pic = PictureAnalyzer.Analyze(picture, Usage.Display, ed, "data item 'T'",
            blankWhenZero: blankWhenZero);
        return (pic, ed.Diagnostics.ToArray());
    }

    /// <summary>The repeat expansion of a character-string this walk has already ACCEPTED, so ISO §13.18.40.3
    /// SR6's factor validation (kb/Work PB531) cannot fail here — the assert is the drift guard that says
    /// so.</summary>
    private static string Expand(string picture)
    {
        var ed = new EditionContext(2023);
        Assert.True(PictureAnalyzer.TryExpandRepeats(picture, ed, "data item 'T'", out string expanded),
            $"PIC {picture} was accepted by Analyze but its repetition factors do not expand");
        return expanded;
    }

    /// <summary>Every accepted character-string of one, two or three symbols, plus the shapes that need more.
    /// 19^1 + 19^2 + 19^3 = 7 239 strings walked; the accepted ones are what the two properties bind.</summary>
    public static IEnumerable<string> EveryShortPicture()
    {
        foreach (char a in Alphabet)
        {
            if (a == ' ') continue;
            yield return a.ToString();
            foreach (char b in Alphabet)
            {
                if (b == ' ') continue;
                yield return string.Concat(a, b);
                foreach (char c in Alphabet)
                {
                    if (c == ' ') continue;
                    yield return string.Concat(a, b, c);
                }
            }
        }
    }

    /// <summary>⛔ THE POPULATION GUARD. Both properties below are stated over the ACCEPTED set and say nothing
    /// about the rejected one, so a walk that accepted nothing — <c>Analyze</c> throwing, the alphabet
    /// mistyped, the composition validator widened into a blanket refusal — would pass both of them while
    /// measuring no picture at all. This asserts what the walk SAW: every one of ISO 13.18.40.4 GR3's eight
    /// categories is represented among the accepted strings, and the accepted count is in the thousands.</summary>
    [Fact]
    public void TheWalk_AcceptsEveryOneOfTheEightCategories()
    {
        var seen = new SortedDictionary<string, int>();
        int accepted = 0;
        foreach (string picture in EveryPicture())
        {
            var (pic, diags) = Analyze(picture);
            if (diags.Length > 0) continue;
            accepted++;
            seen[CategoryName(pic)] = seen.TryGetValue(CategoryName(pic), out int n) ? n + 1 : 1;
        }
        string histogram = string.Join(", ", seen.Select(kv => $"{kv.Key}={kv.Value}"));
        Assert.True(accepted >= 1000, $"the walk accepted only {accepted} picture character-strings ({histogram})");
        foreach (string category in EightCategories)
            Assert.True(seen.ContainsKey(category),
                $"no accepted picture character-string bound category {category}: {histogram}");
    }

    /// <summary>ISO 13.18.40.4 GR3's eight categories, named as <see cref="CategoryName"/> spells them.</summary>
    private static readonly string[] EightCategories =
    [
        "alphabetic", "alphanumeric", "alphanumeric-edited", "boolean",
        "national", "national-edited", "numeric", "numeric-edited",
    ];

    private static string CategoryName(PicInfo pic) => pic.Category switch
    {
        PicCategory.Alphanumeric when pic.IsAlphabetic => "alphabetic",
        PicCategory.Alphanumeric when pic.EditMask is not null => "alphanumeric-edited",
        PicCategory.Alphanumeric => "alphanumeric",
        PicCategory.National when pic.EditMask is not null => "national-edited",
        PicCategory.National => "national",
        PicCategory.Boolean => "boolean",
        PicCategory.Numeric => "numeric",
        PicCategory.NumericEdited => "numeric-edited",
        _ => pic.Category.ToString(),
    };

    /// <summary>The whole walk: every short character-string plus the shapes three symbols cannot spell.</summary>
    private static IEnumerable<string> EveryPicture() => EveryShortPicture().Concat(LongerShapes);

    [Fact]
    public void EveryAcceptedPicture_DefinesTheCategoryItsGeneralRuleDefines()
    {
        var wrong = new List<string>();
        foreach (string picture in EveryPicture())
        {
            var (pic, diags) = Analyze(picture);
            if (diags.Length > 0) continue;               // rejected, or staged — not part of the accepted set
            string? why = CategoryAntecedent(pic, Expand(picture));
            if (why is not null) wrong.Add($"PIC {picture} → {Describe(pic)}: {why}");
        }
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} accepted picture character-string(s) bind a category whose defining general rule "
            + $"(ISO §13.18.40.4 GR5-GR13) they do not satisfy:{Environment.NewLine}"
            + string.Join(Environment.NewLine, wrong.Take(40)));
    }

    [Fact]
    public void EveryAcceptedPicture_HasTheSizeGeneralRule4Gives()
    {
        var wrong = new List<string>();
        foreach (string picture in EveryPicture())
        {
            var (pic, diags) = Analyze(picture);
            if (diags.Length > 0) continue;
            string expanded = Expand(picture);
            // §13.18.40.4 GR4 with GR14: every symbol represents a character position except 'P', 'V', and 'S'
            // without SIGN SEPARATE. The currency string is the default one character here, so GR14's
            // "the first occurrence adds the number of characters in the currency string" adds nothing.
            int expect = expanded.Count(c => c is not ('P' or 'S' or 'V'));
            if (pic.Length != expect)
                wrong.Add($"PIC {picture} → {Describe(pic)}: size {pic.Length}, GR4 gives {expect}");
        }
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} accepted picture character-string(s) have a size other than ISO §13.18.40.4 GR4's "
            + $"count of the symbols representing a character position:{Environment.NewLine}"
            + string.Join(Environment.NewLine, wrong.Take(40)));
    }

    // ── The PICTURE EDITING walk (kb/Work PB3220, PB3219) ────────────────────────────────────────────────────────
    // The two properties above ran with NO EDITING phrase, which is why neither saw that the 'es' widening of GR14
    // read every FOR rule as floating (PIC L999L bound LENGTH 7 for an item that renders 9 characters), nor that
    // SR12 b read a literal period under DECIMAL-POINT IS COMMA. Both are stated here over the ACCEPTED set of a
    // symbol walk, with the literal width, the phrase form and the decimal-point mode as the walked dimensions.

    /// <summary>The symbols of an editing walk: the extended character-1 'L', the digit, the two separators, and
    /// a zero-suppression symbol (FOR admits it, SR12 b; IS admits every Format-1 symbol).</summary>
    private const string EditingAlphabet = "L9.,Z";

    private const int EditingWalkLength = 5;

    /// <summary>Every string of one to <see cref="EditingWalkLength"/> symbols of <see cref="EditingAlphabet"/>.</summary>
    public static IEnumerable<string> EveryEditingShape()
    {
        var level = new List<string> { "" };
        for (int len = 1; len <= EditingWalkLength; len++)
        {
            var next = new List<string>(level.Count * EditingAlphabet.Length);
            foreach (string prefix in level)
                foreach (char c in EditingAlphabet)
                    next.Add(prefix + c);
            foreach (string s in next) yield return s;
            level = next;
        }
    }

    private static (PicInfo Pic, string[] Diagnostics) AnalyzeEditing(string picture, bool forForm, int literalWidth,
        bool decimalPointIsComma)
    {
        var ed = new EditionContext(2023);
        string text = new string(':', literalWidth);
        var literal = new EditLiteral(text, EditLiteralClass.Alphanumeric);
        var phrase = forForm
            ? new EditingPhraseSpec("L", null, literal, null, IsForForm: true)
            : new EditingPhraseSpec("L", literal, null, null, IsForForm: false);
        PicInfo pic = PictureAnalyzer.Analyze(picture, Usage.Display, ed, "data item 'T'", editing: [phrase],
            decimalPointIsComma: decimalPointIsComma);
        return (pic, ed.Diagnostics.ToArray());
    }

    /// <summary>⛔ ISO §13.18.40.4 GR4 + GR14 'es', over the EDITING walk: the size the analyzer binds is the
    /// width of the image the editing rules render, for the IS form (the literal at every occurrence), for a FOR
    /// form with FIXED occurrences (the literal at every occurrence) and for a FOR form's FLOATING string (one
    /// literal plus one character per repetition). The renderer is the oracle here because GR14 states the size
    /// as a property of the image: "the size of literal-1 is counted in the size of the item".</summary>
    [Fact]
    public void EveryAcceptedEditingPicture_HasTheSizeOfItsRenderedImage()
    {
        var wrong = new List<string>();
        int accepted = 0, fixedRepeated = 0, floating = 0, comma = 0, mixed = 0;
        foreach (string picture in EveryEditingShape())
            foreach (bool forForm in new[] { true, false })
                foreach (int width in new[] { 1, 3 })
                    foreach (bool dpc in new[] { false, true })
                    {
                        var (pic, diags) = AnalyzeEditing(picture, forForm, width, dpc);
                        if (diags.Length > 0 || pic.EditMask is null || pic.EditingRules is not { Count: > 0 } rules) continue;
                        accepted++;
                        if (dpc && picture.Contains(',')) comma++;
                        if (rules.Count > 1) mixed++;   // a floating string AND a fixed occurrence of one character-1
                        if (rules[0].Floating) floating++;
                        else if (forForm && picture.Count(c => c == 'L') >= 2) fixedRepeated++;
                        foreach (int value in new[] { -5, 5 })
                        {
                            string image = CobolNet.Runtime.CobolEdit.Format(value, 0, pic.EditMask, commaMode: dpc,
                                edits: [.. rules]);
                            if (image.Length != pic.Length)
                                wrong.Add($"PIC {picture} {(forForm ? "FOR" : "IS")} width {width}{(dpc ? " DPC" : "")} "
                                    + $"value {value}: size {pic.Length}, image \"{image}\" is {image.Length}");
                        }
                    }
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} accepted EDITING picture(s) bind a size other than the width of the image they render "
            + $"(ISO §13.18.40.4 GR4, GR14 'es'):{Environment.NewLine}" + string.Join(Environment.NewLine, wrong.Take(40)));
        // The population guard: the walk must have SEEN the shapes the property exists for.
        Assert.True(fixedRepeated > 0 && floating > 0 && comma > 0 && mixed > 0,
            $"the editing walk accepted {accepted} but saw {fixedRepeated} repeated-fixed, {floating} floating, "
            + $"{comma} DECIMAL-POINT IS COMMA comma pictures, {mixed} mixed floating-and-fixed");
    }

    /// <summary>⛔ ISO §13.18.40.4 GR14 'es' with a floating string AND a fixed occurrence of ONE character-1
    /// (<c>PIC LL9L</c>; kb/Work PB3220, review round 1). The oracle is GR14's own arithmetic, written out per
    /// occurrence and independent of the renderer the walk above compares against: the floating string is one
    /// literal plus one character per repetition, each fixed occurrence is the literal, every other symbol is one
    /// position. The digit positions are rule 6's: the floating string's occurrences after the first, plus the 9s.</summary>
    [Theory]
    [InlineData("LL9L", 3, 8, 2)]      // (3 + 1) + 1 + 3
    [InlineData("LLL9L", 3, 9, 3)]     // (3 + 2) + 1 + 3
    [InlineData("LLL9L", 2, 7, 3)]     // (2 + 2) + 1 + 2
    [InlineData("LL9.99L", 3, 11, 4)]  // (3 + 1) + 1 + 1 + 2 + 3
    public void AFloatingStringAndAFixedOccurrenceOfOneCharacter1_AreSizedEachByItsOwnKind(
        string picture, int literalWidth, int size, int digits)
    {
        var (pic, diags) = AnalyzeEditing(picture, forForm: true, literalWidth, decimalPointIsComma: false);
        Assert.Empty(diags);
        Assert.Equal(size, pic.Length);
        Assert.Equal(digits, pic.DigitPositions);
        Assert.Equal(size, CobolNet.Runtime.CobolEdit.Format(-5, 0, pic.EditMask!, commaMode: false,
            edits: [.. pic.EditingRules!]).Length);
    }

    /// <summary>The same picture with the roles of comma and period interchanged.</summary>
    private static string SwapSeparators(string picture) =>
        string.Concat(picture.Select(c => c switch { '.' => ',', ',' => '.', _ => c }));

    private static string Fingerprint(PicInfo pic, string[] diagnostics) =>
        string.Join(" ", diagnostics.Select(d => System.Text.RegularExpressions.Regex.Match(d, @"COBOLNET\d+").Value).Order())
        + $"|{pic.Category}|{pic.Length}|{pic.Digits}|{pic.Signed}|{pic.IsFloatEdited}";

    /// <summary>The floating-point and sign-bearing shapes a short walk over the plain alphabet cannot spell.</summary>
    private static readonly string[] SeparatorShapes =
    [
        "+9.9.9E+99", "+9,9,9E+99", "+9,9.9E+99", "+9.9,9E+99", "9.999E+99", "9,999E+99", "-9,9(5)E+99", "-9.9(5)E+99",
        "9.9.9.9E+99", "9,9,9,9E+99", "9.99,99E+99", "9,99.99E+99", "9.99E+99", "9,99E+99", "$$$.$$9,99", "$$$,$$9.99",
    ];

    /// <summary>⛔ ISO §13.18.40.3 SR13 as a CLOSURE property, over the plain walk, the float-edited shapes and the
    /// EDITING walk: "The rules for the symbol period apply to the symbol comma, and the rules for the symbol comma
    /// apply to the symbol period" - so a character-string under DECIMAL-POINT IS COMMA is accepted, and means what
    /// it means, exactly when the string with the two symbols interchanged is accepted and means the same without
    /// the clause. A rule that reads a literal period (SR12 b's FOR-form screen and its only-once rule) breaks it.</summary>
    [Fact]
    public void DecimalPointIsComma_InterchangesTheRolesOfPeriodAndComma_InEveryRule()
    {
        var wrong = new List<string>();
        int compared = 0;
        void Compare(string label, string picture, Func<string, bool, (PicInfo, string[])> analyze)
        {
            var (comma, commaDiags) = analyze(picture, true);
            var (period, periodDiags) = analyze(SwapSeparators(picture), false);
            compared++;
            string a = Fingerprint(comma, commaDiags), b = Fingerprint(period, periodDiags);
            if (a != b) wrong.Add($"{label} PIC {picture} under DECIMAL-POINT IS COMMA: {a}; swapped {SwapSeparators(picture)} without: {b}");
        }
        const string plain = "9Z.,+-VP";
        var shapes = new List<string>(SeparatorShapes);
        foreach (char a in plain)
        {
            shapes.Add(a.ToString());
            foreach (char b in plain)
            {
                shapes.Add(string.Concat(a, b));
                foreach (char c in plain)
                {
                    shapes.Add(string.Concat(a, b, c));
                    foreach (char d in plain) shapes.Add(string.Concat(a, b, c, d));
                }
            }
        }
        foreach (string picture in shapes)
            Compare("plain", picture, (p, dpc) =>
            {
                var ed = new EditionContext(2023);
                return (PictureAnalyzer.Analyze(p, Usage.Display, ed, "data item 'T'", decimalPointIsComma: dpc),
                    ed.Diagnostics.ToArray());
            });
        foreach (string picture in EveryEditingShape())
            foreach (bool forForm in new[] { true, false })
                Compare(forForm ? "FOR" : "IS", picture, (p, dpc) => AnalyzeEditing(p, forForm, 1, dpc));
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} of {compared} picture(s) are not interchanged by SR13:{Environment.NewLine}"
            + string.Join(Environment.NewLine, wrong.Take(40)));
        Assert.True(compared > 10_000, $"the SR13 walk compared only {compared} pictures");
    }

    private static string Describe(PicInfo pic) =>
        $"{CategoryName(pic)} len {pic.Length} digits {pic.Digits}";

    /// <summary>The defining general rule of the category the analyzer assigned, asked of the character-string:
    /// null when it holds, else the sentence it fails. The EDITING character-1 set is empty here (no PICTURE
    /// EDITING phrase is passed), so the GR7 / GR10 insertion set is exactly 'B', '0', '/'.</summary>
    private static string? CategoryAntecedent(PicInfo pic, string s)
    {
        bool Insertion(char c) => c is 'B' or '0' or '/';
        switch (pic.Category)
        {
            case PicCategory.Alphanumeric when pic.IsAlphabetic:
                // GR5 — "To define an item as alphabetic, character-string-1 shall contain only one or more
                // occurrences of the symbol 'A'."
                return s.All(c => c == 'A') ? null : "GR5 admits only the symbol 'A' for category alphabetic";
            case PicCategory.Alphanumeric when pic.EditMask is not null:
                // GR7 — at least one 'A' or 'X', AND at least one instance of character-1 or one of 'B', '0', '/'.
                return s.Any(c => c is 'A' or 'X') && s.Any(Insertion)
                    ? null
                    : "GR7 needs at least one 'A' or 'X' and at least one of 'B', '0', '/'";
            case PicCategory.Alphanumeric:
                // GR6 — a combination of symbols from the set 'A', 'X', '9' including at least one 'X' or at
                // least two different symbols from that set.
                if (!s.All(c => c is 'A' or 'X' or '9'))
                    return "GR6 admits only the symbols 'A', 'X' and '9' for category alphanumeric";
                return s.Contains('X') || s.Distinct().Count() >= 2
                    ? null
                    : "GR6 needs at least one 'X' or at least two different symbols from 'A', 'X', '9'";
            case PicCategory.Boolean:
                // GR8 — only one or more occurrences of the symbol '1'.
                return s.All(c => c == '1') ? null : "GR8 admits only the symbol '1' for category boolean";
            case PicCategory.National when pic.EditMask is not null:
                // GR10 — at least one 'N', and at least one instance of character-1 or one of 'B', '0', '/'.
                return s.Contains('N') && s.Any(Insertion)
                    ? null
                    : "GR10 needs at least one 'N' and at least one of 'B', '0', '/'";
            case PicCategory.National:
                // GR9 — only one or more occurrences of the symbol 'N'.
                return s.All(c => c == 'N') ? null : "GR9 admits only the symbol 'N' for category national";
            case PicCategory.Numeric:
                // GR11 — at least one symbol '9', and a combination of symbols from the set 'P', 'S', 'V'.
                if (!s.Contains('9')) return "GR11 needs at least one symbol '9' for category numeric";
                return s.All(c => c is '9' or 'P' or 'S' or 'V')
                    ? null
                    : "GR11 admits beside the '9's only the symbols 'P', 'S' and 'V'";
            case PicCategory.NumericEdited:
                // GR13 a — at least one 'Z'; or at least one '*'; or at least two identical symbols from
                // '+', '-', currency symbol; or at least one '9' and at least one instance of character-1 or of
                // 'B', 'CR', 'DB', '0', '/', ',', '.', '+', '-', the currency symbol. GR13 b is the 'E' form.
                // A BLANK WHEN ZERO clause defines the item as numeric-edited too (GR3's closing sentence), but
                // this walk passes no such clause.
                if (s.Contains('E')) return null;
                if (s.Contains('Z') || s.Contains('*')) return null;
                if ("+-$".Any(c => s.Count(x => x == c) >= 2)) return null;
                return s.Contains('9') && s.Any(c => Insertion(c) || c is ',' or '.' or '+' or '-' or '$'
                                                     || c is 'C' or 'R' or 'D' or 'B')
                    ? null
                    : "GR13 a defines no numeric-edited item for this character-string";
            default:
                return $"category {pic.Category} is not one of ISO §13.18.40.4 GR3's eight";
        }
    }
}
