// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ ISO §14.9.39.3's SET FORMAT 5 TABLE (SR8 / SR10 / SR12 / SR14) IS WRITTEN ONCE, IN THE RUNTIME, AND BOTH PHASES ASK IT
/// (kb/Work PB2463). The compiler asks it for every pair it sees (<c>OoConformance.ObjectRefAssignmentMismatch</c>), the run
/// time for the pairs no compilation sees — §9.3.6 match rule 6 across a universal INVOKE, and §14.8.2.3.3 / §14.8.3.3 at a
/// CALL located at run time (<see cref="ActivationRelations"/>). Before PB2463 the run-time arm was <c>_ =&gt; true</c>
/// because the table lived only in the compiler, over a class table the run time does not have: a method returning an
/// UNRELATED class matched, and the delivery raised EC-OO-UNIVERSAL where rule 6 makes the invocation EC-OO-METHOD. So: no
/// compiler source spells a refusal of the table, the compiler's entry delegates to it, the run-time relations ask it, and
/// the table answers each arm from the class facts an operand carries.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ObjectReferenceSetDriftTests
{
    /// <summary>The refusal texts only the table writes: a second copy of an arm in the compiler would spell one.</summary>
    [Theory]
    [InlineData("(ISO §14.9.39.3 SR12 a)2.)")]
    [InlineData("(ISO §14.9.39.3 SR12 b)2.)")]
    [InlineData("(ISO §14.9.39.3 SR10 a))")]
    [InlineData("(ISO §14.9.39.3 SR14 a))")]
    public void NoCompilerSource_SpellsAnArmOfTheTable(string refusal)
    {
        var offenders = Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(f => File.ReadAllText(f).Contains(refusal, StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToList();
        Assert.True(offenders.Count == 0,
            $"{string.Join(", ", offenders)} spells '{refusal}' — a second copy of an arm of ObjectReferenceSet.Refusal");
    }

    [Fact]
    public void BothPhases_AskTheOneTable()
    {
        string compiler = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Oo", "OoConformance.cs"));
        string runtime = File.ReadAllText(TestRepo.Src("Cobol.Net.Runtime", "Control", "ActivationRelations.cs"));
        Assert.Contains("ObjectReferenceSet.Refusal(table.SetOperand(send), table.SetOperand(recv)", compiler);
        Assert.Contains("ActivationCategory.Object => ObjectReferenceSet.Refusal(", runtime);
    }

    private static ObjectReferenceOperand Class(string name, bool factory = false, bool only = false,
        string[]? classes = null, string[]? interfaces = null) =>
        new(ObjectReferenceKind.ObjectClass, name, factory, only, classes ?? [name], interfaces ?? []);

    /// <summary>Each arm, from the facts alone — the same answers the compiler's SET screens give (the conformance goldens
    /// <c>2002/pb2463_universal_returning_set_match</c> run them through a universal INVOKE).</summary>
    [Fact]
    public void TheTable_AnswersEachArmFromTheClassFacts()
    {
        var sub = Class("S", classes: ["S", "P"], interfaces: ["IB", "IA"]);
        var parent = Class("P", classes: ["P"]);
        var other = Class("O");
        var ifaceA = new ObjectReferenceOperand(ObjectReferenceKind.Interface, "IA", false, false, null, ["IA"]);
        var ifaceB = new ObjectReferenceOperand(ObjectReferenceKind.Interface, "IB", false, false, null, ["IB", "IA"]);
        var universal = new ObjectReferenceOperand(ObjectReferenceKind.Universal, null, false, false, null, null);
        var activeInM = new ObjectReferenceOperand(ObjectReferenceKind.ActiveClass, "M", false, false, ["M"], []);
        var activeReceiver = new ObjectReferenceOperand(ObjectReferenceKind.ActiveClass, "Q", false, false, ["Q"], []);

        Assert.Null(ObjectReferenceSet.Refusal(sub, parent));                                    // SR12 a)2. subclass
        Assert.Contains("SR12 a)2.", ObjectReferenceSet.Refusal(sub, other));                     // unrelated
        Assert.Contains("SR12 a)1.", ObjectReferenceSet.Refusal(sub, Class("S", only: true)));    // ONLY receiver
        Assert.Contains("SR12 a)3.", ObjectReferenceSet.Refusal(sub, Class("S", factory: true))); // FACTORY differs
        Assert.Null(ObjectReferenceSet.Refusal(sub, ifaceA));                                     // SR10 b)2.
        Assert.Contains("SR10 b)2.", ObjectReferenceSet.Refusal(other, ifaceA));
        Assert.Null(ObjectReferenceSet.Refusal(ifaceB, ifaceA));                                  // SR10 a)
        Assert.Contains("SR10 a)", ObjectReferenceSet.Refusal(ifaceA, ifaceB));
        Assert.Contains("SR12 —", ObjectReferenceSet.Refusal(universal, parent));                 // closed list
        Assert.Null(ObjectReferenceSet.Refusal(universal, universal));                            // SR8
        Assert.Null(ObjectReferenceSet.Refusal(activeInM, Class("M")));                           // SR12 b)2.
        Assert.Contains("SR12 b)2.", ObjectReferenceSet.Refusal(activeInM, parent));
        Assert.Null(ObjectReferenceSet.Refusal(activeInM, activeReceiver));                       // SR14 a)
        Assert.Contains("SR14", ObjectReferenceSet.Refusal(sub, activeReceiver));
        Assert.Contains("unresolvable", ObjectReferenceSet.Refusal(Class("X", classes: null) with { Classes = null }, parent));
    }
}
