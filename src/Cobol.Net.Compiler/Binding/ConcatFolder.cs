// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// THE §8.8.3 concatenation-expression fold (P10 Step 14). A concatenation expression joins two or more
/// literals / figurative constants with the <c>&amp;</c> operator (§8.7.3); §8.8.3.3 GR3 makes the result
/// "equivalent to a literal of the same class and value, [usable] anywhere a literal of that class may be
/// used" — so the construct is a COMPILE-TIME literal, not a runtime operator: this folder collapses the
/// parse-tree <c>concatenationExpression</c> into the single equivalent literal ONCE, and every consumer
/// (statement operands, VALUE clauses, FUNCTION arguments, CALL/STOP/ALPHABET literal slots, …) then rides
/// the pre-existing single-literal channels — there is no <c>BoundConcat</c> node and no emitter leg, by
/// GR3's own equivalence. (The parse tree KEEPS the concat shape so the VersionConformancePass parse arm can
/// gate the construct on recognition — concat-operator-2002, COBOLNET0900 below 2002.)
/// </summary>
/// <remarks>
/// Rules enforced here, one diagnostic code per rule (DiagnosticCatalog):
/// <list type="bullet">
/// <item>§8.8.3.2 SR1 first sentence — both operands the same class (alphanumeric — incl. the X"…" hex format
/// of the alphanumeric literal, §8.3.3.2 —, boolean, or national); a figurative constant adapts to the other
/// operand's class (GR1a), both-figurative ⇒ alphanumeric (GR1b) → <c>COBOLNET1540</c>.</item>
/// <item>§8.8.3.2 SR1 second sentence — neither operand a figurative constant beginning with ALL →
/// <c>COBOLNET1541</c>.</item>
/// <item>§8.8.3.2 SR2–SR4 — the resulting value at most 8,191 character positions (per class) →
/// <c>COBOLNET1545</c>.</item>
/// </list>
/// A figurative constant inside a concatenation expression is ONE character (§8.3.3.6.4 GR3a): ZERO '0',
/// SPACE ' ', QUOTE '"', and the HIGH-VALUE/LOW-VALUE collating extremes — the program collating sequence's
/// characters when a PCS is active (§8.3.3.6.3 F4/F5 + §12.3.7 GR8/GR9), else the native U+00FF/U+0000 pins;
/// class national never takes the alphanumeric PCS — it reads its OWN sequence (the explicit national PCS
/// extremes when declared, else the D-N3 pin; same posture as <c>FigurativeConstants.Fill</c>, the codegen
/// twin of this table). NULL has no character value (§8.3.3.6.3
/// Format 8 — a pointer figurative) and is rejected. In class boolean only ZERO folds (a boolean character is
/// 0 or 1, §8.3.3.4 — SPACE/QUOTE/HIGH/LOW have no boolean value; the §13.18.63 SR10 posture).
/// §8.8.3.3 GR2: the value is the concatenation of the operand values; two zero-length operands fold to a
/// zero-length literal (which falls out of plain string concatenation).
/// </remarks>
internal static class ConcatFolder
{
    /// <summary>The folded equivalent literal (§8.8.3.3 GR3): its class and its decoded character value.</summary>
    public readonly record struct Folded(PicCategory Category, string Value)
    {
        /// <summary>The equivalent literal as RAW source text (re-quoted, embedded delimiters doubled per
        /// §8.3.3.2.3 r3; <c>N"…"</c>/<c>B"…"</c> prefixes per class §8.3.3.5/§8.3.3.4) — the currency of the
        /// text-plumbed paths (DATA-division VALUE capture, ALPHABET/CLASS operands), which store raw literal
        /// text and decode at emit time.</summary>
        public string RawText => Category switch
        {
            PicCategory.National => "N\"" + Value.Replace("\"", "\"\"") + "\"",
            PicCategory.Boolean => "B\"" + Value + "\"",
            _ => "\"" + Value.Replace("\"", "\"\"") + "\"",
        };
    }

    /// <summary>The class of the concatenation expression (§8.8.3.3 GR1) — DIAGNOSTIC-FREE, for routing
    /// predicates that must classify without double-reporting (e.g. the boolean-channel discriminator
    /// <c>ConditionBinder.IsBooleanValueOperand</c>): the first non-figurative operand's class (GR1a — a
    /// figurative constant takes the other operand's class), or alphanumeric when every operand is figurative
    /// (GR1b). An <c>ALL B"…"</c>/<c>ALL "…"</c> figurative classifies by its inner literal (it is rejected in
    /// <see cref="Fold"/> per SR1 regardless).</summary>
    public static PicCategory ClassOf(Core.ConcatenationExpressionContext ctx)
    {
        foreach (var op in ctx.concatOperand())
        {
            if (op.NATLIT() is not null) return PicCategory.National;
            if (op.BOOLLIT() is not null) return PicCategory.Boolean;
            if (op.STRINGLIT() is not null || op.HEXLIT() is not null) return PicCategory.Alphanumeric;
            if (op.figurativeConstant() is { } fig && fig.allLiteral() is { } al)   // ALL literal-1: literal-1's class (kb/Work PB71)
            {
                var first = al.allLiteralOperand()[0];
                if (first.NATLIT() is not null) return PicCategory.National;
                if (first.BOOLLIT() is not null) return PicCategory.Boolean;
                return PicCategory.Alphanumeric;
            }
        }
        return PicCategory.Alphanumeric;   // GR1b — both/all operands figurative ⇒ class alphanumeric
    }

    /// <summary>Fold the literal-1 of an <c>ALL literal-1</c> figurative — one literal or a concatenation of them
    /// (§8.3.3.6.3 SR2) — to its equivalent single literal, DIAGNOSTIC-FREE (the version pass's ALL arm reports a
    /// class mix, a zero-length literal-1 and, through <see cref="ResultLengthViolation"/>, an over-long result, once
    /// per written figurative — this fold runs at several binder sites): the value is the operands' decoded texts concatenated (§8.8.3.3 GR2), the class
    /// the first operand's. The text-plumbed DATA-division paths re-quote it through <see cref="Folded.RawText"/>
    /// (kb/Work PB71 — a VALUE ALL "A" &amp; "B" used to reach the raw-text ALL reader as the source text).</summary>
    public static Folded FoldAll(Core.AllLiteralContext al)
    {
        var ops = al.allLiteralOperand();
        var cat = ops[0].NATLIT() is not null ? PicCategory.National
            : ops[0].BOOLLIT() is not null ? PicCategory.Boolean : PicCategory.Alphanumeric;
        return new Folded(cat, string.Concat(ops.Select(o => CobolLiteral.Decode(o.GetText()))));
    }

    /// <summary>Fold <paramref name="ctx"/> to its equivalent single literal (§8.8.3.3 GR2/GR3), reporting the
    /// §8.8.3.2 syntax-rule violations to <paramref name="edition"/>. <paramref name="collate"/> is the active
    /// ALPHANUMERIC PROGRAM COLLATING SEQUENCE table (its HIGH-VALUE/LOW-VALUE extremes, §8.3.3.6.3 F4/F5), and
    /// <paramref name="natCollate"/> its NATIONAL twin (a non-native ALPHABET … FOR NATIONAL sequence — its
    /// extremes govern HIGH-/LOW-VALUE in a class-national concatenation, §12.3.7 GR8/GR9); null when none.
    /// Always returns a best-effort value so the caller's plumbing continues — on any reported error the
    /// compile has already failed (the driver halts before emit).</summary>
    public static Folded Fold(Core.ConcatenationExpressionContext ctx, EditionContext edition,
        AlphabetDef? collate, NationalAlphabetDef? natCollate = null)
    {
        var cat = ClassOf(ctx);
        var sb = new System.Text.StringBuilder();
        foreach (var op in ctx.concatOperand())
        {
            if (op.STRINGLIT() is { } s)
            {
                if (cat is not PicCategory.Alphanumeric) ClassError(edition, cat, $"alphanumeric literal {s.GetText()}");
                sb.Append(CobolLiteral.Decode(s.GetText()));
            }
            else if (op.HEXLIT() is { } x)
            {
                // X"…" is the hexadecimal FORMAT of the alphanumeric literal (§8.3.3.2) — class alphanumeric.
                if (cat is not PicCategory.Alphanumeric) ClassError(edition, cat, $"alphanumeric (hexadecimal) literal {x.GetText()}");
                sb.Append(CobolLiteral.DecodeHex(x.GetText()));
            }
            else if (op.NATLIT() is { } n)
            {
                if (cat is not PicCategory.National) ClassError(edition, cat, $"national literal {n.GetText()}");
                sb.Append(CobolLiteral.Decode(n.GetText()));
            }
            else if (op.BOOLLIT() is { } b)
            {
                if (cat is not PicCategory.Boolean) ClassError(edition, cat, $"boolean literal {b.GetText()}");
                sb.Append(CobolLiteral.Decode(b.GetText()));
            }
            else if (op.figurativeConstant() is { } fig)
            {
                // §8.8.3.2 SR1 second sentence: neither operand shall be a figurative constant that begins
                // with the word ALL (any ALL form — ALL "lit" / ALL X"…" / ALL B"…" / ALL SPACE …).
                if (fig.ALL() is not null)
                {
                    edition.Error(DiagnosticCatalog.ConcatAllFigurative, $"'{fig.GetText()}': a figurative "
                        + "constant beginning with ALL shall not be a concatenation-expression operand "
                        + "(ISO §8.8.3.2 SR1)");
                    continue;
                }
                // One character per §8.3.3.6.4 GR3a; the character per FigurativeChar.
                if (FigurativeChar(fig, cat, collate, natCollate) is { } c) sb.Append(c);
                else ClassError(edition, cat, cat is PicCategory.Boolean
                    ? $"figurative constant '{fig.GetText()}' (no boolean character value)"
                    : $"figurative constant '{fig.GetText()}' (no character value)");
            }
        }
        var folded = new Folded(cat, sb.ToString());
        if (ResultLengthViolation(folded) is { } tooLong) edition.Error(DiagnosticCatalog.ConcatResultTooLong, tooLong);
        return folded;
    }

    /// <summary>§8.8.3.2 SR2–SR4 — the value a concatenation results in is at most 8,191 character positions of its
    /// class (alphanumeric / boolean / national): the message, or null when <paramref name="folded"/> is within
    /// bounds. ONE statement of the rule for both concatenation forms — the <c>&amp;</c> expression, reported by
    /// <see cref="Fold"/>, and the concatenated ALL literal-1, whose fold is diagnostic-free and whose §8.8.3.2 rules
    /// the version pass's ALL arm reports once per written figurative (kb/Work PB1393: that form had no length check
    /// at all).</summary>
    internal static string? ResultLengthViolation(Folded folded) =>
        folded.Value.Length <= CobolLiteral.MaxLiteralPositions ? null
            : $"the concatenated {Name(folded.Category)} value is {folded.Value.Length} character positions — the "
              + $"maximum is {CobolLiteral.MaxLiteralPositionsText} (ISO §8.8.3.2 SR2–SR4)";

    /// <summary>THE ONE CHARACTER a KEYWORD figurative constant (ZERO · SPACE · QUOTE · HIGH-VALUE · LOW-VALUE ·
    /// NULL, with or without ALL) stands for in class <paramref name="cat"/> wherever its string is one character
    /// long (§8.3.3.6.4 GR3a — in a concatenation expression; GR3b — any figurative other than ALL literal-1
    /// whose context specifies no length, e.g. a PICTURE EDITING literal), or null when it has no character value
    /// in that class. ZERO is '0' (GR4); SPACE ' ' (GR5); QUOTE '"' (GR1); HIGH-/LOW-VALUE are the program
    /// collating sequence's extremes when one is active (§8.3.3.6.4 GR6/GR7 + §12.3.7 GR8/GR9), else the native
    /// U+00FF/U+0000 pins (COBOLNET_DESIGN §14.9) — class national never takes the alphanumeric PCS (D-N3 — the
    /// <c>FigurativeConstants.Fill</c> posture). In class boolean only ZERO has a value (a boolean character is 0
    /// or 1, §8.3.3.4); NULL — the pointer figurative, Format 8 — has none in any class. The ALL literal-1 and
    /// ALL symbolic-character-1 forms are not keyword figuratives and answer null.</summary>
    internal static char? FigurativeChar(Core.FigurativeConstantContext fig, PicCategory cat,
        AlphabetDef? collate, NationalAlphabetDef? natCollate)
    {
        if (fig.zeroWord() is not null) return '0';
        if (cat is PicCategory.Boolean) return null;
        if (fig.spaceWord() is not null) return ' ';
        if (fig.quoteWord() is not null) return '"';
        if (fig.highValueWord() is not null)
            return cat is PicCategory.National ? natCollate?.HighValue ?? '\u00ff' : collate?.HighValue ?? '\u00ff';
        if (fig.lowValueWord() is not null)
            return cat is PicCategory.National ? natCollate?.LowValue ?? '\u0000' : collate?.LowValue ?? '\u0000';
        return null;
    }

    private static void ClassError(EditionContext edition, PicCategory cat, string operand) =>
        edition.Error(DiagnosticCatalog.ConcatClassMismatch, $"{operand} in a concatenation expression of class "
            + $"{Name(cat)} — both operands shall be of the same class (ISO §8.8.3.2 SR1)");

    private static string Name(PicCategory cat) => cat switch
    {
        PicCategory.National => "national",
        PicCategory.Boolean => "boolean",
        _ => "alphanumeric",
    };
}
