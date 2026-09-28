// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Common;
using Microsoft.CodeAnalysis.CSharp;

namespace CobolNet.CodeGen;

/// <summary>
/// THE figurative-constant service (P7 Step 4; DESIGN-codegen-backend §2.5 rationale #6): the ONE
/// word-recognition table and the ONE fill-character computation, consolidating the divergent copies
/// (<c>EmitText.FigurativeFill</c>, <c>EmitContext.FigFill</c> ×2, <c>FieldEmitter.FillCharFor</c>,
/// <c>ConditionRenderer.FigurativeFillChar</c>, <c>CSharpEmitter.FigurativeWordFill</c>, and the Report-Writer
/// word map). ISO §8.3.3.6: HIGH-/LOW-VALUE under a PROGRAM COLLATING SEQUENCE are the sequence's extreme
/// CHARACTERS (GR6/GR7 + §12.3.7 GR8/GR9 — character identity, not just comparison weight), natively
/// U+00FF/U+0000 (COBOLNET_DESIGN §14.9); category national/boolean use their OWN sequence, so the alphanumeric
/// PCS never applies to them (the D-N3 pin). It is also THE OPERAND CLASSIFIER (<see cref="Classify"/>, kb/Work
/// PB461): the five ALL-strip variants that once lived at the call sites (whitespace-only, whitespace-or-letter,
/// glued-only, unconditional) each recognized a DIFFERENT subset of the spellings §8.3.3.6.2 admits, so the SET
/// store, the VALUE initializer and the condition TEST disagreed about the same operand text.
/// </summary>
internal static class FigurativeConstants
{
    /// <summary>The §8.3.3.6.2 FORM of one VALUE-clause / level-88 operand text: <paramref name="Kind"/> is the
    /// Formats 1–5 figurative word (see <see cref="KindOf"/>) and <paramref name="AllLiteral"/> the Format-6
    /// <c>ALL literal-1</c> operand's literal-1, still PREFIXED so <see cref="CobolLiteral.ClassOf"/> and
    /// <see cref="CobolLiteral.Decode"/> apply to it. At most one is non-null; both null means an ordinary
    /// literal, which every caller stores or compares verbatim.</summary>
    public readonly record struct FigurativeOperand(char? Kind, string? AllLiteral);

    /// <summary>⛔ THE ONE READER of a VALUE-clause / level-88 VALUE operand's figurative form (ISO §8.3.3.6.2;
    /// kb/Work PB461). §14.9.39.4 GR6 stores a <c>SET condition-name TO TRUE</c> value "<i>according to the rules
    /// for the VALUE clause</i>", and §8.8.4.5.3 GR2 compares the condition against that same value, so the store,
    /// the initializer and the test are required to read the operand IDENTICALLY — they did not, and a
    /// <c>SET</c> whose whole purpose is to make a condition true stored the parse text <c>ALL"*"</c> and left
    /// its own condition FALSE.
    /// <para>In Formats 1–5 the word <c>ALL</c> is an OPTIONAL word (the printed diagrams underline only
    /// <c>ZERO</c>/<c>SPACE</c>/…, never <c>ALL</c>), so <c>ALL SPACES</c> IS the Format-2 constant and takes the
    /// §8.3.3.6.4 GR2 fill; in Format 6 <c>ALL</c> is REQUIRED and underlined, and literal-1 is repeated to the
    /// receiver's size by that same GR2. The parse tree's <c>GetText()</c> glues the two words, so the operand
    /// arrives spelled <c>ALLSPACES</c> / <c>ALL"*"</c> — BOTH spellings are accepted here, once, because a
    /// strip that admits only one of them is a rule that fires for half its inputs.</para></summary>
    /// <param name="includeNull">Whether the pointer figurative NULL/NULLS is admitted as a character fill —
    /// see <see cref="KindOf"/>. The VALUE/level-88 value producers pass true (the historical map); the
    /// §13.18.63.3 class validator and the SR9 implied-picture reader pass false, because NULL names no
    /// character class and implies no picture.</param>
    public static FigurativeOperand Classify(string raw, bool includeNull = true)
    {
        string t = raw.Trim();
        bool hasAll = t.Length > 3 && t.StartsWith("ALL", StringComparison.OrdinalIgnoreCase);
        string rest = hasAll ? t[3..].TrimStart() : t;
        if (KindOf(rest, includeNull) is { } k) return new(k, null);                  // Formats 1–5 (ALL optional)
        if (hasAll && CobolLiteral.IsStringLiteral(rest)) return new(null, rest);     // Format 6 (ALL required)
        return default;
    }

    /// <summary>The figurative KIND of a figurative-constant word (already ALL-stripped by the caller), or
    /// <see langword="null"/> when the text is not a figurative word: Z(ERO/S/ES), S(PACE/S), Q(UOTE/S),
    /// H(IGH-VALUE/S), L(OW-VALUE/S), and — where the caller's context admits the pointer figurative as a
    /// character fill (<paramref name="includeNull"/>, ISO §8.3.3.6.4 — the VALUE-init and SET-TO-TRUE word
    /// maps historically accept NULL/NULLS as LOW-VALUE) — N(ULL/S).</summary>
    public static char? KindOf(string word, bool includeNull = false) => word.ToUpperInvariant() switch
    {
        "ZERO" or "ZEROS" or "ZEROES" => 'Z',
        "SPACE" or "SPACES" => 'S',
        "QUOTE" or "QUOTES" => 'Q',
        "HIGH-VALUE" or "HIGH-VALUES" => 'H',
        "LOW-VALUE" or "LOW-VALUES" => 'L',
        "NULL" or "NULLS" when includeNull => 'L',   // fills like LOW-VALUE (the FieldEmitter/SET-TO-TRUE maps)
        _ => null,
    };

    /// <summary>The RAW fill character for a figurative kind: the PCS extremes for H/L when a collating
    /// sequence is active and no category pin applies. National/boolean anchors never take the alphanumeric
    /// PCS (§8.3.3.6 GR6/GR7 — each category reads its OWN sequence): a NATIONAL anchor under an explicit
    /// non-native NATIONAL program collating sequence (<paramref name="natCollate"/> — an <c>ALPHABET … FOR
    /// NATIONAL</c> literal phrase, §12.3.7 GR8/GR9) takes THAT sequence's extremes; otherwise the D-N3
    /// pins (U+00FF/U+0000 — the flagged native-pin divergence stays byte-stable).
    /// <para>⛔ <b>DETERMINATION D-B2 — the figurative SPACE against a BOOLEAN receiving operand is the boolean
    /// character '0'</b> (CONFORMANCE.md §3; kb/Work PB425). The standard leaves this undefined and the gap is
    /// reachable: §14.9.25.4 Table 17 gives SPACE against a Boolean receiving operand the category BOOLEAN, but
    /// §8.3.3.6.4 GR5 defines the space format only as "one or more of the character space in the computer's
    /// runtime coded character set" and names no boolean value, while §8.3.3.4.3 SR2 admits only '0' and '1' in a
    /// boolean item. §14.9.25.4 GR2's substitution reaches it — <c>MOVE "" TO PIC 1(4)</c> is legal (Table 16
    /// admits an alphanumeric sender into a boolean receiver, and §14.9.25.3 SR7's prohibition names figurative
    /// constants WRITTEN in the source, which a zero-length literal is not) — so a choice is required. The
    /// boolean zero is taken because it is the only boolean character the standard associates with a fill:
    /// §14.6.8.6 aligns a boolean receiver "with zero fill or truncation to the right", and §8.3.3.6.4 GR4 gives
    /// the ZERO format "one or more of the boolean character '0'". It is also the answer GR3 already gives a
    /// boolean zero-length literal, so the two GR arms agree.</para></summary>
    public static char FillChar(char kind, AlphabetDef? collate, PicCategory? cat = null,
        NationalAlphabetDef? natCollate = null)
    {
        bool pinned = cat is PicCategory.National or PicCategory.Boolean;
        bool nat = cat is PicCategory.National && natCollate is not null;
        return kind switch
        {
            'Z' => '0',
            'S' when cat is PicCategory.Boolean => '0',   // DETERMINATION D-B2 — see the remarks above
            'S' => ' ',
            'Q' => '"',
            'H' when nat => natCollate!.HighValue,
            'L' when nat => natCollate!.LowValue,
            'H' => !pinned && collate is { } hc ? hc.HighValue : 'ÿ',
            'L' => !pinned && collate is { } lc ? lc.LowValue : '\0',
            _ => ' ',
        };
    }

    /// <summary>The C# <c>char</c>-literal FRAGMENT a figurative kind fills with. BYTE-IDENTITY NOTE (the 32
    /// characterization snapshots gate this): a PCS-ACTIVE H/L renders through Roslyn's
    /// <see cref="SymbolDisplay.FormatLiteral(char, bool)"/> (e.g. <c>'F'</c>) — exactly the former
    /// <c>EmitContext.FigFill</c>; the native pins keep the FIXED historical escape texts
    /// (<c>'ÿ'</c>/<c>'\0'</c>, never re-rendered) — exactly the former
    /// <c>EmitText.FigurativeFill</c>. A NATIONAL anchor under an explicit non-native NATIONAL PCS
    /// (<paramref name="natCollate"/>) renders THAT sequence's extremes (§12.3.7 GR8/GR9 — a new surface,
    /// no byte-identity constraint).</summary>
    public static string Fill(char kind, AlphabetDef? collate, PicCategory? cat = null,
        NationalAlphabetDef? natCollate = null)
    {
        bool pinned = cat is PicCategory.National or PicCategory.Boolean;
        bool nat = cat is PicCategory.National && natCollate is not null;
        return kind switch
        {
            'H' when nat => SymbolDisplay.FormatLiteral(natCollate!.HighValue, quote: true),
            'L' when nat => SymbolDisplay.FormatLiteral(natCollate!.LowValue, quote: true),
            'H' when !pinned && collate is { } hc => SymbolDisplay.FormatLiteral(hc.HighValue, quote: true),
            'L' when !pinned && collate is { } lc => SymbolDisplay.FormatLiteral(lc.LowValue, quote: true),
            'Z' => "'0'",
            'S' when cat is PicCategory.Boolean => "'0'",   // DETERMINATION D-B2 — see FillChar's remarks
            'S' => "' '",
            'H' => "'\\u00ff'",
            'L' => "'\\u0000'",
            'Q' => "'\\\"'",
            _ => "' '",
        };
    }

    /// <summary>The C# <c>string</c>-literal FRAGMENT holding ONE occurrence of a figurative kind's fill
    /// character — the SEED a §8.3.3.6.4 GR2 sizing repeats (kb/Work PB297). Same fill computation as
    /// <see cref="FillChar"/>, rendered as a string rather than a char, so the relation renderer can hand the
    /// figurative to <c>CobolString.CompareFig</c> without knowing its length: GR3 b) already makes ONE
    /// character the figurative's own length wherever no associated data item sizes it, so this fragment is
    /// both the seed and the length-unspecified value. Rendered through
    /// <see cref="SymbolDisplay.FormatLiteral(string, bool)"/>, which escapes the control characters the
    /// HIGH-/LOW-VALUE pins are (a raw NUL in emitted C# is the DEVLOG 788/790 hazard).</summary>
    public static string FillText(char kind, AlphabetDef? collate, PicCategory? cat = null,
        NationalAlphabetDef? natCollate = null) =>
        SymbolDisplay.FormatLiteral(FillChar(kind, collate, cat, natCollate).ToString(), quote: true);
}
