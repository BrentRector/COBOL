// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Runtime;
namespace CobolNet.Frontend.Preprocessor;

/// <summary>Which migration-flagging directive an option belongs to — <c>&gt;&gt;FLAG-02</c> (§7.3.14, the
/// 2002↔2014 incompatibilities) or <c>&gt;&gt;FLAG-14</c> (§7.3.15, the 2014↔2023 incompatibilities).</summary>
public enum FlagDirective { Flag02, Flag14 }

/// <summary>
/// Every flaggable option of <c>&gt;&gt;FLAG-02</c> / <c>&gt;&gt;FLAG-14</c> (ISO §7.3.14.4 / §7.3.15.4 GR4). The
/// same option WORD may appear in both directives with a DIFFERENT meaning (<c>I-O-STATUS-07</c> is FLAG-02 c =
/// CLOSE NO REWIND/UNIT, but FLAG-14 f = a condition testing FILE STATUS for '07'), so every member is
/// directive-qualified. <c>ALL</c> is not a member: it is a parse-time fan-out to every option of its directive.
/// The canonical spelling is the GENERAL-FORMAT word (not the two GR4 typos — g's <c>NUM-ED-ZERO-FIG-CONSTANT</c>
/// and k's <c>VALUE-FIG-CON-NO-LENTH</c>). Design SSOT: <c>docs/rearchitecture/DESIGN-flag-directives.md</c>.
/// </summary>
public enum FlagOption
{
    // ── FLAG-02 (§7.3.14.4 GR4 b–f) ──
    Flag02EcProgramExceptions,
    Flag02IoStatus07,
    Flag02MoveToSameName,
    Flag02RangeExceptionForIndex,
    Flag02TerminateWithVarying,

    // ── FLAG-14 (§7.3.15.4 GR4 b–m) ──
    Flag14CompileTimeArithmeticExpressions,
    Flag14Evaluate,
    Flag14IoDeclarative,
    Flag14IoStatus04,
    Flag14IoStatus07,
    Flag14NumEdZeroFigconst,
    Flag14ReadPrevious,
    Flag14RefModZeroLength,
    Flag14ValueEditing,
    Flag14ValueFigConLength,
    Flag14ValueZero,
    Flag14WriteEndOfPage,
}

/// <summary>One flaggable option's static identity: which directive owns it, the general-format WORD that selects
/// it, the exact GR4 sub-rule + Annex-E citation, and the one-line behaviour-change summary the warning message
/// carries. The ONE catalog (§7.3.14.4 / §7.3.15.4) — the emitter reads <see cref="Citation"/> / <see
/// cref="Change"/> for a per-option, spec-faithful warning.</summary>
public sealed record FlagOptionInfo(FlagOption Option, FlagDirective Directive, string Word, string Citation, string Change);

/// <summary>The single source of truth for the option catalog + the word→option parse: enumerates every option of
/// both directives once. Reused by BOTH collection sites (the post-COPY <see cref="FlagDirectiveProcessor"/> and
/// the in-scan tracker in <see cref="ConditionalCompilationProcessor"/>) so directive-line parsing is
/// single-sourced.</summary>
public static class FlagOptions
{
    /// <summary>The complete option catalog (FLAG-02 then FLAG-14, in general-format order).</summary>
    public static readonly IReadOnlyList<FlagOptionInfo> All =
    [
        new(FlagOption.Flag02EcProgramExceptions, FlagDirective.Flag02, "EC-PROGRAM-EXCEPTIONS",
            "ISO §7.3.14.4 GR4 b",
            "a >>TURN for an EC-PROGRAM-family exception in a source element that calls a function or invokes a method (2002↔2014)"),
        new(FlagOption.Flag02IoStatus07, FlagDirective.Flag02, "I-O-STATUS-07",
            "ISO §7.3.14.4 GR4 c; Annex E.2 item 16",
            "a CLOSE with the WITH NO REWIND or UNIT phrase (I-O status '07' behaviour, 2002↔2014)"),
        new(FlagOption.Flag02MoveToSameName, FlagDirective.Flag02, "MOVE-TO-SAME-NAME",
            "ISO §7.3.14.4 GR4 d",
            "a MOVE whose sending and receiving operands are the same alphanumeric-edited (or subordinate-ODO) data description entry (2002↔2014)"),
        new(FlagOption.Flag02RangeExceptionForIndex, FlagDirective.Flag02, "RANGE-EXCEPTION-FOR-INDEX",
            "ISO §7.3.14.4 GR4 e",
            "an index-assignment or index-arithmetic SET into an index while EC-RANGE-INDEX checking is enabled (2002↔2014)"),
        new(FlagOption.Flag02TerminateWithVarying, FlagDirective.Flag02, "TERMINATE-WITH-VARYING",
            "ISO §7.3.14.4 GR4 f",
            "a TERMINATE of a report whose description contains a VARYING clause (2002↔2014)"),

        new(FlagOption.Flag14CompileTimeArithmeticExpressions, FlagDirective.Flag14, "COMPILE-TIME-ARITHMETIC-EXPRESSIONS",
            "ISO §7.3.15.4 GR4 b; Annex E.2 item 6",
            "a compile-time arithmetic expression whose result could differ now that the arithmetic mode is implementor-defined (2014↔2023)"),
        new(FlagOption.Flag14Evaluate, FlagDirective.Flag14, "EVALUATE",
            "ISO §7.3.15.4 GR4 c; Annex E.2 item 8",
            "a >>EVALUATE directive containing both a WHEN and a WHEN OTHER phrase (end-of-EVALUATE omission rules changed 2014↔2023)"),
        new(FlagOption.Flag14IoDeclarative, FlagDirective.Flag14, "I-O-DECLARATIVE",
            "ISO §7.3.15.4 GR4 d; Annex E.2 item 19",
            "an I-O statement without its INVALID KEY / AT END phrase while an INPUT/OUTPUT/I-O/EXTEND declarative is in effect (now executed on the exception, 2014↔2023)"),
        new(FlagOption.Flag14IoStatus04, FlagDirective.Flag14, "I-O-STATUS-04",
            "ISO §7.3.15.4 GR4 e; Annex E.2 item 15",
            "a reference to a FILE STATUS item that tests for '04' (its setting is now clarified, 2014↔2023)"),
        new(FlagOption.Flag14IoStatus07, FlagDirective.Flag14, "I-O-STATUS-07",
            "ISO §7.3.15.4 GR4 f; Annex E.2 item 16",
            "a reference to a FILE STATUS item that tests for '07' (its setting is now restricted to OPEN/CLOSE, 2014↔2023)"),
        new(FlagOption.Flag14NumEdZeroFigconst, FlagDirective.Flag14, "NUM-ED-ZERO-FIGCONST",
            "ISO §7.3.15.4 GR4 g; Annex E.2 item 28",
            "the figurative constant ZERO in the VALUE clause of a numeric-edited item (now the numeric literal zero, 2014↔2023)"),
        new(FlagOption.Flag14ReadPrevious, FlagDirective.Flag14, "READ-PREVIOUS",
            "ISO §7.3.15.4 GR4 h; Annex E.2 item 22",
            "a READ PREVIOUS statement (an at-end condition now occurs on a READ PREVIOUS following OPEN, 2014↔2023)"),
        new(FlagOption.Flag14RefModZeroLength, FlagDirective.Flag14, "REF-MOD-ZERO-LENGTH",
            "ISO §7.3.15.4 GR4 i; Annex E.2 item 23",
            "a reference modification where the REF-MOD-ZERO-LENGTH directive is unset and EC-BOUND-REF-MOD is on (a zero-length result now raises the exception, 2014↔2023)"),
        new(FlagOption.Flag14ValueEditing, FlagDirective.Flag14, "VALUE-EDITING",
            "ISO §7.3.15.4 GR4 j; Annex E.2 item 29",
            "a numeric-edited item whose VALUE is a literal without editing symbols (editing symbols are now compulsory, 2014↔2023)"),
        new(FlagOption.Flag14ValueFigConLength, FlagDirective.Flag14, "VALUE-FIG-CON-LENGTH",
            "ISO §7.3.15.4 GR4 k; Annex E.2 item 11",
            "a figurative constant in the VALUE clause of an item with no specified length (the length is now defined, 2014↔2023)"),
        new(FlagOption.Flag14ValueZero, FlagDirective.Flag14, "VALUE-ZERO",
            "ISO §7.3.15.4 GR4 l; Annex E.2 item 28",
            "a numeric-edited item whose VALUE specifies the figurative constant ZERO (now the numeric literal zero, 2014↔2023)"),
        new(FlagOption.Flag14WriteEndOfPage, FlagDirective.Flag14, "WRITE-END-OF-PAGE",
            "ISO §7.3.15.4 GR4 m",
            "a WRITE that allows an END-OF-PAGE phrase (the file has a LINAGE clause) but omits it (2014↔2023)"),
    ];

    private static readonly Dictionary<(FlagDirective, string), FlagOptionInfo> ByWord =
        All.ToDictionary(o => (o.Directive, o.Word), new DirWordComparer());

    private static readonly Dictionary<FlagOption, FlagOptionInfo> ByOption =
        All.ToDictionary(o => o.Option);

    /// <summary>The metadata for one option (citation, change summary, owning directive).</summary>
    public static FlagOptionInfo Info(FlagOption option) => ByOption[option];

    /// <summary>Every option of <paramref name="directive"/> — the <c>ALL</c> fan-out set.</summary>
    public static IReadOnlyList<FlagOption> OptionsOf(FlagDirective directive) =>
        [.. All.Where(o => o.Directive == directive).Select(o => o.Option)];

    /// <summary>Resolve a general-format option WORD (case-insensitive) to its option within
    /// <paramref name="directive"/>; null when the word is not a valid option of that directive.</summary>
    public static FlagOption? TryOption(FlagDirective directive, string word) =>
        ByWord.TryGetValue((directive, word.ToUpperInvariant()), out var info) ? info.Option : null;

    /// <summary>FLAG-14 b's construct test (ISO §7.3.15.4 GR4 b; Annex E.2 item 6): "a compile-time arithmetic
    /// expression that could give a different result" — an expression that EVALUATES something, i.e. carries a binary
    /// <c>addOp</c> or <c>mulOp</c> (a bare literal or a lone unary sign has one value in every arithmetic mode, so
    /// nothing could differ). The ONE predicate every site of a compile-time arithmetic expression asks (§7.3.6.1: the
    /// DEFINE, EVALUATE and DISPLAY directive operands and the constant conditional expression, read by the
    /// conditional-compilation stage, and the CONSTANT entry's arithmetic-expression-1, read by
    /// <c>FlagConformancePass</c>), so the sites cannot disagree about what a flaggable expression is.</summary>
    public static bool IsFlaggableCompileTimeArithmetic(Antlr4.Runtime.Tree.IParseTree expression)
    {
        for (int k = 0; k < expression.ChildCount; k++)
        {
            var child = expression.GetChild(k);
            if (child is Generated.CobolParserCore.AddOpContext or Generated.CobolParserCore.MulOpContext
                || IsFlaggableCompileTimeArithmetic(child)) return true;
        }
        return false;
    }

    private sealed class DirWordComparer : IEqualityComparer<(FlagDirective, string)>
    {
        public bool Equals((FlagDirective, string) x, (FlagDirective, string) y) =>
            x.Item1 == y.Item1 && CobolNames.Same(x.Item2, y.Item2);
        public int GetHashCode((FlagDirective, string) o) =>
            HashCode.Combine(o.Item1, CobolNames.Comparer.GetHashCode(o.Item2));
    }
}

/// <summary>One <c>&gt;&gt;FLAG-02</c> / <c>&gt;&gt;FLAG-14</c> toggle, anchored to its 1-based line in the FINAL
/// preprocessed text (directly comparable to an ANTLR token's <c>Start.Line</c>). A directive applies to the text
/// that FOLLOWS it (the <c>&gt;&gt;TURN</c> GR2 discipline). An empty <see cref="Options"/> list means the
/// <c>ALL</c> selection — every option of <see cref="Which"/>. The compile-time <see cref="Binding.FlagState"/>
/// folds these per option per source line.</summary>
/// <param name="Line">1-based line of the directive in the final preprocessed text.</param>
/// <param name="Which">Which directive (FLAG-02 / FLAG-14).</param>
/// <param name="On">ON (enable flagging) vs OFF (disable) for the selected option(s).</param>
/// <param name="Options">The selected options, or empty ⇒ ALL of <see cref="Which"/>.</param>
public sealed record FlagEvent(int Line, FlagDirective Which, bool On, IReadOnlyList<FlagOption> Options);

/// <summary>Parses the operand of a <c>&gt;&gt;FLAG-02</c> / <c>&gt;&gt;FLAG-14</c> directive line —
/// <c>{ ALL | |option…| } { ON | OFF }</c> (ISO §7.3.14.2 / §7.3.15.2). Both directives print the SAME shape, and in
/// both <c>ALL</c> and <c>ON</c> are NOT underlined — optional words (§5.2.3) — so by §5.2.6.3 each is the DEFAULT
/// of its braces and is selected when no other alternative is written: a bare directive is <c>ALL ON</c>, a lone
/// <c>OFF</c> is <c>ALL OFF</c>, and a lone option list is <c>ON</c>. The option list is one or more words in any
/// order, each at most once (§5.2.6.4 choice indicators within braces). The single source of directive-line
/// syntax, reused by both collection sites.</summary>
public static class FlagDirectiveLine
{
    /// <summary>Parse the operand text that FOLLOWS the <c>FLAG-02</c>/<c>FLAG-14</c> keyword. On success yields the
    /// selected options (empty ⇒ ALL) and the ON/OFF flag; on a malformed operand yields <paramref name="error"/>
    /// (an unknown option word, a word written twice, <c>ALL</c> combined with options, or an ON/OFF that is not the
    /// last word).</summary>
    public static bool TryParse(FlagDirective directive, string operand,
        out IReadOnlyList<FlagOption> options, out bool on, out string? error)
    {
        options = [];
        on = true;   // §5.2.6.3: ON is the un-underlined alternative of its braces, hence the default; §7.3.15.4 2): "explicitly or implicitly specified"
        error = null;

        var tokens = operand.SplitSpaces();

        // The second pair of braces: a trailing ON/OFF, written or not, identically for both directives.
        int end = tokens.Length;
        if (end > 0 && tokens[^1].ToUpperInvariant() is "ON" or "OFF")
        {
            on = tokens[^1].ToUpperInvariant() == "ON";
            end--;
        }

        // The first pair of braces: ALL (un-underlined, so the default when no option is written) or the option list.
        bool allWritten = false;
        var picked = new List<FlagOption>();
        for (int i = 0; i < end; i++)
        {
            string w = tokens[i].ToUpperInvariant();
            if (w == "ALL")
            {
                if (allWritten) { error = Repeated(tokens[i]); return false; }
                allWritten = true;
            }
            else if (w is "ON" or "OFF")
            {
                error = $"'{tokens[i]}' may be written only once, as the last word of the directive";
                return false;
            }
            else if (FlagOptions.TryOption(directive, w) is { } opt)
            {
                if (picked.Contains(opt)) { error = Repeated(tokens[i]); return false; }
                picked.Add(opt);
            }
            else
            {
                error = $"'{tokens[i]}' is not a valid option of >>{DirectiveWord(directive)}";
                return false;
            }
        }

        if (allWritten && picked.Count > 0) { error = "ALL may not be combined with individual options"; return false; }
        options = allWritten || picked.Count == 0 ? [] : picked;   // empty ⇒ ALL fan-out at fold time
        return true;
    }

    /// <summary>§5.2.6.4: within choice indicators enclosed by braces "any single alternative shall be specified only
    /// once" — a repeated option word (or a repeated ALL) is a syntax error, never a silent merge.</summary>
    private static string Repeated(string word) =>
        $"'{word}' is specified more than once (ISO §5.2.6.4: any single alternative shall be specified only once)";

    /// <summary>The directive's general-format word — <c>FLAG-02</c> / <c>FLAG-14</c>.</summary>
    public static string DirectiveWord(FlagDirective directive) =>
        directive == FlagDirective.Flag02 ? "FLAG-02" : "FLAG-14";
}
