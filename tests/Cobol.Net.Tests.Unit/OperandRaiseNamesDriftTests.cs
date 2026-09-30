// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Procedure;
using CobolNet.CodeGen;
using CobolNet.Runtime.Exceptions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1453 / PB1762 — <b>an exception-name an OPERAND can raise is declared by the operand, and every name so
/// declared has a statement guard to land in.</b> ISO §14.6.13.1.1 attaches a raise to the DETECTION inside a
/// statement, wherever it is written, so <c>EcBinder.OperandRaisableNames</c> derives a statement's enabled set from
/// the operands in its own value parts (generated from the semantic model) rather than from its node kind — the
/// shape a hand-maintained <c>QueryFor</c> switch got wrong six times (PB452, PB409, PB326, PB349/PB1036, PB233,
/// PB1453). A name the walker declares that <c>EcEmitter.FatalAmbientGates</c> has no row for would wrap nothing: the
/// statement would bind a <c>BoundEcChecked</c> whose guard catches no name, and the raise would be lost exactly as
/// before. These pins keep the two tables in step.
/// </summary>
public sealed class OperandRaiseNamesDriftTests
{
    [Fact]
    public void EveryOperandDeclaredName_IsACataloguedLevel3Name()
    {
        Assert.NotEmpty(EcBinder.OperandRaises);
        foreach (var (_, name) in EcBinder.OperandRaises)
        {
            Assert.True(ExceptionCatalog.TryGet(name, out var info), $"{name} is not a Table 13 exception-name");
            Assert.Equal(3, info.Level);
        }
    }

    [Fact]
    public void EveryOperandDeclaredName_HasAStatementGuardRow()
    {
        var rows = EcEmitter.FatalAmbientGates.Select(g => g.Ec)
            .Concat(EcEmitter.NonfatalAmbientGates.Select(g => g.Ec)).ToHashSet(StringComparer.Ordinal);
        foreach (var (_, name) in EcBinder.OperandRaises)
            Assert.True(rows.Contains(name),
                $"EcBinder.OperandRaises declares {name}, but EcEmitter has no ambient gate row for it — the "
                + "statement guard would catch nothing and the operand's raise would be lost (kb/Work PB1453).");
    }
}
