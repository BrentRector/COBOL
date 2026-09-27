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
/// <para><b>The operand model (kb/Work PB1406).</b> §8.8.3.1 writes <c>{literal-1 | concatenation-expression-1}
/// &amp; literal-2</c>, and an operand is a literal in the §8.8.3.2 SR1 sense — a quoted literal of class
/// alphanumeric (the X"…" hexadecimal format included, §8.3.3.2), boolean or national, a figurative constant
/// (SR1: "a figurative constant may be specified as one or both operands"), or one of the two WORDS that stand for
/// a literal: a constant-name (§13.10.3 SR2 — "anywhere that a format specifies a literal of the class and category
/// of constant-name-1") and a symbolic-character, which §12.3.7.4 GR11 a) makes a figurative constant. The words
/// resolve through the fold's <see cref="LiteralEnvironment"/>.</para>
/// <para><b>The class is a LEFT FOLD, not a vote.</b> The grammar flattens the left-recursive format to
/// <c>operand (&amp; operand)+</c>, so <c>a &amp; b &amp; c</c> IS <c>(a &amp; b) &amp; c</c>, and §8.8.3.3 GR1 is
/// applied to each (accumulated, next) pair: a figurative takes the other operand's class (GR1a), two figuratives
/// make class alphanumeric (GR1b), two literals keep their common class (GR1c) — and from the first pair on the
/// accumulated operand is a concatenation expression, never a figurative constant, so <c>SPACE &amp; SPACE &amp;
/// N"AB"</c> is an alphanumeric expression concatenated with a national literal, which SR1 forbids. Classing the
/// whole chain by its first non-figurative operand accepted exactly that shape.</para>
/// Rules enforced here, one diagnostic code per rule (DiagnosticCatalog):
/// <list type="bullet">
/// <item>§8.8.3.2 SR1 first sentence — both operands of each pair the same class → <c>COBOLNET1540</c> (a numeric
/// constant-name is of none of the three classes SR1 admits, and a figurative with no character value in the
/// pair's class fails the same rule).</item>
/// <item>§8.8.3.2 SR1 second sentence — neither operand a figurative constant beginning with ALL →
/// <c>COBOLNET1541</c>.</item>
/// <item>§8.8.3.2 SR2–SR4 — the resulting value at most 8,191 character positions (per class) →
/// <c>COBOLNET1545</c>.</item>
/// <item>§8.8.3.1 — a word operand that is neither a constant-name nor a symbolic-character is no literal →
/// <c>COBOLNET2473</c>; inside SPECIAL-NAMES, §12.3.7.3 SR11's literals shall not specify a symbolic-character →
/// <c>COBOLNET2474</c>.</item>
/// </list>
/// A figurative constant inside a concatenation expression is ONE character (§8.3.3.6.4 GR3a): ZERO '0',
/// SPACE ' ', QUOTE '"', a symbolic-character the character it was defined as, and the HIGH-VALUE/LOW-VALUE
/// characters the <see cref="LiteralEnvironment"/> of the fold names for the pair's class. NULL has no character
/// value (§8.3.3.6.3 Format 8 — a pointer figurative) and is rejected. In class boolean only ZERO folds (a boolean
/// character is 0 or 1, §8.3.3.4 — SPACE/QUOTE/HIGH/LOW and a symbolic-character have no boolean value; the
/// §13.18.63 SR10 posture). §8.8.3.3 GR2: the value is the concatenation of the operand values; two zero-length
/// operands fold to a zero-length literal (which falls out of plain string concatenation).
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

    /// <summary>The class of the concatenation expression (§8.8.3.3 GR1, folded pairwise) — DIAGNOSTIC-FREE, for
    /// routing predicates that must classify without double-reporting (e.g. the boolean-channel discriminator
    /// <c>ConditionBinder.IsBooleanValueOperand</c>). The SAME walk <see cref="Fold"/> reports from, so the route a
    /// predicate picks and the class the fold then reports against cannot disagree.</summary>
    public static PicCategory ClassOf(Core.ConcatenationExpressionContext ctx, LiteralEnvironment env) =>
        Walk(ctx, env, report: null).Category;

    /// <summary>The folded equivalent literal, DIAGNOSTIC-FREE — for a reader that must know the value but is not
    /// the expression's reporting site (a §3.178 zero-length test, for instance).</summary>
    public static Folded Peek(Core.ConcatenationExpressionContext ctx, LiteralEnvironment env) =>
        Walk(ctx, env, report: null);

    /// <summary>THE reader of Format 6's literal-1 (<c>ALL literal-1</c>, §8.3.3.6.2), folded to its equivalent single
    /// literal — or null when <paramref name="fig"/> is not Format 6 (a keyword figurative, with or without ALL, and
    /// <c>ALL symbolic-character-1</c>, Format 7, which the caller's own arm reads).
    /// <para>§8.3.3.6.3 SR2 — "Literal-1 shall be an alphanumeric, boolean, or national literal, any of which may be a
    /// concatenation expression. The literal shall be neither a figurative constant nor a zero-length literal." — so
    /// literal-1 is written three ways, and all three resolve HERE (kb/Work PB1627):</para>
    /// <list type="bullet">
    /// <item>one quoted literal token — its own class and decoded value;</item>
    /// <item>a §8.8.3 concatenation expression — THE <see cref="Walk"/> every other concatenation takes, so its operands
    /// are every §8.8.3.2 SR1 operand (a figurative constant, a constant-name, a symbolic-character) and its class is
    /// the pairwise §8.8.3.3 GR1 fold. (A second, quoted-only operand model here used to refuse <c>ALL "A" &amp; SPACE</c>
    /// and <c>ALL "A" &amp; K</c>, and classed a concatenation by its first operand.)</item>
    /// <item>a constant-name — the <c>ALL cobolWord</c> parse arm, since the parse cannot tell it from a
    /// symbolic-character: §13.10.3 SR2 lets constant-name-1 stand "anywhere that a format specifies a literal of the
    /// class and category of constant-name-1". A constant-name wins over the Format 7 reading, as it does in
    /// <see cref="Classify"/>.</item>
    /// </list>
    /// With <paramref name="report"/> it reports SR2 (a numeric constant-name → <c>COBOLNET2491</c>; a zero-length
    /// literal-1 → <c>COBOLNET1648</c>) and, for a concatenation, every §8.8.3.2 rule <see cref="Fold"/> reports; every
    /// binder site that ACCEPTS a Format 6 figurative reports through here, and the per-position diagnostic dedup
    /// (<c>EditionContext.AddOnce</c>) keeps a figurative read at two sites to one report. Without it the fold is
    /// DIAGNOSTIC-FREE, for routing predicates. The value is always best-effort, so the caller's plumbing continues
    /// after a reported error (the driver halts before emit). The text-plumbed DATA-division paths re-quote it through
    /// <see cref="Folded.RawText"/> (kb/Work PB71).</summary>
    public static Folded? FoldAllLiteral(Core.FigurativeConstantContext fig, LiteralEnvironment env, EditionContext? report)
    {
        if (fig.allLiteral() is { } al)
        {
            if (al.concatenationExpression() is { } ce)
            {
                var folded = report is null ? Walk(ce, env, report: null) : Fold(ce, report, env);
                if (ce.concatOperand().All(op => IsZeroLengthOperand(op, env)))
                    ReportZeroLength("ALL " + string.Join(" & ", ce.concatOperand().Select(op => op.GetText())), report);
                return folded;
            }
            string token = al.GetText();
            if (CobolLiteral.IsZeroLength(token)) ReportZeroLength("ALL " + token, report);
            return new Folded(CobolLiteral.ClassOf(token) switch
            {
                LiteralClass.National => PicCategory.National,
                LiteralClass.Boolean => PicCategory.Boolean,
                _ => PicCategory.Alphanumeric,   // X"…" is the alphanumeric literal's Format 2 (hexadecimal-alphanumeric, §8.3.3.2.2)
            }, CobolLiteral.Decode(token));
        }
        if (fig.ALL() is null || fig.cobolWord()?.GetText() is not { } word || env.Constant(word) is not { } k) return null;
        if (k.Category is not (PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean))
        {
            report?.Error(DiagnosticCatalog.AllLiteralClass, $"'ALL {word}': constant-name {word} is a numeric literal "
                + "— the literal-1 of an ALL figurative shall be an alphanumeric, boolean, or national literal (ISO "
                + "§8.3.3.6.3 SR2; §13.10.3 SR2)");
            return new Folded(PicCategory.Alphanumeric, k.Text);
        }
        if (CobolLiteral.IsZeroLength(k.RawText)) ReportZeroLength($"ALL {word}", report);
        return new Folded(k.Category, k.Text);
    }

    /// <summary>Whether a concatenation operand is a zero-length literal — asked STRUCTURALLY (contiguous delimiters,
    /// <see cref="CobolLiteral.IsZeroLength"/>), never as "decodes to nothing": a malformed hexadecimal literal decodes
    /// to nothing too, and is LiteralScreenPass's COBOLNET1635, not SR2. A constant-name is its literal (§13.10.4
    /// GR1); a figurative constant is one character (§8.3.3.6.4 GR3a).</summary>
    private static bool IsZeroLengthOperand(Core.ConcatOperandContext op, LiteralEnvironment env) =>
        op.cobolWord()?.GetText() is { } word ? env.Constant(word) is { } k && CobolLiteral.IsZeroLength(k.RawText)
        : op.figurativeConstant() is null && CobolLiteral.IsZeroLength(op.GetText());

    /// <summary>§8.3.3.6.3 SR2's second sentence — literal-1 "shall be neither a figurative constant nor a zero-length
    /// literal" (a concatenation of zero-length literals IS one, §8.8.3.3 GR2). The figurative-constant half cannot be
    /// written: the grammar's literal-1 is a quoted literal, a concatenation expression or a constant-name, and a
    /// constant-name is never a figurative constant (§13.10.3 SR6).</summary>
    private static void ReportZeroLength(string written, EditionContext? report) =>
        report?.Error(DiagnosticCatalog.AllLiteralZeroLength, $"'{written}': the literal-1 of an ALL figurative "
            + "shall not be a zero-length literal (ISO §8.3.3.6.3 SR2)");

    /// <summary>Fold <paramref name="ctx"/> to its equivalent single literal (§8.8.3.3 GR2/GR3) in the literal
    /// environment <paramref name="env"/> (its HIGH-/LOW-VALUE characters and its constant-name and
    /// symbolic-character tables), reporting the §8.8.3.2 syntax-rule violations to <paramref name="edition"/>.
    /// Always returns a best-effort value so the caller's plumbing continues — on any reported error the compile
    /// has already failed (the driver halts before emit).</summary>
    public static Folded Fold(Core.ConcatenationExpressionContext ctx, EditionContext edition, LiteralEnvironment env)
    {
        var folded = Walk(ctx, env, edition);
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
    /// in that class. ZERO is '0' (GR4); SPACE ' ' (GR5); QUOTE '"' (GR1); HIGH-/LOW-VALUE are the characters
    /// <paramref name="env"/> names for the class (§8.3.3.6.4 GR6/GR7 — the program collating sequence of the
    /// class outside SPECIAL-NAMES, the native sequence of the clause inside it, §12.3.7.4 GR10). In class boolean
    /// only ZERO has a value (a boolean character is 0 or 1, §8.3.3.4); NULL — the pointer figurative, Format 8 —
    /// has none in any class. The ALL literal-1 and ALL symbolic-character-1 forms are not keyword figuratives and
    /// answer null.</summary>
    internal static char? FigurativeChar(Core.FigurativeConstantContext fig, PicCategory cat, LiteralEnvironment env)
    {
        if (fig.zeroWord() is not null) return '0';
        if (cat is PicCategory.Boolean) return null;
        if (fig.spaceWord() is not null) return ' ';
        if (fig.quoteWord() is not null) return '"';
        if (fig.highValueWord() is not null) return env.HighValue(cat);
        if (fig.lowValueWord() is not null) return env.LowValue(cat);
        return null;
    }

    /// <summary>One operand as §8.8.3 classes it. <see cref="Class"/> is the literal's class, or null for a figurative
    /// constant — whose class is its partner's (GR1a) — and for an operand already refused (so a refusal never
    /// cascades into a class mismatch). A literal carries its decoded <see cref="Value"/>; a symbolic-character its
    /// one character; a keyword figurative its parse node, whose character depends on the pair's class.</summary>
    private readonly record struct Term(PicCategory? Class, string Value, Core.FigurativeConstantContext? Keyword,
        bool Refused, string Described)
    {
        public static Term Literal(PicCategory cls, string value, string described) => new(cls, value, null, false, described);
        public static Term Figurative(Core.FigurativeConstantContext fig) => new(null, "", fig, false, $"figurative constant '{fig.GetText()}'");
        public static Term Symbolic(string value, string word) => new(null, value, null, false, $"symbolic-character '{word}'");
        public static Term Refusal(string written) => new(null, "", null, true, written);
    }

    /// <summary>THE fold — the pairwise §8.8.3.3 GR1 class and the §8.8.3.3 GR2 value — reporting to
    /// <paramref name="report"/> when it is not null. <see cref="ClassOf"/>, <see cref="Peek"/> and
    /// <see cref="Fold"/> are its only callers, so the class a predicate routes on and the class the fold reports
    /// against are one computation.</summary>
    private static Folded Walk(Core.ConcatenationExpressionContext ctx, LiteralEnvironment env, EditionContext? report)
    {
        var ops = ctx.concatOperand();
        var terms = new Term[ops.Length];
        for (int i = 0; i < ops.Length; i++) terms[i] = Classify(ops[i], env, report);

        var sb = new System.Text.StringBuilder();
        PicCategory? left = terms[0].Class;          // the accumulated left operand's class (null: a figurative)
        bool leftAllFigurative = left is null;
        for (int i = 1; i < terms.Length; i++)
        {
            var right = terms[i];
            PicCategory pair = (left, right.Class) switch
            {
                (null, null) => PicCategory.Alphanumeric,   // GR1b — both operands figurative constants
                (null, { } r) => r,                         // GR1a — the figurative takes the literal's class
                ({ } l, _) => l,                            // GR1a / GR1c (a mismatch is SR1, reported below)
            };
            if (left is { } lc && right.Class is { } rc && lc != rc && report is not null)
                report.Error(DiagnosticCatalog.ConcatClassMismatch, $"{right.Described} concatenated to "
                    + LeftDescribed(ops, terms, i, lc, leftAllFigurative) + " — both operands shall be of the same "
                    + "class (ISO §8.8.3.2 SR1)");
            if (i == 1) Append(sb, terms[0], pair, env, report);
            Append(sb, right, pair, env, report);
            left = pair;                                // from here on the left operand is a concatenation expression
            leftAllFigurative &= right.Class is null;
        }
        return new Folded(left ?? PicCategory.Alphanumeric, sb.ToString());
    }

    /// <summary>Classify one operand, reporting the rules that belong to the operand ALONE (an ALL figurative, a word
    /// that is no literal, a numeric constant-name, a symbolic-character SR11 forbids here).</summary>
    private static Term Classify(Core.ConcatOperandContext op, LiteralEnvironment env, EditionContext? report)
    {
        if (op.STRINGLIT() is { } s) return Term.Literal(PicCategory.Alphanumeric, CobolLiteral.Decode(s.GetText()), $"alphanumeric literal {s.GetText()}");
        // X"…" is the hexadecimal FORMAT of the alphanumeric literal (§8.3.3.2) — class alphanumeric.
        if (op.HEXLIT() is { } x) return Term.Literal(PicCategory.Alphanumeric, CobolLiteral.DecodeHex(x.GetText()), $"alphanumeric (hexadecimal) literal {x.GetText()}");
        if (op.NATLIT() is { } n) return Term.Literal(PicCategory.National, CobolLiteral.Decode(n.GetText()), $"national literal {n.GetText()}");
        if (op.BOOLLIT() is { } b) return Term.Literal(PicCategory.Boolean, CobolLiteral.Decode(b.GetText()), $"boolean literal {b.GetText()}");
        if (op.figurativeConstant() is { } fig)
        {
            // §8.8.3.2 SR1 second sentence: neither operand shall be a figurative constant that begins with the
            // word ALL (any ALL form — ALL "lit" / ALL X"…" / ALL B"…" / ALL SPACE / ALL symbolic-character …).
            if (fig.ALL() is null) return Term.Figurative(fig);
            report?.Error(DiagnosticCatalog.ConcatAllFigurative, $"'{fig.GetText()}': a figurative constant beginning "
                + "with ALL shall not be a concatenation-expression operand (ISO §8.8.3.2 SR1)");
            return Term.Refusal(fig.GetText());
        }
        string word = op.cobolWord().GetText();
        // §13.10.3 SR2: a constant-name stands for its literal — "as if [the] literal were written" (§13.10.4 GR1).
        if (env.Constant(word) is { } k)
        {
            if (k.Category is PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean)
                return Term.Literal(k.Category, k.Text, $"constant-name {word} ({Name(k.Category)} literal {k.RawText})");
            report?.Error(DiagnosticCatalog.ConcatClassMismatch, $"constant-name {word} is a numeric literal — a "
                + "concatenation-expression operand shall be of class alphanumeric, boolean, or national (ISO §8.8.3.2 SR1)");
            return Term.Refusal(word);
        }
        // §12.3.7.4 GR11 a): a symbolic-character is a figurative constant — one character here (§8.3.3.6.4 GR3a).
        if (env.IsSymbolicCharacter(word))
        {
            if (env.RefusesSymbolicCharacters)
            {
                report?.Error(DiagnosticCatalog.ConcatSymbolicCharacterInSpecialNames, $"'{word}' is a "
                    + "symbolic-character figurative constant; a SPECIAL-NAMES literal-1 … literal-6 or literal-9 shall "
                    + "specify none, as a concatenation-expression operand included (ISO §12.3.7.3 SR11)");
                return Term.Refusal(word);
            }
            if (env.SymbolicValue(word) is { } ch) return Term.Symbolic(ch, word);
        }
        report?.Error(DiagnosticCatalog.ConcatOperandNotLiteral, $"'{word}' is not a literal — a concatenation-expression "
            + "operand is a literal or a figurative constant, and the only words that stand for one are a constant-name "
            + "(ISO §13.10.3 SR2) and a symbolic-character (§12.3.7.4 GR11) — ISO §8.8.3.1");
        return Term.Refusal(word);
    }

    /// <summary>Append <paramref name="t"/>'s value in the class of the pair it is an operand of — a figurative's ONE
    /// character (§8.3.3.6.4 GR3a) depends on that class (GR1: national characters in a national context).</summary>
    private static void Append(System.Text.StringBuilder sb, Term t, PicCategory pair, LiteralEnvironment env,
        EditionContext? report)
    {
        if (t.Refused) return;
        if (t.Class is not null) { sb.Append(t.Value); return; }
        char? c = t.Keyword is { } fig ? FigurativeChar(fig, pair, env)
            : pair is PicCategory.Boolean ? null : t.Value[0];
        if (c is { } ch) { sb.Append(ch); return; }
        report?.Error(DiagnosticCatalog.ConcatClassMismatch, $"{t.Described} has no {Name(pair)} character value "
            + $"in a concatenation expression of class {Name(pair)} — both operands shall be of the same class "
            + "(ISO §8.8.3.2 SR1)");
    }

    /// <summary>The accumulated left operand of pair <paramref name="i"/>, named for a mismatch message: the first
    /// operand itself for the first pair, else the concatenation expression written so far — with GR1b named when
    /// every operand in it is a figurative constant, because that is the case a reader does not expect.</summary>
    private static string LeftDescribed(Core.ConcatOperandContext[] ops, Term[] terms, int i, PicCategory cls,
        bool allFigurative) =>
        i == 1 ? terms[0].Described
        : $"the concatenation expression '{string.Join(" & ", ops.Take(i).Select(o => o.GetText()))}' of class "
          + Name(cls) + (allFigurative ? " (both of its operands are figurative constants — ISO §8.8.3.3 GR1b)" : "");

    private static string Name(PicCategory cat) => cat switch
    {
        PicCategory.National => "national",
        PicCategory.Boolean => "boolean",
        _ => "alphanumeric",
    };
}
