// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ISO/IEC 1989:2023 §4.2.10's warning mechanism, end to end (kb/Work PB1525): "An implementation shall provide a
/// warning mechanism that optionally may be invoked by the user at compile time to indicate use of a nonstandard
/// extension in a compilation group. This warning mechanism shall flag only extensions that are syntactically
/// distinguishable." Each leg below can fail on its own: the flag ON names the extension (COBOLNET2894) at every
/// edition that accepts it; the flag OFF compiles byte-identically to a compiler that never had the option; and the
/// STANDARD spelling beside each extension is never named, which is what "flag only extensions" means. The register
/// itself, its documentation and its use sites are held equal by <c>NonstandardExtensionRegisterDriftTests</c> (Unit).
/// </summary>
public sealed class NonstandardExtensionWarningTests
{
    private const string Code = "COBOLNET2894";

    private static string Prog(string data, string procedure) =>
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. NSX" + Guid.NewGuid().ToString("N")[..8] + ".\n"
        + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n" + data
        + "       PROCEDURE DIVISION.\n" + procedure + "           STOP RUN.\n";

    private static IReadOnlyList<string> Warnings(string source, int edition, bool flag)
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(source, edition, flagExtensions: flag);
        Assert.True(ok, $"--std {edition}: {string.Join("\n", errors)}");
        return warnings;
    }

    /// <summary>Every usage spelling of the register, at all four editions (the COMP-n family is a vendor spelling at
    /// every one of them): named with the flag, silent without it.</summary>
    [Theory]
    [InlineData("COMP-1", "", "USAGE COMP-1")]                       // a floating-point usage takes no PICTURE (COBOLNET1521)
    [InlineData("COMPUTATIONAL-2", "", "USAGE COMPUTATIONAL-2")]
    [InlineData("COMP-3", "PIC S9(4)", "USAGE COMP-3")]
    [InlineData("COMPUTATIONAL-4", "PIC S9(4)", "USAGE COMPUTATIONAL-4")]
    [InlineData("COMP-5", "PIC S9(4)", "USAGE COMP-5")]
    public void AVendorUsageSpelling_IsNamedWithTheFlagAndSilentWithout(string spelling, string picture, string named)
    {
        string source = Prog($"       01 X {picture} {spelling}.\n", "           CONTINUE.\n");
        foreach (int edition in EditionHarness.Editions)
        {
            var flagged = Warnings(source, edition, flag: true);
            Assert.True(flagged.Any(w => w.Contains(Code) && w.Contains(named)),
                $"--std {edition} --flag-extensions: {named} must draw {Code}; got [{string.Join(" | ", flagged)}]");
            Assert.DoesNotContain(Warnings(source, edition, flag: false), w => w.Contains(Code));
        }
    }

    /// <summary>"Flag only extensions": the STANDARD spelling of each usage the vendor words stand for, and the bare
    /// COMP / COMPUTATIONAL ISO §13.18.60.3 SR6 defines, are never named — even with the flag on.</summary>
    [Theory]
    [InlineData("COMP", 85)]
    [InlineData("COMPUTATIONAL", 85)]
    [InlineData("DISPLAY", 85)]
    [InlineData("BINARY", 2002)]
    [InlineData("PACKED-DECIMAL", 2002)]
    public void AStandardUsageSpelling_IsNeverNamed(string spelling, int introducedIn)
    {
        string source = Prog($"       01 X PIC S9(4) {spelling} VALUE 5.\n", "           DISPLAY X.\n");
        foreach (int edition in EditionHarness.Editions.Where(e => e >= introducedIn))
            Assert.DoesNotContain(Warnings(source, edition, flag: true), w => w.Contains(Code));
    }

    /// <summary><c>GOBACK RETURNING</c> (§14.9.18.2 has no such phrase) and <c>SET program-pointer TO ENTRY</c> (the
    /// words occur nowhere in the standard) are accepted from COBOL-2002, the edition that introduced the statements
    /// they extend; each is named at the ONE binder site that accepts it, program and method arm alike.</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void AnExtendedPhraseOrStatement_IsNamedAtTheOneSiteThatAcceptsIt(int edition)
    {
        string gobackReturning = Prog("       77 RC PIC 99 VALUE 7.\n", "           GOBACK RETURNING RC.\n");
        Assert.Contains(Warnings(gobackReturning, edition, flag: true), w => w.Contains(Code) && w.Contains("GOBACK RETURNING"));
        Assert.DoesNotContain(Warnings(gobackReturning, edition, flag: false), w => w.Contains(Code));

        string setToEntry = Prog("       01 PP USAGE PROGRAM-POINTER.\n", "           SET PP TO ENTRY \"X\".\n");
        Assert.Contains(Warnings(setToEntry, edition, flag: true), w => w.Contains(Code) && w.Contains("SET … TO ENTRY"));
        Assert.DoesNotContain(Warnings(setToEntry, edition, flag: false), w => w.Contains(Code));

        // The standard forms of the same two jobs are not extensions.
        string standard = Prog("       01 PP USAGE PROGRAM-POINTER.\n", "           SET PP TO NULL.\n           GOBACK.\n");
        Assert.DoesNotContain(Warnings(standard, edition, flag: true), w => w.Contains(Code));
    }
}
