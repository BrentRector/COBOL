using System.Reflection;
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Binding.Bound;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>kb/Work PB1029 — a refusal must carry its diagnostic IN THE TYPE. Before this, ~140 binder sites
/// reported an error and then returned an ordinary <c>BoundNop</c>, ~110 more returned one after a callee that was
/// merely TRUSTED to have reported, and the operand-level error nodes (<c>BoundExprError</c>,
/// <c>BoundOperandError</c>, <c>BoundBoolError</c>) had public constructors, so a refusal could be built with no
/// report at all: <c>STRING ALL "AB" … INTO X</c> (§14.9.43.3 SR2), <c>INSPECT … FOR ALL 1</c> (§14.9.22.3 SR3),
/// <c>B-AND SPACE</c> (§8.8.2) and <c>SUM(T(ALL, ALL))</c> over a one-dimensional table (§8.4.2.3.3 SR3) all
/// compiled clean and aborted the run unit.
/// <para>The shape now: every refusal node is obtainable only through a factory that puts it on the
/// <see cref="EditionContext"/> refusal ledger, and <c>StatementBinder.BindStatement</c> fails the compile with
/// COBOLNET2362 when a statement bound a refusal and drew no error. These tests keep the constructors closed and
/// keep the two old idioms — report-then-no-op, and failure-guarded no-op — out of the binder.</para></summary>
[Trait("Sweep", "tree")]
public sealed class RefusalNodeDriftTests
{
    private static readonly Type[] RefusalNodes =
        [typeof(BoundRejected), typeof(BoundExprError), typeof(BoundOperandError), typeof(BoundBoolError)];

    [Fact]
    public void RefusalNodes_HaveNoNonPrivateConstructor()
    {
        var open = RefusalNodes
            .SelectMany(t => t.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(c => !c.IsPrivate).Select(c => $"{t.Name}({string.Join(", ", c.GetParameters().Select(p => p.ParameterType.Name))})"))
            .ToList();
        Assert.True(open.Count == 0, "a refusal node has a non-private constructor, so it can be built without "
            + "going on the refusal ledger (kb/Work PB1029):\n  " + string.Join("\n  ", open));
    }

    /// <summary>The ledger's backstop, measured: a refusal bound with no error anywhere fails the compile, and an
    /// unbuilt shape does not (the funnel announces it as COBOLNET1756 instead).</summary>
    [Fact]
    public void Refused_WithNoErrorRecorded_FailsTheCompile_Unbuilt_DoesNot()
    {
        var refused = new EditionContext(2023);
        BoundOperandError.Refused(refused, "probe operand");
        Assert.Contains(refused.Diagnostics, d => d.Contains("COBOLNET2362", StringComparison.Ordinal));

        var unbuilt = new EditionContext(2023);
        BoundExprError.Unbuilt(unbuilt, "probe shape");
        Assert.Empty(unbuilt.Diagnostics);

        var reported = new EditionContext(2023);
        reported.Error("COBOLNET1757", "the rule the source broke");
        BoundBoolError.Refused(reported, "probe boolean");
        BoundRejected.Reported(reported);
        Assert.Single(reported.Diagnostics);
    }

    // ── the source scan ─────────────────────────────────────────────────────────────────────────────────────

    private static readonly Regex Nop = new(@"new\s+BoundNop\s*\(\s*\)", RegexOptions.Compiled);
    private static readonly Regex Report = new(@"\.(Error|Removed)\s*\(", RegexOptions.Compiled);
    /// <summary>A no-op returned on a FAILURE arm: a null/false guard on the same or the preceding line.</summary>
    private static readonly Regex FailureGuard = new(@"is not \{|is null\b|\bif \(!|\bbad \?|RefusedIn\w*\(",
        RegexOptions.Compiled);

    /// <summary>Every <c>new BoundNop()</c> that is a refusal in disguise: one in the same block as (and after) a
    /// report, or one on a failure-guarded arm. A genuine no-op statement (CONTINUE, bare EXIT, the '85 ENTER, an
    /// accept-inert declined facility) is neither.</summary>
    internal static IEnumerable<string> Disguised(string text)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (!Nop.IsMatch(lines[i]) || lines[i].TrimStart().StartsWith("///", StringComparison.Ordinal)) continue;
            // The one explicit exemption: a genuine no-op on a guarded arm says so, and says why.
            if (lines[i].Contains("// no-op:", StringComparison.Ordinal)) continue;
            string here = lines[i] + "\n" + (i > 0 ? lines[i - 1] : "");
            if (FailureGuard.IsMatch(here)) { yield return $"line {i + 1} (failure-guarded): {lines[i].Trim()}"; continue; }
            int depth = 0;
            for (int j = i - 1; j >= 0 && depth >= 0; j--)
            {
                string code = Regex.Replace(lines[j], "\"(?:[^\"\\\\]|\\\\.)*\"", "\"\"");
                int cut = code.IndexOf("//", StringComparison.Ordinal);
                if (cut >= 0) code = code[..cut];
                depth += code.Count(c => c == '}') - code.Count(c => c == '{');
                if (depth <= 0 && Report.IsMatch(code))
                {
                    yield return $"line {i + 1} (after a report at line {j + 1}): {lines[i].Trim()}";
                    break;
                }
            }
        }
    }

    [Fact]
    public void NoRefusal_IsWrittenAsABoundNop()
    {
        var root = TestRepo.Src("Cobol.Net.Compiler");
        var files = Directory.EnumerateFiles(Path.Combine(root, "Binding"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                     && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .ToList();
        Assert.True(files.Count > 50, $"the scan found only {files.Count} source files — it measured nothing");
        int nops = 0;
        var findings = new List<string>();
        foreach (var f in files)
        {
            string text = File.ReadAllText(f);
            nops += Nop.Matches(text).Count;
            findings.AddRange(Disguised(text).Select(h => $"{Path.GetFileName(f)} {h}"));
        }
        Assert.True(nops > 0, "no `new BoundNop()` found at all — the scan's subject moved");
        Assert.True(findings.Count == 0,
            "a refusal is written as a BoundNop (kb/Work PB1029):\n  " + string.Join("\n  ", findings)
            + "\nReport-then-refuse is BoundRejected.Report(edition, rule, message); a refusal a callee reported is "
            + "BoundRejected.Reported(edition); a null from the reference resolver is a BoundUnsupported (the "
            + "statement funnel decides reported-vs-unbuilt).");
    }

    /// <summary>The scan's failure branches, fired once (feedback_green_gates_arent_evidence): the exact
    /// pre-PB1029 ACCEPT index-name refusal and the pre-PB1029 file-name resolution arm.</summary>
    [Fact]
    public void TheScan_ActuallyFails_OnThePrePb1029Shapes()
    {
        const string reportThenNop = """
                    {
                        ctx.Edition.Error("COBOLNET1637", $"ACCEPT receiver '{DataBinder.WrittenText(dref)}' is an index-name — an "
                            + "index-name is not an identifier (ISO §8.4.3.1.2)");
                        return new BoundNop();   // reported above — not a deferral (kb/Work PB236)
                    }
            """;
        const string guardedNop = """
                    if (!ctx.Validation.ResolveFile(name, "DELETE", out var file)) return new BoundNop();
            """;
        const string genuine = """
                if (cont.arithmeticExpression() is { } secs) return Timed(secs);
                return new BoundNop();   // plain CONTINUE — a §14.9.9 no-op
            """;
        const string exempted = """
                if (cont.arithmeticExpression() is not { } secs) return new BoundNop();   // no-op: plain CONTINUE (§14.9.9)
            """;
        Assert.Single(Disguised(reportThenNop));
        Assert.Single(Disguised(guardedNop));
        Assert.Empty(Disguised(genuine));
        Assert.Empty(Disguised(exempted));
        Assert.Single(Disguised(exempted.Replace("// no-op:", "//")));
    }
}
