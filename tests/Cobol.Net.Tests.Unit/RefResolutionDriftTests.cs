using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>kb/Work PB1030 — the reference resolver answers a CLOSED <see cref="RefResolution"/> (Place | Reported |
/// Deferred), and a silent null is unrepresentable only while three things stay true: every deferred shape is in
/// the <see cref="DeferredShape"/> census with a description, the resolver puts every deferral on the unbuilt ledger
/// before a caller sees it, and a caller that gets no place builds its refusal FROM the answer instead of writing
/// its own (the pre-PB1030 shapes: an "unresolvable" error on top of the resolver's COBOLNET1639, or a run-time
/// NotImplemented for an ILLEGAL subscript).</summary>
[Trait("Sweep", "tree")]
public sealed class RefResolutionDriftTests
{
    /// <summary>Every census member has its text and owner — <see cref="DeferredShapes.Describe"/> throws for one
    /// that has not, so a new deferral cannot ship without saying what it is.</summary>
    [Fact]
    public void EveryDeferredShape_IsDescribed()
    {
        var shapes = Enum.GetValues<DeferredShape>();
        Assert.NotEmpty(shapes);
        foreach (var shape in shapes)
            Assert.False(string.IsNullOrWhiteSpace(DeferredShapes.Describe(shape)), $"DeferredShape.{shape} has no description");
    }

    /// <summary>The resolver records a deferral on the unbuilt ledger in BOTH commit entries, so the statement
    /// funnel announces it (COBOLNET1756) whatever the caller does with the answer.</summary>
    [Fact]
    public void BothCommitEntries_LedgerTheirDeferrals()
    {
        string text = File.ReadAllText(Path.Combine(TestRepo.Src("Cobol.Net.Compiler"), "Binding", "ReferenceResolver.cs"));
        Assert.Equal(2, Regex.Matches(text, @"answer\.Outcome == RefOutcome\.Deferred\) data\.Edition\.NoteUnbuilt\(answer\.Feature\)").Count);
        Assert.Contains("public RefResolution Resolve(Core.DataReferenceContext dref)", text, StringComparison.Ordinal);
        Assert.Contains("public RefResolution ResolveForItem(Core.DataReferenceContext dref, DataItem item)", text, StringComparison.Ordinal);
    }

    // A resolver answer tested for "no place", and the lines that follow it up to the refusal the site returns.
    private static readonly Regex NoPlace = new(
        @"(?:ResolveSending|Refs\.Resolve|ResolveForItem)\([^\n]*\bis var (?<a>\w+) && \k<a>\.Place is not \{ \}",
        RegexOptions.Compiled);

    /// <summary>Every "no place" branch over a resolver answer USES the answer within its next lines — a refusal
    /// built from it (<c>Refusal</c> / <c>OperandError</c> / <c>ExprError</c> / <c>BoolError</c>), its outcome
    /// read, or the answer handed to a helper — rather than binding a node of its own.</summary>
    private static IEnumerable<string> Unanswered(string text)
    {
        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var m = NoPlace.Match(lines[i]);
            if (!m.Success) continue;
            string a = m.Groups["a"].Value;
            string window = string.Join("\n", lines.Skip(i).Take(3));
            bool used = Regex.IsMatch(window, $@"\b{a}\.(Refusal|OperandError|ExprError|BoolError|Outcome)\b")
                        || Regex.IsMatch(window, $@"\(\s*[^()]*\b{a}\s*[,)]");
            if (!used) yield return $"line {i + 1}: {lines[i].Trim()}";
        }
    }

    [Fact]
    public void EveryNoPlaceBranch_BuildsItsRefusalFromTheAnswer()
    {
        var root = TestRepo.Src("Cobol.Net.Compiler");
        var files = Directory.EnumerateFiles(Path.Combine(root, "Binding"), "*.cs", SearchOption.AllDirectories).ToList();
        Assert.True(files.Count > 50, $"the scan found only {files.Count} source files — it measured nothing");
        int sites = 0;
        var findings = new List<string>();
        foreach (var f in files)
        {
            string text = File.ReadAllText(f);
            sites += NoPlace.Matches(text).Count;
            findings.AddRange(Unanswered(text).Select(h => $"{Path.GetFileName(f)} {h}"));
        }
        Assert.True(sites >= 15, $"only {sites} no-place branches found — the pattern no longer matches the tree");
        Assert.True(findings.Count == 0,
            "a resolver answer with no place is answered by a node the site built itself:\n  " + string.Join("\n  ", findings)
            + "\nBuild the refusal from the answer (RefResolution.Refusal / OperandError / ExprError / BoolError, or "
            + "PlaceOrReported where the caller cannot carry a deferral) — kb/Work PB1030.");
    }

    /// <summary>The scan's failure branch, fired once (feedback_green_gates_arent_evidence): the pre-PB1030 SET ENTRY
    /// shape — a second, rule-less "unresolvable" error of the site's own — is reported.</summary>
    [Fact]
    public void TheScan_ActuallyFails_OnASiteThatIgnoresTheAnswer()
    {
        const string pre = """
            if (host.Expr.ResolveSending(drefs[^1]) is var entryAnswer && entryAnswer.Place is not { } namePlace)
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                    $"SET {written} TO ENTRY: the ENTRY identifier is unresolvable");
            """;
        Assert.Single(Unanswered(pre));
    }
}
