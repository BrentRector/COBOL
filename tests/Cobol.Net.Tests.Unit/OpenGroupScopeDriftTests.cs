// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ISO §14.9.27.2's REPEATED GROUP kept structural (kb/Work PB316). The OPEN statement's general format has an
/// outer brace pair — verified against the printed page — around the whole group
/// <c>{open-mode} [sharing-phrase] [retry-phrase] {file-name-1 [WITH NO REWIND]} …</c>, carrying its own
/// trailing ellipsis; §14.9.27.4 GR20 then makes the statement equal to "a separate OPEN statement … for each
/// file-name … each hav[ing] the same open mode specification, the sharing-phrase, retry-phrase, and REWIND
/// phrase as specified in the OPEN statement". The open mode in that list is unarguably per group, so the
/// phrases beside it are too.
///
/// <para>⛔ WHAT THIS GATE EXISTS TO CATCH, AND WHY A CONFORMANCE GOLDEN CANNOT. The defect was a SCALAR
/// standing in for a per-group value: <c>BoundOpen</c> carried <c>SharingOverride</c> and <c>Retry</c> as
/// statement properties, so one group's phrase governed every file — and the next phrase added to the format
/// (the NO REWIND phrase, kb/Work PB317; the per-file-name syntax rules, kb/Work PB318) would have been added
/// the same way, because that was the shape on offer. <c>conformance:2002/pb316_open_group_scope</c> proves the
/// two phrases that exist TODAY are scoped correctly; only a check on the SHAPE makes the next one automatic.
/// The rule enforced here is exact: the statement node carries nothing but its file list, and every per-group
/// value lives on <see cref="BoundOpenFile"/>.</para>
///
/// <para>⛔ <see cref="ThePredicate_ActuallyFails_OnAStatementScopedPhrase"/> is not a formality
/// (<c>feedback_green_gates_arent_evidence</c>): this gate will spend its life green, and a green gate that
/// never looked at anything is indistinguishable from one that works. It drives the same pure function over a
/// fabricated node shaped like the pre-PB316 <c>BoundOpen</c>, and over one shaped like today's, so the failure
/// proves discrimination rather than blanket rejection.</para>
/// </summary>
public sealed class OpenGroupScopeDriftTests
{
    /// <summary>THE PREDICATE. The public instance properties of a statement node other than its repeated-group
    /// list — every one of them is a value scoped to the whole statement, which for OPEN is exactly what
    /// §14.9.27.2 forbids.</summary>
    private static string[] StatementScopedMembers(Type statement, string groupListProperty) =>
        [.. statement.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            // The BoundStatement base's own members (OperandEvaluation, kb/Work PB1432) belong to every node, not
            // to this statement's phrase set.
            .Where(p => p.DeclaringType != typeof(BoundStatement))
            .Where(p => p.Name != groupListProperty)
            .Select(p => p.Name)
            .Order()];

    /// <summary>The statement node holds the repeated group and nothing else — no phrase may be hoisted out of
    /// a group onto the statement (§14.9.27.2; §14.9.27.4 GR20).</summary>
    [Fact]
    public void BoundOpen_CarriesTheGroupListAndNothingElse()
    {
        Assert.Equal([], StatementScopedMembers(typeof(BoundOpen), nameof(BoundOpen.Files)));
    }

    /// <summary>The group list's ELEMENT is the per-file-name record — GR20's own normal form, one entry per
    /// file-name rather than a nested group whose members a consumer could re-widen.</summary>
    [Fact]
    public void BoundOpen_Files_IsAListOfPerFileNameEntries()
    {
        Type files = typeof(BoundOpen).GetProperty(nameof(BoundOpen.Files))!.PropertyType;
        Assert.True(files.IsGenericType, $"BoundOpen.Files is {files.Name}, not a generic list");
        Assert.Equal(typeof(IReadOnlyList<>), files.GetGenericTypeDefinition());
        Assert.Equal(typeof(BoundOpenFile), files.GetGenericArguments()[0]);
    }

    /// <summary>Every phrase the general format nests inside the repeated group has a home ON THE ENTRY, one
    /// property each: the open mode (§14.9.27.2's inner braces), the sharing-phrase (§14.9.27.4 GR22/GR23) and
    /// the retry-phrase (§14.7.9). Each is asserted by TYPE, not by name, so renaming a property cannot quietly
    /// remove a phrase's carrier.</summary>
    [Theory]
    [InlineData(typeof(BoundOpenMode))]
    [InlineData(typeof(SharingMode?))]
    [InlineData(typeof(RetrySpec))]
    public void BoundOpenFile_CarriesOneSlotPerGroupScopedPhrase(Type phrase)
    {
        var carriers = typeof(BoundOpenFile).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == phrase)
            .Select(p => p.Name)
            .ToArray();
        Assert.True(carriers.Length == 1,
            $"BoundOpenFile carries {carriers.Length} properties of type {phrase.Name} "
            + $"({string.Join(", ", carriers)}); §14.9.27.2 gives each group exactly one.");
    }

    // ── the discrimination proof ─────────────────────────────────────────────────────────────────────

    /// <summary>The pre-PB316 shape: the phrases hoisted onto the statement, where one group's SHARING governed
    /// every file in it.</summary>
    private sealed record HoistedOpen(IReadOnlyList<BoundOpenFile> Files)
    {
        public SharingMode? SharingOverride { get; init; }
        public RetrySpec? Retry { get; init; }
    }

    /// <summary>Today's shape, fabricated, so the predicate is shown to ACCEPT as well as reject.</summary>
    private sealed record GroupScopedOpen(IReadOnlyList<BoundOpenFile> Files);

    /// <summary>⛔ Drive the predicate over both fabricated nodes: it names the hoisted phrases and passes the
    /// group-scoped node. Without this, a predicate that returned an empty array unconditionally would look
    /// exactly like a working gate.</summary>
    [Fact]
    public void ThePredicate_ActuallyFails_OnAStatementScopedPhrase()
    {
        Assert.Equal(["Retry", "SharingOverride"],
            StatementScopedMembers(typeof(HoistedOpen), nameof(HoistedOpen.Files)));
        Assert.Equal([], StatementScopedMembers(typeof(GroupScopedOpen), nameof(GroupScopedOpen.Files)));
    }
}
