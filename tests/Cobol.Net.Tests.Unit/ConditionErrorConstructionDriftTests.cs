using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>kb/Work PB982 — a condition error node reaches a successful compile only if its refusal forgot the
/// diagnostic, and the emitter lowers it to a run-time <c>NotImplemented</c> throw: <c>IF WS-X</c> over a PIC X item
/// compiled clean and aborted the run unit. <c>ConditionBinder.Refused</c> is now the ONE construction site of
/// <c>BoundConditionError</c>, and it fails the compile (COBOLNET2319) when no failing diagnostic has been recorded.
/// This test keeps every other <c>new BoundConditionError(</c> out of the compiler, so a new refusal path cannot
/// reopen the silent-compile / loud-run channel by writing its own.</summary>
[Trait("Sweep", "tree")]
public sealed class ConditionErrorConstructionDriftTests
{
    private static readonly Regex Construction = new(@"new\s+BoundConditionError\s*\(", RegexOptions.Compiled);

    private static IEnumerable<string> Constructions(string text) =>
        text.Split('\n').Select((l, i) => (l, i)).Where(x => Construction.IsMatch(x.l))
            .Select(x => $"line {x.i + 1}: {x.l.Trim()}");

    [Fact]
    public void BoundConditionError_IsConstructedOnlyByConditionBinderRefused()
    {
        var root = TestRepo.Src("Cobol.Net.Compiler");
        var files = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                     && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .ToList();
        Assert.True(files.Count > 50, $"the scan found only {files.Count} source files under {root} — it measured nothing");

        var findings = new List<string>();
        bool sawTheOneSite = false;
        foreach (var f in files)
        {
            string text = File.ReadAllText(f);
            if (Path.GetFileName(f) == "ConditionBinder.cs")
            {
                // The one permitted site: the body of Refused, whose return is the only construction.
                const string oneSite = "        return new BoundConditionError(feature);";
                sawTheOneSite = text.Contains(oneSite, StringComparison.Ordinal)
                    && text.Contains("internal BoundConditionError Refused(string feature)", StringComparison.Ordinal);
                text = text.Replace(oneSite, "", StringComparison.Ordinal);
            }
            findings.AddRange(Constructions(text).Select(h => $"{Path.GetFileName(f)} {h}"));
        }
        Assert.True(sawTheOneSite, "ConditionBinder.Refused(string feature) and its one construction were not found — "
            + "the scan's exemption no longer names the site it was written for");
        Assert.True(findings.Count == 0,
            "BoundConditionError constructed outside ConditionBinder.Refused:\n  " + string.Join("\n  ", findings)
            + "\nRoute the refusal through Refused (report the rule's own diagnostic first): a diagnostic-less "
            + "condition error node compiles clean and aborts the run unit when reached (kb/Work PB982).");
    }

    /// <summary>The scan's failure branch, fired once (feedback_green_gates_arent_evidence): the exact pre-PB982
    /// return in <c>BindSoleOperandCondition</c> must be reported.</summary>
    [Fact]
    public void TheScan_ActuallyFails_OnThePb982Return()
    {
        const string pre = """
            if (carry is { Subject: { } subject, Op: { } op })
                return CheckedRelational(subject, op, bindOperand());
            return new BoundConditionError($"condition '{vo?.GetText() ?? "operand"}'");
            """;
        Assert.Single(Constructions(pre));
    }
}
