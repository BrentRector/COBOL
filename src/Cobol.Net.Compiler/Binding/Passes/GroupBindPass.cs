// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using System.Linq;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;

using CobolNet.Compiler.Oo;

namespace CobolNet.Binding.Passes;

/// <summary>The shared spine of the whole-group middle-end passes (P6 Step 3): the parse ROOT (the terminal
/// <c>VersionConformancePass</c>'s parse-tree arm walks it — carrying it on the GROUP context puts no parse context
/// on any bound NODE), the collected units + class units, and the group-bind session (turn state, class table,
/// edition, uid bands). Deliberately does NOT carry the OO bind driver — no group pass needs the
/// emitter-hosted OO bind bodies (they all run BEFORE the tail), so a pass reaching for emit state is a compile
/// error by construction. <see cref="SkeletonClasses"/> / <see cref="SkeletonInterfaceData"/> are the unexpanded
/// PARAMETERIZED definitions' forests (OO deep-dive D12 determination 6): bound for their own rules, never emitted, so
/// they are deliberately OUTSIDE <see cref="AllBinders"/> (no producing pass runs over them) and are read only by the
/// edition pass, which gates their data and walks their bound method bodies (kb/Work PB2937).</summary>
internal sealed record GroupBindContext(
    CobolParserCore.CompilationUnitContext Tree,
    IReadOnlyList<BoundUnit> Units,
    IReadOnlyList<OoClassUnit> Classes,
    IReadOnlyDictionary<OoInterfaceSymbol, DataBinder> InterfaceData,
    IReadOnlyList<OoClassUnit> SkeletonClasses,
    IReadOnlyDictionary<OoInterfaceSymbol, DataBinder> SkeletonInterfaceData,
    BindSession Session)
{
    /// <summary>Every DataBinder of the group, in the fused pipeline's order: class OBJECT + FACTORY forests,
    /// then the program units. THE one group-forest enumerator — the pass bodies and the watermark
    /// advance/require loops all ride it. (The INTERFACE forests are deliberately separate —
    /// <see cref="InterfaceData"/> — they carry prototype formals only and joined the pass inputs at P5.7 so the
    /// storage-level harmonize can classify both sides of an implements pair.)</summary>
    public IEnumerable<DataBinder> AllBinders() =>
        Classes.SelectMany(c => new[] { c.Data, c.FactoryData }).Concat(Units.Select(u => u.Data));

    /// <summary>The group forests PLUS the interface prototype forests (the storage-form pass's full domain).</summary>
    public IEnumerable<DataBinder> AllBindersAndInterfaces() => AllBinders().Concat(InterfaceData.Values);
}

/// <summary>
/// One declared GROUP pass of the bind pipeline (P6 Step 3): the <see cref="IPassInfo"/> contract plus the work it
/// runs over the WHOLE compilation (<see cref="GroupBindContext"/>). These are the middle-end passes that need
/// every unit's BOUND tree — procedure binding, whole-group usage collection, the storage-form computation — and,
/// terminally, the <c>VersionConformancePass</c>. The manifest is <c>BindPipeline.GroupTail</c>; the runner is
/// <c>BinderDriver.Bind</c>; the order is validated together with the per-unit prefix by
/// <c>BindPipeline.ValidateFullChainOnce</c>.
/// </summary>
internal sealed record GroupBindPass(string Name, PassPhase Requires, PassPhase Produces, Action<GroupBindContext> Body)
    : IPassInfo
{
    public void Run(GroupBindContext ctx) => Body(ctx);
}
