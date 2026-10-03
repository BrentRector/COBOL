// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Binding.Bound;
using CobolNet.Tests.Shared;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ kb/Work PB909 — <c>BoundUnsupported</c>'s TWO JOBS, split into two TYPES. The deferral carrier meant both
/// "WiseOwl COBOL has not built this" and "no general format prints this", so an ungrammatical statement and a
/// missing feature looked the same: INSPECT … REPLACING TRAILING and SEARCH … NOT AT END compiled with a
/// COBOLNET1756 warning and aborted the run unit, where ISO §4.2.2 ¶2 owes a compile-time indication of
/// "violations of the general formats and the explicit syntax rules of standard COBOL".
/// <para>The split: <see cref="BoundRejected"/> is a REFUSAL, obtainable only through
/// <see cref="BoundRejected.Report"/>, which records an error first — so a refusal can be neither silent nor
/// compiled; <see cref="BoundUnsupported"/> is a DEFERRAL and nothing else. These tests pin the three things that
/// keep it that way: no deferral site states a violated rule as its reason (the SCAN — it is what makes the next
/// site automatic, since a new site that holds a citation for why the source is wrong fails here); the refusal
/// type cannot be built around its report; and the measured members refuse, while a genuine deferral still
/// compiles and still announces itself.</para>
/// </summary>
public sealed class BoundDeferralDriftTests
{
    /// <summary>The vocabulary of a violated rule. A deferral names what WiseOwl COBOL has not built; if its message
    /// needs a syntax-rule number, "shall", or "extension", the SOURCE is what is wrong and the node is wrong.
    /// General-rule (GR) citations stay legal — a deferral may name the semantics it has not built.</summary>
    private static readonly Regex RuleVocabulary = new(
        @"\bSR\s?\d|syntax rule|\bshall\b|extension|general format|non-ISO|\bFormat \d",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    [Fact]
    public void NoDeferralSite_StatesAViolatedRuleAsItsReason()
    {
        var sites = new List<(string File, int Line, string Args)>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs",
                     SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;
            string text = File.ReadAllText(file);
            const string marker = "new BoundUnsupported(";
            for (int at = text.IndexOf(marker, StringComparison.Ordinal); at >= 0;
                 at = text.IndexOf(marker, at + 1, StringComparison.Ordinal))
            {
                int lineStart = text.LastIndexOf('\n', at) + 1;
                if (text.AsSpan(lineStart, at - lineStart).TrimStart().StartsWith("//")) continue;   // prose about the type
                int open = at + marker.Length - 1, depth = 0, end = open;
                for (; end < text.Length; end++)
                {
                    if (text[end] == '(') depth++;
                    else if (text[end] == ')' && --depth == 0) break;
                }
                int line = 1 + text.AsSpan(0, at).Count('\n');
                sites.Add((Path.GetFileName(file), line, text[(open + 1)..end]));
            }
        }
        // ⛔ A SCAN THAT FINDS NOTHING PASSES FOR THE WRONG REASON — assert the population it read.
        Assert.True(sites.Count >= 30, $"the scan found only {sites.Count} deferral sites — has the type moved?");
        var offenders = sites.Where(s => RuleVocabulary.IsMatch(s.Args))
            .Select(s => $"{s.File}:{s.Line}: {s.Args.ReplaceLineEndings(" ")}").ToList();
        Assert.True(offenders.Count == 0,
            "a BoundUnsupported whose message states a violated rule is a refusal — build it with "
            + "BoundRejected.Report instead (kb/Work PB909):\n" + string.Join("\n", offenders));
    }

    /// <summary>The refusal cannot be constructed around its diagnostic: no public or internal constructor, and
    /// <see cref="BoundRejected.Report"/> (a descriptor or a bare code) and <see cref="BoundRejected.Reported"/> (a callee
    /// reported; the statement funnel verifies it — kb/Work PB1029) are the only factories.</summary>
    [Fact]
    public void BoundRejected_IsObtainableOnlyThroughReport()
    {
        var ctors = typeof(BoundRejected).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.All(ctors, c => Assert.True(c.IsPrivate, $"{c} is not private"));
        var factories = typeof(BoundRejected).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => m.ReturnType == typeof(BoundRejected)).Select(m => m.Name).ToList();
        Assert.Equal(["Report", "Report", "Reported"], factories.Order(StringComparer.Ordinal));
    }

    private const string Head = """
IDENTIFICATION DIVISION.
PROGRAM-ID. PB909{0}.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT IX-FILE ASSIGN TO "pb909.dat" ORGANIZATION INDEXED
        ACCESS MODE DYNAMIC RECORD KEY IX-KEY.
DATA DIVISION.
FILE SECTION.
FD IX-FILE.
01 IX-REC.
   05 IX-KEY PIC X(4).
   05 IX-DATA PIC X(8).
WORKING-STORAGE SECTION.
01 S PIC X(6) VALUE "AB    ".
01 N PIC 9(4) VALUE 0.
01 T.
   05 E PIC 9(2) OCCURS 3 INDEXED BY IX.
01 TT.
   05 TROW OCCURS 3.
      10 TCELL PIC 9 OCCURS 2.
PROCEDURE DIVISION.
MAIN.
    GO TO SKIPPER.
""";

    /// <summary>Each measured member of the illegal-shape population, behind a GO TO so the flow never reaches
    /// it — the case the old deferral left entirely silent at run time. Each must be an ERROR under the code
    /// that names its rule, and must NOT be the deferral announce.</summary>
    [Theory]
    [InlineData("A", "INSPECT S REPLACING TRAILING \" \" BY \"*\".", "COBOLNET2269", "REPLACING TRAILING")]
    [InlineData("B", "INSPECT S TALLYING N FOR TRAILING \" \".", "COBOLNET2269", "FOR TRAILING")]
    [InlineData("C", "INSPECT S TALLYING N FOR FIRST \" \".", "COBOLNET2269", "FOR FIRST")]
    [InlineData("D", "SEARCH E AT END DISPLAY \"N\" NOT AT END DISPLAY \"Y\" WHEN E(IX) = 1 DISPLAY \"H\" END-SEARCH.",
        "COBOLNET2269", "SEARCH … NOT AT END")]
    [InlineData("E", "ENTRY \"PB909ENTRY\".", "COBOLNET2269", "ENTRY")]
    [InlineData("F", "OPEN OUTPUT IX-FILE. WRITE IX-REC AFTER ADVANCING 1 LINE.", "COBOLNET1720", "SR3")]
    [InlineData("G", "SORT TROW ASCENDING KEY TCELL.", "COBOLNET1757", "SR14 e")]
    public void IllegalShape_IsARefusal_NeverADeferral(string id, string stmt, string code, string text)
    {
        var (ok, errors, warnings) = Compile(string.Format(Head, id) + "    " + stmt + "\nSKIPPER.\n    STOP RUN.\n");
        Assert.False(ok);
        Assert.Contains(errors, e => e.Contains(code, StringComparison.Ordinal)
                                     && e.Contains(text, StringComparison.Ordinal));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1756", StringComparison.Ordinal));
    }

    /// <summary>The three Report Writer operand rules that used to stage a deferral naming their own syntax rule:
    /// INITIATE §14.9.21.3 SR1 and TERMINATE §14.9.46.3 SR1 ("Report-name-1 shall be defined by a report
    /// description entry in the report section") and GENERATE §14.9.16.3 SR1/SR2 (a detail report group, or a
    /// report). <c>ReportGroupResolution.Match.None</c> is undiagnosed by contract, so the GENERATE arm is the
    /// report — each now an ERROR under COBOLNET1757, and never the deferral announce.</summary>
    [Theory]
    [InlineData("INITIATE NO-SUCH-RPT.", "§14.9.21.3 SR1")]
    [InlineData("TERMINATE NO-SUCH-RPT.", "§14.9.46.3 SR1")]
    [InlineData("GENERATE NO-SUCH-GRP.", "§14.9.16.3 SR1")]
    public void ReportWriterOperand_NamingNoReport_IsARefusal(string stmt, string rule)
    {
        string src = $"""
IDENTIFICATION DIVISION.
PROGRAM-ID. PB909RW.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT PRT-FILE ASSIGN TO "pb909rw.txt".
DATA DIVISION.
FILE SECTION.
FD PRT-FILE REPORT IS RPT.
REPORT SECTION.
RD RPT.
01 DET TYPE DETAIL LINE PLUS 1.
   05 COLUMN 1 PIC X(5) VALUE "HELLO".
PROCEDURE DIVISION.
MAIN.
    GO TO SKIPPER.
    {stmt}
SKIPPER.
    STOP RUN.
""";
        var (ok, errors, warnings) = Compile(src);
        Assert.False(ok);
        Assert.Contains(errors, e => e.Contains("COBOLNET1757", StringComparison.Ordinal)
                                     && e.Contains(rule, StringComparison.Ordinal));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1756", StringComparison.Ordinal));
    }

    /// <summary>SEARCH ALL is the SECOND arm of the same dispatch (feedback_two_arm_dispatch) — its own row.</summary>
    [Fact]
    public void SearchAll_NotAtEnd_IsRefusedToo()
    {
        const string src = """
IDENTIFICATION DIVISION.
PROGRAM-ID. PB909SA.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 T.
   05 E PIC 9(2) OCCURS 3 ASCENDING KEY E INDEXED BY IX.
PROCEDURE DIVISION.
    SEARCH ALL E AT END DISPLAY "N" NOT AT END DISPLAY "Y" WHEN E(IX) = 1 DISPLAY "H" END-SEARCH.
    STOP RUN.
""";
        var (ok, errors, warnings) = Compile(src);
        Assert.False(ok);
        Assert.Contains(errors, e => e.Contains("COBOLNET2269", StringComparison.Ordinal)
                                     && e.Contains("SEARCH ALL … NOT AT END", StringComparison.Ordinal));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1756", StringComparison.Ordinal));
    }

    /// <summary>The funnel's classification of an unresolvable operand: a subscript on a non-table item is
    /// COBOLNET2096 (§8.4.2.3.3 SR2), and the INSPECT whose identifier-1 it was used to ALSO announce "not
    /// implemented". A statement whose bind already drew an error makes no claim about the compiler.</summary>
    [Fact]
    public void RefusedOperand_DrawsNoDeferralAnnounce()
    {
        var (ok, errors, warnings) = Compile(string.Format(Head, "H")
            + "    INSPECT S(1) TALLYING N FOR ALL \"A\".\nSKIPPER.\n    STOP RUN.\n");
        Assert.False(ok);
        Assert.Contains(errors, e => e.Contains("COBOLNET2096", StringComparison.Ordinal));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1756", StringComparison.Ordinal));
    }

    /// <summary>And the other half, which a compiler that refused everything would fail: legal source the
    /// compiler has not built still COMPILES and still announces itself (a table SORT keyed on a REDEFINES view
    /// inside the element — kb/Work PB599; the table SORT over a REDEFINES view itself is built, kb/Work PB1175).</summary>
    [Fact]
    public void GenuineDeferral_StillCompiles_AndAnnounces()
    {
        const string src = """
IDENTIFICATION DIVISION.
PROGRAM-ID. PB909DEF.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 TBL.
   05 TE OCCURS 3.
      10 TK PIC 99.
      10 TKR REDEFINES TK PIC XX.
PROCEDURE DIVISION.
MAIN.
    GO TO SKIPPER.
    SORT TE ASCENDING KEY TKR.
SKIPPER.
    STOP RUN.
""";
        var (ok, errors, warnings) = Compile(src);
        Assert.True(ok, string.Join("\n", errors));
        Assert.Contains(warnings, w => w.Contains("COBOLNET1756", StringComparison.Ordinal));
    }

    /// <summary>kb/Work PB938 — a test that PINS a capability limit is a decision about the standard. Now that the
    /// deferral has one code of its own (COBOLNET1756 means "WiseOwl COBOL has not built this" and nothing else), a
    /// NEGATIVE golden expecting it would be asserting that the standard refuses what only this compiler does —
    /// the `pb17-function-subscript-varying-by` shape, which held conforming source as a rejection for months
    /// and was found only by a fix walking past it. The population is zero; this keeps it zero.</summary>
    [Fact]
    public void NoNegativeGolden_ExpectsTheDeferralCode()
    {
        var errs = Directory.GetFiles(TestRepo.At("tests", "conformance", "negative"), "*.err");
        Assert.True(errs.Length >= 1000, $"only {errs.Length} negative expectations found — has the corpus moved?");
        var pins = errs.Where(f => File.ReadAllText(f).Contains("COBOLNET1756", StringComparison.Ordinal))
            .Select(Path.GetFileName).ToList();
        Assert.True(pins.Count == 0, "a negative golden expects the DEFERRAL code — a capability limit pinned as "
            + "the standard's refusal (kb/Work PB938):\n" + string.Join("\n", pins));
    }

    private static (bool Ok, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings) Compile(string source)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_PB909_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "pb909.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "pb909.dll"), DialectLevel: 2023, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            return (r.Success, r.Errors, r.Warnings);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }
}
