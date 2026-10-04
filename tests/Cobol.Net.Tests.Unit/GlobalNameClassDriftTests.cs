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
/// <c>tests/conformance/2014/pb1674_global_capacity_register.cob</c>.</para>
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
        ["_debugRegisters"] = "the X3.23-1985 DEBUG-ITEM special register of the element's own debugging declaratives, "
            + "never declared by a data description entry",
    };

    private static IEnumerable<string> NameIndexFields()
    {
        foreach (var f in typeof(DataBinder).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (f.FieldType == typeof(IndexNameRegistry)) { yield return MemberName(f); continue; }
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
        var found = NameIndexFields().ToHashSet(StringComparer.Ordinal);
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
        string text = File.ReadAllText(Path.Combine(TestRepo.Root, "src", "Cobol.Net.Compiler", "Binding",
            "DataBinder.Linkage.cs"));
        var m = Regex.Match(text, @"internal void InheritGlobalSubtree\(DataItem item, int depth\)\s*\{(?<body>.*?)\n    \}",
            RegexOptions.Singleline);
        Assert.True(m.Success, "InheritGlobalSubtree not found in DataBinder.Linkage.cs");
        foreach (var (index, reach) in Inherited)
            Assert.True(m.Groups["body"].Value.Contains(reach, StringComparison.Ordinal),
                $"InheritGlobalSubtree no longer reaches the name index '{index}' (expected '{reach}').");
    }
}
