// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;

namespace CobolNet.Compiler.Oo;

/// <summary>One resolved USING formal: the LINKAGE item, the 0-based positional slot, the emitted C# parameter name
/// (the argument's carrier), and whether the
/// procedure division header specifies the OPTIONAL phrase for it (ISO §14.2.1 general format; §14.2.3 GR3 — what §14.9.23.3 SR18 and
/// §14.8.2.1 read, and what §9.3.8.2.3 rule 8 compares between a method and its prototype). <paramref name="ByValue"/>
/// is the passing mode of the formal — the BY VALUE phrase specified or implied for it (§14.2.1; §14.2.3 GR4: "Both the
/// BY REFERENCE and the BY VALUE phrases are transitive"), what §14.9.23.3 SR5 and §14.9.23.4 GR6 select the argument's
/// mode by and what §9.3.8.2.3 rule 1 compares ("consistent BY REFERENCE and BY VALUE specifications"). A BY VALUE
/// formal crosses the method ABI exactly as a BY REFERENCE one does — the carrier, the area and the omitted flag —
/// but the activator hands it a DETACHED cell and no area (never its own storage) and the method never copies it back
/// (§14.2.3 GR10), so the mode is a fact of the binding, not a second C# signature.
/// <para>⛔ THE FORMAL OCCUPIES ITS ARGUMENT'S STORAGE (ISO §14.2.3 GR8; kb/Work PB2087, the method arm). The parameter is
/// the argument's CARRIER (<c>ManagedPointer&lt;T&gt;</c>) plus its AREA (<see cref="AreaParam"/>, the cell the argument
/// lives in, or null), exactly the pair a program's <c>CobolArg</c> carries. A <paramref name="CarrierResident"/>
/// formal (an elementary one no other LINKAGE entry REDEFINES and whose ADDRESS OF is not taken — the program arm's
/// <c>DataBinder.FormalIsCarrierResident</c>) reads and writes through the carrier on every access: its
/// <see cref="DataItem.CsName"/> IS <c>{CarrierLocal}.Value</c>. Every other formal is an AREA formal when a cell
/// can carry it (<see cref="IsArea"/>): its description is laid over the argument's area, as a BASED item's is.</para></summary>
public sealed record OoFormal(DataItem Item, int Position, string ParamName, bool Optional = false, bool ByValue = false,
    bool CarrierResident = false)
{
    /// <summary>The method-local carrier a resident formal's references read and write (<c>{CarrierLocal}.Value</c>),
    /// and — the same Uid-keyed name — the per-activation data-address pointer an AREA formal's class is described
    /// over (<see cref="IsArea"/>).</summary>
    public string CarrierLocal => $"__lnk{Item.Uid}";

    /// <summary>The C# name of the formal's AREA parameter (<c>CellPointer?</c>): the cell area its argument occupies,
    /// or null — positional, outside every COBOL-word family, like <see cref="OmittedFlag"/>.</summary>
    public string AreaParam => $"__area{Position}";

    /// <summary>True for an AREA formal: its class was claimed onto a cell whose implicit data-address pointer IS
    /// <see cref="CarrierLocal"/> (<c>DataBinder.PtrBindBasedAndAddressables</c>), so the binder and the emitter cannot
    /// disagree about which formals are areas.</summary>
    public bool IsArea => !CarrierResident && Item.Class?.BasedPointerField == CarrierLocal;

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
