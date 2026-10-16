// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A POST-BUILD CLAUSE RESOLVER WALKS THE TYPE DECLARATIONS TOO, AND THE LIST IS CLOSED (kb/Work PB2548, PB2071).
/// A type declaration has no storage (ISO §13.18.58.4 GR2) but its clauses are source, and a clause that names another
/// entry of the declaration — a RENAMES operand, an OCCURS KEY data-name, a DEPENDING ON counter, a CAPACITY phrase's
/// FROM / TO — has an answer without any use. Every resolver but REDEFINES used to walk <c>AllItems()</c>, the
/// storage roots, so an unused template's <c>RENAMES NOSUCH</c> or <c>KEY IS NOSUCH</c> compiled clean while the same
/// clause in a record was refused. <c>DataBinder.ClauseResolutionForest()</c> is the one forest that includes the
/// templates; this pins that the resolvers of those clauses ask it and not the roots, so the next resolver added to
/// the list is a one-line addition here, and one that forgets the templates is a red test, not a silent hole.
/// </summary>
public sealed class TemplateClauseResolutionDriftTests
{
    /// <summary>(file, method) — each one's body must walk <c>ClauseResolutionForest()</c> and never <c>AllItems()</c>.</summary>
    public static TheoryData<string, string> Resolvers => new()
    {
        { "DataBinder.Odo.cs", "OccursKeyResolve" },
        { "DataBinder.Odo.cs", "OdoResolve" },
        { "DataBinder.Odo.cs", "DynamicResolve" },
        { "DataBinder.cs", "RenamesOwners" },
        { "DataBinder.cs", "ResolveRestrictedTypes" },
        { "DataBinder.cs", "ResolveRestrictedPrototypes" },
        { "DataBinder.cs", "RestrictedPrototypePointers" },
    };

    private static string BodyOf(string file, string method)
    {
        string text = File.ReadAllText(Path.Combine(TestRepo.Src("Cobol.Net.Compiler", "Binding"), file));
        var start = Regex.Match(text, $@"^    (?:internal |private )?[\w<>\[\]][\w<>\[\], ]* {method}\(", RegexOptions.Multiline);
        Assert.True(start.Success, $"{method} not found in {file} — the pin measured nothing");
        // the body ends at the next member at the same indent: the first line "    }" (block body) or ";" (expression body)
        var end = Regex.Match(text[start.Index..], @"^    (?:\}|[^\s/].*;)\s*$", RegexOptions.Multiline);
        Assert.True(end.Success, $"the end of {method} was not found in {file}");
        return text.Substring(start.Index, end.Index + end.Length);
    }

    [Theory]
    [MemberData(nameof(Resolvers))]
    public void ThePostBuildClauseResolver_WalksTheTypeDeclarationsToo(string file, string method)
    {
        string body = BodyOf(file, method);
        Assert.Contains("ClauseResolutionForest()", body);
        Assert.DoesNotContain("AllItems()", body);
    }

    /// <summary>The placement-dependent rules stay out of a declaration: its counter may be supplied by the use site
    /// (ISO §13.18.57.4 GR1, "coded in place"), and it defines no CAPACITY register (§13.18.58.4 GR2).</summary>
    [Fact]
    public void TheRulesThatReadPlacement_AskWhetherTheEntryIsInATypeDeclaration()
    {
        string odo = BodyOf("DataBinder.Odo.cs", "OdoResolve");
        Assert.Contains("InTypeDeclaration(item)", odo);
        Assert.Contains("if (declaration) continue;", odo);
        Assert.Contains("!declaration && DependingAttributeFault", odo);
        Assert.Contains("if (InTypeDeclaration(item)) continue;", BodyOf("DataBinder.Odo.cs", "DynamicResolve"));
    }
}
