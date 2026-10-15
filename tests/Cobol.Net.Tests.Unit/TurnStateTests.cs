// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Runtime.Exceptions;
using CobolNet.Runtime.IO;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The compile-time <c>&gt;&gt;TURN</c> fold (ISO/IEC 1989:2023 §7.3.25.4 GR1–GR8; TurnState) and the
/// exception-name catalog (§14.6.13.1.1/.6 Table 13; ExceptionCatalog) — the pure-logic GRs the conformance
/// surface cannot reach directly (EC-I-O-WARNING's explicit-only toggle has no warning-raising operation in the
/// corpus; file-scoped TURN events; the strict succeeding-statements line rule).
/// </summary>
public sealed class TurnStateTests
{
    private static TurnState Build(params TurnEvent[] events) =>
        TurnState.Build(events, new EditionContext(2023));

    [Fact]   // GR2: EC-ALL covers every exception-name…
    public void EcAll_EnablesAnyLevel3Name()
    {
        var ts = Build(new TurnEvent(5, [("EC-ALL", null)], On: true, WithLocation: false));
        Assert.True(ts.Enabled("EC-SIZE-ZERO-DIVIDE", null, 10));
        Assert.True(ts.Enabled("EC-USER-ANYTHING", null, 10));
    }

    [Fact]   // …except EC-I-O-WARNING, which toggles only explicitly (GR2/GR4).
    public void EcAll_DoesNotEnableIoWarning()
    {
        var ts = Build(new TurnEvent(5, [("EC-ALL", null)], On: true, WithLocation: false));
        Assert.False(ts.Enabled("EC-I-O-WARNING", null, 10));
        var explicitOn = Build(new TurnEvent(5, [("EC-I-O-WARNING", null)], On: true, WithLocation: false));
        Assert.True(explicitOn.Enabled("EC-I-O-WARNING", null, 10));
    }

    [Fact]   // GR3: a level-2 name covers its level-3 children — and no other family.
    public void Level2_EnablesItsChildrenOnly()
    {
        var ts = Build(new TurnEvent(5, [("EC-SIZE", null)], On: true, WithLocation: false));
        Assert.True(ts.Enabled("EC-SIZE-TRUNCATION", null, 10));
        Assert.False(ts.Enabled("EC-OVERFLOW-STRING", null, 10));
        Assert.False(ts.Enabled("EC-I-O-WARNING", null, 10));   // GR3 also excludes WARNING
    }

    [Fact]   // GR5: a directive applies to SUCCEEDING statements only — strict line comparison.
    public void Directive_AppliesToSucceedingStatementsOnly()
    {
        var ts = Build(new TurnEvent(5, [("EC-SIZE", null)], On: true, WithLocation: false));
        Assert.False(ts.Enabled("EC-SIZE-TRUNCATION", null, 5));   // same line — not yet in effect
        Assert.False(ts.Enabled("EC-SIZE-TRUNCATION", null, 3));   // before the directive
        Assert.True(ts.Enabled("EC-SIZE-TRUNCATION", null, 6));
    }

    [Fact]   // GR6/GR8: the LAST matching event wins — ON…OFF…ON toggles in source order.
    public void LastMatchingEventWins()
    {
        var ts = Build(
            new TurnEvent(5, [("EC-SIZE", null)], On: true, WithLocation: false),
            new TurnEvent(10, [("EC-ALL", null)], On: false, WithLocation: false),
            new TurnEvent(15, [("EC-SIZE-TRUNCATION", null)], On: true, WithLocation: false));
        Assert.True(ts.Enabled("EC-SIZE-TRUNCATION", null, 7));    // level-2 ON
        Assert.False(ts.Enabled("EC-SIZE-TRUNCATION", null, 12));  // EC-ALL OFF covers it
        Assert.True(ts.Enabled("EC-SIZE-TRUNCATION", null, 20));   // level-3 ON re-enables
        Assert.False(ts.Enabled("EC-SIZE-ZERO-DIVIDE", null, 20)); // the sibling stays off
    }

    [Fact]   // GR6/GR8: a file-scoped event applies only to that file connector.
    public void FileScopedEvent_AppliesOnlyToThatFile()
    {
        var ts = Build(new TurnEvent(5, [("EC-I-O-AT-END", "TF")], On: true, WithLocation: false));
        Assert.True(ts.Enabled("EC-I-O-AT-END", "TF", 10));
        Assert.True(ts.Enabled("EC-I-O-AT-END", "tf", 10));        // file-names compare case-insensitively
        Assert.False(ts.Enabled("EC-I-O-AT-END", "OTHER", 10));
        Assert.False(ts.Enabled("EC-I-O-AT-END", null, 10));       // an unscoped query does not match
    }

    [Fact]   // GR7: WITH LOCATION is a property of the ENABLING event.
    public void WithLocation_TracksTheEnablingEvent()
    {
        var ts = Build(
            new TurnEvent(5, [("EC-SIZE", null)], On: true, WithLocation: true),
            new TurnEvent(10, [("EC-SIZE", null)], On: true, WithLocation: false));
        Assert.True(ts.WithLocation("EC-SIZE-TRUNCATION", null, 7));
        Assert.False(ts.WithLocation("EC-SIZE-TRUNCATION", null, 12));   // re-enabled WITHOUT location
    }

    [Fact]   // §7.3.25.3 SR2 is the DIRECTIVE STAGE's (TurnDirectiveExceptionNameTests; kb/Work PB2740): a name that reaches
             // the fold unresolved is a broken hand-off, never a user error, so it throws and reports no COBOLNET0711.
    public void UnresolvedName_IsABrokenHandOff()
    {
        var edition = new EditionContext(2023);
        Assert.Throws<InvalidOperationException>(() => TurnState.Build(
            [new TurnEvent(5, [("EC-NOT-A-NAME", null)], On: true, WithLocation: false)], edition));
        Assert.Empty(edition.Diagnostics);
    }
}

/// <summary>ISO §14.6.13.1.1/.6 Table 13 — the exception-name catalog's hierarchy, fatality, open families,
/// and the §9.1.13.1 I-O status correspondence.</summary>
public sealed class ExceptionCatalogTests
{
    [Fact]   // §14.6.13.1.1: EC-USER-<suffix> is an open family — level 3 under EC-USER, ALWAYS nonfatal (¶24505).
    public void EcUserFamily_IsOpenAndNonfatal()
    {
        Assert.True(ExceptionCatalog.TryGet("EC-USER-ANY-SUFFIX", out var info));
        Assert.Equal(3, info.Level);
        Assert.Equal("EC-USER", info.Level2Parent);
        Assert.Equal(EcFatality.Nonfatal, info.Fatality);
    }

    [Fact]   // Names are case-insensitive; level-2 and level-1 rows exist with the Hierarchy category.
    public void Hierarchy_LevelsAndCaseInsensitivity()
    {
        Assert.True(ExceptionCatalog.TryGet("ec-size", out var l2));
        Assert.Equal(2, l2.Level);
        Assert.Equal(EcFatality.Hierarchy, l2.Fatality);
        Assert.True(ExceptionCatalog.TryGet("EC-ALL", out var l1));
        Assert.Equal(1, l1.Level);
        Assert.True(ExceptionCatalog.UnderLevel2("EC-SIZE-TRUNCATION", "EC-SIZE"));
        Assert.False(ExceptionCatalog.UnderLevel2("EC-SIZE-TRUNCATION", "EC-OVERFLOW"));
    }

    [Fact]   // Table 13 fatality spot checks: every EC-SIZE-* and EC-PROGRAM-* row is fatal; the OVERFLOW pair
             // is nonfatal; EC-I-O-AT-END/-INVALID-KEY are nonfatal.
    public void Table13_FatalityCategories()
    {
        Assert.True(ExceptionCatalog.TryGet("EC-SIZE-ZERO-DIVIDE", out var zd) && zd.Fatality == EcFatality.Fatal);
        Assert.True(ExceptionCatalog.TryGet("EC-PROGRAM-NOT-FOUND", out var nf) && nf.Fatality == EcFatality.Fatal);
        Assert.True(ExceptionCatalog.TryGet("EC-OVERFLOW-STRING", out var os) && os.Fatality == EcFatality.Nonfatal);
        Assert.True(ExceptionCatalog.TryGet("EC-I-O-AT-END", out var ae) && ae.Fatality == EcFatality.Nonfatal);
    }

    [Fact]   // §9.1.13.1: the I-O status → EC-I-O correspondence and the fatal status classes (3x/4x/7x/9x).
    public void IoStatusCorrespondence()
    {
        Assert.Equal("EC-I-O-AT-END", ExceptionCatalog.IoEcOfStatus("10"));
        Assert.Equal("EC-I-O-INVALID-KEY", ExceptionCatalog.IoEcOfStatus("23"));
        Assert.Equal("EC-I-O-PERMANENT-ERROR", ExceptionCatalog.IoEcOfStatus("35"));
        Assert.Equal("EC-I-O-WARNING", ExceptionCatalog.IoEcOfStatus("04"));
        Assert.Null(ExceptionCatalog.IoEcOfStatus("00"));
        Assert.False(ExceptionCatalog.IsFatalIoStatus("10"));
        Assert.False(ExceptionCatalog.IsFatalIoStatus("23"));
        Assert.True(ExceptionCatalog.IsFatalIoStatus("35"));
        Assert.True(ExceptionCatalog.IsFatalIoStatus("48"));
        Assert.True(ExceptionCatalog.IsFatalIoStatus("90"));
        // ⛔ AND THE BOUNDARY OF THE CORRESPONDENCE (kb/Work PB526). '90' is this implementation's
        // §9.1.13.11 implementor-defined status, which it uses for the §13.18.34.4 GR6 b) LINAGE
        // value-rule violation — and the correspondence answers EC-I-O-IMP for it, because a '9' is
        // all a status can say. The condition §13.18.34.4 GR6 b) 2 NAMES is EC-I-O-LINAGE, which is
        // not in §9.1.13.1's list at all, so the connector carries the name beside the status and
        // FileRegistry.IoConditionName is the ONE place the two are combined. This assertion is the
        // reason that override exists: delete it and every LINAGE violation raises EC-I-O-IMP.
        Assert.Equal("EC-I-O-IMP", ExceptionCatalog.IoEcOfStatus(FileStatusCode.LinageValueViolation));
        Assert.NotEqual(0, ExceptionCatalog.IoBit(ExceptionCatalog.IoLinage));
    }

    [Fact]   // The mask-bit roundtrip: every status-raised name has a distinct bit (the per-statement enable mask).
    public void IoMaskBits_AreDistinctAndRoundTrip()
    {
        var seen = new HashSet<int>();
        foreach (string name in ExceptionCatalog.IoMaskNames)
        {
            int bit = ExceptionCatalog.IoBit(name);
            Assert.True(bit != 0, name);
            Assert.True(seen.Add(bit), $"duplicate bit for {name}");
        }
    }

    /// <summary>⛔ THE DIRECTION THAT WAS MISSING (kb/Work PB854): every level-3 EC-I-O name Table 13 catalogues
    /// is a condition a file connector can set to exist, so every one needs a mask bit — without one,
    /// <c>&gt;&gt;TURN … CHECKING ON</c> enables nothing and a declarative for it silently never runs.
    /// EC-I-O-EOP and EC-I-O-EOP-OVERFLOW were catalogued with none; a name added to the catalog later fails
    /// here until it is given its bit (and, by the same token, a raise site).</summary>
    [Fact]
    public void EveryCataloguedEcIoName_HasAMaskBit()
    {
        var missing = ExceptionCatalog.Level3Rows
            .Where(r => ExceptionCatalog.IsIoName(r.Name) && ExceptionCatalog.IoBit(r.Name) == 0)
            .Select(r => r.Name).ToList();
        Assert.True(missing.Count == 0, "catalogued EC-I-O names with no mask bit: " + string.Join(", ", missing));
    }
}
