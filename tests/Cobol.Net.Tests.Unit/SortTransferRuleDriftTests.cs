// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.CodeGen;
using CobolNet.Tests.Shared;
using Xunit;
using static CobolNet.CodeGen.SortEmitter;

namespace CobolNet.Tests.Unit;

/// <summary>⛔ THE SORT/MERGE IMPLICIT-TRANSFER TERMINATION RULES ARE ONE TABLE (kb/Work PB993). Every as-if OPEN,
/// READ, WRITE and CLOSE the USING/GIVING transfers perform is disposed of by <c>SortEmitter.RuleFor</c> after its
/// USE procedure; before PB993 each site ran its declarative and then carried on, so a fatal implicit OPEN of a GIVING
/// file ran the declarative again for every record's WRITE against the unopened file and the SORT "completed". These
/// tests pin (1) every cell of the table to the rule that decides it and (2) that no as-if statement in the emitter
/// reaches the USE hook except through the disposition, so a new as-if statement cannot skip it.</summary>
public sealed class SortTransferRuleDriftTests
{
    /// <summary>Each cell, as four letters (C continue · T terminate · B bypass): (fatal, USE completed) · (fatal,
    /// otherwise) · (nonfatal, USE completed) · (nonfatal, otherwise). The default is §9.1.13.1's — a fatal status
    /// transfers control "to the end of the statement that produced the fatal exception condition", the SORT/MERGE
    /// — and §14.6.13.1.4's nonfatal "execution continues".</summary>
    public static TheoryData<bool, string, string> Cells => new()
    {
        // SORT — §14.9.40.4 GR12 a)/b) (USING), GR15 (GIVING): fatal terminates, nonfatal continues, USE or not.
        { false, "UsingOpen", "TTCC" },
        { false, "UsingRead", "TTCC" },
        { false, "UsingClose", "TTCC" },
        { false, "GivingOpen", "TTCC" },
        { false, "GivingWrite", "TTCC" },
        { false, "GivingClose", "TTCC" },
        // MERGE — §14.9.24.4 GR7 a): a nonfatal OPEN terminates unless a USE procedure completes normally.
        { true, "UsingOpen", "TTCT" },
        { true, "UsingRead", "TTCC" },
        // GR7's closing paragraph: a nonfatal CLOSE continues with a normal USE and with none.
        { true, "UsingClose", "TTCC" },
        // GR12 a): fatal OPEN + a USE that completes normally => that file is BYPASSED.
        { true, "GivingOpen", "BTCC" },
        // GR12 b): any WRITE exception — "the MERGE continues execution, otherwise the MERGE statement is terminated".
        { true, "GivingWrite", "CTCT" },
        { true, "GivingClose", "TTCC" },
    };

    private static string Letters(TransferRule r)
    {
        static char L(Disposition d) => d switch { Disposition.Continue => 'C', Disposition.Terminate => 'T', _ => 'B' };
        return new string([L(r.FatalCompleted), L(r.FatalOtherwise), L(r.NonfatalCompleted), L(r.NonfatalOtherwise)]);
    }

    [Theory]
    [MemberData(nameof(Cells))]
    public void EveryCell_IsTheRuleThatDecidesIt(bool merge, string io, string cells)
        => Assert.Equal(cells, Letters(RuleFor(merge, Enum.Parse<TransferIo>(io))));

    [Fact]
    public void TheCellTable_CoversEveryVerbAndEveryAsIfStatement()
    {
        var covered = Cells.Select(row => ((bool)row[0], (string)row[1])).ToHashSet();
        foreach (bool merge in new[] { false, true })
            foreach (var io in Enum.GetValues<TransferIo>())
                Assert.Contains((merge, io.ToString()), covered);
    }

    /// <summary>A Bypass is only meaningful for a GIVING file (it skips that file's WRITE loop and CLOSE).</summary>
    [Fact]
    public void Bypass_OnlyOnAGivingOpen()
    {
        foreach (bool merge in new[] { false, true })
            foreach (var io in Enum.GetValues<TransferIo>())
            {
                var r = RuleFor(merge, io);
                bool any = Letters(r).Contains('B');
                if (any) Assert.Equal(TransferIo.GivingOpen, io);
            }
    }

    /// <summary>The USE hook is reached from ONE place in the SORT/MERGE emitter — <c>EmitTransferUse</c>, which every
    /// as-if statement's disposition goes through. A direct <c>seqIo.EmitUseHook(</c> call elsewhere would be an as-if
    /// statement whose status nothing disposes of.</summary>
    [Fact]
    public void SortEmitter_ReachesTheUseHookOnlyThroughTheDisposition()
    {
        string path = TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", "SortEmitter.cs");
        Assert.True(File.Exists(path), $"SortEmitter moved: {path}");
        var code = File.ReadAllLines(path).Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)
            && !l.TrimStart().StartsWith("///", StringComparison.Ordinal));
        int hooks = code.Count(l => l.Contains("seqIo.EmitUseHook(", StringComparison.Ordinal));
        Assert.Equal(1, hooks);
    }

    /// <summary>⛔ AN AS-IF STATEMENT ASKS THE EXPLICIT STATEMENT'S UNCOVERED-FATAL QUESTION (kb/Work PB2700, DOC-A.1-103).
    /// The abnormal termination of a fatal status nothing covers is rendered by ONE method,
    /// <c>SequentialIoEmitter.EmitUncoveredFatalTermination</c>, which is the only renderer of
    /// <c>TerminateOnUncoveredFatalStatus</c> in the code generator; and the SORT/MERGE emitter reaches it from ONE place,
    /// its disposition, so no as-if statement can skip it. Before PB2700 the hook suppressed it for every as-if statement
    /// and a SORT whose USING file was missing carried on silently.</summary>
    [Fact]
    public void UncoveredFatalTermination_HasOneRenderer_AndTheDispositionAsksIt()
    {
        string codegen = TestRepo.Src("Cobol.Net.Compiler", "CodeGen");
        var renderers = new List<string>();
        foreach (string file in Directory.EnumerateFiles(codegen, "*.cs", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(file) == "RuntimeApi.cs") continue;
            foreach (var (line, i) in File.ReadAllLines(file).Select((l, i) => (l.TrimStart(), i)))
                if (!line.StartsWith("//", StringComparison.Ordinal) && line.Contains("RuntimeApi.TerminateOnUncoveredFatalStatus(", StringComparison.Ordinal))
                    renderers.Add($"{Path.GetRelativePath(codegen, file)}:{i + 1}");
        }
        Assert.True(renderers.Count == 1 && renderers[0].StartsWith(Path.Combine("Verbs", "SequentialIoEmitter.cs"), StringComparison.Ordinal),
            "TerminateOnUncoveredFatalStatus must be rendered only by SequentialIoEmitter.EmitUncoveredFatalTermination:\n" + string.Join("\n", renderers));

        string sort = TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", "SortEmitter.cs");
        var code = File.ReadAllLines(sort).Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)).ToArray();
        Assert.Equal(1, code.Count(l => l.Contains("seqIo.EmitUncoveredFatalTermination(", StringComparison.Ordinal)));
    }

    /// <summary>§9.1.13.1's fatal class is asked through <c>IoStatusClass.Fatal</c> (which renders the runtime's
    /// <c>ExceptionCatalog.IsFatalIoStatus</c>) — never re-spelled in the emitters.</summary>
    [Fact]
    public void FatalIoStatus_IsRenderedOnlyByIoStatusClass()
    {
        string codegen = TestRepo.Src("Cobol.Net.Compiler", "CodeGen");
        var offenders = new List<string>();
        var direct = new Regex(@"IsFatalIoStatus\(", RegexOptions.Compiled);
        foreach (string file in Directory.EnumerateFiles(codegen, "*.cs", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(file) == "IoStatusClass.cs") continue;
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string t = lines[i].TrimStart();
                if (t.StartsWith("//", StringComparison.Ordinal)) continue;
                if (direct.IsMatch(t)) offenders.Add($"{Path.GetRelativePath(codegen, file)}:{i + 1}: {t}");
            }
        }
        Assert.True(offenders.Count == 0, "render IoStatusClass.Fatal instead:\n" + string.Join("\n", offenders));
    }
}
