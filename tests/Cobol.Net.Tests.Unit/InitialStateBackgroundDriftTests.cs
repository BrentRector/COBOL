// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using CobolNet.Binding;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The §14.6.2.3.2 action-1 background has ONE choke point, and the §11.9.10.4 GR5 fill map has ONE definition
/// of the alphanumeric extremes (kb/Work PB152).
///
/// <para><b>Why this test exists.</b> "What fills storage that has no VALUE clause" was written in THREE places
/// and only one was ever fixed. PB151 wired the ALLOCATE arm with a fill decoder PRIVATE to <c>PtrEmitter</c>;
/// the native-field arm and the Tier-B image arm went on hardcoding <c>' '</c> / <c>'0'</c>, and — because the
/// private copy also carried its own GR5 map — the landed arm spelled HIGH-VALUES as U+FFFF while every other
/// HIGH-VALUE in the compiler was U+00FF. One rule, three places, two different answers. Goldens catch the
/// behaviour; this catches the SHAPE that produced it, which is what comes back.</para>
///
/// <para>The end-to-end counterpart is <c>pb152_options_initialize_arm_agreement</c>, which asks all three arms
/// the same question in one COBOL program. Together they cover both directions: that test would fail if an arm
/// answered differently, and these fail if an arm acquires its own copy of the rule to answer from.</para>
/// </summary>
public sealed class InitialStateBackgroundDriftTests : CobolNetTestBase
{
    private static readonly Type Background =
        typeof(CobolNet.CodeGen.ValueInitializer).Assembly
            .GetType("CobolNet.CodeGen.InitialStateBackground")
        ?? throw new InvalidOperationException("InitialStateBackground is gone — the §14.6.2.3.2 action-1 choke "
            + "point was removed or renamed. Re-derive the rule before re-shaping this test.");

    /// <summary>⛔ THE CHOKE-POINT PIN. Exactly the members that may compose a background seed, and no others:
    /// ONE seed (both storage axes ask it the same question) plus the ALLOCATE literal, over one private
    /// resolver. A third public producer means a third answer now exists and needs its own golden — which is
    /// the thing that went wrong here twice, silently, because nothing was watching the shape.</summary>
    [Fact]
    public void TheBackgroundHasExactlyTwoPublicProducers()
    {
        var producers = Background
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(["AllocateFillLiteral", "Seed"], producers);
    }

    /// <summary>⛔ THE ONE-DEFINITION PIN. The GR5 b/d extremes must be resolved through
    /// <c>FigurativeConstants.FillChar</c> — the compiler's single definition of the alphanumeric high and low
    /// value, which §8.3.3.6.4 GR6 makes a PROGRAM-COLLATING-SEQUENCE-dependent fact rather than a constant.
    /// A hand-written map in the OPTIONS path is exactly how the landed ALLOCATE arm came to disagree with the
    /// rest of the compiler, so the model is asserted to carry NO figurative character at all: the only fill
    /// character it may resolve at bind time is literal-1's, whose §11.9.10.3 SR1 check belongs there.</summary>
    [Fact]
    public void TheOptionsModelResolvesOnlyTheLiteralFillCharacter()
    {
        var props = typeof(OptionsInitialize)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name).ToHashSet();
        Assert.Contains("LiteralFillChar", props);
        Assert.Equal(typeof(char?), typeof(OptionsInitialize).GetProperty("LiteralFillChar")!.PropertyType);
        // A resolved figurative character on the model would mean a second GR5 map exists.
        Assert.DoesNotContain("SpecifiedFillChar", props);
        Assert.DoesNotContain("FigurativeFillChar", props);
    }

    /// <summary>The §11.9.10.3 SR1 decode, at the bind-time resolver. "Literal-1 shall specify a one-byte
    /// hexadecimal-alphanumeric literal" — §8.3.3.2.2's FORMAT 2 (<c>X"…"</c> / <c>X'…'</c>) with exactly two
    /// hexadecimal digits. ⛔ The format-1 rows are the point: a one-character quoted literal READS admissible
    /// and is not, and the pre-screen decoder accepted every one of them by taking <c>raw[0]</c>.</summary>
    [Theory]
    [InlineData("X\"5A\"", 0x5A)]
    [InlineData("X'5A'", 0x5A)]
    [InlineData("x\"00\"", 0x00)]
    [InlineData("X\"ff\"", 0xFFFF)]   // kb/Work PB2919: the byte 0xFF is U+FFFF (the storage-byte law), as in every X"hh"
    [InlineData("\"Z\"", -1)]         // format 1 — NOT hexadecimal-alphanumeric, however short
    [InlineData("'Z'", -1)]
    [InlineData("\"AB\"", -1)]
    [InlineData("X\"5A5B\"", -1)]     // right format, TWO bytes
    [InlineData("X\"5\"", -1)]        // half a byte
    [InlineData("X\"GG\"", -1)]       // §8.3.3.2.3 SR5 — not hexadecimal digits
    [InlineData("X\"5A'", -1)]        // mismatched delimiters
    [InlineData("", -1)]
    public void Sr1AdmitsOnlyAOneByteHexadecimalAlphanumericLiteral(string raw, int expected)
    {
        var m = typeof(OptionsBinder).GetMethod("OneByteHexAlphanumeric",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        char? got = (char?)m.Invoke(null, [raw]);
        if (expected < 0) Assert.Null(got);
        else Assert.Equal((char)expected, got);
    }

    /// <summary>The end-to-end invariant the whole landing rests on, stated as a test rather than as trust:
    /// with NO OPTIONS INITIALIZE clause the seed is unchanged from the documented §11.9.10.4 GR6 baseline.
    /// Every program in the corpus is in this population, so a regression here is a corpus-wide wrong answer.
    /// </summary>
    [Fact]
    public void WithNoClauseTheBaselineSeedIsUnchanged()
    {
        var (ok, stdout, detail) = CompileAndRun("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB152NOCLAUSE.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 A PIC X(4).
                   01 N PIC 9(4).
                   01 B PIC 1(3).
                   PROCEDURE DIVISION.
                   MAIN.
                       DISPLAY "A=[" A "]".
                       DISPLAY "N=[" N "]".
                       DISPLAY "B=[" B "]".
                       STOP RUN.
            """, dialectLevel: 2023);
        Assert.True(ok, detail);
        Assert.Equal("A=[    ]\r\nN=[0000]\r\nB=[000]", stdout);
    }
}
