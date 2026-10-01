// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ ONE "ONE INSTANCE OF THE PICTURE SYMBOL" PREDICATE FOR BOTH CLAUSES THAT ASK IT (kb/Work PB1210). ISO
/// §13.18.2.3 SR1 (ANY LENGTH — 'N', 'X' or '1') and §13.18.19.3 SR1 (DYNAMIC LENGTH — 'N' or 'X') ask the same
/// question of a PICTURE character-string with different symbol sets, and a repetition factor of 1 in ANY spelling
/// (<c>X(01)</c>, a constant-name) is one instance (§13.18.40.3 SR6). The two arms had two spellings of the test — a
/// closed list of six strings and a count-1 regex — so <c>PIC X(01) ANY LENGTH</c> was refused while its DYNAMIC
/// LENGTH twin compiled. Both now call <c>PictureAnalyzer.IsSingleInstance</c>, whose count comes from the one
/// repetition-factor reader; this suite keeps the arms on it and keeps a hand-written spelling from returning.
/// </summary>
public sealed class SingleInstancePictureDriftTests
{
    [Fact]
    public void BothLengthClauses_AskTheOnePredicate()
    {
        string binder = System.IO.File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "DataBinder.cs"));
        Assert.Contains("PictureAnalyzer.IsSingleInstance(pictureText, \"NX1\")", binder);   // ANY LENGTH SR1
        Assert.Contains("PictureAnalyzer.IsSingleInstance(pictureText, \"NX\")", binder);    // DYNAMIC LENGTH SR1
        // No hand-written count-1 spelling survives beside it: a closed list of "(1)" strings or a `(0*1)` regex.
        Assert.DoesNotMatch(@"""[XN1]\(1\)""", binder);
        Assert.DoesNotMatch(@"\\\(0\*1\\\)", binder);
    }

    /// <summary>The predicate itself, over the spellings §13.18.40.3 SR6 makes one instance (a repetition factor of
    /// 1 in any spelling) and the ones that are not: more than one occurrence, a zero or empty factor (SR6 requires "an
    /// unsigned nonzero integer"), an unclosed or spaced factor, a trailing symbol, a symbol outside the caller's set,
    /// and no PICTURE at all.</summary>
    [Theory]
    [InlineData("X", "NX1", true)]
    [InlineData("x", "NX1", true)]
    [InlineData("N", "NX", true)]
    [InlineData("1", "NX1", true)]
    [InlineData("X(1)", "NX1", true)]
    [InlineData("X(01)", "NX1", true)]
    [InlineData("N(001)", "NX", true)]
    [InlineData("1(1)", "NX1", true)]
    [InlineData(" X(01) ", "NX1", true)]
    [InlineData("1", "NX", false)]               // DYNAMIC LENGTH SR1 admits only 'N' or 'X'
    [InlineData("1(1)", "NX", false)]
    [InlineData("XX", "NX1", false)]
    [InlineData("X(2)", "NX1", false)]
    [InlineData("X(10)", "NX1", false)]
    [InlineData("X(0)", "NX1", false)]
    [InlineData("X()", "NX1", false)]
    [InlineData("X(1", "NX1", false)]
    [InlineData("X(1)X", "NX1", false)]
    [InlineData("X (1)", "NX1", false)]
    [InlineData("X(+1)", "NX1", false)]
    [InlineData("A", "NX1", false)]
    [InlineData("9", "NX1", false)]
    [InlineData("", "NX1", false)]
    [InlineData(null, "NX1", false)]
    public void IsSingleInstance_AnswersSr6CountRule(string? picture, string symbols, bool expected)
        => Assert.Equal(expected, CobolNet.Binding.PictureAnalyzer.IsSingleInstance(picture, symbols));
}
