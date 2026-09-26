// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using CobolNet.Binding.Bound;
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB75 — the EC-SIZE family reaches NON-arithmetic statements through the generic fatal statement guard
/// (<c>EcEmitter.FatalAmbientGates</c>), while ARITHMETIC statements own their §14.7.5 shape in
/// <c>ArithmeticEmitter.EmitArith</c>. The seam between the two is the <see cref="IArithmeticStatement"/> marker: a
/// statement that carries a SIZE ERROR phrase and does not declare it would be guarded TWICE (its own dispatch and
/// the guard's), and one that declares it without EmitArith would be guarded by nobody. These pins hold the marker,
/// the gate table and the exception hierarchy in step.
/// </summary>
public sealed class EcSizeGuardDriftTests
{
    /// <summary>Every bound statement whose constructor takes a <c>SizeErrorPhrase</c> IS an arithmetic statement —
    /// declares the marker — and no marker-bearing statement lacks the phrase.</summary>
    [Fact]
    public void EveryStatementWithASizeErrorPhrase_IsAnArithmeticStatement()
    {
        var asm = typeof(BoundStatement).Assembly;
        var withPhrase = asm.GetTypes()
            .Where(t => typeof(BoundStatement).IsAssignableFrom(t) && !t.IsAbstract)
            .Where(t => t.GetConstructors().Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(SizeErrorPhrase))))
            .ToList();
        Assert.True(withPhrase.Count >= 10, $"expected the ten arithmetic statements + CORRESPONDING; found {withPhrase.Count}");
        foreach (var t in withPhrase)
            Assert.True(typeof(IArithmeticStatement).IsAssignableFrom(t),
                $"{t.Name} carries a SizeErrorPhrase but is not an IArithmeticStatement — the generic EC-SIZE guard "
                + "would dispatch its size error a second time (kb/Work PB75).");
        foreach (var t in asm.GetTypes().Where(t => typeof(IArithmeticStatement).IsAssignableFrom(t) && !t.IsInterface))
            Assert.Contains(t, withPhrase);
    }

    /// <summary>The gate table names the four level-3 EC-SIZE conditions with no flag (unconditional raise sites),
    /// and the guard excludes arithmetic statements from them.</summary>
    [Fact]
    public void GateTable_CarriesTheEcSizeFamily_AndSkipsArithmeticStatements()
    {
        string src = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "EcEmitter.cs"));
        foreach (string ec in new[] { "EC-SIZE-OVERFLOW", "EC-SIZE-ZERO-DIVIDE", "EC-SIZE-EXPONENTIATION", "EC-SIZE-TRUNCATION" })
            Assert.Contains($"(\"{ec}\", null)", src);
        Assert.Contains("ec.Inner is IArithmeticStatement", src);
    }

    /// <summary>The runtime hierarchy: a size error IS a fatal exception condition (§14.7.5 → §14.6.13.1.3), so the
    /// statement guard's <c>catch (CobolFatalException)</c> and RunMain's boundary catch both see it; its EcName is
    /// the Table 13 level-3 name the guard's <c>when</c> filter compares.</summary>
    [Fact]
    public void CobolSizeError_IsACobolFatalException_WithItsLevel3Name()
    {
        Assert.True(typeof(CobolFatalException).IsAssignableFrom(typeof(CobolSizeError)));
        var e = new CobolSizeError("x", "EC-SIZE-ZERO-DIVIDE");
        Assert.Equal("EC-SIZE-ZERO-DIVIDE", e.EcName);
        // The level-3 name is REQUIRED (kb/Work PB1147): a defaulted EC-SIZE-OVERFLOW is how the outermost-quotient
        // PROHIBITED raise latched OVERFLOW where §14.7.4.3 r7 names EC-SIZE-TRUNCATION.
        Assert.False(typeof(CobolSizeError).GetConstructors().Single().GetParameters()
            .Single(p => p.Name == "ecName").HasDefaultValue);
        Assert.StartsWith("EC-SIZE-ZERO-DIVIDE (fatal): x", e.Message);
    }
}
