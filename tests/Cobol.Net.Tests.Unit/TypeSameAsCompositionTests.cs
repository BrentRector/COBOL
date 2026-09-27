// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1300 — what a TYPE / SAME AS clause composes into its subject, and the rules the COMPOSED entry then
/// answers to. §13.18.57.4 GR1 / §13.18.49.4 GR1: "The effect of the TYPE [SAME AS] clause is as though the data
/// description identified by type-name-1 [data-name-1] had been coded in place", excluding only the clauses each
/// GR-1 names — so a BASED clause travels (§13.18.58.4 GR3: "All other data description clauses … are assumed by
/// data defined using the type-name"), and the composed entry is held to the placement rules a written BASED
/// clause is (§13.16.3 SR5/SR13/SR16, COBOLNET2510). The subject's OWN VALUE literal initializes the composed item
/// (§13.18.57.4 GR3), so it is screened against the composed PICTURE — except for the size sentence, which
/// §13.18.63.3 SR4 writes over "an explicit PICTURE clause" only.
/// <para>The corpus pins the level arm and the positive shapes (<c>2002/w66g_pb1300_type_same_as_composition</c>,
/// <c>negative/w66g-pb1300-composed-based-level</c>); a negative golden's <c>.err</c> is one substring, so the
/// arms that share COBOLNET2510 are told apart here by the rule each message names.</para>
/// </summary>
public sealed class TypeSameAsCompositionTests : CobolNetTestBase
{
    private static string Program(string id, string entries, string body) =>
        "       IDENTIFICATION DIVISION.\n"
        + $"       PROGRAM-ID. {id}.\n"
        + "       DATA DIVISION.\n"
        + "       WORKING-STORAGE SECTION.\n"
        + entries
        + "       PROCEDURE DIVISION.\n"
        + "       MAIN-PARA.\n"
        + body
        + "           STOP RUN.\n";

    /// <summary>§13.16.3 SR16 — "The level number of such data description entries shall be 1 or 77" — asked of
    /// the composed entry through BOTH carriers: a level-05 TYPE subject of a BASED type declaration and a
    /// level-05 SAME AS subject of a BASED item.</summary>
    [Fact]
    public void ComposedBased_BelowLevelOne_IsRefused_ForTypeAndSameAs()
    {
        var (ok, _, detail) = CompileAndRun(
            Program("CBLVL",
                "       01 T TYPEDEF BASED PIC X(5).\n       01 B BASED PIC X(5).\n"
                + "       01 G.\n          05 X TYPE T.\n          05 S SAME AS B.\n",
                "           DISPLAY G\n"), dialectLevel: 2002);
        Assert.False(ok);
        Assert.Contains("COBOLNET2510: 'X': TYPE 'T' brings a BASED clause", detail, StringComparison.Ordinal);
        Assert.Contains("COBOLNET2510: 'S': SAME AS 'B' brings a BASED clause", detail, StringComparison.Ordinal);
        Assert.Contains("§13.16.3 SR16", detail, StringComparison.Ordinal);
    }

    /// <summary>§13.16.3 SR5 — "The EXTERNAL clause shall not be specified in the same data description entry as
    /// the REDEFINES or BASED clause" — the subject writes EXTERNAL beside a TYPE whose declaration is BASED.</summary>
    [Fact]
    public void ComposedBased_WithTheSubjectsExternalClause_IsRefused()
    {
        var (ok, _, detail) = CompileAndRun(
            Program("CBEXT", "       01 T TYPEDEF BASED PIC X(5).\n       01 E EXTERNAL TYPE T.\n",
                "           DISPLAY \"X\"\n"), dialectLevel: 2002);
        Assert.False(ok);
        Assert.Contains("COBOLNET2510: 'E': TYPE 'T' brings a BASED clause", detail, StringComparison.Ordinal);
        Assert.Contains("§13.16.3 SR5", detail, StringComparison.Ordinal);
    }

    /// <summary>The level-01 subject is legal and IS a based item: no storage of its own until SET ADDRESS OF,
    /// after which it is the addressed storage (a MOVE to it is a MOVE to Y).</summary>
    [Fact]
    public void ComposedBased_AtLevelOne_IsABasedItem()
    {
        var (ok, stdout, detail) = CompileAndRun(
            Program("CBOK",
                "       01 T TYPEDEF BASED PIC X(5).\n       01 X TYPE T.\n"
                + "       01 Y PIC X(5) VALUE \"HELLO\".\n",
                "           SET ADDRESS OF X TO ADDRESS OF Y\n           MOVE \"WORLD\" TO X\n"
                + "           DISPLAY \"[\" Y \"]\"\n"), dialectLevel: 2002);
        Assert.True(ok, detail);
        Assert.Equal("[WORLD]", stdout);
    }

    /// <summary>§13.18.57.4 GR3's first sentence with §13.18.63.3 SR2 and SR4: the subject's own literal is
    /// screened against the COMPOSED (here explicit) PICTURE. Both used to compile and store a truncated value
    /// (<c>345</c>, <c>AB</c>) because the entry had no PICTURE when BindEntry screened it.</summary>
    [Fact]
    public void TypeSubjectOwnValue_IsScreenedAgainstTheComposedExplicitPicture()
    {
        var (ok, _, detail) = CompileAndRun(
            Program("TSVAL",
                "       01 P TYPEDEF PIC X(2).\n       01 Q TYPE P VALUE \"ABCD\".\n"
                + "       01 N TYPEDEF PIC 9(3).\n       01 M TYPE N VALUE 12345.\n",
                "           DISPLAY Q M\n"), dialectLevel: 2002);
        Assert.False(ok);
        Assert.Contains("COBOLNET1740: data item 'Q'", detail, StringComparison.Ordinal);
        Assert.Contains("COBOLNET1625: data item 'M'", detail, StringComparison.Ordinal);
    }

    /// <summary>⚠ DETERMINATION (kb/Work PB1300): over a VALUE-IMPLIED template picture, §13.18.63.3 SR4's size
    /// sentence ("the size indicated by an explicit PICTURE clause") does not apply, so the subject's longer
    /// literal is not a syntax error and the item keeps the template's implied X(2). The shorter-literal twin
    /// shows the implied picture is the template's, not one implied from the subject's own literal.</summary>
    [Fact]
    public void TypeSubjectOwnValue_OverAnImplicitTemplatePicture_TakesTheTemplatesSize()
    {
        var (ok, stdout, detail) = CompileAndRun(
            Program("TSIMP",
                "       01 U TYPEDEF VALUE \"AB\".\n       01 W TYPE U VALUE \"QRSTUV\".\n"
                + "       01 U4 TYPEDEF VALUE \"ABCD\".\n       01 W4 TYPE U4 VALUE \"Q\".\n",
                "           DISPLAY \"[\" W \"]\" FUNCTION LENGTH(W) \"[\" W4 \"]\" FUNCTION LENGTH(W4)\n"),
            dialectLevel: 2002);
        Assert.True(ok, detail);
        Assert.Equal("[QR]2[Q   ]4", stdout);
    }
}
