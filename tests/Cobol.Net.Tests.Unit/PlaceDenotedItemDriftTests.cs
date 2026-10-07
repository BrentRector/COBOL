// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CobolNet.Binding.Model;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY <c>Place</c> KIND HAS AN ADJUDICATED ANSWER TO "WHICH DATA ITEM DOES THIS REFERENCE DENOTE?"
/// (kb/Work PB602).
/// <para><see cref="Place.Item"/> answers what ISO §13.18.45.4 GR1 calls the data ATTRIBUTES and the STORAGE
/// AREA; <see cref="Place.DenotedItem"/> answers the other half of that same sentence — which data item the
/// reference IS. A screen that asks the first where the rule asks the second gets the wrong answer for every
/// decorated reference, which is how <c>START RLF KEY IS = WS-RK(1:2)</c> passed §14.9.41.3 SR5's "data-name-1
/// … shall be the data item specified in the RELATIVE KEY clause" and positioned a relative file on the number
/// the key's first two characters spell.</para>
/// <para>The defect's SHAPE is a silent inherit: a new <c>Place</c> kind gets the base's forwarding answer for
/// free and no test notices. So the roster is asserted here against the assembly — a kind must appear in
/// exactly one of the two tables below, and its membership must match whether it actually overrides. Adding a
/// row is an adjudication: name the rule that makes the reference denote its own item, or the reason the
/// reference still denotes the item underneath.</para>
/// </summary>
public sealed class PlaceDenotedItemDriftTests
{
    /// <summary>Kinds whose reference denotes the item the place already carries — the base answer, with the
    /// reason it is right for that kind.</summary>
    private static readonly Dictionary<string, string> Forwards = new(StringComparer.Ordinal)
    {
        ["MemberPlace"] = "A plain data reference: the name IS the item (ISO §8.4.2.1).",
        ["DynTablePlace"] = "A subscripted OCCURS DYNAMIC element — §13.18.38.4 makes a subscripted reference a "
            + "reference to ONE occurrence of the table element, which is the declared element item.",
        ["RedefViewPlace"] = "A REDEFINES view: §13.18.44.4 GR1 makes the redefining entry its own declared data "
            + "item over the redefined storage, and ViewItem IS that entry.",
        ["RenamesPlace"] = "The THROUGH form of a level-66 alias: §13.18.45.4 GR2 makes data-name-1 an "
            + "alphanumeric group item of its own, and AliasItem IS it — the identity never forwarded here, "
            + "which is why the THROUGH form was already refused by START's SR5 when the non-THROUGH form was not.",
        ["CapacityRegisterPlace"] = "The OCCURS DYNAMIC CAPACITY register (§13.18.38 GR15) — implicitly defined, "
            + "but a data item the source names, and RegisterItem is it.",
        ["ReportSumCounterPlace"] = "A REPORT SECTION sum counter (§13.18.54.4 GR1) — the same: implicitly "
            + "defined, named by the source, carried as RegisterItem.",
        ["ReportPageCounterPlace"] = "A report's PAGE-COUNTER (§8.4.3.15.4 GR1 — \"temporary unsigned integer "
            + "data items of class and category numeric, which are maintained for each report\") — implicitly "
            + "defined, named by the source, carried as RegisterItem, exactly like the sum counter beside it.",
        ["DebugRegisterPlace"] = "The X3.23-1985 DEBUG-ITEM register family — implicitly defined and named.",
        ["ExceptionObjectPlace"] = "EXCEPTION-OBJECT — §8.4.3.6.3 SR2 implicitly DESCRIBES the name, so the "
            + "reference denotes that one predefined object reference (§8.4.3.6.4 GR2).",
        ["OdoGroupPlace"] = "A decoration about the OPERAND's current extent (§13.18.38.4 GR8), not about which "
            + "item is referenced — the reference is still to the group.",
        ["TableAllPlace"] = "The table(ALL) intrinsic argument (§15.3) enumerates the occurrences of ONE declared "
            + "table element; the argument names that element.",
        ["NumericImagePlace"] = "An as-if character-image view of a numeric-display item (§8.4.3.3.4 GR6) — a "
            + "coding of the same item.",
        ["GroupImagePlace"] = "The whole-group image view — a coding of the same group item.",
        ["BitImagePlace"] = "The as-if-elementary boolean view of a bit group (§13.18.29.4 GR1 b) — same item.",
        ["NatImagePlace"] = "The as-if-elementary national view of a national group (§13.18.29.4 GR2 b) — same item.",
        ["NationalBytesPlace"] = "A national cell's storage bytes as a RENAMES THROUGH alias composes them "
            + "(§13.18.45.4 GR2; kb/Work PB2466) — a coding of the same item, never a reference of its own.",
    };

    /// <summary>Kinds that OVERRIDE the answer, with the rule that makes the reference denote something the
    /// place's own <see cref="Place.Item"/> is not.</summary>
    private static readonly Dictionary<string, string> Overrides = new(StringComparer.Ordinal)
    {
        ["RefModPlace"] = "ISO §8.4.3.3.4 GR5 — \"Reference modification creates a unique data item that is a "
            + "subset of the data item referenced by identifier-1\": a data item NO data description entry "
            + "declares, so the answer is null and every \"shall be the data item …\" rule refuses it.",
    };

    private static IEnumerable<Type> ConcretePlaceKinds() =>
        typeof(Place).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(Place).IsAssignableFrom(t));

    /// <summary>A kind the roster does not name has never been adjudicated — it silently inherited the
    /// forwarding answer, which is the defect this file exists to prevent.</summary>
    [Fact]
    public void EveryPlaceKindIsAdjudicated()
    {
        string[] unlisted =
        [
            .. ConcretePlaceKinds().Select(t => t.Name)
                .Where(n => !Forwards.ContainsKey(n) && !Overrides.ContainsKey(n))
                .OrderBy(n => n, StringComparer.Ordinal)
        ];
        Assert.True(unlisted.Length == 0,
            "These Place kinds have no adjudicated answer to \"which data item does this reference denote?\" "
            + "(kb/Work PB602). Add each to Forwards (with the reason the reference still denotes the item "
            + "underneath) or to Overrides (with the rule that makes it denote its own):\n  "
            + string.Join("\n  ", unlisted));
    }

    /// <summary>The roster may not disagree with the code: a kind listed as forwarding must not override, and a
    /// kind listed as overriding must. Either mismatch means the reason written down is not the behaviour.</summary>
    [Fact]
    public void TheRosterMatchesTheCode()
    {
        var mismatches = new List<string>();
        foreach (var t in ConcretePlaceKinds())
        {
            var prop = t.GetProperty(nameof(Place.DenotedItem),
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            bool overridden = prop?.GetMethod?.DeclaringType != typeof(Place);
            if (Forwards.ContainsKey(t.Name) && overridden)
                mismatches.Add($"{t.Name} is listed as FORWARDING but overrides DenotedItem");
            if (Overrides.ContainsKey(t.Name) && !overridden)
                mismatches.Add($"{t.Name} is listed as OVERRIDING but inherits Place.DenotedItem");
        }
        Assert.True(mismatches.Count == 0, string.Join("\n  ", mismatches));
    }

    /// <summary>The rows themselves may not rot: a name that is no longer a Place kind excuses nothing and
    /// hides the next one written there.</summary>
    [Fact]
    public void NoRosterRowNamesAMissingKind()
    {
        var live = ConcretePlaceKinds().Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
        string[] stale =
        [
            .. Forwards.Keys.Concat(Overrides.Keys).Where(n => !live.Contains(n))
                .OrderBy(n => n, StringComparer.Ordinal)
        ];
        Assert.True(stale.Length == 0,
            "These roster rows no longer name a Place kind — delete them:\n  " + string.Join("\n  ", stale));
    }

    /// <summary>⛔ The identity question is asked THROUGH <see cref="Place.DenotedItem"/>, not re-derived. A
    /// hand-written <c>is not RefModPlace</c> beside a <c>.Item</c> test is one caller's private copy of
    /// §8.4.3.3.4 GR5, and it is how PB128 had to route around the model instead of correcting it — twelve such
    /// copies were collapsed into this property, and a thirteenth would re-open the hole for the NEXT kind that
    /// denotes its own item (feedback_one_rule_one_place).</summary>
    [Fact]
    public void NoProductSourceReDerivesTheIdentityQuestion()
    {
        string[] offenders =
        [
            .. System.IO.Directory
                .EnumerateFiles(CobolNet.Tests.Shared.TestRepo.Src(), "*.cs", System.IO.SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{System.IO.Path.DirectorySeparatorChar}obj{System.IO.Path.DirectorySeparatorChar}")
                         && !p.Contains($"{System.IO.Path.DirectorySeparatorChar}bin{System.IO.Path.DirectorySeparatorChar}")
                         && !p.Contains($"{System.IO.Path.DirectorySeparatorChar}Generated{System.IO.Path.DirectorySeparatorChar}")
                         && !p.EndsWith("Place.cs", StringComparison.Ordinal))
                .SelectMany(p => System.IO.File.ReadAllLines(p)
                    .Select((line, i) => (line, n: i + 1))
                    .Where(x => !x.line.TrimStart().StartsWith("//", StringComparison.Ordinal)
                                && !x.line.TrimStart().StartsWith("///", StringComparison.Ordinal)
                                && (x.line.Contains("is not RefModPlace", StringComparison.Ordinal)
                                    || x.line.Contains("Place: not RefModPlace", StringComparison.Ordinal)))
                    .Select(x => $"{System.IO.Path.GetFileName(p)}:{x.n}"))
                .OrderBy(s => s, StringComparer.Ordinal)
        ];
        Assert.True(offenders.Length == 0,
            "A hand-written \"is this reference a whole item\" test re-derives ISO §8.4.3.3.4 GR5 instead of "
            + "asking Place.DenotedItem (kb/Work PB602):\n  " + string.Join("\n  ", offenders));
    }
}
