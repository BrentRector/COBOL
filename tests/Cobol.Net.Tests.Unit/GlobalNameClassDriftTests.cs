// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY NAME CLASS A DATA DESCRIPTION ENTRY DECLARES REACHES A CONTAINED PROGRAM (kb/Work PB1674, PB1243).
/// <para>ISO §8.4.6.2.2 — "All data-names and screen-names subordinate to a global name are global names … All
/// condition-names associated with a global name are global names" — and §8.4.6.2.3 for index-names: a GLOBAL record
/// hands every name it declares to the programs it contains. <c>DataBinder.InheritGlobalSubtree</c> is the ONE place
/// that inheritance is written, and it once carried data-names, condition-names, RENAMES and index-names but not the
/// OCCURS DYNAMIC CAPACITY register (§13.18.38.3 SR30 defines it at the OCCURS entry's level, so it is subordinate to
/// the record): <c>DISPLAY D-CAP</c> in a contained program was COBOLNET1639 "is not defined" — legal source rejected.
/// The register lives in its own name index because it is a view, not storage, and nothing tied that index to the
/// inheritance list.</para>
/// <para>This test ties them. Every name index of <see cref="DataBinder"/> — a field mapping a name to a data-division
/// declaration (a <see cref="DataItem"/>, a <see cref="Condition88"/>, an <see cref="IndexDeclaration"/>) — is found
/// by REFLECTION, so a name index added tomorrow is discovered without anyone remembering this list, and it must
/// either be reached from <c>InheritGlobalSubtree</c> or be excluded below with the reason it is not a per-entry name
/// a GLOBAL record hands down. The behavioural half is
/// <c>conformance-suite/cases/_unassigned/pb1674_global_capacity_register.cob</c>.</para>
/// </summary>
public sealed class GlobalNameClassDriftTests
{
    /// <summary>Name index → the text <c>InheritGlobalSubtree</c> reaches it by.</summary>
    private static readonly Dictionary<string, string> Inherited = new()
    {
        ["ByName"] = "ByName",                                  // data-names and RENAMES (§8.4.6.2.2)
        ["Conditions"] = "Conditions",                          // condition-names (§8.4.6.2.2)
        ["IndexNames"] = "IndexNames.Inherit(",                 // index-names (§8.4.6.2.3)
        ["_capacityRegisters"] = "AddCapacityRegister(",        // CAPACITY registers (§13.18.38.3 SR30, PB1674)
    };

    /// <summary>Name indexes that are NOT names a GLOBAL data description entry hands to a contained program.</summary>
    private static readonly Dictionary<string, string> NotPerEntryGlobalNames = new()
    {
        ["TypeDecls"] = "a type-name is inherited BEFORE Bind by InheritGlobalTypeDecls (§13.18.58.4 GR3; kb/Work PB1303), "
            + "and a template's subordinate names are kept off the name index",
        ["_inheritedTypeDecls"] = "the inherited half of the type-name index itself (InheritGlobalTypeDecls)",
        ["_lapsedTypeMembers"] = "the members of THIS element's referenced type declarations, which COUNT for §8.4.2.2.1 "
            + "rule 4 and never resolve (kb/Work PB1476) — a derived view of TypeDecls, declaring no name of its own",
        ["_debugRegisters"] = "the X3.23-1985 DEBUG-ITEM special register of the element's own debugging declaratives, "
            + "never declared by a data description entry",
        ["_constants"] = "a constant entry occupies no storage, so a GLOBAL constant is inherited BEFORE Bind by "
            + "InheritGlobalConstants (§13.18.27.4 GR1-GR2; kb/Work PB1009), not bridged by InheritGlobalSubtree",
    };

    private static IEnumerable<string> NameIndexFields(Type owner)
    {
        foreach (var f in owner.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            // A registry type that is itself a name index: the index-names, and an element's constant entries (a
            // constant-name is a user-defined word its element declares, so it hides a same-spelled word of a
            // containing element; kb/Work PB3164, PB3153 review round 3).
            if (f.FieldType == typeof(IndexNameRegistry) || f.FieldType == typeof(DataBinder.ConstantScope))
            {
                yield return MemberName(f);
                continue;
            }
            if (!f.FieldType.IsGenericType) continue;
            var args = f.FieldType.GetGenericArguments();
            if (f.FieldType.GetInterfaces().Any(i => i.IsGenericType
                    && i.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                && args[0] == typeof(string) && Mentions(args[1]))
                yield return MemberName(f);
        }
    }

    /// <summary>The source name of a field — an auto-property's backing field reports the property.</summary>
    private static string MemberName(FieldInfo f) =>
        Regex.Match(f.Name, @"^<(?<p>\w+)>k__BackingField$") is { Success: true } m ? m.Groups["p"].Value : f.Name;

    private static bool Mentions(Type t) =>
        t == typeof(DataItem) || t == typeof(Condition88) || t == typeof(IndexDeclaration)
        || (t.IsGenericType && t.GetGenericArguments().Any(Mentions));

    [Fact]
    public void EveryNameIndex_IsInheritedOrExcludedWithItsReason()
    {
        var found = NameIndexFields(typeof(DataBinder)).ToHashSet(StringComparer.Ordinal);
        var unclassified = found.Where(n => !Inherited.ContainsKey(n) && !NotPerEntryGlobalNames.ContainsKey(n)).ToList();
        Assert.True(unclassified.Count == 0,
            "DataBinder gained a name index that neither InheritGlobalSubtree reaches nor this test excludes — decide "
            + "whether a GLOBAL record hands that name class to a contained program (ISO §8.4.6.2.2):\n  "
            + string.Join("\n  ", unclassified));
        var stale = Inherited.Keys.Concat(NotPerEntryGlobalNames.Keys).Where(n => !found.Contains(n)).ToList();
        Assert.True(stale.Count == 0, "Rows name a name index DataBinder no longer has:\n  " + string.Join("\n  ", stale));
    }

    [Fact]
    public void InheritGlobalSubtree_ReachesEveryInheritedNameIndex()
    {
        string text = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "DataBinder.Linkage.cs"));
        var m = Regex.Match(text, @"internal void InheritGlobalSubtree\(DataItem item, int depth\)\s*\{(?<body>.*?)\n    \}",
            RegexOptions.Singleline);
        Assert.True(m.Success, "InheritGlobalSubtree not found in DataBinder.Linkage.cs");
        foreach (var (index, reach) in Inherited)
            Assert.True(m.Groups["body"].Value.Contains(reach, StringComparison.Ordinal),
                $"InheritGlobalSubtree no longer reaches the name index '{index}' (expected '{reach}').");
    }

    /// <summary>Inherited name index → the text <c>DataBinder.DeclaredNearerThan</c> asks it by.</summary>
    private static readonly Dictionary<string, string> AskedByShadowing = new()
    {
        ["ByName"] = "ByName.TryGetValue(",
        ["Conditions"] = "Conditions.TryGetValue(",
        ["IndexNames"] = "IndexNames.Declares(",
        ["_capacityRegisters"] = "_capacityRegisters.TryGetValue(",
    };

    /// <summary>⛔ THE SAME LIST DECIDES SHADOWING (kb/Work PB3154 review R2-1). An inherited constant-name or
    /// symbolic-character is hidden by a NEARER declaration of its spelling in ANY name class (ISO §8.4.6.1 — "even
    /// when it is a different type of user-defined word, is to the item which that source element describes"), and
    /// <c>DeclaredNearerThan</c> is the one predicate that asks. It asked <c>ByName</c>, <c>Conditions</c> and
    /// <c>IndexNames</c> but not the CAPACITY register index, so a contained program's register spelled like its
    /// container's symbolic-character read the figurative. Every name index a contained program inherits is a name
    /// class a nearer element can declare, so the predicate must ask each one.</summary>
    [Fact]
    public void DeclaredNearerThan_AsksEveryInheritedNameIndex()
    {
        Assert.Equal(Inherited.Keys.Order(StringComparer.Ordinal), AskedByShadowing.Keys.Order(StringComparer.Ordinal));
        string text = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "DataBinder.Constants.cs"));
        var m = Regex.Match(text, @"internal bool DeclaredNearerThan\(string name, int depth\)\s*\{(?<body>.*?)\n    \}",
            RegexOptions.Singleline);
        Assert.True(m.Success, "DeclaredNearerThan not found in DataBinder.Constants.cs");
        foreach (var (index, ask) in AskedByShadowing)
            Assert.True(m.Groups["body"].Value.Contains(ask, StringComparison.Ordinal),
                $"DeclaredNearerThan no longer asks the name index '{index}' (expected '{ask}').");
        // The REPORT SECTION sum counters are a name index too, kept off ByName, and they hold a container's GLOBAL
        // report's counters beside this element's own: asked by spelling alone, a FARTHER container's counter hid a
        // NEARER container's constant (kb/Work PB3154 review R3-1). The ask places each counter by its report.
        Assert.True(m.Groups["body"].Value.Contains("ReportDepth(c.Report) < depth", StringComparison.Ordinal),
            "DeclaredNearerThan no longer places a sum counter by its report's depth (ReportDepth).");
    }

    /// <summary>A METHOD's name index → the text <c>OoMethodDataScope.Declares</c> asks it by.</summary>
    private static readonly Dictionary<string, string> AskedByMethodShadowing = new()
    {
        ["ByName"] = "ByName.ContainsKey(",
        ["Conditions"] = "Conditions.ContainsKey(",
        ["IndexNames"] = "IndexNames.Declares(",
        ["CapacityRegisters"] = "CapacityRegisters.ContainsKey(",
        ["Constants"] = "Constants.Declared.ContainsKey(",
    };

    /// <summary>⛔ THE METHOD TIER ASKS EVERY NAME INDEX OF THE METHOD (the kb/Work PB3165 merge-queue bounce). A method
    /// is a source element of its own (§10.5), its names the NEAREST tier a reference in it can reach, so a method's
    /// declaration of a spelling in ANY name class hides the class's constant-name or symbolic-character of it
    /// (§8.4.6.1, §8.4.6.2.1 3) a)). When PB3165 moved a method's CAPACITY registers out of the binder's index into
    /// <see cref="OoMethodDataScope.CapacityRegisters"/>, the binder's hand-written method predicate kept asking the
    /// old index, and <c>conformance-suite/cases/_unassigned/pb3154_half_capacity_register_hides_symbolic.cob</c> displayed the
    /// figurative <c>CCC</c> for the method's own register. The scope's name indexes are found by REFLECTION, so an
    /// index added to the scope must be asked by <c>OoMethodDataScope.Declares</c> too.</summary>
    [Fact]
    public void MethodDeclares_AsksEveryMethodNameIndex()
    {
        Assert.Equal(AskedByMethodShadowing.Keys.Order(StringComparer.Ordinal),
            NameIndexFields(typeof(OoMethodDataScope)).Order(StringComparer.Ordinal));
        string text = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "DataBinder.Oo.cs"));
        var m = Regex.Match(text, @"internal bool Declares\(string word\)\s*\{(?<body>.*?)\n    \}", RegexOptions.Singleline);
        Assert.True(m.Success, "OoMethodDataScope.Declares not found in DataBinder.Oo.cs");
        foreach (var (index, ask) in AskedByMethodShadowing)
            Assert.True(m.Groups["body"].Value.Contains(ask, StringComparison.Ordinal),
                $"OoMethodDataScope.Declares no longer asks the method's name index '{index}' (expected '{ask}').");
    }

    /// <summary>⛔ EVERY SYMBOL-TABLE LOOKUP WITH A METHOD TIER ASKS THE METHOD'S WHOLE DECLARATION (kb/Work PB3165 branch
    /// review R2-1). ISO §11.7.4 GR5 is about the WORD: a method's declaration of a spelling in any name class makes
    /// the object's declaration of it, of any class, "inaccessible to this method" (§8.4.6.1 "even when it is a
    /// different type of user-defined word"). <c>CapacityCandidates</c> asked <see cref="OoMethodDataScope.Declares"/>,
    /// but <c>IndexCandidates</c>, <c>TryResolve</c> and <c>TryResolveCondition</c> each asked only their own class's
    /// method index and then fell through to the object's: a method CAPACITY register spelled like an object
    /// index-name was refused as an index in COMPUTE, and a method condition-name let <c>MOVE X</c> read the object's
    /// data-name <c>X</c>. Every <see cref="SymbolTable"/> member whose body opens a method tier
    /// (<c>scope.Method is { } m</c>) must ask <c>m.Declares(name)</c> before it reaches the unit's maps. The
    /// behavioural half is <c>conformance-suite/cases/_unassigned/pb3165_method_capacity_register_scope.cob</c> (SHOWF) and the
    /// three <c>negative/pb3165-method-*-hides-object-*</c> cases.</summary>
    [Fact]
    public void EverySymbolTableMethodTier_AsksTheMethodsWholeDeclaration()
    {
        string text = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "Model", "SymbolTable.cs"));
        var members = Regex.Matches(text, @"^    public [^\r\n]*?\b(?<name>\w+)(?:<\w+>)?\((?<rest>.*?)(?=^    public |\z)",
            RegexOptions.Multiline | RegexOptions.Singleline);
        var tiered = members.Where(m => m.Groups["rest"].Value.Contains("scope.Method is { } m", StringComparison.Ordinal))
            .ToList();
        Assert.True(tiered.Count >= 4, $"expected the four method-tiered lookups in SymbolTable.cs, found {tiered.Count}");
        foreach (var m in tiered)
            Assert.True(Regex.IsMatch(m.Groups["rest"].Value, @"\bm\.Declares\(name\)"),
                $"SymbolTable.{m.Groups["name"].Value} opens a method tier but never asks m.Declares(name): a method-local "
                + "name of the spelling in another name class would not hide the object's (ISO §11.7.4 GR5).");
    }
}
