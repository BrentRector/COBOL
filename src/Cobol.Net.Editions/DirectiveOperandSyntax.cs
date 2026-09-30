// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Editions;

/// <summary>How a compiler directive's operand is checked — the TOTAL partition (kb/Work PB794).</summary>
public enum DirectiveOperandForm
{
    /// <summary>A closed set of words, given by the directive's own general format: the operand is validated
    /// centrally, by <see cref="CompilerDirectiveCatalog.CheckOperand"/>, against
    /// <see cref="DirectiveOperandSyntax.Choice"/> (plus the optional words, a catalogued directive-name, or a
    /// user-defined word, as that format allows).</summary>
    Words,

    /// <summary>Text whose CONTENT this stage does not check — either because the standard says it is not checked
    /// (§7.3.19.3 SR2, PAGE's comment-text-1) or because the operand's own syntax is not implemented here. Only
    /// PRESENCE is checked, per <see cref="DirectiveOperandSyntax.OperandRequired"/>, and
    /// <see cref="DirectiveOperandSyntax.Citation"/> carries which of the two it is: an unchecked operand is a
    /// DECLARED and cited decision, never a silence.</summary>
    Text,

    /// <summary>A structured operand that a named downstream stage parses and diagnoses
    /// (<see cref="DirectiveOperandSyntax.Owner"/>) — the TURN exception-name list, the FLAG option lists, the
    /// COBOL-WORDS entries, and the conditional-compilation driver's own expression operands. The central check
    /// is a no-op for these; the drift test asserts the owner is real.</summary>
    Stage,
}

/// <summary>
/// One directive's OPERAND syntax — the column <c>constructs.json</c> gained beside <c>directiveWords</c> at
/// kb/Work PB794, so that "which words may follow this directive word" is DATA on the same row that already
/// carries "from which edition may this directive word appear at all".
///
/// <para>ISO §7.3.3 SR6: "Compiler-instruction is composed of compiler-directive words, system-names, and
/// user-defined words <b>as specified in the syntax of each directive</b>." Each directive's general format says
/// exactly which; for ten of them that syntax is a closed word set, and modelling it as data means the next such
/// directive is one row plus a regen rather than another hand-written check with another diagnostic code. Before
/// PB794 seven directive lines that violate their own printed general format compiled in silence
/// (<c>&gt;&gt;SOURCE FORMAT UNKNOWN</c> among them) while six other stages each wrote the same rule again with a
/// code of its own.</para>
///
/// <para>⛔ <see cref="ChoiceOmissible"/> is decided by the PRINTED UNDERLINING, never by the braces
/// (feedback_underlining_not_bracketing). §7.3.24.2 underlines both FIXED and FREE, so SOURCE FORMAT's choice
/// shall be written; §7.3.17.2, §7.3.18.2, §7.3.21.2 and §7.3.23.2 leave ON un-underlined, which per §5.2.3 makes
/// it an OPTIONAL word — the ON alternative may be selected with the word omitted, so a bare
/// <c>&gt;&gt;LEAP-SECOND</c> is conforming and must not be diagnosed.</para>
/// </summary>
public sealed record DirectiveOperandSyntax
{
    /// <summary>Which of the three checking regimes this row is in. There is no fourth, and no default:
    /// <c>CompilerDirectiveOperandDriftTests</c> asserts every catalogued row declares one.</summary>
    public required DirectiveOperandForm Form { get; init; }

    /// <summary>The general format's OPTIONAL words (§5.2.3) — written or omitted freely, and ignored when
    /// present. SOURCE FORMAT's <c>FORMAT</c> and <c>IS</c> are the whole population today.</summary>
    public IReadOnlyList<string> OptionalWords { get; init; } = [];

    /// <summary>The closed set of operand words the general format admits — one of which is written, unless
    /// <see cref="ChoiceOmissible"/>.</summary>
    public IReadOnlyList<string> Choice { get; init; } = [];

    /// <summary>True when the choice may be omitted entirely, because the general format leaves one alternative
    /// un-underlined (§5.2.3). False when every alternative is underlined and one shall be written.</summary>
    public bool ChoiceOmissible { get; init; }

    /// <summary>True when a catalogued compiler-directive NAME is also admissible in the operand — §7.3.20.2 /
    /// §7.3.22.2's <c>directive-name</c>. The admissible names are derived from the catalog itself, minus
    /// <see cref="ExcludedDirectives"/>, so a new directive joins the PUSH/POP operand set automatically.</summary>
    public bool DirectiveName { get; init; }

    /// <summary>The directive words §7.3.20.3 SR1 / §7.3.22.3 SR1 exclude from <c>directive-name</c> — naming
    /// ONE word of a multi-word row (EVALUATE, IF) excludes that whole row, because the rule excludes the
    /// DIRECTIVE, not the spelling.</summary>
    public IReadOnlyList<string> ExcludedDirectives { get; init; } = [];

    /// <summary>True when a user-defined word is admissible as the operand — §7.3.9.2's
    /// <c>call-convention-name-1</c>, whose meaning is implementor-defined (§7.3.9.3 GR2 b).</summary>
    public bool UserWord { get; init; }

    /// <summary>For <see cref="DirectiveOperandForm.Text"/>: whether SOME operand shall be present. PAGE's
    /// comment-text-1 is bracketed and optional (§7.3.19.2); DISPLAY's operand braces are not (§7.3.12.2).</summary>
    public bool OperandRequired { get; init; }

    /// <summary>The directive words of THIS row whose own general format writes NO operand at all, whatever the row's
    /// <see cref="Form"/> says about its other words (kb/Work PB806): a row is a directive FAMILY — <c>IF</c> /
    /// <c>ELSE</c> / <c>END-IF</c>, <c>EVALUATE</c> / <c>WHEN</c> / <c>END-EVALUATE</c> — and its stage-owned
    /// operand is true of <c>&gt;&gt;IF</c> only. <see cref="CompilerDirectiveCatalog.CheckOperand"/> rejects any
    /// operand word after one of these, ISO §7.3.3 SR3/SR4 (only spaces and an inline comment may follow a
    /// directive), so the next multi-word directive states its operand-less words here and is checked.</summary>
    public IReadOnlyList<string> NoOperandWords { get; init; } = [];

    /// <summary>For <see cref="DirectiveOperandForm.Stage"/>: the type name of the stage that parses and
    /// diagnoses this operand. The drift test resolves it, so a renamed stage is a red test rather than a
    /// silently unchecked directive.</summary>
    public string Owner { get; init; } = "";

    /// <summary>The ISO citation this row's operand syntax is derived from — the general format clause, plus the
    /// syntax rule where one governs. Required on every row, including the unchecked ones: a
    /// <see cref="DirectiveOperandForm.Text"/> row states WHY its content is not checked.</summary>
    public required string Citation { get; init; }

    /// <summary>A human rendering of the admissible operand, for the diagnostic message: "FIXED or FREE",
    /// "ON or OFF (or omitted)", "ALL or a compiler-directive name".</summary>
    public string Admissible(IEnumerable<string>? directiveNames = null)
    {
        var parts = new List<string>(Choice);
        if (DirectiveName) parts.Add("a compiler-directive name" + (directiveNames is null
            ? "" : " (" + string.Join(", ", directiveNames.Order(StringComparer.Ordinal)) + ")"));
        if (UserWord) parts.Add("an implementor-defined name");
        string set = parts.Count switch
        {
            0 => "no operand",
            1 => parts[0],
            _ => string.Join(", ", parts[..^1]) + " or " + parts[^1],
        };
        return ChoiceOmissible ? set + " (or nothing — the omitted phrase is implied)" : set;
    }
}
