// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// DISPLAY … UPON mnemonic-name-1 (ISO/IEC 1989:2023 §14.9.11.3 SR2 / §14.9.11.4 GR8; review finding V6). The legacy
/// silently DROPPED the UPON phrase — every DISPLAY went to the standard device regardless of the named device, and an
/// undeclared / input-only mnemonic compiled clean. Fixed: the mnemonic resolves through SPECIAL-NAMES; SR2 rejects an
/// undeclared name or a device not capable of receiving data (COBOLNET0817); SYSERR routes to standard error while
/// CONSOLE / SYSOUT and the no-UPON default use the standard display device (standard output — the implementor choice
/// documented in CONFORMANCE.md §3 / A.1 item 59).
/// </summary>
public sealed class DisplayUponTests
{
    private static readonly CobolNetCompiler CobolNet = new(2023);

    /// <summary>A program whose SPECIAL-NAMES binds the output device-names to mnemonics, then runs
    /// <paramref name="body"/>. <c>CompileAndRun</c> returns (ok, STDOUT, STDERR-as-detail).</summary>
    private static string Prog(string body) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. DISPUP.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        SPECIAL-NAMES.
            CONSOLE IS CON-DEV
            SYSOUT IS OUT-DEV
            SYSERR IS ERR-DEV
            SYSIN IS IN-DEV.
        PROCEDURE DIVISION.
        MAIN.
        {body}
            STOP RUN.
        """;

    [Fact]   // §14.9.11.4 GR8 default + UPON SYSOUT / CONSOLE → the standard display device (standard output).
    public void Upon_OutputDevices_And_Default_GoToStdout()
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(Prog("""
                DISPLAY "A-DEFAULT"
                DISPLAY "B-SYSOUT" UPON OUT-DEV
                DISPLAY "C-CONSOLE" UPON CON-DEV
            """));
        Assert.True(ok, detail);
        Assert.Equal("A-DEFAULT\nB-SYSOUT\nC-CONSOLE", stdout);
    }

    [Fact]   // §14.9.11.3 SR2 device routing: UPON SYSERR writes the standard-error stream, NOT standard output.
    public void Upon_Syserr_GoesToStderr_NotStdout()
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(Prog("""
                DISPLAY "ON-OUT" UPON OUT-DEV
                DISPLAY "ON-ERR" UPON ERR-DEV
            """));
        Assert.True(ok, detail);
        Assert.Equal("ON-OUT", stdout);           // only the SYSOUT display reaches stdout
        Assert.Contains("ON-ERR", detail);         // the SYSERR display reaches stderr
        Assert.DoesNotContain("ON-ERR", stdout);
    }

    [Fact]   // §14.9.11.3 SR2: mnemonic-name-1 shall be declared in SPECIAL-NAMES — an undeclared name is rejected
             // (COBOLNET0817), not silently dropped.
    public void Upon_UndeclaredMnemonic_Rejected0817()
        => EditionHarness.AssertHasDiagnostic(
            EditionHarness.GetDiagnostics(Prog("            DISPLAY \"X\" UPON NO-SUCH-DEV"), 2023), "COBOLNET0817");

    [Fact]   // §14.9.11.3 SR2: the device shall be capable of RECEIVING data — SYSIN is input-only (the ACCEPT side),
             // so DISPLAY UPON an SYSIN-bound mnemonic is rejected (COBOLNET0817).
    public void Upon_InputOnlyDevice_Rejected0817()
        => EditionHarness.AssertHasDiagnostic(
            EditionHarness.GetDiagnostics(Prog("            DISPLAY \"X\" UPON IN-DEV"), 2023), "COBOLNET0817");
}
