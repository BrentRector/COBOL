// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Model;

using Core = CobolParserCore;

/// <summary>One program unit of the compilation group: its identity, containment, PROGRAM-ID attributes
/// (ISO §11.10 / §8.6.6), bound model, and the GLOBAL bridges its class must emit (§13.18.27 GR2).
/// <para>Relocated from the emitter's private <c>CallUnit</c> (rearch PHASE-06 Step 2 — the BINDER owns the bound
/// model; the emitter consumes it read-only through <see cref="BoundCompilation"/>).</para></summary>
internal sealed class BoundUnit
{
    /// <summary>program-name-1 / user-function-name-1 as WRITTEN — the declared COBOL name. This is what a
    /// reference by a user-defined WORD resolves against (§10.7.3 SR2's END PROGRAM match, §14.9.4.3 SR15's
    /// AS NESTED literal, <c>FUNCTION user-function-name</c>) and what FUNCTION MODULE-NAME reports
    /// (CONFORMANCE.md DOC-A.1-135, the §15.65.4 r4 determination). NEVER the AS literal.</summary>
    public required string Name;
    /// <summary>The name externalized to the operating environment: the <c>AS</c> phrase's literal-1 when the
    /// identification division specifies one, else <see cref="Name"/> (ISO §11.10.4 GR1 "Literal-1, if
    /// specified, is the name of the program that is externalized to the operating environment"; §11.5.4 GR1/GR2
    /// for a function; §8.3.2.2 2) is the rule that makes the two distinct). This is what a reference by a
    /// LITERAL resolves against, and §8.3.2.2 makes that exhaustive: "Externalized names shall be referenced
    /// in a source element only: 1) in the AS phrase in a repository paragraph entry, 2) in the AS phrase in
    /// an EXTERNAL clause, 3) as program-name in a CALL statement, 4) as program-name in a CANCEL statement,
    /// 5) as program-name in a program-address-identifier, 6) as method-name in an INVOKE statement or inline
    /// method invocation.  All other references to names for which externalization is permitted shall be
    /// specified using the user-defined words, as opposed to the externalized names." It is also the key of
    /// §12.3.8.4 GR10 a)'s program-definition search. kb/Work PB303.</summary>
    public required string ExternalizedName;
    public required string ClassName;
    public required Core.ProgramUnitContext Ctx;
    /// <summary>Where this unit's source text STARTS in the compilation group — the character offset of its first
    /// token, a total order over every source element of the group (§12.3.8.4 GR10 a) / GR11 a): "a program
    /// definition specified PREVIOUSLY in the same compilation group" means a definition whose position is less
    /// than the referencing element's; kb/Work PB989). A contained program's own context carries no start token,
    /// so this reads the first token under it.</summary>
    public required int SourcePosition;
    public BoundUnit? Parent;
    public List<BoundUnit> Children = [];
    public bool Initial, Common, Recursive;
    /// <summary>True for a FUNCTION-ID unit (ISO §9.4 — a user-defined function; program-shaped except it
    /// RETURNs a value and always possesses the recursive attribute).</summary>
    public bool IsFunction;
    /// <summary>True for a prototype definition — FUNCTION-ID … IS PROTOTYPE or PROGRAM-ID … IS PROTOTYPE
    /// (ISO §11.10.2 Format 2; kb/Work PB894) — a signature-only unit (§10.6.2 SR4: LINKAGE-only data + a
    /// header-only procedure division, screened by <c>PrototypeUnitRules</c>). A function prototype contributes
    /// its signature to the user-function table (M2-UDF-3), a program prototype to the REPOSITORY program-specifier
    /// resolution (§12.3.8.4 GR10 b)); neither emits a body nor registers in the run unit — the definition
    /// (in-group, else separately compiled) is the activation target. <see cref="IsFunction"/> says which kind.</summary>
    public bool IsPrototype;
    public DataBinder Data = null!;
    public ReferenceResolver Refs = null!;
    public BoundProgram Bound = null!;
    public List<CallBridge> Bridges = [];

    /// <summary>The run-unit-unique containment path id (registry key; §8.4.6.3 scoping).</summary>
    public string Path => Parent is null ? Name : Parent.Path + "/" + Name;

    /// <summary>The C# nested-type reference from the top-level scope (factory construction).</summary>
    public string ClassRef => Parent is null ? ClassName : Parent.ClassRef + "." + ClassName;

    /// <summary>⛔ THE ONE ANCHOR OF A CONTAINER MEMBER (kb/Work PB1133): the C# expression prefix by which a program
    /// <paramref name="depth"/> containment levels INSIDE this unit reaches this unit's class member
    /// <paramref name="csMember"/> — the <c>__outer</c> instance chain for an instance member, this unit's CLASS name
    /// for a static one (<see cref="DataBinder.IsStaticMember"/>: ISO §13.5.4 GR1's one copy per run unit is a C#
    /// static, which no instance expression may name). Every place a contained program's text reaches into a
    /// container's data asks here: the GLOBAL bridges (<see cref="DataBinder.GlobalBridgesOf"/>) and the inherited
    /// FILE STATUS places (<c>ProgramEmitter</c>).</summary>
    public string AnchorOf(string csMember, int depth) =>
        Data.IsStaticMember(csMember) ? ClassName + "." : CodeGen.RuntimeApi.OuterChain(depth);

    /// <summary>The FORMAL PARAMETERS of containing programs that this unit sees as GLOBAL names (ISO §13.18.27.4 GR2),
    /// read off the bridges that reach them (kb/Work PB2096). Their member names are Uid-keyed and bridged under the same
    /// names, so a contained program renders them as the container does: a forward of one BY REFERENCE passes on the
    /// incoming carrier, its presence (§8.8.4.8.4 GR1c) and the argument area it occupies (§14.2.3 GR8).</summary>
    public IEnumerable<LinkageFormal> InheritedFormals => Bridges.Select(b => b.Formal).OfType<LinkageFormal>().Distinct();
}

/// <summary>One inherited-GLOBAL bridge a nested class emits: a property aliasing the containing instance's
/// member (ISO §13.18.27 GR2 — the name is visible in every contained program; the STORAGE stays the
/// container's). Built ONLY by <c>DataBinder.GlobalBridgesOf</c>, the one list of the members a reference to a
/// global root can render (kb/Work PB1009). <paramref name="Formal"/> is set when the bridged member belongs to a
/// FORMAL PARAMETER of the container — its carrier (<see cref="CallBridgeKind.Carrier"/>, whose cell type the emitter
/// derives from it), its argument area (<see cref="CallBridgeKind.ArgumentArea"/>) or an area formal's data-address
/// pointer (<see cref="CallBridgeKind.Address"/>) — so the contained program knows the GLOBAL formals in its scope
/// (<see cref="BoundUnit.InheritedFormals"/>; kb/Work PB2096).</summary>
internal sealed record CallBridge(string Field, string Path, CallBridgeKind Kind, DataItem? Item,
    LinkageFormal? Formal = null);

/// <summary>The member shape a <see cref="CallBridge"/> aliases (kb/Work PB1009).</summary>
internal enum CallBridgeKind
{
    /// <summary>A global root's own typed field (<c>ref {ElementType}</c>).</summary>
    Field,
    /// <summary>A Tier-B class's string backing (<c>ref string</c>).</summary>
    Backing,
    /// <summary>The <c>StorageCell</c> behind a cell-backed class (EXTERNAL / BASED / ADDRESS-OF-taken) — a
    /// get-only alias, since two of the three are computed properties.</summary>
    Cell,
    /// <summary>A BASED class's implicit data-address pointer (<c>ref ManagedPointer</c>, ISO §13.18.5.4 GR2).</summary>
    Address,
    /// <summary>A carrier-resident LINKAGE formal's <c>ManagedPointer&lt;T&gt;</c> carrier (its field IS
    /// <c>carrier.Value</c>).</summary>
    Carrier,
    /// <summary>A carrier-resident BY REFERENCE formal's ARGUMENT AREA (<c>ref CellPointer?</c>,
    /// <c>LinkageFormal.ArgumentAreaField</c>): what a contained program's forward of the GLOBAL formal passes on, so an
    /// area formal of the next activation occupies the argument's own storage (ISO §14.2.3 GR8; kb/Work PB2096).</summary>
    ArgumentArea,
    /// <summary>An INDEXED BY <c>long</c> field of a global table.</summary>
    Index,
    /// <summary>A GLOBAL formal's omitted-argument presence member (a plain <c>bool</c>, not a ref — kb/Work PB971).</summary>
    Presence,
}
