// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB1091 — THE SPECIAL-NAMES ORDINAL HAS ONE READER, so the three clauses that write one answer alike for
/// every magnitude: ISO §12.3.7.3 SR14 b1/c1 (ALPHABET literal phrase), SR17 b2/c2 (CLASS) and SR16 e/f (SYMBOLIC
/// CHARACTERS). An ordinal is an unsigned INTEGER naming a 1-based position in a character set; its magnitude can only
/// be out of range. Each site used to read it with <c>int.TryParse</c>, which answers "not an integer" for one too long
/// for an <c>int</c>, and the sites then diverged: SYMBOLIC CHARACTERS skipped the name WITHOUT a diagnostic (a later
/// reference said "not defined"), and ALPHABET / CLASS reported the noninteger-literal class rule for an integer. They
/// now read through <c>IntegerOperandRules.HostValue</c>, which saturates; <c>SpecialNamesOrdinalReaderDriftTests</c>
/// holds the paragraph to that one reader.
/// <para>Every expected diagnostic below is derived from the rule it names, never from a run: the native alphanumeric and
/// national sets have 65,536 characters (DOC-A.1-188), STANDARD-1 has 128 (§12.3.7.4 GR7 c) and UCS-4 has 1,112,064
/// scalar values (GR7 f), so an ordinal above the set's count does not exist in it.</para>
/// </summary>
public sealed class SpecialNamesOrdinalTests
{
    private static string Prog(string pid, string specialNames) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        SPECIAL-NAMES.
            ALPHABET AA IS NATIVE
            ALPHABET NA FOR NATIONAL IS NATIVE
            ALPHABET S1 IS STANDARD-1
            ALPHABET U4 FOR NATIONAL IS UCS-4
            {specialNames}
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 FILLER PIC X.
        PROCEDURE DIVISION.
            STOP RUN.
        """;

    private static void Rejects(string pid, string specialNames, params string[] expected)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Prog(pid, specialNames), 2023);
        Assert.False(ok, $"[{pid}] must be REJECTED");
        foreach (string e in expected) EditionHarness.AssertHasDiagnostic(errors, e);
        // The ordinal is an INTEGER whatever its length: never the noninteger-literal class rule.
        Assert.DoesNotContain(errors, m => m.Contains("noninteger literal shall be"));
    }

    /// <summary>The same ordinal text, at the six sites that read one — every magnitude from just past the class's own
    /// range to a 31-digit literal (§8.3.3.3.2's maximum) — is refused under the ORDINAL range rule of ITS clause and
    /// class. 2147483647 is the last <c>int</c>, 2147483648 the first that <c>int.TryParse</c> refused.</summary>
    [Theory]
    [InlineData("a", "65537")]
    [InlineData("b", "2147483647")]
    [InlineData("c", "2147483648")]
    [InlineData("d", "4294967297")]
    [InlineData("e", "99999999999")]
    [InlineData("f", "9999999999999999999999999999999")]
    public void OneOrdinalText_IsOutOfRange_AtEverySite(string tag, string ordinal)
    {
        Rejects("PB1091AB" + tag, $"ALPHABET AX IS {ordinal}.",
            $"the ordinal {ordinal} does not exist in the native alphanumeric character set", "ISO §12.3.7.3 SR14 b1");
        Rejects("PB1091AC" + tag, $"ALPHABET AX FOR NATIONAL IS {ordinal}.",
            $"the ordinal {ordinal} does not exist in the native national character set", "ISO §12.3.7.3 SR14 c1");
        Rejects("PB1091CB" + tag, $"CLASS CX IS {ordinal}.",
            $"the ordinal {ordinal} does not exist in the native alphanumeric character set", "ISO §12.3.7.3 SR17 b2");
        Rejects("PB1091CC" + tag, $"CLASS CX FOR NATIONAL IS {ordinal}.",
            $"the ordinal {ordinal} does not exist in the native national character set", "ISO §12.3.7.3 SR17 c2");
        Rejects("PB1091SE" + tag, $"SYMBOLIC CHARACTERS SX IS {ordinal}.",
            $"SYMBOLIC CHARACTERS SX IS {ordinal}: the ordinal position does not exist in the native character set", "SR16 e2");
        Rejects("PB1091SF" + tag, $"SYMBOLIC CHARACTERS FOR NATIONAL SX IS {ordinal}.",
            $"SYMBOLIC CHARACTERS SX IS {ordinal}: the ordinal position does not exist in the native character set", "SR16 f2");
    }

    /// <summary>SR16 e)1 / f)1 — under IN, "the ordinal position specified by integer-1 shall exist in that character
    /// set": STANDARD-1 has 128 characters, UCS-4 1,112,064. The IN arms shared the silent <c>continue</c>.</summary>
    [Theory]
    [InlineData("PB1091E1A", "SYMBOLIC CHARACTERS SX IS 129 IN S1.", "(STANDARD-1, 128 characters) — ISO §12.3.7.3 SR16 e1")]
    [InlineData("PB1091E1B", "SYMBOLIC CHARACTERS SX IS 99999999999 IN S1.", "(STANDARD-1, 128 characters) — ISO §12.3.7.3 SR16 e1")]
    [InlineData("PB1091F1A", "SYMBOLIC CHARACTERS FOR NATIONAL SX IS 1112065 IN U4.", "(UCS-4, 1112064 characters) — ISO §12.3.7.3 SR16 f1")]
    [InlineData("PB1091F1B", "SYMBOLIC CHARACTERS FOR NATIONAL SX IS 4294967297 IN U4.", "(UCS-4, 1112064 characters) — ISO §12.3.7.3 SR16 f1")]
    public void SymbolicOrdinalUnderIn_MustExistInTheInSet(string pid, string clause, string expected) =>
        Rejects(pid, clause, "COBOLNET1670", expected);

    /// <summary>One bad ordinal draws ONE diagnostic — the SR16 range rule's — not a second, false one at the name's first
    /// use ("'BIG' is not defined": the name was written, and declared, by the clause that failed). Beyond Int32 the
    /// clause used to bind NOTHING and say nothing, so the use was the only diagnostic; below it the clause said so
    /// and the use cascaded.</summary>
    [Theory]
    [InlineData("PB1091U1", "65537")]
    [InlineData("PB1091U2", "99999999999")]
    public void AnOutOfRangeOrdinal_DoesNotMakeItsNameUndefined(string pid, string ordinal)
    {
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {pid}.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                SYMBOLIC CHARACTERS BIG IS {ordinal}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 X PIC X.
            PROCEDURE DIVISION.
                MOVE BIG TO X.
                STOP RUN.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(source, 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "SR16 e2");
        Assert.DoesNotContain(errors, e => e.Contains("'BIG' is not defined", StringComparison.Ordinal));
    }

    /// <summary>The positive controls: the LAST position of each set is legal — a check that refused every large
    /// ordinal could not pass here — and the symbolic character is then DEFINED (an unparsed ordinal used to leave the
    /// name undefined).</summary>
    [Theory]
    [InlineData("PB1091OK1", "SYMBOLIC CHARACTERS SX IS 65536.")]
    [InlineData("PB1091OK2", "SYMBOLIC CHARACTERS SX IS 128 IN S1.")]
    [InlineData("PB1091OK3", "SYMBOLIC CHARACTERS FOR NATIONAL SX IS 1112064 IN U4.")]
    [InlineData("PB1091OK4", "ALPHABET AX IS 65536.")]
    [InlineData("PB1091OK5", "CLASS CX FOR NATIONAL IS 65536.")]
    public void LastPositionOfEachSet_IsLegal(string pid, string clause)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Prog(pid, clause), 2023);
        Assert.True(ok, $"[{pid}] must COMPILE: {string.Join("\n", errors)}");
    }
}
