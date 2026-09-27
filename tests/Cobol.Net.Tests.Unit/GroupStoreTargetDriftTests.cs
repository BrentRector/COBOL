// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1411 — EVERY GROUP-LEVEL STORE IS CALLED ON <c>PlaceRenderer.GroupTarget</c>, the one receiver that
/// reaches an element of a dynamic-capacity table through <c>RefReceiving</c>. ISO §8.5.1.9.3: a receiving
/// reference past the current capacity creates the element and raises the capacity; the sending accessor
/// <c>RefSending</c> hands back benign scratch instead, so a store through it is silently discarded. Only
/// <c>WriteGroupImage</c> used to carry the dynamic-table arm; the variable-length group writer
/// (<c>FromVarImage</c>), the contiguous-record writer and the bit / national writers each spelled their target as
/// <c>Read(group)</c> — the two-arm dispatch with one arm fixed. This pins the structure: a generated group store
/// (<c>.FromImage(</c> / <c>.FromVarImage(</c> / <c>.FromContiguousImage(</c> / <c>.FromBits(</c> /
/// <c>.FromNat(</c>) rendered by <c>PlaceRenderer</c> whose receiver is not <c>GroupTarget(…)</c> is a new copy
/// of the defect.
/// </summary>
public sealed class GroupStoreTargetDriftTests
{
    private static readonly Regex Store = new(@"\}\.From(Image|VarImage|ContiguousImage|Bits|Nat)\(");

    [Fact]
    public void EveryPlaceRendererGroupStore_ReceivesThroughGroupTarget()
    {
        string[] lines = File.ReadAllLines(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Roslyn", "PlaceRenderer.cs"));
        int stores = 0;
        var offenders = new List<string>();
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.TrimStart().StartsWith("//")) continue;
            foreach (Match m in Store.Matches(line))
            {
                stores++;
                // The receiver is the interpolation hole the store is called on: `{GroupTarget(x)}.FromY(`.
                int open = line.LastIndexOf('{', m.Index);
                string receiver = open < 0 ? "" : line[(open + 1)..m.Index];
                if (!receiver.StartsWith("GroupTarget(", StringComparison.Ordinal))
                    offenders.Add($"PlaceRenderer.cs:{i + 1}: {line.Trim()}");
            }
        }
        // The scan must be able to fail: if the writers stop matching the pattern, it would pass vacuously.
        Assert.True(stores >= 7, $"expected the group writers' stores in PlaceRenderer.cs, found {stores}");
        Assert.True(offenders.Count == 0,
            "A group-level store is called on a receiver other than PlaceRenderer.GroupTarget — an element of a "
            + "dynamic-capacity table would be reached through RefSending and the store discarded "
            + "(ISO §8.5.1.9.3; kb/Work PB1411):\n" + string.Join("\n", offenders));
    }
}
