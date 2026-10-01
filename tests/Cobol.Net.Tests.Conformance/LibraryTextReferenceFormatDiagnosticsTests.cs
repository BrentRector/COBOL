// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ LIBRARY TEXT IS READ BY THE SAME §6.5 WALKER AS THE SOURCE, WITH THE SAME DIAGNOSTICS (kb/Work PB1640).
/// ISO §6.5 applies logical conversion to the lines "of source text and library text", so a rule of reference
/// format broken in a copybook is reported exactly as it is in the main source — sited at the COPYBOOK's file and
/// line — and a once-per-compilation gate fires once for the compilation, not once per file. Before, the copybook
/// was converted with no diagnostics at all: <c>MOVE A TO X*&gt; c</c> compiled clean in a copybook and drew
/// COBOLNET2495 in the main source.
/// <para>These are xUnit facts, not corpus entries, because the negative corpus compiles a program with no library
/// text beside it (<see cref="EditionHarness.CompileFull"/> stages copybooks only when it is handed some) and a
/// <c>.err</c> substring cannot see WHERE a diagnostic is sited or HOW MANY times it is reported.</para>
/// </summary>
public sealed class LibraryTextReferenceFormatDiagnosticsTests
{
    private const string Program =
        "       IDENTIFICATION DIVISION.\n"
        + "       PROGRAM-ID. PB1640.\n"
        + "       DATA DIVISION.\n"
        + "       WORKING-STORAGE SECTION.\n"
        + "       01 N PIC 9.\n"
        + "       PROCEDURE DIVISION.\n"
        + "           COPY BK1.\n"
        + "           STOP RUN.\n";

    [Theory] // §6.2.3.2 SR2: "The floating comment indicator of an inline comment shall be preceded by a separator space."
    [InlineData("           MOVE 1 TO N*> c\n")]                       // fixed form library text
    [InlineData(">>SOURCE FORMAT FREE\nMOVE 1 TO N*> c\n")]           // free form library text
    public void UnseparatedFloatingComment_InACopybook_IsDiagnosed_AtTheCopybooksOwnFile(string library)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Program, 2023,
            copybooks: new Dictionary<string, string> { ["BK1.cpy"] = library });
        Assert.False(ok, "the copybook breaks §6.2.3.2 SR2");
        string error = Assert.Single(errors, e => e.Contains("COBOLNET2495"));
        Assert.Contains("BK1.cpy", error, StringComparison.OrdinalIgnoreCase);   // sited at the copybook, not at the COPY statement of the main source
    }

    [Fact] // the 2023 obsolescence warning of the column-7 hyphen is one warning per COMPILATION, not per file
    public void ObsoleteContinuationIndicator_IsReportedOnce_ForTheMainSourceAndItsCopybooks()
    {
        const string hyphen = "           MOVE 1 TO N\n      -    .\n";
        string main = Program.Replace("           COPY BK1.\n", hyphen + "           COPY BK1.\n");
        var (ok, _, warnings) = EditionHarness.CompileFull(main, 2023,
            copybooks: new Dictionary<string, string> { ["BK1.cpy"] = hyphen });
        Assert.True(ok);
        Assert.Single(warnings, w => w.Contains("COBOLNET0903"));
    }

    [Fact] // the copybook is the FIRST file to use the obsolete hyphen: the one warning is sited there
    public void ObsoleteContinuationIndicator_FirstUsedInACopybook_IsSitedAtTheCopybook()
    {
        var (ok, _, warnings) = EditionHarness.CompileFull(Program, 2023,
            copybooks: new Dictionary<string, string> { ["BK1.cpy"] = "           MOVE 1 TO N\n      -    .\n" });
        Assert.True(ok);
        string warning = Assert.Single(warnings, w => w.Contains("COBOLNET0903"));
        Assert.Contains("BK1.cpy", warning, StringComparison.OrdinalIgnoreCase);
    }
}
