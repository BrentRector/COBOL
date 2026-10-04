// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY RESIDENCE A GLOBAL NAME CAN HAVE IS REACHABLE FROM A CONTAINED PROGRAM (kb/Work PB1009).
/// </summary>
/// <remarks>
/// ISO §13.18.27.3 SR1 b) admits GLOBAL on a level-1 entry "specified in the file, working-storage, local-storage,
/// or linkage section", and §13.18.27.4 GR2 lets a contained program "reference that name without describing it
/// again" — the storage stays the container's, reached through a ref-bridge to each container member a reference
/// renders. Those members differ by residence: a plain field, a Tier-B backing, the cell behind a cell-backed class,
/// a BASED item's address pointer, a carrier-resident formal's carrier, a table's index fields. The bridge list used
/// to be spelled per residence at its one call site, and two residences were missing — the carrier-resident formal
/// (the bridge wrote the accessor text <c>__lnkp0.Value</c> where a member name belongs: CS0106 on legal source)
/// and the BASED item's address pointer (CS0120). It is now ONE list,
/// <c>DataBinder.GlobalBridgesOf</c>, and this matrix is the drift pin: a residence a future change adds belongs
/// here as a row, and a row whose members the list forgets fails to compile.
/// Every row: the contained program has its OWN first formal (the container's carrier must not alias it), reads the
/// global (<c>INIT</c>) and stores through it (<c>NEWV</c>), and the container then sees the store.
/// </remarks>
public sealed class GlobalBridgeResidenceDriftTests : CobolNetTestBase
{
    [Theory]
    [InlineData(1, "working-storage", "01 GX PIC X(4) VALUE \"INIT\" GLOBAL.", "", "", "", "", "GX")]
    [InlineData(2, "redefined", "01 GX PIC X(4) VALUE \"INIT\" GLOBAL.\n       01 GY REDEFINES GX PIC 9(4).", "", "", "", "", "GX")]
    [InlineData(3, "external", "01 GX PIC X(4) EXTERNAL GLOBAL.", "", "", "", "MOVE \"INIT\" TO GX", "GX")]
    [InlineData(4, "based", "01 GX PIC X(4) BASED GLOBAL.", "", "", "", "SET ADDRESS OF GX TO ADDRESS OF WB", "GX")]
    [InlineData(5, "address-of-taken", "01 GX PIC X(4) VALUE \"INIT\" GLOBAL.", "", "", "", "SET P TO ADDRESS OF GX", "GX")]
    [InlineData(6, "indexed-table", "01 GT GLOBAL.\n          05 GX PIC X(4) OCCURS 2 INDEXED BY GI VALUE \"INIT\".", "", "", "", "SET GI TO 2", "GX(GI)")]
    [InlineData(7, "local-storage", "", "01 GX PIC X(4) VALUE \"INIT\" GLOBAL.", "", "", "", "GX")]
    [InlineData(8, "linkage-elementary", "", "", "GLOBAL", "", "", "X")]
    [InlineData(9, "linkage-elementary-addressed", "", "", "GLOBAL", "", "SET P TO ADDRESS OF X", "X")]
    [InlineData(10, "linkage-group", "", "", "", "GLOBAL", "", "X1")]
    // §13.18.27.4 GR3 — only the SUBJECT of a GLOBAL REDEFINES is global. A Tier-A (Alias) subject owns no member, so its
    // bridge names the original's field; an elementary and a group subject; both entries global (one member, one bridge)
    // (kb/Work PB1523).
    [InlineData(11, "global-alias-redefiner", "01 GX PIC X(4) VALUE \"INIT\".\n       01 GY REDEFINES GX GLOBAL PIC X(4).", "", "", "", "", "GY")]
    [InlineData(12, "global-group-redefiner", "01 GX PIC X(4) VALUE \"INIT\".\n       01 GY REDEFINES GX GLOBAL.\n          05 GY1 PIC X(2).\n          05 GY2 PIC X(2).", "", "", "", "", "GY")]
    [InlineData(13, "global-original-and-redefiner", "01 GX PIC X(4) VALUE \"INIT\" GLOBAL.\n       01 GY REDEFINES GX GLOBAL.\n          05 GY1 PIC X(2).\n          05 GY2 PIC X(2).", "", "", "", "", "GX")]
    public void AContainedProgramReachesTheGlobal(int id, string residence, string ws, string ls, string xGlobal,
        string groupGlobal, string setup, string reference)
    {
        _ = residence;   // the row's name, for the test explorer
        string lsSection = ls.Length == 0 ? "" : $"       LOCAL-STORAGE SECTION.\n       {ls}\n";
        string src = $"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. GB{id}M.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 A PIC X(4) VALUE "INIT".
                   01 GRP.
                      05 A1 PIC X(4) VALUE "INIT".
                   PROCEDURE DIVISION.
                       CALL "GB{id}C" USING A GRP
                       STOP RUN.
                   END PROGRAM GB{id}M.
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. GB{id}C.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 WB PIC X(4) VALUE "INIT".
                   01 P USAGE POINTER.
                   {ws}

            """ + lsSection + $"""
                   LINKAGE SECTION.
                   01 X PIC X(4) {xGlobal}.
                   01 XG {groupGlobal}.
                      05 X1 PIC X(4).
                   PROCEDURE DIVISION USING X XG.
                       {setup}
                       CALL "GB{id}N" USING WB
                       DISPLAY "C=" {reference}
                       GOBACK.
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. GB{id}N.
                   DATA DIVISION.
                   LINKAGE SECTION.
                   01 Z PIC X(4).
                   PROCEDURE DIVISION USING Z.
                       DISPLAY "N=" {reference} " Z=" Z
                       MOVE "NEWV" TO {reference}
                       GOBACK.
                   END PROGRAM GB{id}N.
                   END PROGRAM GB{id}C.

            """;
        var (ok, stdout, detail) = CompileAndRun(src, dialectLevel: 2023);
        Assert.True(ok, detail);
        Assert.Equal("N=INIT Z=INIT\nC=NEWV", stdout.Replace("\r\n", "\n").TrimEnd());
    }

    /// <summary>The storage-less residence: a GLOBAL constant-name (§13.18.27.4 GR1 lists constant-names among the
    /// global names) substitutes in a contained program — including in its DATA DIVISION, since the substitution is
    /// compile-time (§13.10.4 GR1) — and a contained program's own declaration of the name, constant or data item,
    /// shadows it (§8.4.6). The contained-program reference used to fail as an undefined name (kb/Work PB1009).</summary>
    [Fact]
    public void AGlobalConstantIsVisibleInAContainedProgramAndShadowable()
    {
        const string src = """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. GBKM.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 K CONSTANT IS GLOBAL AS 42.
                   01 J CONSTANT IS GLOBAL AS "JJ".
                   01 H CONSTANT IS GLOBAL AS 5.
                   PROCEDURE DIVISION.
                       CALL "GBKN"
                       STOP RUN.
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. GBKN.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 J PIC X(3) VALUE "LOC".
                   01 H CONSTANT AS 7.
                   01 T PIC 9(3) VALUE K.
                   PROCEDURE DIVISION.
                       DISPLAY K " " T " " J " " H
                       CALL "GBKO"
                       GOBACK.
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. GBKO.
                   PROCEDURE DIVISION.
                       DISPLAY K " " J
                       GOBACK.
                   END PROGRAM GBKO.
                   END PROGRAM GBKN.
                   END PROGRAM GBKM.

            """;
        var (ok, stdout, detail) = CompileAndRun(src, dialectLevel: 2023);
        Assert.True(ok, detail);
        Assert.Equal("42 042 LOC 7\n42 JJ", stdout.Replace("\r\n", "\n").TrimEnd());
    }
}
