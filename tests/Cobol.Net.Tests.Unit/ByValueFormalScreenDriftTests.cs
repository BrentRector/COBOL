// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.IO;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE BY VALUE FORMAL'S CLASS RULE IS WRITTEN ONCE (kb/Work PB1051). ISO §14.2.2 SR2 — "Each data-name-1 specified in
/// a BY VALUE phrase shall be defined as a data item of class numeric, message-tag, object, or pointer" (cite.py --check
/// 14.2.2 → OK 14.2.2 2)) — is <c>ProcedureHeaderScreen.ByValueClass</c>; the program/function header arm
/// (<c>CallBindLinkage</c>) and the METHOD header arm (<c>OoBindMethodData</c>) both ASK it, and neither spells the
/// class set or the <c>COBOLNET1553</c> code itself. Before this the program arm held the rule inline and the method arm
/// refused every BY VALUE formal outright — a second copy of the rule waiting to disagree. Likewise the method formal's
/// passing mode is compared by the ONE §9.3.8.2.3 rule set (<c>OoConformance.MethodConformanceMismatches</c>, rule 1).
/// </summary>
public sealed class ByValueFormalScreenDriftTests
{
    private static string Compiler(params string[] parts) => File.ReadAllText(TestRepo.Src(["Cobol.Net.Compiler", .. parts]));

    [Theory]
    [InlineData("Binding", "DataBinder.Linkage.cs")]
    [InlineData("Binding", "DataBinder.Oo.cs")]
    public void EveryHeaderArm_AsksTheOneByValueClassScreen_AndSpellsNoClassSetOfItsOwn(string folder, string file)
    {
        string text = Compiler(folder, file);
        Assert.Contains("header.ByValueClass(", text);
        Assert.DoesNotContain("COBOLNET1553", text);
    }

    [Fact]
    public void TheMethodRuleSet_ComparesThePassingMode()
    {
        string text = Compiler("Oo", "OoConformance.cs");
        Assert.Contains("Formals[i].ByValue != m2.Binding!.Formals[i].ByValue", text);
    }
}
