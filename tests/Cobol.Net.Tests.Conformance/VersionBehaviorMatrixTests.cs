// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// INV-3 — the behavior-variant matrix (rearch P3 step 7; VERSION_TEST_MATRIX_DESIGN §2.10), the weakest leg of
/// "four compilers in one": does the SAME source produce different OUTPUT across <c>--std</c> editions? Stood up as
/// a LOUD DISCOVERY TOOL (the investigated de-sign/DISPLAY diffs were all version-INVARIANT, DEVLOG 517; WiseOwl COBOL's
/// one scaled-integer numeric pipeline makes most RUN-TIME arithmetic edition-invariant). Its first CONFIRMED variant
/// is compile-time arithmetic (<c>arithmetic-intermediate-precision-2023</c>, kb/Work PB1592): standard arithmetic
/// at 2002/2014, the documented System.Decimal mode at 2023. Uses the SAME <c>[Theory][MemberData]</c> mechanism + the SAME <c>constructs.json</c>
/// catalogue as <see cref="VersionMatrixTests"/> (a `variant` block per candidate row) — NOT a new mechanism.
/// A candidate is `confirmed:false` (pending) until an investigation confirms its edition-dependence and populates
/// per-edition <c>outputs</c>; then it becomes an asserted per-edition golden. Pending candidates still RUN at every
/// edition (the case must compile+run) but their output is catalogued, not asserted.
/// </summary>
public sealed class VersionBehaviorMatrixTests
{
    private sealed record VariantRow(
        string Id, int IntroducedIn, int? RemovedIn, string Source, bool Confirmed, string Confirm,
        IReadOnlyDictionary<string, string> Outputs);

    private static readonly IReadOnlyList<VariantRow> Variants = Load();

    private static IReadOnlyList<VariantRow> Load()
    {
        string path = TestRepo.VersionMatrix("constructs.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var list = new List<VariantRow>();
        foreach (var e in doc.RootElement.GetProperty("constructs").EnumerateArray())
        {
            if (!e.TryGetProperty("variant", out var v)) continue;
            var outputs = new Dictionary<string, string>();
            if (v.TryGetProperty("outputs", out var o) && o.ValueKind == JsonValueKind.Object)
                foreach (var kv in o.EnumerateObject())
                    outputs[kv.Name] = kv.Value.GetString()!;
            list.Add(new VariantRow(
                e.GetProperty("id").GetString()!,
                e.GetProperty("introducedIn").GetInt32(),
                e.TryGetProperty("removedIn", out var r) && r.ValueKind != JsonValueKind.Null ? r.GetInt32() : null,
                e.GetProperty("source").GetString()!,
                v.TryGetProperty("confirmed", out var c) && c.GetBoolean(),
                v.TryGetProperty("confirm", out var cf) ? cf.GetString()! : "",
                outputs));
        }
        return list;
    }

    /// <summary>Every variant-bearing row × the editions in its availability window.</summary>
    public static IEnumerable<object[]> Cells() =>
        from v in Variants
        from edition in new[] { 85, 2002, 2014, 2023 }
        where edition >= v.IntroducedIn && (v.RemovedIn is null || edition < v.RemovedIn)
        select new object[] { v.Id, edition };

    [Theory]
    [MemberData(nameof(Cells))]
    public void Construct_BehaviorMatchesPerEdition(string id, int edition)
    {
        var v = Variants.First(x => x.Id == id);
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(v.Source, edition, permissive: true);
        Assert.True(ok, $"[behavior-variant {id}] case failed to compile+run at COBOL-{edition}: {detail}");

        if (v.Confirmed)
        {
            // A CONFIRMED edition-dependent behavior: assert the per-edition golden output.
            Assert.True(v.Outputs.TryGetValue(edition.ToString(), out var expected),
                $"[behavior-variant {id}] confirmed but missing outputs[{edition}]");
            Assert.Equal(Normalize(expected), Normalize(stdout));
        }
        else
        {
            // A PENDING INV-3 discovery candidate: the case runs at every edition, but its output's
            // edition-dependence is UNCONFIRMED — catalogued LOUD (the confirm note), never silently absent.
            Assert.False(string.IsNullOrWhiteSpace(v.Confirm),
                $"[behavior-variant {id}] pending candidate must carry a 'confirm' note (a loud catalogue entry, "
                + "not a silent gap)");
        }
    }

    /// <summary>The matrix is a DISCOVERY tool: it is well-formed with pending candidates and confirmed variants
    /// alike, and every CONFIRMED variant carries its per-edition goldens — an empty/absent surface would hide
    /// whether any edition-variant behavior exists.</summary>
    [Fact]
    public void BehaviorVariantCatalogue_IsWellFormed()
    {
        Assert.NotEmpty(Variants);   // at least the seeded candidate(s) — the surface exists
        foreach (var v in Variants.Where(v => v.Confirmed))
            Assert.NotEmpty(v.Outputs);   // a confirmed variant must carry per-edition goldens
    }

    private static string Normalize(string s) => s.ReplaceLineEndings("\n").TrimEnd('\n');
}
