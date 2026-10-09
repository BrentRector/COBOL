// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.Linq;
using CobolNet.Binding.Model;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A <c>Place</c> THAT IS A VIEW OVER ENGINE STATE SAYS SO, AND NO IMAGE-BACKING FACT IS EVER RECORDED FOR IT
/// (kb/Work PB1943).
/// <para>The image-backing facts (<c>DataBinder.MarkImageForced</c>, read mid-bind by <c>IsImageBackedEarly</c>) are
/// keyed by <see cref="DataItem"/> and say "this item's cell stores its character image". The implicitly-defined
/// registers — a report's sum counter, PAGE-COUNTER, the CAPACITY register, DEBUG-ITEM, EXCEPTION-OBJECT — carry a
/// synthetic <c>RegisterItem</c> whose value the engine owns and which has no cell, so a fact recorded for it describes
/// storage that does not exist; once the sum counter's register became a USAGE DISPLAY item the receiving
/// reference-modification store recorded one, and every LATER <c>CF-T (3:)</c> skipped the numeric-image wrap and
/// emitted <c>CobolString.RefMod(long, …)</c>. <see cref="Place.OwnsStorageCell"/> is the ONE answer that gates the three
/// readers and writers of those facts (<c>ReferenceResolver.RefModView</c>, <c>MoveBinder.MarkRefModStoreImage</c>,
/// <c>MoveBinder.MarkFillImageStorage</c>).</para>
/// <para>The defect's SHAPE is a silent inherit: a new register-shaped <c>Place</c> kind gets the base's
/// <c>true</c> for free and no test notices. So the roster is asserted against the assembly: a kind that carries a
/// <c>RegisterItem</c> must be in the table below and must override the answer to <c>false</c>, and no other kind may.</para>
/// </summary>
public sealed class PlaceStorageCellDriftTests
{
    /// <summary>The views over engine state, with what the engine owns for each.</summary>
    private static readonly Dictionary<string, string> Views = new(StringComparer.Ordinal)
    {
        ["ReportSumCounterPlace"] = "The report engine's sum counter (ISO §13.18.54.4 GR1), read and written as its character image by SumImage / SetSumImage.",
        ["ReportPageCounterPlace"] = "The report engine's PAGE-COUNTER (ISO §8.4.3.15.4 GR1).",
        ["CapacityRegisterPlace"] = "The OCCURS DYNAMIC table's current capacity (ISO §13.18.38 GR15).",
        ["DebugRegisterPlace"] = "The X3.23-1985 DEBUG-ITEM register family, populated by the debug trigger.",
        ["ExceptionObjectPlace"] = "The run unit's one EXCEPTION-OBJECT (ISO §8.4.3.6.4 GR2).",
    };

    private static IEnumerable<Type> ConcretePlaceKinds() =>
        typeof(Place).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(Place).IsAssignableFrom(t));

    /// <summary>Every kind that carries an implicitly-defined register item is a view, and says so.</summary>
    [Fact]
    public void EveryRegisterShapedPlaceIsAView()
    {
        var registerShaped = ConcretePlaceKinds()
            .Where(t => t.GetProperty("RegisterItem") is not null)
            .Select(t => t.Name)
            .ToHashSet(StringComparer.Ordinal);
        var missing = registerShaped.Except(Views.Keys).OrderBy(n => n, StringComparer.Ordinal).ToList();
        Assert.True(missing.Count == 0,
            "These Place kinds carry a RegisterItem but are not named in PlaceStorageCellDriftTests.Views, so they "
            + "silently inherit OwnsStorageCell = true: " + string.Join(", ", missing));
    }

    /// <summary>The roster matches the overrides exactly: a view overrides <see cref="Place.OwnsStorageCell"/>, and nothing
    /// else does.</summary>
    [Fact]
    public void TheViewsAreExactlyTheKindsThatOverrideOwnsStorageCell()
    {
        var overriding = ConcretePlaceKinds()
            .Where(t => t.GetProperty(nameof(Place.OwnsStorageCell))!.DeclaringType == t)
            .Select(t => t.Name)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Equal(Views.Keys.OrderBy(n => n, StringComparer.Ordinal), overriding.OrderBy(n => n, StringComparer.Ordinal));
    }

    /// <summary>A decorator forwards the answer: a reference modification over a counter owns no cell either.</summary>
    [Fact]
    public void DecoratorsForwardTheAnswer()
    {
        var item = new DataItem { Level = 49, CobolName = "CTR", CsName = "CTR" };
        var counter = new ReportSumCounterPlace(0, 0, item);
        Assert.False(counter.OwnsStorageCell);
        Assert.False(new NumericImagePlace(counter).OwnsStorageCell);
        Assert.False(new RefModPlace(new NumericImagePlace(counter), new PositionConstant(1), new PositionConstant(2)).OwnsStorageCell);
    }
}
