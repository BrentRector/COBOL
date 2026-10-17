// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE TYPE OF A RESTRICTED PROGRAM- OR FUNCTION-POINTER IS ITS PROTOTYPE'S SIGNATURE, SO NO RELATION COMPARES THE
/// PROTOTYPE'S NAME (kb/Work PB2464).
/// <para>ISO §13.18.60.4 GR25 / GR26 type such a pointer by the SIGNATURE of the prototype it names ("shall contain
/// only … the address of a program with the same signature as that identified by the specified
/// program-prototype-name"), so §14.9.39.3 SR20 / SR22 admit a SET between pointers restricted to two differently
/// named prototypes of one signature, and §14.8.2.3.2 ("If either is a restricted pointer, both shall be restricted
/// and of the same type") is the same question at an argument. The relations that have a unit's prototype tables in
/// hand ask <c>PrototypeSignatures.Same</c>; the ones that do not — the §9.3.6 match of a UNIVERSAL INVOKE, whose two
/// descriptions travel to run time, and the typed comparator (since deleted, kb/Work PB3101) — compared
/// <c>PicInfo.RestrictedPrototypeName</c> instead, which refused two prototypes of one signature in the first and
/// rejected legal source in the second. Both now read the pointer's resolved TYPE,
/// <c>PrototypeSignatures.RestrictionIdentity</c> (the signature class <c>PrototypeSignatureClasses.Classify</c>
/// attached; while the classes are refined it is the prototype's current block, kb/Work PB2495 / PB3101), and this
/// test keeps the NEXT relation from comparing the name again: no source line may compare
/// <c>RestrictedPrototypeName</c>. Behaviour is pinned by <c>conformance:cases/_unassigned/pb2464_prototype_pointer_signature_type</c>,
/// <c>conformance:cases/_unassigned/pb2464_function_pointer_signature_type</c> and the negative
/// <c>pb2464-typed-pointer-signature-mismatch</c>.</para>
/// <para>Proven to FAIL before it was trusted: <see cref="ThePatternThisTestKeysOn_FiresOnThePB2464Defect"/> feeds the
/// matcher the exact line the tree carried and asserts it is caught, and feeds it the repaired line and the signature
/// readers and asserts they are not.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class PrototypePointerTypeDriftTests
{
    /// <summary>A COMPARISON over the prototype name: a name-equality call or an operator with the property on either side.</summary>
    private static readonly Regex NameComparison = new(
        @"(?:CobolNames\.Same|string\.Equals|\.Equals|Comparer\.Equals)\s*\([^;]*RestrictedPrototypeName"
        + @"|RestrictedPrototypeName\s*(?:==|!=)|(?:==|!=)\s*[\w.?!]*RestrictedPrototypeName",
        RegexOptions.Compiled);

    [Fact]
    public void NoSourceLineComparesARestrictedPrototypeName()
    {
        var offenders = new List<string>();
        string root = TestRepo.Src("Cobol.Net.Compiler");
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                                 && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)))
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string code = lines[i].TrimStart();
                if (code.StartsWith("//", StringComparison.Ordinal)) continue;
                if (NameComparison.IsMatch(code))
                    offenders.Add($"{Path.GetRelativePath(root, file)}:{i + 1}  {code}");
            }
        }
        Assert.True(offenders.Count == 0,
            "A source line compares a restricted program- or function-pointer's prototype NAME. ISO §13.18.60.4 GR25 / GR26 "
            + "type such a pointer by its prototype's SIGNATURE, so two pointers restricted to differently named prototypes of "
            + "one signature are of the same type (§14.8.2.3.2; §14.9.39.3 SR20 / SR22). Ask the pointer's TYPE — "
            + "PrototypeSignatures.RestrictionIdentity — or, with the unit's prototype tables in hand, "
            + "PrototypeSignatures.Same (kb/Work PB2464):\n  " + string.Join("\n  ", offenders));
    }

    /// <summary>A <c>PicInfo</c> record-equality compare as the type model writes it: a <c>with { EditingRules = null, … }</c>
    /// projection, the shape that elides the members a record compare cannot carry.</summary>
    private static readonly Regex ProfileProjection = new(@"with\s*\{\s*EditingRules\s*=\s*null[^}]*\}", RegexOptions.Compiled);

    /// <summary>The restricted-pointer names a <c>PicInfo</c> record compare must not read (kb/Work PB3120): the prototype
    /// name (a program- or function-pointer is typed by its prototype's SIGNATURE) and the type-name (a data-pointer is
    /// typed by the DECLARATION it names, which the record compare elides).</summary>
    private static readonly string[] RestrictionNames = ["RestrictedPrototypeName", "RestrictedTypeName"];

    /// <summary>⛔ A <c>PicInfo</c> RECORD COMPARE DOES NOT DECIDE A RESTRICTED POINTER'S TYPE (kb/Work PB3120).
    /// <c>StrongTypeModel.SameAnalyzedProfile</c> compares two elementary items' analyzed profiles with <c>PicInfo</c>'s own
    /// record equality, which compares <c>RestrictedPrototypeName</c> and <c>RestrictedTypeName</c> by value — the NAMES
    /// §8.5.3.1 does not compare. A group formal that held a pointer restricted to two differently named prototypes of one
    /// signature (or to two declarations of one type-name) was therefore decided by the name while the elementary formal of
    /// the same pair was decided by the type, refusing the first (COBOLNET1513) and admitting the second. Every
    /// <c>with { EditingRules = null, … }</c> projection in the compiler must elide BOTH names, and the comparison reads
    /// <c>PrototypeSignatures.SameType</c> / <c>StrongTypeModel.SameRestriction</c> instead.</summary>
    [Fact]
    public void EveryPicInfoProfileProjectionElidesTheRestrictedNames()
    {
        var offenders = new List<string>();
        string root = TestRepo.Src("Cobol.Net.Compiler");
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                                 && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)))
            foreach (string missing in ProjectionsMissingARestrictedName(File.ReadAllText(file)))
                offenders.Add($"{Path.GetRelativePath(root, file)}  {missing}");
        Assert.True(offenders.Count == 0,
            "A PicInfo profile projection leaves a restricted-pointer name in the record compare, so a restricted pointer is "
            + "decided by its NAME (kb/Work PB3120) - elide it and ask the pointer's TYPE:\n  " + string.Join("\n  ", offenders));
    }

    private static IEnumerable<string> ProjectionsMissingARestrictedName(string source) =>
        ProfileProjection.Matches(source)
            .Where(m => RestrictionNames.Any(n => !m.Value.Contains(n + " = null", StringComparison.Ordinal)))
            .Select(m => m.Value);

    [Fact]
    public void TheProjectionPin_FiresOnThePB3120Defect()
    {
        // The exact projection StrongTypeModel.SameAnalyzedProfile carried before the repair.
        Assert.NotEmpty(ProjectionsMissingARestrictedName(
            "if ((x with { EditingRules = null, RestrictedTypeDecl = null }) != (y with { EditingRules = null, RestrictedTypeDecl = null }))"));
        // Eliding only the prototype name still leaves a data-pointer's type-name in the compare.
        Assert.NotEmpty(ProjectionsMissingARestrictedName(
            "x with { EditingRules = null, RestrictedTypeDecl = null, RestrictedPrototypeName = null }"));
        Assert.Empty(ProjectionsMissingARestrictedName(
            "x with { EditingRules = null, RestrictedTypeName = null, RestrictedTypeDecl = null, RestrictedPrototypeName = null }"));
    }

    /// <summary>⛔ EVERY GROUP-LEVEL SIGNATURE CHECK RUNS AFTER THE POINTER TYPES EXIST (kb/Work PB2495). A restricted
    /// program- or function-pointer formal is typed by the signature class <c>BinderDriver.ResolvePointerRestrictions</c>
    /// attaches; a check that runs before it reads an unstamped pointer, whose identity is its prototype's own NAME, and so
    /// refuses an override, an implementation or a §10.6.2 SR2 prototype / definition pair restricted to two differently
    /// named prototypes of one signature. Being after the program units' DATA phase is not enough: the four OO checks
    /// once ran ahead of that phase, but <c>CheckPrototypeSignaturePairs</c> ran inside <c>BuildGroupRepository</c> —
    /// after it, and still ahead of <c>ResolvePointerRestrictions</c> — and refused the same pairs.
    /// <para>The pin is EXECUTION order, not text order: a helper defined below <c>Bind</c> can run before a call written
    /// above it, which is exactly where <c>CheckPrototypeSignaturePairs</c> sat. So each check is invoked ONCE in the
    /// compiler, as a top-level statement of <c>BinderDriver.Bind</c>'s body (no loop, branch, lambda or helper between),
    /// as is <c>ResolvePointerRestrictions</c>, and the check's statement follows it. Proven to fail on the pre-repair
    /// shape by <see cref="TheOrderPin_FiresOnThePB2495Defect"/>. Behaviour is pinned by
    /// <c>conformance:cases/_unassigned/pb2495_pointer_type_in_signature_checks</c>.</para></summary>
    [Theory]
    [InlineData("CheckPrototypeSignaturePairs")]
    [InlineData("ValidateInterfaceInheritance")]
    [InlineData("ValidateOverrideSignatures")]
    [InlineData("ValidatePropertyAccessorPairs")]
    [InlineData("ValidateImplements")]
    public void EverySignatureCheckRunsAfterThePointerTypesAreResolved(string check)
    {
        string? violation = OrderViolation(CompilerSources.Value, check);
        Assert.True(violation is null, violation);
    }

    /// <summary>The order pin, proven to fail on the shape the tree carried before kb/Work PB2495:
    /// <c>CheckPrototypeSignaturePairs</c> inside <c>BuildGroupRepository</c> (a helper written BELOW <c>Bind</c>, which a
    /// text-offset comparison reads as "after" <c>ResolvePointerRestrictions</c>) and an OO check written ahead of it are
    /// both caught; the repaired shape passes, and a check hidden in a loop or a lambda, or missing, is caught.</summary>
    [Fact]
    public void TheOrderPin_FiresOnThePB2495Defect()
    {
        const string defect = """
            internal sealed class BinderDriver
            {
                public BoundCompilation Bind()
                {
                    OoConformance.ValidateOverrideSignatures(table, edition);
                    foreach (var unit in units) BindUnitData(unit, session);
                    session.Repository = BuildGroupRepository(units, classes, session);
                    ResolvePointerRestrictions(units, classes, oo.InterfaceData, session);
                    return null;
                }
                private static GroupRepository BuildGroupRepository()
                {
                    CheckPrototypeSignaturePairs(units, session.Edition);
                    return null;
                }
            }
            """;
        SyntaxTree[] before = [CSharpSyntaxTree.ParseText(defect)];
        Assert.Contains("not a top-level statement", OrderViolation(before, "CheckPrototypeSignaturePairs"));
        Assert.Contains("BEFORE ResolvePointerRestrictions", OrderViolation(before, "ValidateOverrideSignatures"));

        const string repaired = """
            internal sealed class BinderDriver
            {
                public BoundCompilation Bind()
                {
                    session.Repository = BuildGroupRepository(units, classes, session);
                    ResolvePointerRestrictions(units, classes, oo.InterfaceData, session);
                    CheckPrototypeSignaturePairs(units, session.Edition);
                    var ooAdapters = OoConformance.ValidateImplements(table, edition);
                    foreach (var cls in classes) OoConformance.ValidateOverrideSignatures(table, edition);
                    Action later = () => OoConformance.ValidatePropertyAccessorPairs(table, edition);
                    return null;
                }
            }
            """;
        SyntaxTree[] after = [CSharpSyntaxTree.ParseText(repaired)];
        Assert.Null(OrderViolation(after, "CheckPrototypeSignaturePairs"));
        Assert.Null(OrderViolation(after, "ValidateImplements"));
        Assert.Contains("not a top-level statement", OrderViolation(after, "ValidateOverrideSignatures"));
        Assert.Contains("not a top-level statement", OrderViolation(after, "ValidatePropertyAccessorPairs"));
        Assert.Contains("invoked 0 times", OrderViolation(after, "ValidateInterfaceInheritance"));
    }

    private const string Resolver = "ResolvePointerRestrictions";

    private static readonly string[] PinnedMethods =
    [
        Resolver, "CheckPrototypeSignaturePairs", "ValidateInterfaceInheritance", "ValidateOverrideSignatures",
        "ValidatePropertyAccessorPairs", "ValidateImplements",
    ];

    /// <summary>The compiler's sources that name a pinned method, parsed once for every case.</summary>
    private static readonly Lazy<SyntaxTree[]> CompilerSources = new(() =>
        Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                        && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .Select(f => (Path: f, Text: File.ReadAllText(f)))
            .Where(f => PinnedMethods.Any(m => f.Text.Contains(m, StringComparison.Ordinal)))
            .Select(f => CSharpSyntaxTree.ParseText(f.Text, path: f.Path))
            .ToArray());

    /// <summary>Null when <paramref name="check"/> is invoked exactly once in <paramref name="trees"/>, as a top-level
    /// statement of <c>BinderDriver.Bind</c>'s body that follows the one top-level statement invoking
    /// <c>ResolvePointerRestrictions</c>; otherwise why not. The statements directly in one method body run in the order
    /// they are written, so for them, and only for them, text order IS execution order.</summary>
    private static string? OrderViolation(IReadOnlyList<SyntaxTree> trees, string check)
    {
        var bind = trees.SelectMany(t => t.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
            .SingleOrDefault(m => m.Identifier.Text == "Bind"
                                  && m.Parent is ClassDeclarationSyntax { Identifier.Text: "BinderDriver" });
        if (bind?.Body is not { } body) return "BinderDriver.Bind was not found — the pin measured nothing";

        string? Place(string method, out int index)
        {
            index = -1;
            var calls = trees.SelectMany(t => t.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
                .Where(i => CalledName(i) == method).ToList();
            if (calls.Count != 1)
                return $"'{method}' is invoked {calls.Count} times in the compiler; the pin needs it exactly once, in BinderDriver.Bind";
            var call = calls[0];
            var statement = call.Ancestors().OfType<StatementSyntax>().FirstOrDefault();
            if (statement?.Parent != body
                || call.Ancestors().TakeWhile(a => a != statement).Any(a => a is AnonymousFunctionExpressionSyntax))
            {
                string host = call.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault()?.Identifier.Text ?? "no method";
                return $"'{method}' is not a top-level statement of BinderDriver.Bind (it is in {host}, or under a loop, "
                       + "branch, local function or lambda there), so its place in the text says nothing about when it runs";
            }
            index = body.Statements.IndexOf(statement);
            return null;
        }

        if (Place(Resolver, out int resolved) is { } unresolved) return unresolved;
        if (Place(check, out int at) is { } unplaced) return unplaced;
        return at > resolved ? null
            : $"'{check}' runs BEFORE ResolvePointerRestrictions, so it compares restricted prototype-pointers by the prototype's "
              + "NAME (an unstamped pointer's identity) and refuses two differently named prototypes of one signature "
              + "(ISO §13.18.60.4 GR25 / GR26; kb/Work PB2495)";
    }

    private static string? CalledName(InvocationExpressionSyntax call) => call.Expression switch
    {
        IdentifierNameSyntax id => id.Identifier.Text,
        MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
        _ => null,
    };

    [Fact]
    public void ThePatternThisTestKeysOn_FiresOnThePB2464Defect()
    {
        // The exact line OoConformance.CategoryArmMismatch carried before the repair.
        Assert.Matches(NameComparison, "if (!CobolNames.Same(f.RestrictedPrototypeName, a.RestrictedPrototypeName))");
        Assert.Matches(NameComparison, "string.Equals(receiving.Pic.RestrictedPrototypeName, sending.Pic.RestrictedPrototypeName, StringComparison.Ordinal)");
        Assert.Matches(NameComparison, "if (a.Pic.RestrictedPrototypeName == b.Pic.RestrictedPrototypeName)");
        // The repaired line, and the readers that legitimately name the property, are not caught.
        Assert.DoesNotMatch(NameComparison,
            "if (!string.Equals(PrototypeSignatures.RestrictionIdentity(formal), PrototypeSignatures.RestrictionIdentity(arg), StringComparison.Ordinal))");
        Assert.DoesNotMatch(NameComparison, "&& !PrototypeSignatures.SameType(a, b)) return false;");
        Assert.DoesNotMatch(NameComparison,
            "? OwnIdentity(pointer.RestrictedPrototype?.ExternalizedName ?? name)");
        Assert.DoesNotMatch(NameComparison, "string? targetProto = receiver.Pic.RestrictedPrototypeName;");
        Assert.DoesNotMatch(NameComparison, "if (item.Pic is { RestrictedPrototypeName: { } name } pic)");
        Assert.DoesNotMatch(NameComparison, "!PrototypeSignatures.Same(host.ProgramSignatureOf(held), named.Signature)");
    }
}
