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
}
