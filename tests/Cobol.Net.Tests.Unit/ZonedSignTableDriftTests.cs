// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Runtime;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE OVER-PUNCH TABLES (kb/Work PB803). ISO §13.18.52.4 GR4 leaves the REPRESENTATION of a fused operational
/// sign to the implementor and GR5 b) leaves the VALID SIGN SET to the implementor — Annex A.1 items 177 and 178 —
/// and the owner's 2026-09-09 decision is both surveyed conventions behind one option, defaulting to IBM / Micro
/// Focus compatibility. <see cref="ZonedSign"/> is the ONE place either table is written; these facts are what keep
/// "one place" true and what a THIRD convention inherits for free.
/// <para>Every fact below iterates <see cref="System.Enum.GetValues{TEnum}"/>, never a hand-listed pair, so a
/// convention added to <see cref="SignEncoding"/> is checked the moment it exists rather than the day somebody
/// remembers to widen a test.</para>
/// </summary>
public sealed class ZonedSignTableDriftTests
{
    private static SignEncoding[] All => Enum.GetValues<SignEncoding>();

    /// <summary>A row exists for every member. A member added without a row would throw
    /// <see cref="IndexOutOfRangeException"/> on the first program that selected it — at run time, in generated
    /// code, in whatever program happened to use a signed DISPLAY item.</summary>
    [Fact]
    public void EveryConvention_HasATable()
    {
        Assert.Equal(All.Length, ZonedSign.TableCount);
        foreach (var enc in All)
        {
            var t = ZonedSign.Table(enc);
            Assert.Equal(10, t.Positive.Length);
            Assert.Equal(10, t.Negative.Length);
        }
    }

    /// <summary>Each table is a BIJECTION on the ten digits and the two tables of one convention are DISJOINT: a
    /// repeated character would make one digit unreachable, and an overlap would make a character's SIGN
    /// ambiguous — the decode would have to pick, and the encode/decode round trip would stop being one.</summary>
    [Fact]
    public void EachConvention_IsTenDistinctCharactersPerSign_AndTheTwoSignsAreDisjoint()
    {
        foreach (var enc in All)
        {
            var t = ZonedSign.Table(enc);
            Assert.Equal(10, t.Positive.Distinct().Count());
            Assert.Equal(10, t.Negative.Distinct().Count());
            Assert.Empty(t.Positive.Intersect(t.Negative));
        }
    }

    /// <summary>⛔ THE NEGATIVE TABLE — and ONLY the negative table — is disjoint from the plain digits.
    /// <para>The obvious phrasing ("both tables are disjoint from the digits") is FALSE and must not be written
    /// here: <see cref="SignEncoding.Ascii"/>'s positive table IS <c>"0123456789"</c>, because that convention
    /// leaves a positive digit alone. What actually has to hold is the asymmetric half — a NEGATIVE value's image
    /// can never be mistaken for an unpunched positive one, which is the property that keeps a sign from being
    /// silently lost.</para></summary>
    [Fact]
    public void NoNegativePunch_IsAPlainDigit()
    {
        foreach (var enc in All)
            foreach (char c in ZonedSign.Table(enc).Negative)
                Assert.False(c is >= '0' and <= '9', $"{enc}: negative punch '{c}' is a plain digit");
    }

    /// <summary>Encode and decode are inverse over all 10 digits x both signs, for every convention — the property
    /// the three consumers (<c>FormatDisplaySigned</c>, <c>ParseDisplay</c>, <c>IsNumericZoned</c>) rest on.</summary>
    [Fact]
    public void PunchAndUnpunch_RoundTrip()
    {
        foreach (var enc in All)
            for (int d = 0; d <= 9; d++)
                foreach (bool neg in new[] { false, true })
                {
                    char c = ZonedSign.Punch(enc, d, neg);
                    Assert.True(ZonedSign.TryUnpunch(enc, c, out int digit, out bool decoded), $"{enc} {d} {neg}");
                    Assert.Equal(d, digit);
                    Assert.Equal(neg, decoded);
                    Assert.True(ZonedSign.IsSignCharacter(enc, c));
                }
    }

    /// <summary>A plain digit is always a valid sign character (an item whose sign was never punched — what a group
    /// MOVE of raw characters deposits), and a character outside both tables never is. The negative case is what
    /// makes §8.8.4.4.4 GR3 n)1.a able to answer FALSE at all.</summary>
    [Fact]
    public void ValidSignSet_AdmitsPlainDigits_AndRefusesForeignCharacters()
    {
        foreach (var enc in All)
        {
            for (char d = '0'; d <= '9'; d++) Assert.True(ZonedSign.IsSignCharacter(enc, d), $"{enc} '{d}'");
            foreach (char c in "+- .*&")
                Assert.False(ZonedSign.IsSignCharacter(enc, c), $"{enc} '{c}'");
        }
    }

    /// <summary>⛔ THE TWO CONVENTIONS ACTUALLY DIFFER, asserted rather than assumed: some character is a valid
    /// sign under one and not under the other. Without this, every fact above would still pass if the option
    /// selected the same table twice — the exact failure a "green gate that never looked at what changed" is.</summary>
    [Fact]
    public void TheConventionsAreNotTheSameTable()
    {
        Assert.NotEqual(ZonedSign.Table(SignEncoding.Ibm), ZonedSign.Table(SignEncoding.Ascii));
        Assert.True(ZonedSign.IsSignCharacter(SignEncoding.Ibm, 'C'));
        Assert.False(ZonedSign.IsSignCharacter(SignEncoding.Ascii, 'C'));
        Assert.True(ZonedSign.IsSignCharacter(SignEncoding.Ascii, 't'));
        Assert.False(ZonedSign.IsSignCharacter(SignEncoding.Ibm, 't'));
    }

    /// <summary>THE DEFAULT IS IBM — the owner's decision, at every layer that has one: the enum's zero value, an
    /// unstated <c>NumProfile</c>, a <c>CompilerDriver.Options</c> built without the option, and the emitted
    /// profile text, which states the convention only when it is NOT the default — so an ordinary compile's
    /// emitted PROFILES are byte-identical to what they were before the option existed.
    /// <para>⚠ That is a claim about the profiles ONLY, not about the whole generated file: the emitted class
    /// condition (<c>ConditionRenderer.RenderClass</c>) states the convention ALWAYS, deliberately — a defaulted
    /// parameter on <c>CobolClass.IsNumericZoned</c> would hide exactly the drift this option can cause.</para></summary>
    [Fact]
    public void TheDefaultIsIbm_AtEveryLayer()
    {
        Assert.Equal(SignEncoding.Ibm, default);
        Assert.Equal(SignEncoding.Ibm, new NumProfile
        {
            Digits = 3, FractionDigits = 0, Signed = true,
            Truncation = NumericTruncation.DigitCount, ByteForm = NumericByteForm.Zoned,
        }.SignEncoding);
        Assert.Equal(SignEncoding.Ibm, new CobolNet.CompilerDriver.Options("x.cob", SourceFormat: InitialReferenceFormat.Auto).SignEncoding);

        var pic = new PicInfo(PicCategory.Numeric, Usage.Display, Length: 3, Digits: 3, Scale: 0, Signed: true);
        Assert.DoesNotContain("SignEncoding", pic.ProfileInitializer(SignEncoding.Ibm));
        Assert.Contains("SignEncoding = SignEncoding.Ascii", pic.ProfileInitializer(SignEncoding.Ascii));
    }

    /// <summary>The option's SPELLINGS are derived from the enum, case-insensitive, and refuse anything else —
    /// including the ordinal spellings <c>Enum.TryParse</c> would otherwise accept, which are an internal detail
    /// and not part of the option's contract.</summary>
    [Theory]
    [InlineData("ibm", true, SignEncoding.Ibm)]
    [InlineData("IBM", true, SignEncoding.Ibm)]
    [InlineData("ascii", true, SignEncoding.Ascii)]
    [InlineData("Ascii", true, SignEncoding.Ascii)]
    [InlineData("ebcdic", false, SignEncoding.Ibm)]
    [InlineData("1", false, SignEncoding.Ibm)]
    [InlineData("", false, SignEncoding.Ibm)]
    public void OptionSpelling_ParsesExactlyTheEnumNames(string text, bool ok, SignEncoding expected)
    {
        Assert.Equal(ok, ZonedSign.TryParseOption(text, out var enc));
        Assert.Equal(expected, enc);
    }

    /// <summary>The advertised spelling list IS the enum, lower-cased — so the CLI's help text, its error message
    /// and what it accepts cannot drift apart.</summary>
    [Fact]
    public void OptionSpellings_AreTheEnumNamesLowerCased()
    {
        Assert.Equal([.. All.Select(e => e.ToString().ToLowerInvariant())], ZonedSign.OptionSpellings);
        foreach (string s in ZonedSign.OptionSpellings) Assert.True(ZonedSign.TryParseOption(s, out _));
    }
}
