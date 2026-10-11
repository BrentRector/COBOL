// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using Xunit;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A DYNAMIC-CAPACITY TABLE'S MAXIMUM IS THE IMPLEMENTOR'S, NEVER ZERO AND NEVER <c>TO</c> (kb/Work PB2833).
/// ISO §8.5.1.9.1 2) — a dynamic-capacity table "may have an unlimited capacity" — and §13.18.38.4 GR17 — "Integer-5
/// is the expected capacity of the table" — a SET past it sets EC-BOUND-SET (§14.9.39.4 GR30) and still grows the table.
/// <c>OccursSpec.Max</c> is zero for such a table (it has no allocated maximum), and the two layouts that size a
/// component by its maximum — the cell group window (<see cref="VarGroupWindow"/>, with its element shapes) and a
/// declared group's contiguous layout — each read it, so a contiguous image stored into a group holding one dropped
/// the table's occurrences. <see cref="DataItem.VariableComponentMax"/> is now the one reader; this pins its answer for
/// every component kind and that the cell window carries it, so the table arm cannot go back to reading
/// <see cref="OccursSpec.Max"/>.
/// </summary>
public sealed class DynamicTableMaximumTests
{
    private const int ImplementorMaximum = CobolNet.Runtime.CobolDynTable<byte>.MaxOccurrences;

    private static DataBinder Bind(string ws)
    {
        string src = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DTM{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            {ws}
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """;
        string path = Path.Combine(Path.GetTempPath(), "cn_dtm_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            Assert.NotNull(tree);
            var bound = new BinderDriver().Bind(tree!, new EditionContext(2023));
            return bound.Units[0].Data;
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }

    private static VarGroupWindow WindowOf(DataItem group) =>
        VarGroupWindow.Of(group, new AccessPath([]), new PositionConstant(0))
        ?? throw new InvalidOperationException($"'{group.CobolName}' holds no variable-length component");

    [Fact]
    public void DynamicTable_MaximumIsTheImplementorsWhateverFromAndToSay()
    {
        var d = Bind("""
            01 G EXTERNAL.
               05 PRE PIC XX.
               05 PLAIN PIC X OCCURS DYNAMIC CAPACITY IN C1.
               05 EXPECTED PIC X OCCURS DYNAMIC CAPACITY IN C2 FROM 2 TO 5.
            """);
        var plain = d.ByName["PLAIN"][0];
        var expected = d.ByName["EXPECTED"][0];
        Assert.Equal(0, plain.OccursSpec!.Max);   // the premise: no allocated maximum
        Assert.Equal(ImplementorMaximum, plain.OccursSpec!.MaxCapacity);
        Assert.Equal(ImplementorMaximum, plain.VariableComponentMax);
        // TO integer-5 is the EXPECTED capacity (GR17), not a bound
        Assert.Equal(5, expected.OccursSpec!.ExpectedMax);
        Assert.Equal(ImplementorMaximum, expected.VariableComponentMax);
    }

    [Fact]
    public void CellGroupWindow_CarriesTheDynamicTablesMaximum_NotZero()
    {
        var d = Bind("""
            01 G EXTERNAL.
               05 PRE PIC XX.
               05 DYNLEN PIC X DYNAMIC LENGTH LIMIT IS 10.
               05 T PIC X OCCURS DYNAMIC CAPACITY IN C1.
            """);
        var window = WindowOf(d.ByName["G"][0]);
        // storage order: the dynamic-length item, then the table
        Assert.Equal(new[] { 10, ImplementorMaximum }, window.DynMax);
    }

    [Fact]
    public void ElementShape_OfADynamicTableOfGroups_CarriesTheInnerTablesMaximum()
    {
        var d = Bind("""
            01 G EXTERNAL.
               05 OUTER OCCURS DYNAMIC CAPACITY IN C1.
                  10 K PIC X.
                  10 INNER PIC X OCCURS DYNAMIC CAPACITY IN C2.
            """);
        var window = WindowOf(d.ByName["G"][0]);
        Assert.Equal(new[] { ImplementorMaximum }, window.DynMax);
        var shape = Assert.Single(window.DynElem)!;
        Assert.Equal(new[] { ImplementorMaximum }, shape.DynMax);
    }

    [Fact]
    public void FixedAndDependingTables_KeepTheirOccursMaximum()
    {
        var d = Bind("""
            01 REC.
               05 CNT PIC 9.
               05 FIXED-T PIC X OCCURS 4.
               05 ODO-T PIC X OCCURS 1 TO 6 DEPENDING ON CNT.
            """);
        Assert.Equal(6, d.ByName["ODO-T"][0].OccursSpec!.MaxCapacity);
    }
}
