// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB93 — a REDEFINES data-name-2 / RENAMES data-name-2/-3 that names nothing (or, for REDEFINES, a LATER
/// sibling) is a diagnosed error at every edition (ISO §13.18.44.3 SR4/SR7/SR10, §13.18.45.3 SR4, §8.4.2.1); it used to
/// compile silently into a storage shape no edition defines. A REDEFINES naming a redefinition (SR7) is an error
/// strict and the anchor-chased chain under <c>--permissive</c>.
/// </summary>
public sealed class RedefinesTargetTests
{
    private const string Head = "IDENTIFICATION DIVISION.\nPROGRAM-ID. PB93T.\nDATA DIVISION.\nWORKING-STORAGE SECTION.\n";

    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void UndefinedRedefinesTarget_IsRejected(int edition)
    {
        string src = Head + "01 W PIC X.\n01 B REDEFINES NOPE PIC 9(2).\nPROCEDURE DIVISION.\n    MOVE 1 TO B.\n    STOP RUN.\n";
        var (ok, errors, _) = EditionHarness.CompileFull(src, edition);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1654");
    }

    [Fact]
    public void LaterSiblingTarget_IsRejected()
    {
        string src = Head + "01 REC.\n   05 A REDEFINES B PIC X(2).\n   05 B PIC X(2) VALUE \"ab\".\nPROCEDURE DIVISION.\n    DISPLAY A.\n    STOP RUN.\n";
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1654");
    }

    [Fact]
    public void RedefinesOfRedefinition_IsAnErrorStrict_AndTheChainUnderPermissive()
    {
        string src = Head + "01 REC.\n   05 A PIC X(2) VALUE \"ab\".\n   05 B REDEFINES A PIC X(2).\n   05 C REDEFINES B PIC X(2).\n"
            + "PROCEDURE DIVISION.\n    MOVE \"xy\" TO C.\n    DISPLAY A B C.\n    STOP RUN.\n";
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1656");
        var (pok, stdout, detail) = EditionHarness.CompileAndRun(src, 2023, permissive: true);
        Assert.True(pok, detail);
        Assert.Equal("xyxyxy", stdout.Trim());   // the anchor-chased chain: C, B and A share the one area
        var (_, _, warnings) = EditionHarness.CompileFull(src, 2023, permissive: true);
        Assert.Contains(warnings, w => w.Contains("COBOLNET1656"));
    }

    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void UndefinedRenamesOperand_IsRejected(int edition)
    {
        string src = Head + "01 REC.\n   05 A PIC X(2) VALUE \"ab\".\n   05 B PIC X(2) VALUE \"cd\".\n66 AB RENAMES A THRU NOPE.\n"
            + "PROCEDURE DIVISION.\n    DISPLAY AB.\n    STOP RUN.\n";
        var (ok, errors, _) = EditionHarness.CompileFull(src, edition);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1655");
    }

    [Fact]   // the legal shapes still bind (a RENAMES range spanning a REDEFINES view is kb/Work PB96 — kept out of this shape)
    public void LegalRedefinesAndRenames_StillBind()
    {
        string src = Head + "01 REC.\n   05 A PIC X(2) VALUE \"ab\".\n   05 B REDEFINES A PIC X(2).\n   05 C PIC X(2) VALUE \"cd\".\n   05 D PIC X(2) VALUE \"ef\".\n66 CD RENAMES C THRU D.\n"
            + "PROCEDURE DIVISION.\n    DISPLAY B CD.\n    STOP RUN.\n";
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, 2023);
        Assert.True(ok, detail);
        Assert.Equal("abcdef", stdout.Trim());
    }

    /// <summary>kb/Work PB2518 — a REDEFINES clause inside a TYPEDEF is screened where the type is DECLARED, used or not
    /// (the roots-only walk never reached a template), and a used type is diagnosed once, not once per clone.</summary>
    [Theory]
    [InlineData("05 A PIC X(2).\n   05 C PIC X.\n   05 B REDEFINES A PIC X(2).\n", "COBOLNET2739")]   // SR10 across storage
    [InlineData("05 A PIC X.\n   05 B REDEFINES A PIC X(2).\n", "COBOLNET1539")]                      // SR8 size screen
    [InlineData("05 A PIC X.\n   05 B REDEFINES ZZ PIC X(2).\n", "COBOLNET1654")]                    // unresolved data-name-2
    public void RedefinerInsideUnusedTypedef_IsScreenedAtTheDeclaration(string members, string code)
    {
        string src = Head + "01 T TYPEDEF.\n   " + members + "01 U PIC X.\nPROCEDURE DIVISION.\n    STOP RUN.\n";
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, code);
    }

    [Fact]
    public void RedefinerInsideUsedTypedef_IsDiagnosedOnce()
    {
        string src = Head + "01 T TYPEDEF.\n   05 A PIC X(2).\n   05 C PIC X.\n   05 B REDEFINES A PIC X(2).\n01 U TYPE T.\n01 V TYPE T.\n"
            + "PROCEDURE DIVISION.\n    STOP RUN.\n";
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2023);
        Assert.False(ok);
        Assert.Single(errors, e => e.Contains("COBOLNET2739", StringComparison.Ordinal));
    }
}
