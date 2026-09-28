// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Validation;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Generated;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The P2.8/W2 position classifier behind the §8.9 reserved-word funnel
/// (<c>VersionConformancePass.IsProvableUserWordPosition</c>): an allowlist-band context-keyword token rejects as a
/// reserved word (ISO §8.3.2.1 rule 1 / §8.3.2.4.1) ONLY when its <c>cobolWord</c> occurrence sits in a
/// grammar position that is unambiguously a user-defined-word use (§8.3.2.2) — the data/parameter entry-name
/// slot (§13.16), a paragraph/section definition (§14.4.2/§14.4.3), the SELECT file-name (§12.4.5.1), and the
/// program-name sites (§11.10.2/§11.5/§10.6.1). Mis-parse-prone optional entry-name slots (the RW104A hazard:
/// the report-group COLUMN clause keyword, §13.18.14, binds into the <c>reportGroupName?</c> slot under the
/// permissive grammar) and every reference position classify FALSE. Each fact feeds REAL parse trees through
/// the frontend — no hand-built contexts.
/// </summary>
public sealed class ReservedWordPositionTests : CobolNetTestBase
{
    /// <summary>Parse <paramref name="source"/> at <paramref name="dialectLevel"/> and return every
    /// <c>cobolWord</c> context whose token text equals <paramref name="word"/> (case-insensitive). May be
    /// EMPTY when every occurrence of the word parses through a dedicated keyword rule instead — the funnel
    /// never sees such occurrences, which is itself the no-false-reject guarantee.</summary>
    private List<CobolParserCore.CobolWordContext> CobolWords(string source, string word, int dialectLevel = 85)
    {
        string path = Path.Combine(TempDir, "pos.cob");
        File.WriteAllText(path, source);
        var bag = new DiagnosticBag();
        var tree = new CobolNet.Frontend.Frontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = dialectLevel }.Parse(path, bag);
        Assert.True(tree is not null && !bag.HasErrors,
            $"snippet must parse: {string.Join("; ", bag.Diagnostics.Select(d => d.ToString()))}");
        var hits = new List<CobolParserCore.CobolWordContext>();
        Collect(tree!, word, hits);
        return hits;
    }

    private static void Collect(IParseTree node, string word, List<CobolParserCore.CobolWordContext> hits)
    {
        if (node is CobolParserCore.CobolWordContext cw
            && string.Equals(cw.Start.Text, word, StringComparison.OrdinalIgnoreCase))
            hits.Add(cw);
        for (int i = 0; i < node.ChildCount; i++)
            Collect(node.GetChild(i), word, hits);
    }

    [Fact]   // §13.16: `01 SCREEN PIC …` — the entry-name slot is provably a user-word use (no data
             // description clause begins with a cobolWord-admitted token).
    public void DataDescriptionEntryName_IsProvableUserWordPosition()
    {
        var hits = CobolWords("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TDRWP1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 SCREEN PIC X(3) VALUE "ABC".
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "X".
                STOP RUN.
            """, "SCREEN");
        Assert.NotEmpty(hits);
        Assert.Contains(hits, VersionConformancePass.IsProvableUserWordPosition);
    }

    [Fact]   // The RW104A hazard (§13.18.14): a report-group COLUMN clause KEYWORD either parses through the
             // dedicated reportColumnClause token (never reaching the funnel) or binds into the optional
             // report-group entry-name slot under the permissive grammar (DEVLOG 585) — EITHER way no COLUMN
             // occurrence may classify as a provable user-word position (a conforming CCVS-85 shape must
             // never reject). Both RW104A clause shapes included (bare `COLUMN 27` and `COLUMN NUMBER 70`).
    public void ReportGroupColumnClauseKeyword_IsNotProvableUserWordPosition()
    {
        var hits = CobolWords("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TDRWP2.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT RPT ASSIGN TO "TDRWP2F".
            DATA DIVISION.
            FILE SECTION.
            FD RPT
                REPORT IS R-1.
            REPORT SECTION.
            RD R-1.
            01 DET TYPE DETAIL.
                03 LINE 1.
                    05 COLUMN 27 PIC X(4) VALUE "MARK".
                    05 COLUMN NUMBER 70 PIC X(5) VALUE "PAGE ".
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT RPT.
                INITIATE R-1.
                GENERATE DET.
                TERMINATE R-1.
                CLOSE RPT.
                STOP RUN.
            """, "COLUMN");
        Assert.DoesNotContain(hits, VersionConformancePass.IsProvableUserWordPosition);
    }

    [Fact]   // §14.4.3: the paragraph DEFINITION `COL.` classifies true; the `PERFORM COL` REFERENCE does not
             // (references stay unchecked — conservative subset).
    public void ParagraphDefinition_True_PerformReference_False()
    {
        var hits = CobolWords("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TDRWP3.
            PROCEDURE DIVISION.
            MAIN.
                PERFORM COL.
                STOP RUN.
            COL.
                DISPLAY "C".
            """, "COL");
        Assert.Equal(2, hits.Count);
        Assert.Equal(1, hits.Count(VersionConformancePass.IsProvableUserWordPosition));
        Assert.True(VersionConformancePass.IsProvableUserWordPosition(
            hits.Single(h => h.Parent is CobolParserCore.ProcedureNameContext
                { Parent: CobolParserCore.ParagraphNameContext })));
    }

    [Fact]   // §14.4.2: a section DEFINITION (`BIT SECTION.`) classifies true.
    public void SectionDefinition_IsProvableUserWordPosition()
    {
        var hits = CobolWords("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TDRWP4.
            PROCEDURE DIVISION.
            BIT SECTION.
            P-1.
                DISPLAY "S1".
                STOP RUN.
            """, "BIT");
        Assert.Contains(hits, VersionConformancePass.IsProvableUserWordPosition);
    }

    [Fact]   // §12.4.5.1: the SELECT file-name classifies true; the FD / OPEN / CLOSE REFERENCES of the same
             // name do not (one provable defining occurrence suffices — the funnel dedups per word anyway).
    public void SelectFileName_True_FdAndStatementReferences_False()
    {
        var hits = CobolWords("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TDRWP5.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT DEFAULT ASSIGN TO "TDRWP5F" ORGANIZATION IS LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD DEFAULT.
            01 REC PIC X(5).
            PROCEDURE DIVISION.
            MAIN.
                OPEN OUTPUT DEFAULT.
                MOVE "HELLO" TO REC.
                WRITE REC.
                CLOSE DEFAULT.
                STOP RUN.
            """, "DEFAULT");
        Assert.True(hits.Count >= 2, "expected the SELECT occurrence plus at least one reference");
        Assert.Equal(1, hits.Count(VersionConformancePass.IsProvableUserWordPosition));
        Assert.True(VersionConformancePass.IsProvableUserWordPosition(
            hits.Single(h => h.Parent is CobolParserCore.FileNameContext
                { Parent: CobolParserCore.FileControlClauseGroupContext })));
    }

    [Fact]   // §11.10.2/§10.6.1: every programName site (PROGRAM-ID header, END PROGRAM marker) names the
             // source unit itself — classifies true (here via the ordinary IDENTIFIER program name; the slot
             // matters for band tokens, which occupy the identical context).
    public void ProgramName_IsProvableUserWordPosition()
    {
        var hits = CobolWords("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TDRWP6.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "P".
                STOP RUN.
            END PROGRAM TDRWP6.
            """, "TDRWP6");
        Assert.Equal(2, hits.Count);
        Assert.All(hits, h => Assert.True(VersionConformancePass.IsProvableUserWordPosition(h)));
    }
}
