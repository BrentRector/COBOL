// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// ⛔ THE SYNTAX-ONLY OPERAND CHECK OF EVERY DIRECTIVE WHOSE OPERAND A DOWNSTREAM STAGE PARSES (kb/Work PB2003).
/// ISO §7.2.1 requires compiler directives to be "syntactically correct in the initial source text and library text",
/// and the false path of a <c>&gt;&gt;IF</c> or <c>&gt;&gt;EVALUATE</c> is part of that text. The conditional-compilation
/// driver therefore asks every directive line's operand in an OMITTED branch too — the closed-word rows through
/// <see cref="CompilerDirectiveCatalog.CheckOperand"/>, the directives the driver itself evaluates through its own arms —
/// and for the directives a LATER stage owns (a row whose <see cref="DirectiveOperandSyntax.Form"/> is
/// <see cref="DirectiveOperandForm.Stage"/> and whose <see cref="DirectiveOperandSyntax.Owner"/> is not the driver) it
/// asks the owning stage's check-only entry, registered here. Before this, the driver consumed such a line unseen in an
/// omitted branch, the stage that would have parsed it never ran, and <c>&gt;&gt;TURN GARBAGE ON</c> compiled clean there.
/// <para>A check-only entry parses the operand exactly as the stage does for a compiled line, reports what the stage
/// reports on its own channel, and applies nothing: it reads no timeline and changes no state. <b>One entry per owner
/// stage</b>; the owner is named by the catalog row (<c>directiveOperand.owner</c> in <c>constructs.json</c>), so a new
/// Stage row whose owner is neither the driver nor registered here fails
/// <c>CompilerDirectiveCatalogDriftTests.EveryStageOwner_HasACheckOnlyEntryOrIsTheDriver</c>, and the next case is the
/// entry and nothing else.</para>
/// </summary>
public static class StageOperandChecks
{
    /// <summary>The conditional-compilation driver, which evaluates (and so already parses) the operands of its own
    /// directives in every branch.</summary>
    public const string Driver = nameof(ConditionalCompilationProcessor);

    /// <summary>A check-only entry: the directive word, its operand, the targeted dialect level, and where to report.</summary>
    private delegate void Check(string word, string operand, int dialectLevel, DiagnosticBag diagnostics, SourceLocation loc);

    private static readonly IReadOnlyDictionary<string, Check> ByOwner = new Dictionary<string, Check>
    {
        [nameof(TurnDirectiveProcessor)] = (_, operand, dialectLevel, bag, loc) =>
            TurnDirectiveProcessor.CheckOperand(operand, dialectLevel, bag, loc),
        [nameof(FlagDirectiveProcessor)] = (word, operand, _, bag, loc) =>
            FlagDirectiveProcessor.CheckOperand(word, operand, bag, loc),
        [nameof(CobolWordsDirectiveProcessor)] = (_, operand, _, bag, loc) =>
            CobolWordsDirectiveProcessor.CheckOperand(operand, bag, loc),
    };

    /// <summary>The owner stages that have a check-only entry (the drift test's population).</summary>
    public static IEnumerable<string> Owners => ByOwner.Keys;

    /// <summary>Ask the stage that owns <paramref name="word"/>'s operand to syntax-check it, reporting to
    /// <paramref name="diagnostics"/>. A no-op for a word whose row declares no stage operand, and for one the driver
    /// itself parses.</summary>
    public static void CheckOmitted(string word, string operand, int dialectLevel, DiagnosticBag diagnostics, SourceLocation loc)
    {
        if (CompilerDirectiveCatalog.Find(word)?.DirectiveOperand is { Form: DirectiveOperandForm.Stage, Owner: { } owner }
            && owner != Driver
            && ByOwner.TryGetValue(owner, out var check))
            check(word, operand, dialectLevel, diagnostics, loc);
    }
}
