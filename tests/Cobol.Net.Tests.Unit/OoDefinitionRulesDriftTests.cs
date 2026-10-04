// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.IO;
using System.Linq;
using CobolNet.Binding;
using CobolNet.Frontend.Generated;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE OO DATA-DIVISION PLACEMENT TABLE IS TOTAL AND IS THE ONLY JUDGE (kb/Work PB1251). Which sections of a data
/// division a method, factory or instance definition may carry is <c>OoDefinitionRules</c>'s one table; this pins its
/// shape: (1) every (data-bearing definition kind × data-division section) cell answers — admitted, or the clause that
/// refuses it — so a section added to the grammar or a definition kind added to the table cannot leave a silent cell;
/// (2) the table's section list is exactly the grammar's <c>dataDivision</c> alternatives; (3) the table's verdicts are
/// the standard's (§13.4.3, §13.5.3, §13.6.3, §13.7.3, §13.8.3, §13.9.3 SR1 — each citation run through
/// <c>cite.py --check</c>); (4) no site spells a placement refusal of its own: the method arm of
/// <c>DataBinder.OoBindMethodData</c> and the class driver ask the table, and nothing raises
/// <c>COBOLNET1519</c> by literal code.
/// </summary>
public sealed class OoDefinitionRulesDriftTests
{
    [Fact]
    public void EveryDefinitionKindAndSection_HasAVerdict()
    {
        foreach (var kind in OoDefinitionRules.DataBearingKinds)
            foreach (var section in Enum.GetValues<OoDataSection>())
            {
                var ex = Record.Exception(() => OoDefinitionRules.RefusedBy(kind, section));
                Assert.True(ex is null, $"{kind} x {section} has no verdict in OoDefinitionRules: {ex?.Message}");
            }
    }

    [Fact]
    public void TheTablesSections_AreTheGrammarsDataDivisionSections()
    {
        var grammar = typeof(CobolParserCore.DataDivisionContext)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.DeclaredOnly)
            .Where(m => m.GetParameters().Length == 0 && m.Name.EndsWith("Section", StringComparison.Ordinal))
            .Select(m => m.Name.ToLowerInvariant())
            .Order().ToList();
        var table = Enum.GetNames<OoDataSection>()
            .Select(n => n switch { "WorkingStorage" => "workingstorage", "LocalStorage" => "localstorage", _ => n.ToLowerInvariant() } + "section")
            .Order().ToList();
        Assert.Equal(grammar, table);
    }

    [Theory]
    // Method definition: LOCAL-STORAGE and LINKAGE only (§13.6.3 SR1, §13.7.3 SR1); the other four are refused.
    [InlineData("Method", "File", "§13.4.3 SR1")]
    [InlineData("Method", "WorkingStorage", "§13.5.3 SR1")]
    [InlineData("Method", "LocalStorage", null)]
    [InlineData("Method", "Linkage", null)]
    [InlineData("Method", "Report", "§13.8.3 SR1")]
    [InlineData("Method", "Screen", "§13.9.3 SR1")]
    // Factory and instance definitions: the four "only in a factory or instance" sections; no LOCAL-STORAGE or LINKAGE.
    [InlineData("Factory", "File", null)]
    [InlineData("Factory", "WorkingStorage", null)]
    [InlineData("Factory", "LocalStorage", "§13.6.3 SR1")]
    [InlineData("Factory", "Linkage", "§13.7.3 SR1")]
    [InlineData("Factory", "Report", null)]
    [InlineData("Factory", "Screen", null)]
    [InlineData("Instance", "File", null)]
    [InlineData("Instance", "WorkingStorage", null)]
    [InlineData("Instance", "LocalStorage", "§13.6.3 SR1")]
    [InlineData("Instance", "Linkage", "§13.7.3 SR1")]
    [InlineData("Instance", "Report", null)]
    [InlineData("Instance", "Screen", null)]
    public void TheVerdicts_AreTheStandards(string kind, string section, string? refusedBy) =>
        Assert.Equal(refusedBy, OoDefinitionRules.RefusedBy(Enum.Parse<OoDefinition>(kind), Enum.Parse<OoDataSection>(section)));

    [Fact]
    public void NoSiteSpellsAPlacementRefusalOfItsOwn()
    {
        string oo = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "DataBinder.Oo.cs"));
        Assert.Contains("OoDefinitionRules.Screen(OoDefinition.Method", oo);
        string driver = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Oo", "OoDriver.cs"));
        Assert.Contains("OoDefinitionRules.Screen(OoDefinition.Factory", driver);
        Assert.Contains("OoDefinitionRules.Screen(OoDefinition.Instance", driver);
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                                 && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")))
            Assert.False(File.ReadAllText(file).Contains("\"COBOLNET1519\"", StringComparison.Ordinal),
                $"{file} raises COBOLNET1519 by literal code — the placement family is DiagnosticCatalog.OoDataDivisionPlacement, asked only through OoDefinitionRules.Screen");
    }
}
