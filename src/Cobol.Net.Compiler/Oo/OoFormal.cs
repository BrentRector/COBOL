// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;

namespace CobolNet.Compiler.Oo;

/// <summary>One resolved USING formal: the LINKAGE item (its <see cref="DataItem.CsName"/> is the capturable
/// LOCAL the body addresses), the 0-based positional slot, the emitted C# parameter name, and whether the
/// procedure division header specifies the OPTIONAL phrase for it (ISO §14.2.1 general format; §14.2.3 GR3 — what §14.9.23.3 SR18 and
/// §14.8.2.1 read, and what §9.3.8.2.3 rule 8 compares between a method and its prototype). <paramref name="ByValue"/>
/// is the passing mode of the formal — the BY VALUE phrase specified or implied for it (§14.2.1; §14.2.3 GR4: "Both the
/// BY REFERENCE and the BY VALUE phrases are transitive"), what §14.9.23.3 SR5 and §14.9.23.4 GR6 select the argument's
/// mode by and what §9.3.8.2.3 rule 1 compares ("consistent BY REFERENCE and BY VALUE specifications"). A BY VALUE
/// formal crosses the method ABI exactly as a BY REFERENCE one does — the typed <c>ref</c> value and the omitted flag —
/// but the activator hands it a DETACHED value (never its own storage) and the method never copies it back
/// (§14.2.3 GR10), so the mode is a fact of the binding, not a second C# signature.</summary>
public sealed record OoFormal(DataItem Item, int Position, string ParamName, bool Optional = false, bool ByValue = false)
{
    /// <summary>The C# name of the formal's omitted-presence parameter — the METHOD arm of the one presence fact
    /// (<see cref="OmittedProbe.MethodFlag"/>; kb/Work PB757). EVERY formal carries one, not only an OPTIONAL
    /// one: §8.8.4.8.4 GR1c makes omission transitive through a forwarded formal whatever the receiving
    /// formal's own phrase, exactly as the program arm's null carrier is. The name is positional and outside
    /// every COBOL-word-derived family: every <c>ParamName</c> carries the <c>__formal_</c> tag
    /// (<see cref="NamingConvention.FormalParameterName"/>), so no formal can collide with it.</summary>
    public string OmittedFlag => $"__omitted{Position}";

    /// <summary>The presence fact every consumer reads (§8.8.4.8.4 GR1).</summary>
    public OmittedProbe Probe => new OmittedProbe.MethodFlag(OmittedFlag);
}
