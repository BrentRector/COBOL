// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ISO §7.3.25.3 SR2 — "Exception-name-1 shall be one of the exception names listed in 14.6.13.1, Exception
/// conditions" — is a syntax rule of the <c>&gt;&gt;TURN</c> directive, so it is asked by the directive stage, once, for
/// a COMPILED line (<see cref="TurnDirectiveProcessor.Process"/>) and an OMITTED-branch line
/// (<see cref="TurnDirectiveProcessor.CheckOperand"/>, §7.2.1) alike (kb/Work PB2740). The binder fold used to be the
/// rule's only home and reads compiled events alone, so an omitted branch compiled clean.
/// </summary>
public sealed class TurnDirectiveExceptionNameTests
{
    private static readonly SourceLocation Loc = new("t.cob", 0, 0, 0);

    private static List<string> Compiled(string operand, int dialectLevel, out IReadOnlyList<(string Ec, string? File)> names)
    {
        var bag = new DiagnosticBag();
        var (_, events) = TurnDirectiveProcessor.Process(">>TURN " + operand + "\n", dialectLevel, bag, "t.cob");
        names = events.Count == 0 ? [] : events.Single().Names;
        return [.. bag.Diagnostics.Select(d => d.Code)];
    }

    private static List<string> Omitted(string operand, int dialectLevel)
    {
        var bag = new DiagnosticBag();
        TurnDirectiveProcessor.CheckOperand(operand, dialectLevel, bag, Loc);
        return [.. bag.Diagnostics.Select(d => d.Code)];
    }

    [Theory]   // SR2: unknown name, the EC-IMP- family this implementation does not define, a 2023-only family at 2002/2014
    [InlineData("EC-NOSUCH CHECKING ON", 2023, "COBOLNET0711")]
    [InlineData("EC-IMP-WIBBLE CHECKING ON", 2023, "COBOLNET0711")]
    [InlineData("EC-USER-BAD- CHECKING ON", 2023, "COBOLNET0711")]
    [InlineData("EC-EXTERNAL-FORMAT-CONFLICT CHECKING ON", 2002, "COBOLNET0878")]
    [InlineData("EC-EXTERNAL-FORMAT-CONFLICT CHECKING ON", 2014, "COBOLNET0878")]
    [InlineData("EC-SIZE EC-NOSUCH CHECKING OFF", 2023, "COBOLNET0711")]
    public void CompiledAndOmitted_ReportTheSameSr2Diagnostic(string operand, int dialectLevel, string code)
    {
        Assert.Equal([code], Compiled(operand, dialectLevel, out _));
        Assert.Equal([code], Omitted(operand, dialectLevel));
    }

    [Theory]   // a name the edition has is accepted by both, including the open EC-USER- family and an EC-I-O file-name
    [InlineData("EC-ALL CHECKING ON", 2002)]
    [InlineData("EC-USER-MINE CHECKING ON", 2002)]
    [InlineData("EC-I-O-AT-END F1 CHECKING ON WITH LOCATION", 2002)]
    [InlineData("ec-size CHECKING OFF", 2014)]
    [InlineData("EC-EXTERNAL-FORMAT-CONFLICT CHECKING ON", 2023)]
    public void CompiledAndOmitted_AcceptANameTheEditionHas(string operand, int dialectLevel)
    {
        Assert.Empty(Compiled(operand, dialectLevel, out _));
        Assert.Empty(Omitted(operand, dialectLevel));
    }

    [Fact]   // the event carries only the resolved names, in the catalog's spelling; the refused one contributes no toggle
    public void RefusedName_ContributesNoToggle_AndTheOthersStay()
    {
        var codes = Compiled("ec-size EC-NOSUCH EC-ALL CHECKING ON", 2023, out var names);
        Assert.Equal(["COBOLNET0711"], codes);
        Assert.Equal(["EC-SIZE", "EC-ALL"], names.Select(n => n.Ec));
    }

    [Fact]   // every name refused: SR2 says so once, and the directive is not ALSO called malformed (COBOLNET0718)
    public void EveryNameRefused_IsNotAlsoMalformed()
    {
        Assert.Equal(["COBOLNET0711"], Compiled("EC-NOSUCH CHECKING ON", 2023, out var names));
        Assert.Empty(names);
        Assert.Equal(["COBOLNET0711", "COBOLNET0711"], Compiled("EC-NOSUCH EC-OTHER CHECKING ON", 2023, out _));
    }

    [Fact]   // below COBOL-2002 the directive itself is the refusal (COBOLNET0900); the name window adds nothing
    public void Below2002_TheNameWindowAddsNoSecondDiagnostic()
    {
        Assert.DoesNotContain("COBOLNET0878", Compiled("EC-EXTERNAL-FORMAT-CONFLICT CHECKING ON", 85, out _));
        Assert.DoesNotContain("COBOLNET0878", Omitted("EC-SIZE CHECKING ON", 85));
    }
}
