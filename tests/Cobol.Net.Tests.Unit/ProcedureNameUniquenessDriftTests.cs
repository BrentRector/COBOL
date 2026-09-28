// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet;
using CobolNet.Tests.Shared;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB466 — the invariant that keeps procedure-name resolution able to ANSWER the question ISO §8.4.2.2.1
/// asks: <b>a procedure-name scope records every declaration of a spelling, so "is this reference unique?" is
/// still decidable at the reference.</b>
///
/// <para><b>Why a drift test and not just the goldens.</b> The defect was not a wrong branch, it was a
/// DESTROYED FACT: five scopes registered their names with <c>Dictionary.TryAdd</c>, whose documented contract
/// is to keep the first and discard the rest, so by the time a GO TO asked whether <c>DUP-P</c> was ambiguous
/// the multiplicity no longer existed anywhere in the compiler. A conformance golden can only prove that
/// today's five scopes behave; it cannot stop a SIXTH scope from being added next year with the same
/// one-line habit, and that scope's programs would compile silently wrong exactly as these did. So the shape
/// itself is pinned here, over the source of the procedure-binding files, and the behaviour is pinned beside
/// it end to end.</para>
///
/// <para>The rules being kept true: §8.4.2.2.1 ("uniqueness shall be established through qualification for each
/// user-defined name explicitly referenced" — rule 1 "No other name has the identical spelling", rule 6 the
/// paragraph/section excuse) and §8.4.2.2.3 SR1 and SR7.</para>
/// </summary>
public sealed class ProcedureNameUniquenessDriftTests
{
    /// <summary>The files that own a procedure-name SCOPE. A new one belongs in this list — the point is that
    /// adding a scope is a deliberate act, not a silent one.</summary>
    private static readonly string[] ScopeSources =
    [
        TestRepo.Src("Cobol.Net.Compiler", "Binding", "Procedure", "ProcedureTableBuilder.cs"),
        TestRepo.Src("Cobol.Net.Compiler", "Binding", "Procedure", "SectionInfo.cs"),
        TestRepo.Src("Cobol.Net.Compiler", "Binding", "Procedure", "OoMethodScope.cs"),
        TestRepo.Src("Cobol.Net.Compiler", "Binding", "Bound", "StatementBinder.cs"),
    ];

    /// <summary>A registration into a paragraph/section name map that SWALLOWS a repeat. Matched on the
    /// receiver, so it fires for any of the four maps and for one a later scope invents with the same
    /// names — <c>…Paras.TryAdd(…)</c>, <c>…Sections.TryAdd(…)</c>, <c>_paraIndex.TryAdd(…)</c>,
    /// <c>_sections.TryAdd(…)</c>, and the indexer form, which overwrites instead.</summary>
    private static readonly Regex Swallowing = new(
        @"(?<!\w)(_paraIndex|_sections|\w*\.\s*Paras|\w*\.\s*Sections)\s*(\.\s*TryAdd\s*\(|\[)",
        RegexOptions.Compiled);

    /// <summary>⛔ NO PROCEDURE-NAME DECLARATION IS RECORDED WITH <c>TryAdd</c> (or an overwriting indexer).
    /// Every one goes through <c>ProcedureNameMap.Declare</c>, which KEEPS the repeat.</summary>
    [Fact]
    public void NoProcedureNameScopeSwallowsARedeclaration()
    {
        var found = new List<string>();
        foreach (string path in ScopeSources)
        {
            Assert.True(File.Exists(path), $"scope source missing — this list is stale: {path}");
            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                string code = StripComment(lines[i]);
                if (Swallowing.IsMatch(code))
                    found.Add($"{Path.GetFileName(path)}:{i + 1}: {lines[i].Trim()}");
            }
        }
        Assert.True(found.Count == 0,
            "a procedure-name scope registers a declaration with TryAdd or an overwriting indexer, so a repeated "
            + "spelling is discarded and ISO §8.4.2.2.1 can no longer be answered at the reference. Use "
            + "ProcedureNameMap.Declare (kb/Work PB466):\n" + string.Join("\n", found));
    }

    /// <summary>The scanner FAILS on the pre-fix source, verbatim from <c>AddParagraph</c> as it stood, and
    /// passes the line that replaced it. Without this, a green run above would prove only that the scanner found
    /// nothing — not that it can find anything.</summary>
    [Fact]
    public void TheScannerFlagsThePreFixRegistration()
    {
        foreach (string preFix in new[]
        {
            "            ms.Paras.TryAdd(name, _paras.Count);",
            "            _paraIndex.TryAdd(name, _paras.Count);",
            "        section?.Paras.TryAdd(name, _paras.Count);",
            "                _sections.TryAdd(info.Name, info);",
            "                        scope.Sections.TryAdd(info.Name, info);",
            "        _paraIndex[name] = _paras.Count;",   // the overwriting form, never written but forbidden too
        })
        {
            Assert.Matches(Swallowing, preFix);
        }

        foreach (string landed in new[]
        {
            "            _paraIndex.Declare(name, _paras.Count);",
            "        section?.Paras.Declare(name, _paras.Count);",
            "            if (q.Paras.IsDuplicated(head))",
        })
        {
            Assert.DoesNotMatch(Swallowing, landed);
        }
    }

    /// <summary>A comment may quote the forbidden idiom while explaining it — the files here do exactly that —
    /// so the scanner reads CODE. (The golden-runner's own option header was bitten by the contains-test form of
    /// this mistake; the lesson generalizes.)</summary>
    private static string StripComment(string line)
    {
        int slashes = line.IndexOf("//", StringComparison.Ordinal);
        int doc = line.IndexOf("///", StringComparison.Ordinal);
        int cut = doc >= 0 ? doc : slashes;
        return cut >= 0 ? line[..cut] : line;
    }

    // ── The behaviour, end to end ──────────────────────────────────────────────────────────────────────────

    private const string Ambiguous = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB466DRIFTA.
        PROCEDURE DIVISION.
        S-REF SECTION.
        P-REF.
            GO TO DUP-P.
        S-ONE SECTION.
        DUP-P.
            DISPLAY "ONE".
            STOP RUN.
        S-TWO SECTION.
        DUP-P.
            DISPLAY "TWO".
            STOP RUN.
        """;

    private const string Excused = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB466DRIFTB.
        PROCEDURE DIVISION.
        S-REF SECTION.
        P-REF.
            PERFORM DUP-P IN S-TWO.
            GO TO DUP-P IN S-ONE.
        S-ONE SECTION.
        R6-ONE.
            GO TO DUP-P.
        DUP-P.
            DISPLAY "ONE".
            STOP RUN.
        S-TWO SECTION.
        DUP-P.
            DISPLAY "TWO".
        """;

    /// <summary>⛔ AT EVERY EDITION. §8.4.2.2 is unchanged across 85/2002/2014/2023, so a per-edition arm that
    /// forgot the rule would be a conformance hole the single-edition goldens cannot see.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void AnUnqualifiedDuplicatedParagraphNameIsRejected(int edition)
    {
        var errors = Compile("ambiguous", Ambiguous, edition);
        Assert.Contains(errors, e => e.Contains("COBOLNET2121", StringComparison.Ordinal));
    }

    /// <summary>…and the two legal shapes stay legal, at every edition: §8.4.2.2.2 format 4 qualification, and
    /// rule 6's same-section excuse. A "reject anything duplicated" over-correction fails here.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void AQualifiedOrSameSectionReferenceIsAccepted(int edition)
    {
        var errors = Compile("excused", Excused, edition);
        Assert.Empty(errors);
    }

    private static List<string> Compile(string tag, string source, int edition)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_PB466_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, $"{tag}_{edition}.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(src, DialectLevel: edition, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            return [.. r.Errors];
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }
}
