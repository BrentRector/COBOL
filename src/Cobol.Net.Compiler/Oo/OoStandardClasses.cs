// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;

namespace CobolNet.Compiler.Oo;

/// <summary>Which method of the standard class BASE an <see cref="OoMethodSymbol"/> is (ISO §16.2).</summary>
public enum StandardMethod
{
    /// <summary>A method written in the compilation group.</summary>
    None,

    /// <summary>BaseFactoryInterface's <c>New</c> — ISO §16.2.1.2 GR1: "The New method allocates storage for an
    /// object, initializes its instance data in accordance with 14.6.2.4, Initial state of object data, and
    /// returns a reference to the created object."</summary>
    New,

    /// <summary>BaseInterface's <c>FactoryObject</c> — ISO §16.2.2.2 GR1: "When invoked on an instance object,
    /// the FactoryObject method determines the class of the object and returns a reference to the factory object
    /// associated with that class."</summary>
    FactoryObject,
}

/// <summary>
/// ⛔ THE STANDARD CLASSES OF ISO/IEC 1989:2023 CLAUSE 16 — the one place the compiler knows what they are.
///
/// <para><b>The rule.</b> §16.1: "A standard class BASE shall be provided by the implementation. It may be used
/// as the root of a class hierarchy to provide standard object life-cycle function. This use is not required".
/// §16.2 gives its two formal interfaces — BaseFactoryInterface (the factory method <c>New</c>, returning
/// <c>object reference active-class</c>) and BaseInterface (the instance method <c>FactoryObject</c>, returning
/// <c>object reference factory of active-class</c>) — and its NOTE says "The standard class BASE need not be
/// implemented in COBOL". It is not: its implementation is the runtime pair <c>CobolNet.Runtime.BASE</c> /
/// <c>BASE__FACTORY</c>, whose names are exactly the ones the emitted-type convention
/// (<see cref="ObjectRefDescriptor.ClrTypeName"/>: sanitize + uppercase, plus
/// <see cref="NamingConvention.FactorySuffix"/>) gives the class-name BASE — so a class inheriting it, a typed
/// <c>USAGE OBJECT REFERENCE BASE</c> and a <c>FACTORY OF BASE</c> all name the runtime types with no special
/// case anywhere in the emitter.</para>
///
/// <para><b>How a source element reaches it.</b> Like every class, only through its REPOSITORY paragraph
/// (§8.4.6.4; §11.3.3 SR2 for INHERITS): a class-specifier <c>CLASS BASE</c> requires "information in the
/// external repository for the class" (§12.3.8.3 SR6 b), and the implementation's own standard class is always
/// there. How a specifier and that information choose the class "is implementor-defined" (§12.3.8.4 GR6): a class
/// DEFINED in the compilation group under the same name is the one the group's own specifiers mean, and the
/// standard class answers only a name no definition of the group claims (<see cref="OoClassTable.Find"/>).</para>
///
/// <para><b>What it is NOT.</b> It is never in <see cref="OoClassTable.Classes"/> — it has no source, binds no
/// data and emits nothing — so every pass over the group's definitions is unaffected. It is reached only through
/// a base link (<see cref="OoClassSymbol.Base"/>), which is how its methods join a subclass's factory and instance
/// interfaces (§9.3.9: "The subclass has all the methods defined for the inherited class definition"). A class
/// that does not inherit BASE has no New at all, which is what makes <c>INVOKE root-class "NEW"</c> the §14.9.23.3
/// SR3 violation it is (kb/Work PB1548).</para>
/// </summary>
public static class OoStandardClasses
{
    /// <summary>The name of the standard class (§16.1). Case-insensitive like every class-name (§8.3.2.2).</summary>
    public const string BaseName = "BASE";

    /// <summary>The method-name of BaseFactoryInterface's factory method, as §16.2 spells it (method-names compare
    /// case-insensitively, §8.3.2.2, so <c>INVOKE C "NEW"</c> names it).</summary>
    public const string NewMethodName = "New";

    /// <summary>The method-name of BaseInterface's instance method, as §16.2 spells it.</summary>
    public const string FactoryObjectMethodName = "FactoryObject";

    /// <summary>The runtime member that implements New: the virtual <c>__New()</c> of <c>BASE__FACTORY</c>, which
    /// creates through the covariant <c>__Create</c> every BASE-derived factory class overrides (OO deep-dive D4). It is
    /// NOT the uppercase method-name: a COBOL <c>METHOD-ID. NEW OVERRIDE.</c> in a BASE subclass adopts it as a C#
    /// <c>override</c> (the override takes its base slot's CsName, <c>OoClassTable.ResolveOverrides</c>), while a factory
    /// method NEW in a class outside BASE's hierarchy is an ordinary method emitted under its own name (kb/Work
    /// PB1582).</summary>
    public const string NewCsName = "__New";

    /// <summary>The runtime member that implements FactoryObject — the uppercase method-name, the same convention
    /// every emitted method follows, so a COBOL <c>METHOD-ID. FactoryObject OVERRIDE.</c> adopts it as a C#
    /// <c>override</c> exactly as it would a superclass method written in COBOL.</summary>
    public const string FactoryObjectCsName = "FACTORYOBJECT";

    /// <summary>Build one compilation group's symbol for the standard class BASE, with its §16.2 method rosters.
    /// One per <see cref="OoClassTable"/> (never a shared static): a group's pass-1 marks overrides against it,
    /// and a symbol shared across compilations would carry one group's facts into the next.</summary>
    public static OoClassSymbol BuildBase()
    {
        var cls = new OoClassSymbol(BaseName, DataItem.Sanitize(BaseName).ToUpperInvariant(), ctx: null);

        // §16.2 BaseFactoryInterface: `Method-id. New. … 01 outObject usage object reference active-class.
        // Procedure division returning outObject.` No formal parameters.
        cls.TryAddFactoryMethod(Standard(cls, NewMethodName, StandardMethod.New, factory: true,
            returning: ObjectRefDescriptor.ActiveClass(BaseName, factory: false), returningName: "outObject"));

        // §16.2 BaseInterface: `Method-id. FactoryObject. … 01 outFactory usage object reference factory of
        // active-class. Procedure division returning outFactory.` No formal parameters.
        cls.TryAddMethod(Standard(cls, FactoryObjectMethodName, StandardMethod.FactoryObject, factory: false,
            returning: ObjectRefDescriptor.ActiveClass(BaseName, factory: true), returningName: "outFactory"));
        return cls;
    }

    private static OoMethodSymbol Standard(OoClassSymbol owner, string name, StandardMethod which, bool factory,
        ObjectRefDescriptor returning, string returningName)
    {
        // The symbol carries the §16.2 signature as a real OoMethodBinding, so every consumer that reads a method's
        // signature — override conformance (§11.7.3 SR9), the storage-crossing harmonization, the universal
        // dispatch descriptors — sees BASE's methods exactly as it sees a method written in COBOL.
        var m = new OoMethodSymbol(name, HasUsing: false, HasReturning: true, Ctx: null!)
        {
            CsName = which is StandardMethod.New ? NewCsName : FactoryObjectCsName,
            Owner = owner, IsFactory = factory, Standard = which,
        };
        m.Binding = new OoMethodBinding
        {
            Returning = new DataItem
            {
                Level = 1,
                CobolName = returningName,
                CsName = returningName,
                Pic = PicInfo.ObjectReferenceItem(returning),
            },
        };
        return m;
    }
}
