// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using CobolNet.Runtime.Exceptions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1122 Task A / PB1761 — <b>THE DECLARATIVE DISPATCH-RESULT PROTOCOL IS ONE NAMED TYPE.</b> Every selection
/// path answers its raise site with one <c>int</c> — <c>-1</c> normal, <c>-2</c> RESUME NEXT, <c>-3</c> no handler,
/// <c>-4</c> handled nonfatal, <c>≥ 0</c> a RESUME AT pc — and the emitters spelled those as bare literals at a dozen
/// sites, so adding a value (PB1122 Task B's <c>NotNormal</c>) meant finding every literal by hand. The emitters now
/// render <see cref="DispatchResult"/>'s names and predicates. These pins keep a literal from creeping back, and keep
/// every value reachable from a consumer predicate so a new value cannot be added without saying what a consumer asks
/// of it.
/// </summary>
public sealed class DispatchResultProtocolDriftTests
{
    private static string CodeGenDir =>
        Path.GetDirectoryName(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "EcEmitter.cs"))!;

    /// <summary>A dispatch-result variable of the emitted text compared with, or assigned, a bare literal of the
    /// protocol. The variable spellings are the ones the emitters give a dispatch result (<c>__r&lt;n&gt;</c> the raise
    /// site, <c>__ior&lt;n&gt;</c>/<c>__or&lt;n&gt;</c> the I-O hook and propagation pickup, <c>__sel</c>/<c>__w</c>/<c>__a</c>
    /// the generated selectors and <c>__RunF3</c>, <c>{r}</c>/<c>{resultVar}</c> the emitter-side parameter that
    /// carries one).</summary>
    private static readonly Regex VariableVsLiteral = new(
        @"(__r\d*|__ior\{?\w*\}?|__or\{?\w*\}?|__sel|__w|__a|\{r\}|\{resultVar\})\s*(==|!=|>=|<=|=|<|>)\s*(-[1-5]\b|0\b)",
        RegexOptions.Compiled);

    /// <summary>A bare protocol literal RETURNED by emitted text (<c>"return -3;"</c>, <c>return{ret}</c>'s
    /// <c>" -1"</c>) or chosen by a conditional expression of it.</summary>
    private static readonly Regex ReturnedLiteral = new(@"""[^""]*\breturn\s*-[1-5]\b|""\s-[1-5]""|:\s*""-[1-5]""", RegexOptions.Compiled);

    private static IEnumerable<(string File, int Line, string Text)> CodeLines() =>
        Directory.EnumerateFiles(CodeGenDir, "*.cs", SearchOption.AllDirectories).SelectMany(f =>
            File.ReadLines(f).Select((t, i) => (Path.GetFileName(f), i + 1, t))
                .Where(l => !l.t.TrimStart().StartsWith("//")));

    [Fact]
    public void NoEmitter_SpellsTheProtocolAsBareLiterals()
    {
        var offenders = CodeLines()
            .Where(l => VariableVsLiteral.IsMatch(l.Text) || ReturnedLiteral.IsMatch(l.Text))
            .Select(l => $"{l.File}:{l.Line}  {l.Text.Trim()}")
            .ToList();
        Assert.True(offenders.Count == 0,
            "an emitter renders a declarative dispatch result as a bare literal — use DispatchResult.Normal / ResumeNext / "
            + "NoHandler / HandledNonfatal / NotNormal and the IsTransfer / SuppressesFatal / TerminatesSortMerge "
            + "predicates (kb/Work PB1122 Task A):" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    /// <summary>The protocol has exactly one definition: no other type redefines a dispatch-result constant, and the
    /// two names the protocol used to be spread over (<c>ResumeSignal.NextStatement</c>/<c>HandledNonfatal</c>,
    /// <c>ExceptionState.DeclarativeCompleted</c>/<c>NoDeclarative</c>) are gone from every source.</summary>
    [Fact]
    public void TheProtocolHasOneDefinition()
    {
        var stale = new Regex(@"ResumeSignal\.(NextStatement|HandledNonfatal)|\b(DeclarativeCompleted|NoDeclarative)\b");
        var offenders = Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Runtime"), "*.cs", SearchOption.AllDirectories))
            .Where(f => stale.IsMatch(File.ReadAllText(f)))
            .Select(Path.GetFileName)
            .ToList();
        Assert.True(offenders.Count == 0,
            "a pre-DispatchResult spelling of the protocol survives (no forwarding constants — CLAUDE.md rule 4): "
            + string.Join(", ", offenders));
    }

    /// <summary>Every protocol constant is named by at least one consumer predicate of <see cref="DispatchResult"/>,
    /// so a value added to the protocol without a statement of what a consumer asks of it fails here.</summary>
    [Fact]
    public void EveryConstant_IsNamedByAConsumerPredicate()
    {
        var constants = typeof(DispatchResult).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral).Select(f => f.Name).ToList();
        Assert.NotEmpty(constants);
        // The code lines (comments dropped) from the first predicate on: the constants precede it.
        var code = File.ReadLines(TestRepo.Src("Cobol.Net.Runtime", "Exceptions", "DispatchResult.cs"))
            .Where(l => !l.TrimStart().StartsWith("//")).ToList();
        string predicates = string.Join("\n", code.SkipWhile(l => !l.Contains("public static bool IsTransfer")));
        foreach (string name in constants)
            Assert.True(Regex.IsMatch(predicates, @"\b" + name + @"\b"),
                $"DispatchResult.{name} is not named by any consumer predicate (IsTransfer / SuppressesFatal / "
                + "TerminatesSortMerge / ForHandledWarning / RanAHandler) — say what a consumer asks of it.");
    }

    /// <summary>The documented semantics of the predicates, as values (they are emitted into every EC-model program,
    /// so a change here is a change to every generated raise site).</summary>
    [Theory]
    [InlineData(DispatchResult.Normal, false, false, false)]
    [InlineData(DispatchResult.ResumeNext, false, true, true)]
    [InlineData(DispatchResult.NoHandler, false, false, false)]
    [InlineData(DispatchResult.HandledNonfatal, false, false, false)]
    [InlineData(DispatchResult.NotNormal, false, false, true)]
    [InlineData(0, true, true, false)]
    [InlineData(57, true, true, false)]
    public void Predicates_AnswerPerTheProtocol(int result, bool transfer, bool suppressesFatal, bool terminatesSortMerge)
    {
        Assert.Equal(transfer, DispatchResult.IsTransfer(result));
        Assert.Equal(suppressesFatal, DispatchResult.SuppressesFatal(result));
        Assert.Equal(terminatesSortMerge, DispatchResult.TerminatesSortMerge(result));
    }

    [Theory]
    [InlineData(DispatchResult.NoHandler, DispatchResult.Normal)]
    [InlineData(DispatchResult.Normal, DispatchResult.HandledNonfatal)]
    [InlineData(DispatchResult.ResumeNext, DispatchResult.ResumeNext)]
    [InlineData(12, 12)]
    public void ForHandledWarning_MapsTheSuccessfulArm(int selected, int expected) =>
        Assert.Equal(expected, DispatchResult.ForHandledWarning(selected));
}
