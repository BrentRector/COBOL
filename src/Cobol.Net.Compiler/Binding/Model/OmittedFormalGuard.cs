// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

namespace CobolNet.Binding.Model;

/// <summary>
/// ⛔ <b>THE REFERENCE-TIME *-ARG-OMITTED FACT OF A FORMAL PARAMETER</b> (kb/Work PB971) — carried by the formal's
/// root <see cref="DataItem"/> (<see cref="DataItem.OmittedGuard"/>) and copied onto the ROOT segment of every
/// <see cref="AccessPath"/> built from that item, so every reference the backend renders through the path is
/// checked, whatever verb or operand position produced it. ISO §14.9.4.4 GR12 (program), §8.4.3.2.4 GR8 (function)
/// and §14.9.23.4 GR10 (method) each set the kind's condition to exist when "a parameter for which the
/// omitted-argument condition is true is referenced … except as an argument or in the omitted-argument
/// condition"; the two exemptions never render the path (the condition reads <see cref="OmittedProbe"/>; a
/// forwarded whole formal forwards its carrier), so they need no list here.
/// <para>Set only when the compilation group can enable the kind's condition at all
/// (<c>TurnState.AnyEnabledFor</c>): with no such >>TURN the reference renders exactly as before — the
/// zero-scaffolding invariant — because the raise could never fire.</para>
/// </summary>
/// <param name="Presence">The C# boolean expression, valid in any class that can reference the formal, that is
/// TRUE when the argument was omitted: a program/function formal's <c>__omit{Uid}</c> member (a Uid-keyed name, so
/// a contained program's GLOBAL bridge of it cannot collide with its own formals), a method formal's
/// <c>__omittedN</c> presence parameter.</param>
/// <param name="Kind">The activated element that owns the formal — which of the three conditions is raised.</param>
/// <param name="FormalName">The COBOL name, for the diagnostic detail.</param>
/// <param name="CarrierPrefix">Non-null when the root segment's field text BEGINS with a reference-type carrier
/// that is passed through the guard rather than taken by <c>ref</c>: a carrier-resident formal's
/// <c>__lnk{Uid}</c> (its field text is <c>__lnk{Uid}.Value</c>) or a cell-backed class's <c>StorageCell</c>.</param>
public sealed record OmittedFormalGuard(string Presence, ActivatedElementKind Kind, string FormalName,
    string? CarrierPrefix = null)
{
    /// <summary>The program/function arm's presence member name for <paramref name="formal"/>.</summary>
    public static string PresenceMember(DataItem formal) => $"__omit{formal.Uid}";

    /// <summary>The guard a root segment over <paramref name="root"/> carries: the item's own, else — for a
    /// Tier-A/Tier-B view of a REDEFINES class whose canonical is the formal — the canonical's (a redefinition
    /// of the formal names the formal's storage).</summary>
    public static OmittedFormalGuard? Of(DataItem root) => root.OmittedGuard ?? root.Class?.Canonical.OmittedGuard;
}
