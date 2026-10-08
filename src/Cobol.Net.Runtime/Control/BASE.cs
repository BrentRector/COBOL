// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

// ⛔ THE TYPE NAMES ARE A WIRE CONTRACT. The compiler emits a COBOL class-name as its sanitized, uppercased
// spelling (ObjectRefDescriptor.ClrTypeName) and a factory object's type as that name plus "__FACTORY"
// (NamingConvention.FactorySuffix). Naming the standard class's implementation BASE / BASE__FACTORY is what lets
// `CLASS-ID. C INHERITS FROM BASE`, `USAGE OBJECT REFERENCE BASE` and `FACTORY OF BASE` name these types with no
// special case in the emitter. Emitted code lives in the global namespace and imports CobolNet.Runtime, so a
// compilation group that DEFINES its own class BASE shadows these (a global-namespace type wins over a using-
// imported one) — the same precedence OoClassTable.Find gives a group definition over the standard class.
//
// ⛔ BASE AND BASE__FACTORY ARE CONCRETE, AND BASE__FACTORY HAS THE SAME __Instance SINGLETON EVERY GENERATED
// FACTORY HAS (kb/Work PB2489). §16.2.1.2 GR1 makes New on BASE's own factory create "an object" — an instance of
// BASE itself — so `INVOKE BASE "NEW"` and `SET f TO FACTORY OF BASE` are legal source, and the emitter reaches them
// as `BASE__FACTORY.__Instance`, exactly as it reaches `CLS__FACTORY.__Instance` for any class: one shape for every
// factory, so no emit site has a BASE arm. A generated factory that inherits this one hides the accessor with `new`.
#pragma warning disable CA1707 // the underscore is the emitted factory-type convention, not a style choice

/// <summary>
/// The OBJECT half of the standard class BASE — ISO/IEC 1989:2023 §16.1: "A standard class BASE shall be provided
/// by the implementation. It may be used as the root of a class hierarchy to provide standard object life-cycle
/// function." Its §16.2 object interface, BaseInterface, is the one instance method FactoryObject. Every class that
/// INHERITS FROM BASE (directly or through a superclass) derives from this type; a class that does not derives from
/// <see cref="CobolObject"/> directly, and has neither New nor FactoryObject (§16.1: "This use is not required").
/// §16.2's NOTE — "The standard class BASE need not be implemented in COBOL" — is what this class is.
/// </summary>
public class BASE : CobolObject
{
    /// <summary>The factory object of THIS object's runtime class (§16.2.2.2 GR1: FactoryObject "determines the
    /// class of the object"). Every emitted BASE-derived class overrides it with its own factory singleton, so the
    /// most-derived override IS the runtime class's factory; a plain BASE object (created by New on BASE's own
    /// factory) is of class BASE.</summary>
    protected virtual BASE__FACTORY __FactoryOfClass => BASE__FACTORY.__Instance;

    /// <summary>§9.3.6 match rule 3 d) 5.: the FACTORY type of a plain BASE object's class.</summary>
    protected internal override Type? __FactoryClassType => typeof(BASE__FACTORY);

    /// <summary>FactoryObject (§16.2.2.2 GR1): "When invoked on an instance object, the FactoryObject method
    /// determines the class of the object and returns a reference to the factory object associated with that
    /// class." Virtual, because BaseInterface's method is not FINAL — a COBOL subclass may override it
    /// (<c>METHOD-ID. FactoryObject OVERRIDE.</c>), and its emitted C# member then overrides this one by name.</summary>
    public virtual CobolObject? FACTORYOBJECT() => __FactoryOfClass;

    /// <summary>The universal-receiver dispatch (D10; §14.9.23.3 SR6/SR7) for BaseInterface: an emitted class's
    /// switch falls through <c>default:</c> to here for a method no COBOL class of the hierarchy declares.</summary>
    public override void __CobolInvoke(string name, CobolInvokeArg[] args, CobolInvokeArg? returning)
    {
        if (CobolNames.Same(name, "FACTORYOBJECT")   // the normalized key is the Annex C fold (PB1402)
            && StandardMethodCrossing.Matches(args, returning))
        {
            returning!.Value = FACTORYOBJECT();
            return;
        }
        base.__CobolInvoke(name, args, returning);
    }
}

/// <summary>
/// The FACTORY half of the standard class BASE: the factory object of every BASE-derived class derives from this
/// type, and its §16.2 factory interface, BaseFactoryInterface, is the one factory method New — "a factory method
/// that provides a standard mechanism for creating instance objects of a class" (§16.2.1.1). §9.3.14.3: "An
/// instance object is created as the result of the NEW method being invoked on a factory object."
/// </summary>
public class BASE__FACTORY : CobolObject
{
    /// <summary>The factory object of the standard class BASE in the CURRENT run unit, created on first reference
    /// (§9.3.14.2: "A factory object is created before it is first referenced by a run unit") — the same accessor,
    /// by the same name and through the same <see cref="RunUnit.FactoryObject{T}"/>, every generated factory
    /// emits for its own class.</summary>
    public static BASE__FACTORY __Instance => RunUnit.Current.FactoryObject<BASE__FACTORY>();

    /// <summary>§9.3.6 match rule 3 d) 4.: the INSTANCE type of the class this factory belongs to — BASE itself.</summary>
    protected internal override Type? __InstanceClassType => typeof(BASE);

    /// <summary>Allocate and initialize one instance of this factory's class — §16.2.1.2 GR1's "allocates storage
    /// for an object, initializes its instance data in accordance with 14.6.2.4". Every emitted BASE-derived factory
    /// overrides it covariantly with <c>new C()</c> (the generated constructor IS the initialization, OO deep-dive
    /// D4), so New invoked on a subclass's factory — or through SELF in an inherited factory method — creates the
    /// runtime factory's class. BASE's own factory creates a plain BASE.</summary>
    protected virtual BASE __Create() => new BASE();

    /// <summary>New (§16.2.1.2): create an object through <see cref="__Create"/>, or — GR2 — "If resources needed to
    /// create a new object are not available, the returned object reference is set to NULL, and the EC-OO-RESOURCE
    /// exception condition is set to exist and is propagated back to the runtime element that invoked the New
    /// method". An allocation failure is the one resource a managed object's creation can lack; the condition
    /// raises through the ordinary fatal path when checking for it is enabled (<see
    /// cref="ExceptionState.OoResourceError"/>), and otherwise NULL is returned. Every New form reaches it (the
    /// class-name, FACTORY OF, SELF/SUPER and universal forms), and the emitted delivery narrows the result to the
    /// receiving item's type — a cast the binder's §14.8.3.3 conformance check has already proved safe.
    /// <para>VIRTUAL, because §16.2 does not declare New FINAL: a BASE subclass may write <c>METHOD-ID. NEW
    /// OVERRIDE.</c> (§11.7.3 SR3; kb/Work PB1582), whose emitted member overrides this one, and <c>INVOKE SUPER
    /// "NEW"</c> inside it reaches this body as <c>base.__New()</c>. Its result type is the universal object type
    /// because that is how §16.2's returning item — <c>object reference active-class</c> — crosses the method ABI
    /// (an ACTIVE-CLASS item crosses as the universal type, OoEmitter.OoFormalCrossingType), so a COBOL override's
    /// emitted signature is this one.</para></summary>
    public virtual CobolObject? __New()
    {
        try
        {
            return __Create();
        }
        catch (OutOfMemoryException)
        {
            ExceptionState.OoResourceError(
                $"New: the resources needed to create an object of the class of factory '{GetType().Name}' are not "
                + "available (ISO §16.2.1.2 GR2)");
            return null;
        }
    }

    /// <summary>The universal-receiver dispatch (D10) for BaseFactoryInterface — a factory object held in a
    /// universal reference, <c>INVOKE u "New" RETURNING r</c>.</summary>
    public override void __CobolInvoke(string name, CobolInvokeArg[] args, CobolInvokeArg? returning)
    {
        if (CobolNames.Same(name, "NEW")   // the normalized key is the Annex C fold (PB1402)
            && StandardMethodCrossing.Matches(args, returning))
        {
            returning!.Value = __New();
            return;
        }
        base.__CobolInvoke(name, args, returning);
    }
}

/// <summary>ISO §9.3.6's MATCH for a universal invocation of a BASE method (kb/Work PB480). Both §16.2 methods take no
/// parameters and return an object reference described ACTIVE-CLASS, so an invocation with an argument (match rule 1:
/// "The number of invocation parameters shall be equal"), without a RETURNING item (rule 1: "if there is a returning item
/// in the invoked method …"), or whose RETURNING item a SET cannot receive an object reference into (rule 6) does not
/// match: the search goes on up the chain and ends in EC-OO-METHOD (§9.3.6 resolution step 6), exactly as for a method
/// written in COBOL — it used to raise EC-OO-UNIVERSAL, which §14.9.23.4 GR7 c) reserves for a BOUND method. Whether
/// the returned object's CLASS conforms to a typed receiving item is a question only the object can answer, so the
/// caller's delivery asks it (<see cref="CobolObject.NarrowUniversal{T}"/>; §14.8.3.3 rule 2).</summary>
internal static class StandardMethodCrossing
{
    /// <summary>The §16.2 returning item, <c>OBJECT REFERENCE ACTIVE-CLASS</c>.</summary>
    private static readonly ActivationDescription ActiveClassReturning = new()
    {
        Shape = ActivationShape.ObjectReference, Category = ActivationCategory.Object,
        ObjectKind = ObjectReferenceKind.ActiveClass,
    };

    public static bool Matches(CobolInvokeArg[] args, CobolInvokeArg? returning) =>
        args.Length == 0 && returning is not null
        && ActivationRelations.ReturningMatches(returning.Description, ActiveClassReturning);
}
