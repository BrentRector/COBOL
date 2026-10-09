// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// An intrinsic argument of class POINTER under <c>--permissive</c> (kb/Work PB2079). The migration mode's coercion
/// extension decodes the CHARACTERS of an argument of the wrong class; an item of class pointer holds a reference
/// (ISO §8.5.2.1 Table 2), so there is nothing to coerce and every argument-rule violation it draws stays an ERROR
/// whose text does not offer <c>--permissive</c> (<c>IntrinsicBinder.ReportClass</c>, over the class screen and
/// CONCAT's §15.18.3 r2/r3 arm alike). Before, the class arm warned and the program died in Roslyn (CS1503), and
/// CONCAT's usage arm still said "--permissive accepts it" beside the class error.
/// </summary>
/// <remarks>The negative corpus runs strict only, so <c>EditionHarness.CompileFull(…, permissive: true)</c> is the seam
/// (as <see cref="ApplyCommitSubjectRuleTests"/>). The sources ARE the corpus fixtures, read from disk.</remarks>
public sealed class IntrinsicPointerArgumentPermissiveTests
{
    /// <summary>A negative fixture's program text without its <c>*&gt;</c> header comment lines.</summary>
    private static string Fixture(string name) =>
        string.Join('\n', ConformanceCorpus.NegativeCase(name).Source.Split('\n')
            .Where(line => !line.TrimStart().StartsWith("*>", StringComparison.Ordinal)));

    /// <summary>A pointer argument is refused under <c>--permissive</c>, and no diagnostic about it claims the mode
    /// accepts it.</summary>
    [Theory]
    [InlineData("pb58-concat-pointer")]
    [InlineData("pb2079-baseconvert-pointer-base")]
    // kb/Work PB2074: an address-identifier (class pointer, §8.4.3.11.4 GR1) as a class-alphanumeric argument.
    [InlineData("pb2074-upper-case-address-of")]
    public void PointerArgument_IsRefusedUnderPermissive_WithoutOfferingTheCoercion(string fixture)
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(Fixture(fixture), 2023, permissive: true);
        Assert.False(ok, $"{fixture}: a class-pointer argument has no characters for the --permissive coercion");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1627");
        Assert.DoesNotContain(errors.Concat(warnings), d => d.Contains("COBOLNET1627", StringComparison.Ordinal)
                                                             && d.Contains("--permissive", StringComparison.Ordinal));
    }

    /// <summary>THE ARM THAT PROVES THE PROBE CAN FAIL: an alphanumeric base holds characters, so the same screen
    /// offers the coercion for it under <c>--permissive</c> — the pointer arm above is the operand's class, not the
    /// mode.</summary>
    [Fact]
    public void AlphanumericArgument_IsCoercedUnderPermissive()
    {
        var (_, _, warnings) = EditionHarness.CompileFull(Fixture("pb2079-baseconvert-alphanumeric-base"), 2023, permissive: true);
        Assert.Contains(warnings, w => w.Contains("COBOLNET1627", StringComparison.Ordinal)
                                       && w.Contains("accepted under --permissive", StringComparison.Ordinal));
    }
}
