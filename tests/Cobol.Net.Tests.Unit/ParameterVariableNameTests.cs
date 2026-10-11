// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Linq;
using CobolNet.Frontend;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB2990 — the PARAMETER phrase of <c>&gt;&gt;DEFINE</c> (§7.3.11.4 GR4, DOC-A.1-49) and of
/// <c>&gt;&gt;DISPLAY</c> (§7.3.12.4 GR3, DOC-A.1-55) names its environment variable by the compilation-variable-name's
/// canonical upper-case spelling, which is <see cref="CobolNames.UpperFold"/> — the Annex C fold of §8.1.3.2 GR3 b) with
/// the basic letters raised — and not the host's <c>ToUpperInvariant</c>. A compilation-variable-name is a COBOL word, so
/// two spellings Annex C makes ONE word (the Kelvin sign and K: Annex C.2 maps (212A,006B)) read ONE variable, and two
/// spellings it keeps apart (a long s and S: C.2 lists no mapping for U+017F) read two. The witness is the NAME the
/// compilation recorded on its inputs (<see cref="CompilationInputs.EnvironmentReads"/>), so the test holds on a host whose
/// environment names are case-insensitive and on one whose names are case-sensitive alike. The Kelvin sign and the long s
/// are built from code points, never written as literals, so no tool can rewrite them.
/// </summary>
public sealed class ParameterVariableNameTests
{
    private static readonly string Kelvin = char.ConvertFromUtf32(0x212A);
    private static readonly string LongS = char.ConvertFromUtf32(0x017F);
    private static readonly string ECapital = char.ConvertFromUtf32(0x00C9);   // E with acute, capital; Annex C.2 (00C9,00E9)
    private static readonly string ESmall = char.ConvertFromUtf32(0x00E9);

    /// <summary>The variable names one directive text read from the environment, in the order read.</summary>
    private static string[] NamesRead(string directives)
    {
        var inputs = new CompilationInputs();
        var bag = new DiagnosticBag();
        var copy = new CopyProcessor([], bag, dialectLevel: 2023, permissive: false);
        ConditionalCompilationProcessor.ProcessWithCopyMapped(
            MappedText.Identity(directives + " PROCEDURE DIVISION.\n", "t.cob"), copy, CobolNet.Frontend.Frontend.LeftDirectives, bag, "t.cob",
            dialectLevel: 2023, permissive: false, inputs: inputs);
        Assert.False(bag.HasErrors, string.Join("\n", bag.Diagnostics));
        return [.. inputs.EnvironmentReads.Select(r => r.Name)];
    }

    /// <summary>(a name as written, the variable the canonical spelling names) — DOC-A.1-49 / DOC-A.1-55.</summary>
    public static TheoryData<string, string> Spellings => new()
    {
        { "pb2990-plain", "PB2990-PLAIN" },
        { "Pb2990-Plain", "PB2990-PLAIN" },
        { "PB2990-LA" + Kelvin + "E", "PB2990-LAKE" },    // Annex C.2 (212A,006B): the Kelvin sign IS k — the host keeps it apart
        { "pb2990-la" + Kelvin + "e", "PB2990-LAKE" },
        { "PB2990-LAKE", "PB2990-LAKE" },
        { "PB2990-" + ECapital + "T", "PB2990-" + ESmall + "T" },   // an extended letter stays in its fold: Annex C maps only upper to lower
        { "pb2990-" + ESmall + "t", "PB2990-" + ESmall + "T" },
        { "PB2990-" + LongS, "PB2990-" + LongS },         // Annex C.2 lists no mapping for U+017F — the host would read it as S
    };

    [Theory] // §7.3.11.4 GR4 + DOC-A.1-49: the PARAMETER phrase of DEFINE
    [MemberData(nameof(Spellings))]
    public void Define_ReadsTheVariableOfTheCanonicalSpelling(string written, string variable)
        => Assert.Equal([variable], NamesRead($" >>DEFINE {written} AS PARAMETER\n"));

    [Theory] // §7.3.12.4 GR3 + DOC-A.1-55: the PARAMETER phrase of DISPLAY — the same lookup
    [MemberData(nameof(Spellings))]
    public void Display_ReadsTheVariableOfTheCanonicalSpelling(string written, string variable)
        => Assert.Equal([variable], NamesRead($" >>DISPLAY PARAMETER {written}\n"));

    [Fact] // two spellings of ONE COBOL word (Annex C.2 (212A,006B)) read ONE variable; two words Annex C keeps apart, two
    public void SpellingsAnnexCJoinsReadOneVariable_AndSpellingsItKeepsApartReadTwo()
    {
        Assert.Single(NamesRead($" >>DISPLAY PARAMETER PB2990-X{Kelvin}\n >>DISPLAY PARAMETER PB2990-Xk\n").Distinct());
        Assert.Equal(2, NamesRead($" >>DISPLAY PARAMETER PB2990-{LongS}\n >>DISPLAY PARAMETER PB2990-S\n").Distinct().Count());
    }
}
