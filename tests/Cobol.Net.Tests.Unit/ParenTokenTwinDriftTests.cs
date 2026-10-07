// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// Every source site that tests for the PLAIN paren token types shall either name the FUNCTION-ARGUMENT twins
/// (<c>FNARG_LPAREN</c> / <c>FNARG_RPAREN</c>) or say in a comment that it means the grouping paren ONLY.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ WHY THIS EXISTS, AND IT IS THE MOST EXPENSIVE LESSON OF fix-queue PB48. Splitting one lexeme into two
/// token types — the argument-list <c>(</c> of ISO §8.4.3.2.3 SR6 versus an arithmetic grouping <c>(</c> — is
/// invisible to the compiler's type system: every existing `t.Type is LPAREN` keeps compiling and silently
/// stops matching half the parens it used to. The GRAMMAR consumers were swept (`functionCall`, `refModPart`);
/// the CODE consumers were not, and the former legacy engine's <c>MapFunctionArgTokens</c> — which maps a nested call's
/// argument tokens down to their SUBSCRIPT-mode twins, and already had an arm for the FNARG_SEPARATOR twin —
/// silently dropped the paren twins. Cost: <b>31 NIST IF-suite regressions</b>, every one a clean compile that
/// threw <c>IndexOutOfRangeException</c> at RUN TIME, found only by the 11-minute comprehensive battery. The
/// wave-local gate, the full greenfield Conformance suite, the unit suite and the GnuCOBOL differential were
/// ALL GREEN on that tree (feedback_scan_all_similar, feedback_two_arm_dispatch).
/// </para>
/// <para>
/// ⚠ THE RULE IS "DECIDE", NOT "INCLUDE" — a blanket helper would be WRONG here, and that is the whole design.
/// The sites genuinely differ: <c>ZeroTokenRewriter</c> must see grouping parens ONLY (including the FNARG twin
/// is precisely the defect PB48 fixed), while <c>MapFunctionArgTokens</c> must see both. So this asserts that
/// the author CONSIDERED the twin, and leaves which answer is right to the site.
/// </para>
/// <para>
/// ⚠ It is a SOURCE-FORM guard because no runtime test can see the omission: the missing arm is not a wrong
/// answer at any single site, it is a whole category of input silently taking a different path. The
/// <c>DiagnosticEmitFormDriftTests</c> precedent — "no runtime test can see a mistake only a caller can make".
/// </para>
/// </remarks>
public sealed class ParenTokenTwinDriftTests
{
    /// <summary>The escape hatch, spelled so it is greppable and so writing it is a visible choice.</summary>
    private const string Marker = "GROUPING-PAREN-ONLY";

    /// <summary>A reference to the plain paren token type — <c>LPAREN</c>/<c>RPAREN</c> not prefixed by
    /// <c>SUB_</c> or <c>FNARG_</c>, and not part of a longer identifier.</summary>
    private static readonly Regex PlainParen = new(@"(?<![A-Z0-9_])(?<!SUB_)(?<!FNARG_)[LR]PAREN\b",
        RegexOptions.Compiled);

    /// <summary>How far from the match a satisfying mention may sit — enough for a doc-comment above the
    /// member, small enough that an unrelated mention elsewhere in the file cannot excuse the site.</summary>
    private const int Window = 12;

    /// <summary>One row per source file, as (project, path relative to the project). ⛔ The row carries no absolute
    /// path: a theory's arguments are part of its display name, and a worktree path there makes the name differ
    /// between worktrees, so the gate's plan could never key on it (kb/Work PB1719; GateLegDriftTests).</summary>
    public static IEnumerable<object[]> SourceFiles()
    {
        foreach (string proj in new[] { "Cobol.Net.Frontend", "Cobol.Net.Compiler" })
        {
            string root = TestRepo.Src(proj);
            // A renamed or moved project must fail loudly here, never shrink the scan to nothing.
            if (!Directory.Exists(root))
                throw new DirectoryNotFoundException($"ParenTokenTwinDriftTests scans {root}, which does not exist");
            foreach (string f in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                // Generated/ is a BUILD OUTPUT (feedback_generated_parser_is_a_build_output); obj/ likewise.
                string rel = Path.GetRelativePath(root, f).Replace('\\', '/');
                if (rel.StartsWith("Generated/", StringComparison.Ordinal)
                    || rel.StartsWith("obj/", StringComparison.Ordinal)
                    || rel.StartsWith("bin/", StringComparison.Ordinal)
                    || rel.EndsWith(".g.cs", StringComparison.Ordinal)) continue;
                yield return [proj, rel];
            }
        }
    }

    private static string PathOf(string proj, string rel) => TestRepo.Src([proj, .. rel.Split('/')]);

    [Theory]
    [MemberData(nameof(SourceFiles))]
    public void EveryPlainParenSite_NamesTheFnargTwin_OrDeclaresGroupingOnly(string proj, string rel)
    {
        string[] lines = File.ReadAllLines(PathOf(proj, rel));
        var unconsidered = new List<int>();
        for (int i = 0; i < lines.Length; i++)
        {
            if (!PlainParen.IsMatch(lines[i])) continue;
            int lo = Math.Max(0, i - Window), hi = Math.Min(lines.Length - 1, i + Window);
            bool considered = false;
            for (int j = lo; j <= hi && !considered; j++)
                considered = lines[j].Contains("FNARG_", StringComparison.Ordinal)
                          || lines[j].Contains(Marker, StringComparison.Ordinal);
            if (!considered) unconsidered.Add(i + 1);
        }

        Assert.True(unconsidered.Count == 0,
            $"{proj}/{rel} line(s) {string.Join(", ", unconsidered)} test the PLAIN paren token type without "
            + $"considering the FUNCTION-ARGUMENT twin. The lexer types the '(' after `FUNCTION <name>` as "
            + $"FNARG_LPAREN (ISO §8.4.3.2.3 SR6), so a site matching only LPAREN/RPAREN silently stops seeing "
            + $"every function argument list — which is how PB48 shipped 31 NIST regressions past a green "
            + $"wave-local gate. Either handle FNARG_LPAREN/FNARG_RPAREN, or write {Marker} in a comment on or "
            + $"near the line saying why the grouping paren alone is meant.");
    }

    /// <summary>The guard is only worth its runtime if it is actually looking at the real sites — a file-per-row
    /// theory that matched nothing would pass forever and prove nothing
    /// (feedback_green_gates_arent_evidence).</summary>
    [Fact]
    public void TheGuard_ActuallyInspectsTheKnownParenSites()
    {
        var seen = new List<string>();
        foreach (object[] row in SourceFiles())
        {
            string proj = (string)row[0], rel = (string)row[1];
            if (File.ReadAllLines(PathOf(proj, rel)).Any(l => PlainParen.IsMatch(l))) seen.Add(rel);
        }
        // The sites the PB48 sweep enumerated. Each must still be REACHED by the theory above; if one is
        // renamed the list is wrong and this says so, rather than the theory silently covering less.
        foreach (string expected in new[]
                 { "Parsing/ZeroTokenRewriter.cs", "Parsing/CobolParserCoreBase.cs", "Binding/ReferenceResolver.cs" })
        {
            Assert.True(seen.Any(s => s.EndsWith(expected, StringComparison.Ordinal)),
                $"the paren-twin guard no longer inspects {expected} — it was one of the sites the PB48 "
                + $"sweep found, so either it moved (update this list) or the matcher stopped working. "
                + $"Inspected: {string.Join(", ", seen)}");
        }
    }
}
