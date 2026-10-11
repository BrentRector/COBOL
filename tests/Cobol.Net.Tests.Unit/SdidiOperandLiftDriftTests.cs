// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A FIXED-POINT OPERAND ENTERS THE STANDARD-DECIMAL INTERMEDIATE THROUGH ONE LIFT, AND THAT LIFT ROUNDS (kb/Work
/// PB2918). ISO §8.8.1.5.2 r1: "If the operand value cannot be expressed exactly in an SDIDI, the value is rounded
/// according to the rules in 11.9.11" — and a 16-byte binary container holds 39 digits to the SDIDI's 34.
/// <c>CobolDec.From</c> is the EXACT lift (it carries the value as given); the emitter reaches it only through
/// <c>NumericRenderer.DecOperandExact</c>, whose callers are the two ROUND-TO-ODD consumers (the exact product and
/// quotient reduce the exact value once, so a pre-rounded operand would be rounded twice). Every other operand lift
/// goes through <c>DecOperand</c>, which rounds an operand whose digit bound passes 34. A new emit site that spells
/// <c>CobolDec.From(</c> itself puts an unrounded significand past the 34-digit invariant of every CobolDec
/// operation back into the intermediate, which is the defect.
/// </summary>
public sealed class SdidiOperandLiftDriftTests
{
    private static string[] CodeGenFiles() =>
        Directory.GetFiles(TestRepo.Src("Cobol.Net.Compiler", "CodeGen"), "*.cs", SearchOption.AllDirectories);

    private static bool IsCode(string line)
    {
        string t = line.TrimStart();
        return !t.StartsWith("//", StringComparison.Ordinal);
    }

    [Fact]
    public void TheExactLift_IsSpelledInExactlyOnePlace_AndThroughTheRuntimeApiHelper()
    {
        var offenders = new List<string>();
        foreach (string file in CodeGenFiles())
        {
            string name = Path.GetFileName(file);
            foreach (string line in File.ReadLines(file).Where(IsCode))
            {
                if (!Regex.IsMatch(line, @"CobolDec\.From\(")) continue;   // the exact lift, spelled out in emitted text
                if (name == "NumericRenderer.cs" && line.Contains("var v => $\"CobolDec.From(")) continue;   // DecOperandExact's arm
                offenders.Add($"{name}: {line.Trim()}");
            }
        }
        Assert.True(offenders.Count == 0,
            "An emit site spells `CobolDec.From(` itself. The exact lift is NumericRenderer.DecOperandExact's, for the "
            + "round-to-odd consumers; every other operand rounds at the lift (DecOperand, ISO 8.8.1.5.2 r1; kb/Work PB2918). "
            + "Offenders: " + string.Join("; ", offenders));
    }

    [Fact]
    public void TheExactOperandLift_HasOnlyTheRoundToOddConsumers()
    {
        string src = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Emit", "NumericRenderer.cs"));
        var offenders = new List<string>();
        foreach (string line in src.Split('\n').Where(IsCode))
        {
            if (!line.Contains("DecOperandExact(")) continue;
            if (line.Contains("private string DecOperandExact(")) continue;                // the declaration
            if (line.Contains("=> DecOperandExact(x)")) continue;                          // DecOperand's own pass-through arms
            if (line.Contains("DecMulToOdd(") || line.Contains("DecDivToOdd(")) continue;   // the round-to-odd consumers
            offenders.Add(line.Trim());
        }
        Assert.True(offenders.Count == 0,
            "DecOperandExact lifts a fixed-point operand WITHOUT rounding, which only a round-to-odd consumer may do "
            + "(it reduces the exact value once). Offenders: " + string.Join("; ", offenders));
    }
}
