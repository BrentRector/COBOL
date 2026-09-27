// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.CompilerServices;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>The dynamic-capacity table runtime (ISO §8.5.1.9; <c>CobolDynTable&lt;T&gt;</c>) at the seams the wave-67
/// group O notes measured wrong: the §14.6.9.2 recreation (kb/Work PB1144), the §8.5.1.9.5 INITIALIZED seed of a
/// created element (PB1267) and the §8.5.1.9.6 2) resource-bounded maximum capacity (PB1410). Checking is off in
/// these tests, so the nonfatal and fatal conditions take their "no exception" outcomes; the goldens under
/// <c>tests/conformance/2014/pb1*_dyn_*</c> cover them with checking on.</summary>
public sealed class CobolDynTableTests
{
    /// <summary>16 KiB per element, so MaxOccurrences elements are ~16 TiB — more than any host's memory plus swap,
    /// so the allocation is refused outright (the same technique as
    /// <c>CobolDynStringTests.SetSize_StorageNotAvailable_LeavesTheSizeUnchanged</c>), never a slow partial commit.
    /// Kept well below the CLR's limit on an array element's size (a 1 MiB element is a TypeLoadException, not an
    /// allocation failure — measured) and small enough that a seed returning it BY VALUE is safe on a test
    /// thread's stack.</summary>
    [InlineArray(2048)]
    private struct Chunk { private long _e; }

    private static CobolDynTable<string> Strings(int min, Func<string>? created = null) =>
        new(() => "SEED", min, null, created);

    /// <summary>§8.5.1.9.6 2): "The fatal EC-BOUND-TABLE-LIMIT exception condition shall exist when an operation
    /// attempts to increase the capacity of a dynamic-capacity table to a value higher than the maximum capacity of
    /// the table based on the resources available at runtime" — with checking off the explicit SET leaves the
    /// capacity unchanged (§14.9.39.4 GR30) instead of an OutOfMemoryException escaping (kb/Work PB1410).</summary>
    [Fact]
    public void SetCapacity_BeyondRuntimeResources_LeavesTheCapacityUnchanged()
    {
        var t = new CobolDynTable<Chunk>(() => default, 0, null, null);
        t.SetCapacity(CobolDynTable<Chunk>.MaxOccurrences);
        Assert.Equal(0, t.Capacity);
    }

    /// <summary>The IMPLICIT path shares the one growth primitive: a receiving reference the runtime cannot hold
    /// hands back the benign scratch slot with the capacity unchanged (kb/Work PB1410).</summary>
    [Fact]
    public void RefReceiving_BeyondRuntimeResources_LeavesTheCapacityUnchanged()
    {
        var t = new CobolDynTable<Chunk>(() => default, 0, null, null);
        t.RefReceiving(CobolDynTable<Chunk>.MaxOccurrences)[0] = 7;
        Assert.Equal(0, t.Capacity);
    }

    /// <summary>§14.6.9.2: "further elements are created and filled with spaces until the current capacity of the
    /// table is equal to its minimum capacity" — the receiver's stale occurrences are freed, never kept by a
    /// minimum clamp (kb/Work PB1144).</summary>
    [Fact]
    public void FromCurrentImage_ShorterThanTheMinimum_SpaceFillsUpToTheMinimum()
    {
        var t = Strings(min: 3);
        for (int i = 1; i <= 4; i++) t.RefReceiving(i) = $"OL{i}";
        t.FromCurrentImage("AAA", 3, (_, x) => x);
        Assert.Equal(3, t.Capacity);
        Assert.Equal(["AAA", "   ", "   "], [t.RefSending(1), t.RefSending(2), t.RefSending(3)]);
    }

    /// <summary>The recreation takes the SENDER's count above the minimum, and a later shorter sender lowers it
    /// again — the table is a copy of the sender, not a SET (kb/Work PB1144).</summary>
    [Fact]
    public void FromCurrentImage_TakesTheSendersCapacity()
    {
        var t = Strings(min: 1);
        t.FromCurrentImage("AAABBBCCC", 3, (_, x) => x);
        Assert.Equal(3, t.Capacity);
        t.FromCurrentImage("DDD", 3, (_, x) => x);
        Assert.Equal(1, t.Capacity);
        Assert.Equal("DDD", t.RefSending(1));
    }

    /// <summary>§8.5.1.9.5 (kb/Work PB1267): the occurrences the table OPENS with take the initial-state seed; an
    /// occurrence a statement CREATES — a receiving reference or a SET — takes the INITIALIZED seed.</summary>
    [Fact]
    public void CreatedOccurrences_TakeTheInitializedSeed_OpeningOccurrencesTheInitialState()
    {
        var t = Strings(min: 1, created: () => "INIT");
        Assert.Equal("SEED", t.RefSending(1));
        t.RefReceiving(3) = "X";
        Assert.Equal(["INIT", "X"], [t.RefSending(2), t.RefSending(3)]);
        t.CapacityUpBy(1);
        Assert.Equal("INIT", t.RefSending(4));
    }

    /// <summary>Without INITIALIZED (a null created seed) every occurrence takes the one seed.</summary>
    [Fact]
    public void WithoutInitialized_CreatedOccurrencesTakeTheOneSeed()
    {
        var t = Strings(min: 0);
        t.RefReceiving(2) = "X";
        Assert.Equal("SEED", t.RefSending(1));
    }
}
