// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A FLOAT AT A §15.3 TYPE-6 (INTEGER) POSITION MOVES NO CALL ONTO THE FLOAT LANE (kb/Work PB3268, review R2-2).
/// <c>IntrinsicRenderer.RawArg</c> converts a binary64 or fractional operand at an integer position to its integer on the
/// exact carrier BEFORE the lane predicates ask their question, so <c>COMBINED-DATETIME(F1 12345.678901)</c> with
/// <c>F1</c> a COMP-2 stays an exact value (§15.17.4 r1: "argument-1 + (argument-2 / 100000)"). The
/// bind-time prediction of the §14.9.25.4 GR1 intermediate result item (<c>FunctionValueLane</c>) has to read the
/// arguments the same way: reading the COMP-2 as a float argument gave the intermediate a COMP-2 that rounded the exact
/// value, so a MOVE to two receivers stored a different digit than the MOVE to one (§15.4.1: "the returned value is the
/// same for all instances of a given function"). Strict conformance rejects a float at a type-6 position
/// (COBOLNET1627, §15.3), so the witness runs under <c>--permissive</c> — the only mode in which the shape exists,
/// which is why it is not a conformance golden.
/// </summary>
public sealed class FunctionValueLanePermissiveTests : CobolNetTestBase
{
    [Fact]
    public void AFloatAtAnIntegerPosition_DoesNotGiveTheIntermediateAFloatCarrier()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB3268PM.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 F1 USAGE FLOAT-LONG VALUE 150000.
            01 Q1 PIC 9(6)V9(15).
            01 Q2 PIC 9(6)V9(15).
            01 Q3 PIC 9(6)V9(15).
            PROCEDURE DIVISION.
            MAIN.
                MOVE FUNCTION COMBINED-DATETIME(F1 12345.678901) TO Q1 Q2
                MOVE FUNCTION COMBINED-DATETIME(F1 12345.678901) TO Q3
                DISPLAY "CDT " Q1 " " Q2 " " Q3
                STOP RUN.
            """;
        string srcPath = Path.Combine(TempDir, "pb3268pm.cob");
        string dllPath = Path.Combine(TempDir, "pb3268pm.dll");
        File.WriteAllText(srcPath, source);
        var compiled = CompilerDriver.Compile(new CompilerDriver.Options(
            srcPath, dllPath, DialectLevel: 2023, Permissive: true, SourceFormat: InitialReferenceFormat.Free));
        Assert.True(compiled.Success, $"({compiled.Status}):\n  {string.Join("\n  ", compiled.Errors)}");

        var (ran, stdout, detail) = RunAssembly(dllPath);
        Assert.True(ran, detail);
        // 150000 + 12345.678901 / 100000 = 150000.12345678901 (§15.17.4 r1), exactly, per receiver and whatever the
        // receiver set; a binary64 intermediate rounds the trailing digits away.
        string[] fields = stdout.Trim().Split(' ');
        Assert.Equal(4, fields.Length);
        Assert.True(fields[1] == fields[3] && fields[2] == fields[3],
            $"a MOVE of one function to two receivers must store what the MOVE to one receiver stores (§15.4.1): {stdout.Trim()}");
        Assert.Equal("150000123456789010000", fields[3]);
    }
}
