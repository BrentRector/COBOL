// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Compiler.Oo;
using CobolNet.Runtime;

namespace CobolNet.Binding.Model;

/// <summary>Which of the three alternatives of the <c>USAGE OBJECT REFERENCE</c> general format
/// (ISO §13.18.60.2) a description took — plus the "no optional phrase at all" case §13.18.60.4 GR22 b) names
/// UNIVERSAL. The printed format is
/// <code>
/// OBJECT REFERENCE [ interface-name-1                            ]
///                  [ [ FACTORY OF ] ACTIVE-CLASS                 ]
///                  [ [ FACTORY OF ] object-class-name-1 [ ONLY ] ]
/// </code>
/// (rendered from the canonical PDF, printed folio 503 — the bracket pair encloses three stacked
/// alternatives, so at most one is written).</summary>
public enum ObjectRefKind
{
    /// <summary>No optional phrase — §13.18.60.4 GR22 b): "If none of the optional phrases is specified,
    /// this data item is called a universal object reference. Its content may be a reference to any
    /// object."</summary>
    Universal,

    /// <summary>interface-name-1 — §13.18.60.4 GR22 c): "If interface-name-1 is specified, the object
    /// referenced by this data item shall implement interface-1." Neither FACTORY nor ONLY may accompany it:
    /// this alternative of the general format carries no such phrase.</summary>
    Interface,

    /// <summary>object-class-name-1, optionally FACTORY OF and/or ONLY — §13.18.60.4 GR22 d).</summary>
    ObjectClass,

    /// <summary>ACTIVE-CLASS, optionally FACTORY OF — §13.18.60.4 GR22 e): "the object referenced by this
    /// data item shall be of the same class as the object that was used to invoke the method in which this
    /// data description entry is specified". §13.18.60.3 SR16 confines it to a factory definition, an
    /// instance definition, or the linkage or local-storage section of a method definition.</summary>
    ActiveClass,
}

/// <summary>
/// ⛔ THE OBJECT-REFERENCE DESCRIPTION, AS A TUPLE — the four INDEPENDENT axes ISO §13.18.60.2 prints
/// (<see cref="Kind"/> × <see cref="Factory"/> × <see cref="Only"/> × <see cref="Name"/>), carried on
/// <see cref="PicInfo.ObjectRef"/> for every category-<c>ObjectReference</c> item.
/// <para>
/// It replaced a single nullable <c>string? ObjectClassName</c> (kb/Work PB389). That scalar could spell only
/// "universal" (null) and "some name" (non-null): ACTIVE-CLASS and ONLY had no grammar alternative and were
/// REJECTED OUTRIGHT as legal COBOL-2002+ source, FACTORY OF was loud-staged, and the KIND was re-derived
/// downstream by asking the class table twice — so every one of §14.9.39.3's SET Format-5 rules that
/// DISCRIMINATES on an unmodelled axis (SR10 b/c, SR11, SR12 a1/a3/b/c, SR13, SR14) had no expressible
/// subject. Consumers now ASK this descriptor instead of guessing, which is also what retires the
/// "universal" test being spelled "no class name recorded" — a test that is equally true of a factory, an
/// interface or an ACTIVE-CLASS reference and so misdiagnosed them the moment they became declarable.
/// </para>
/// <para>
/// <b>The GR22 reading of each axis</b> (§13.18.60.4 GR22 — "It shall contain either null or a reference to an
/// object, subject to the following rules"): b) no phrase ⇒ any object; c) interface-name-1 ⇒ an object that
/// IMPLEMENTS it; d) object-class-name-1 ⇒ d)1. without ONLY, that class OR A SUBCLASS (d)1.a. its factory
/// object when FACTORY is written, d)1.b. an instance object otherwise), d)2. with ONLY, EXACTLY that class
/// (d)2.a. factory, d)2.b. instance); e) ACTIVE-CLASS ⇒ the class of the object that invoked the containing
/// method (e)1. its factory object when FACTORY is written, e)2. an instance object otherwise).
/// </para>
/// </summary>
/// <param name="Kind">Which general-format alternative was written.</param>
/// <param name="Name">
/// The class or interface this reference is STATICALLY BOUND to, uppercase-insensitive as written:
/// interface-name-1 for <see cref="ObjectRefKind.Interface"/>, object-class-name-1 for
/// <see cref="ObjectRefKind.ObjectClass"/>, and — for <see cref="ObjectRefKind.ActiveClass"/> — the CONTAINING
/// class when the entry is written in a factory definition, an instance definition, or a method definition of a
/// class — and NULL when it is written in a method PROTOTYPE of an interface definition, which §13.18.60.3 SR16 also
/// admits (a prototype is a method definition) and which has no class: the description is then owner-less
/// (<see cref="ActiveClass"/>). Otherwise null ONLY for <see cref="ObjectRefKind.Universal"/>. Carrying the containing class here is what lets §14.9.39.3 SR12 b)2.
/// ("the class containing the data item referenced by identifier-4 shall be the same class or a subclass of
/// …") be asked of the descriptor rather than re-derived from the binder's ambient state at each use.
/// </param>
/// <param name="Factory">The FACTORY OF phrase (GR22 d)1.a./d)2.a./e)1.) — the item holds a class's FACTORY
/// object, never an instance. Never true for <see cref="ObjectRefKind.Interface"/> or
/// <see cref="ObjectRefKind.Universal"/>: their general-format alternatives carry no FACTORY phrase.</param>
/// <param name="Only">The ONLY phrase (GR22 d)2.) — the referenced object is of EXACTLY the named class, not a
/// subclass. Legal only on the object-class-name-1 alternative.</param>
public readonly record struct ObjectRefDescriptor(ObjectRefKind Kind, string? Name, bool Factory, bool Only)
{
    /// <summary>The bare <c>USAGE OBJECT REFERENCE</c> — §13.18.60.4 GR22 b).</summary>
    public static ObjectRefDescriptor Universal { get; } = new(ObjectRefKind.Universal, null, false, false);

    /// <summary><c>USAGE OBJECT REFERENCE interface-name-1</c> — GR22 c).</summary>
    public static ObjectRefDescriptor Interface(string name) => new(ObjectRefKind.Interface, name, false, false);

    /// <summary><c>USAGE OBJECT REFERENCE [FACTORY OF] object-class-name-1 [ONLY]</c> — GR22 d).</summary>
    public static ObjectRefDescriptor ObjectClass(string name, bool factory = false, bool only = false)
        => new(ObjectRefKind.ObjectClass, name, factory, only);

    /// <summary><c>USAGE OBJECT REFERENCE [FACTORY OF] ACTIVE-CLASS</c> — GR22 e). <paramref name="containing"/>
    /// is the class the entry is written in: a factory definition, an instance definition, or a method definition of
    /// a class (§13.18.60.3 SR16). It is NULL for the one SR16 placement that has no class — a method PROTOTYPE of an
    /// interface definition (a prototype IS a method definition, §10.6.1 NOTE): the description is OWNER-LESS there,
    /// and rightly so, because the class whose object the phrase names is a property of the activation (GR22 e)), not
    /// of the description (§9.3.8.2.3 rule 2 d) compares only the phrase and the FACTORY presence).</summary>
    public static ObjectRefDescriptor ActiveClass(string? containing, bool factory = false)
        => new(ObjectRefKind.ActiveClass, containing, factory, false);

    /// <summary>True for an ACTIVE-CLASS description: the class it names is the ACTIVATION's (GR22 e)), so it crosses
    /// the method ABI as the universal object type and is viewed as its containing class inside the body (see
    /// <c>OoEmitter.OoFormalCrossingType</c>) — one wire type for the method, every override of it and every
    /// interface prototype it implements.</summary>
    public bool IsActiveClass => Kind is ObjectRefKind.ActiveClass;

    /// <summary>True for the bare form — GR22 b), "its content may be a reference to any object". This is the
    /// ONE place "universal" is decided; before PB389 every consumer spelled it "no class name recorded",
    /// which is equally true of every other shape.</summary>
    public bool IsUniversal => Kind is ObjectRefKind.Universal;

    /// <summary>True when the item is statically bound to a named CLASS (not an interface, not universal) —
    /// i.e. §14.9.39.3's "described with an object-class-name". ACTIVE-CLASS is deliberately NOT included: the
    /// rules treat it as its own alternative throughout (SR10 b vs c, SR12 a vs b, SR14).</summary>
    public bool IsObjectClass => Kind is ObjectRefKind.ObjectClass;

    /// <summary>The emitted C# type of a reference with this description, WITHOUT the trailing <c>?</c>.
    /// Universal → <c>CobolObject</c> (D-U1). Otherwise the name mapping the ClassUnit emission convention
    /// uses (<c>DataItem.WordIdentifier</c> — two spellings of one COBOL word are one class, §8.1.3.2 GR3), plus
    /// <c>NamingConvention.FactorySuffix</c> when FACTORY OF was written, because a factory object's emitted
    /// type is the sibling singleton class (design D11), not the instance class. ACTIVE-CLASS emits its
    /// CONTAINING class: GR22 e) admits that class or, through inheritance of the method, a subclass — both of
    /// which are assignable to it, so the containing class is the sound static bound.</summary>
    public string ClrTypeName => Kind is ObjectRefKind.Universal || Name is null
        ? UniversalClrName
        : DataItem.WordIdentifier(Name) + (Factory ? NamingConvention.FactorySuffix : "");

    /// <summary>The emitted C# type of a universal object reference (D-U1) — the runtime root every emitted class derives
    /// from. The ONE spelling <see cref="ClrTypeName"/> and the method ABI's return-type projection
    /// (<c>OoEmitter.OoReturnClrType</c>, kb/Work PB1499) both read.</summary>
    public const string UniversalClrName = "CobolObject";

    /// <summary>True when the emitted C# type is an INTERFACE (<c>OoEmitter.EmitInterfaceUnit</c>) — the one description
    /// kind C# cannot convert to <see cref="UniversalClrName"/> without a cast (a class type converts up implicitly; an
    /// interface type to a class type does not, CS0266), and so the one a COVARIANT RETURN needs the explicit
    /// conversion for: §9.3.8.2.3 rule 5 a) admits ANY object reference where interface-2's returning item is
    /// universal (kb/Work PB1499).</summary>
    public bool IsClrInterface => Kind is ObjectRefKind.Interface;

    /// <summary>ISO §9.3.8.2.3 rule 2 / §14.8.2.3.2 IDENTICAL-DESCRIPTION equality: the same kind, the same
    /// name (case-insensitively — COBOL words are, §8.3.2.2) and the same FACTORY and ONLY presence. Rule 2 c)
    /// says it in one breath: "the corresponding parameter in interface-1 is described with the same
    /// object-class-name, and the presence or absence of the FACTORY and ONLY phrases is the same in both
    /// interfaces." Rule 2 d) is the ACTIVE-CLASS clause: "the corresponding parameter in interface-1 is described
    /// with the ACTIVE-CLASS phrase, and the presence or absence of the FACTORY phrase is the same in both
    /// interfaces" — the CONTAINING class is not part of the description (GR22 e) binds it at activation), so two
    /// ACTIVE-CLASS descriptions agree whatever classes they were written in: an override in a subclass, a class
    /// implementing an interface prototype's (owner-less) formal.</summary>
    public bool SameDescriptionAs(ObjectRefDescriptor other)
        => Kind == other.Kind && Factory == other.Factory && Only == other.Only
           && (IsActiveClass || CobolNames.Same(Name, other.Name));

    /// <summary>How the description reads back in a diagnostic — the phrases as written, so a message never
    /// has to say "universal" about a typed reference (the PB389 misdiagnosis).</summary>
    public string Spelled => Kind switch
    {
        ObjectRefKind.Universal => "a UNIVERSAL object reference",
        ObjectRefKind.Interface => $"an object reference described with interface-name '{Name}'",
        ObjectRefKind.ActiveClass => $"an object reference described {(Factory ? "FACTORY OF " : "")}ACTIVE-CLASS "
            + (Name is null ? "(in an interface method prototype)" : $"(containing class '{Name}')"),
        _ => "an object reference described "
             + (Factory ? "FACTORY OF " : "") + $"'{Name}'" + (Only ? " ONLY" : ""),
    };

    /// <summary>The general-format phrase text, for a message that must echo the description verbatim.</summary>
    public override string ToString() => Kind switch
    {
        ObjectRefKind.Universal => "OBJECT REFERENCE",
        ObjectRefKind.Interface => $"OBJECT REFERENCE {Name}",
        ObjectRefKind.ActiveClass => $"OBJECT REFERENCE {(Factory ? "FACTORY OF " : "")}ACTIVE-CLASS",
        _ => $"OBJECT REFERENCE {(Factory ? "FACTORY OF " : "")}{Name}{(Only ? " ONLY" : "")}",
    };
}
