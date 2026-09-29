// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ ISO §8.4.6.3 2)'s COMMON-program exception is written ONCE, in <see cref="ProgramNameScope"/>, and BOTH scope
/// implementations ask it (kb/Work PB1460): the bind-time AS NESTED table (<c>BinderDriver.NestedCallablesOf</c>) and
/// the run-time resolver (<c>ProgramTable.ResolveVisible</c>). The bind-time table used to admit every COMMON child
/// of every ancestor, so a CALL the resolver would never satisfy compiled clean.
/// </summary>
public sealed class ProgramNameScopeDriftTests
{
    private sealed record P(string Name, P? Container);

    /// <summary>The rule by value over a generated containment chain: OUTER ⊃ Q (common) ⊃ Q1 ⊃ Q2, and a sibling S.</summary>
    [Theory]
    [InlineData("S", false, true)]    // a program elsewhere in the container: always referable
    [InlineData("OUTER", false, true)]
    [InlineData("Q", false, false)]   // the common program itself …
    [InlineData("Q1", false, false)]  // … and every program within it: only when RECURSIVE
    [InlineData("Q2", false, false)]
    [InlineData("Q", true, true)]
    [InlineData("Q2", true, true)]
    public void CommonProgramException_ByValue(string caller, bool recursive, bool referable)
    {
        var outer = new P("OUTER", null);
        var q = new P("Q", outer);
        var q1 = new P("Q1", q);
        var progs = new Dictionary<string, P>
        {
            ["OUTER"] = outer, ["Q"] = q, ["Q1"] = q1, ["Q2"] = new("Q2", q1), ["S"] = new("S", outer),
        };
        Assert.Equal(referable, ProgramNameScope.CommonProgramReferable(q, recursive, progs[caller], p => p.Container));
    }

    /// <summary>Both scope implementations call the one predicate, and neither re-spells the own-chain test.</summary>
    [Theory]
    [InlineData("Cobol.Net.Compiler", "Binding", "BinderDriver.cs")]
    [InlineData("Cobol.Net.Runtime", "Control", "ProgramTable.cs")]
    public void BothScopes_AskTheOnePredicate(string project, string dir, string file)
    {
        string text = File.ReadAllText(Path.Combine(TestRepo.Root, "src", project, dir, file));
        Assert.Contains("ProgramNameScope.CommonProgramReferable(", text);
        Assert.DoesNotContain("onOwnChain", text);
    }
}
