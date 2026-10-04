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
#pragma warning disable CA1707 // the underscore is the emitted factory-type convention, not a style choice

/// <summary>
/// The OBJECT half of the standard class BASE — ISO/IEC 1989:2023 §16.1: "A standard class BASE shall be provided
/// by the implementation. It may be used as the root of a class hierarchy to provide standard object life-cycle
/// function." Its §16.2 object interface, BaseInterface, is the one instance method FactoryObject. Every class that
/// INHERITS FROM BASE (directly or through a superclass) derives from this type; a class that does not derives from
/// <see cref="CobolObject"/> directly, and has neither New nor FactoryObject (§16.1: "This use is not required").
/// §16.2's NOTE — "The standard class BASE need not be implemented in COBOL" — is what this class is.
/// </summary>
public abstract class BASE : CobolObject
{
    /// <summary>The factory object of THIS object's runtime class (§16.2.2.2 GR1: FactoryObject "determines the
    /// class of the object"). Every emitted BASE-derived class overrides it with its own factory singleton, so the
    /// most-derived override IS the runtime class's factory.</summary>
    protected abstract BASE__FACTORY __FactoryOfClass { get; }

    /// <summary>FactoryObject (§16.2.2.2 GR1): "When invoked on an instance object, the FactoryObject method
    /// determines the class of the object and returns a reference to the factory object associated with that
    /// class." Virtual, because BaseInterface's method is not FINAL — a COBOL subclass may override it
    /// (<c>METHOD-ID. FactoryObject OVERRIDE.</c>), and its emitted C# member then overrides this one by name.</summary>
    public virtual CobolObject? FACTORYOBJECT() => __FactoryOfClass;

    /// <summary>The universal-receiver dispatch (D10; §14.9.23.3 SR6/SR7) for BaseInterface: an emitted class's
    /// switch falls through <c>default:</c> to here for a method no COBOL class of the hierarchy declares.</summary>
    public override void __CobolInvoke(string name, CobolInvokeArg[] args, CobolInvokeArg? returning)
    {
        if (CobolNames.Same(name, "FACTORYOBJECT"))   // the normalized key is the Annex C fold (PB1402)
        {
            StandardMethodCrossing.Check("FactoryObject", args, returning);
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
public abstract class BASE__FACTORY : CobolObject
{
    /// <summary>Allocate and initialize one instance of this factory's class — §16.2.1.2 GR1's "allocates storage
    /// for an object, initializes its instance data in accordance with 14.6.2.4". Every emitted BASE-derived factory
    /// overrides it covariantly with <c>new C()</c> (the generated constructor IS the initialization, OO deep-dive
    /// D4), so New invoked on a subclass's factory — or through SELF in an inherited factory method — creates the
    /// runtime factory's class.</summary>
    protected abstract BASE __Create();

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
        if (CobolNames.Same(name, "NEW"))   // the normalized key is the Annex C fold (PB1402)
        {
            StandardMethodCrossing.Check("New", args, returning);
            returning!.Value = __New();
            return;
        }
        base.__CobolInvoke(name, args, returning);
    }
}

/// <summary>The §14.9.23.4 GR7c runtime conformance check for a universal invocation of a BASE method. Both §16.2
/// methods take no parameters and return an object reference described ACTIVE-CLASS, so an argument, a missing
/// RETURNING, or a RETURNING item that is not an object reference does not conform. Whether the returned object's
/// CLASS conforms to a typed receiving item is a question only the object can answer — the receiver's class is
/// the one the activator named — so the caller's delivery asks it (<see cref="CobolObject.NarrowUniversal{T}"/>).
/// The EC-OO-UNIVERSAL half is gated on the activator's checking (§14.9.23.4 GR7c "enabled in both" — BASE's
/// methods carry no &gt;&gt;TURN of their own, so the activator's state decides); when it is off the crossing still
/// cannot proceed into typed code and stops through <see cref="CobolImplementorFatalException"/>, the same two-arm
/// stop every emitted case renders.</summary>
internal static class StandardMethodCrossing
{
    /// <summary>The descriptor prefix of every object-reference description (the compiler's
    /// <c>OoConformance.ConformanceDescriptor</c>: <c>"O:" + ObjectRefDescriptor.SignatureKey</c>).</summary>
    private const string ObjectDescriptorPrefix = "O:";

    public static void Check(string method, CobolInvokeArg[] args, CobolInvokeArg? returning)
    {
        string? problem =
            args.Length != 0 ? $"{args.Length} argument(s) for 0 formal(s) (ISO §14.9.23.4 GR7c/§14.8.2.1)"
            : returning is null ? "the method returns an object reference but no RETURNING is specified (ISO §14.9.23.4 GR7c/§14.8.3)"
            : !returning.Descriptor.StartsWith(ObjectDescriptorPrefix, StringComparison.Ordinal)
                ? $"the RETURNING item ({returning.Descriptor}) is not an object reference (ISO §14.9.23.4 GR7c/§14.8.3)"
            : null;
        if (problem is null) return;
        string detail = $"INVOKE 'BASE' '{method}': {problem}";
        if (ExceptionState.OoUniversalChecking) throw new CobolFatalException("EC-OO-UNIVERSAL", detail);
        throw new CobolImplementorFatalException(detail);
    }
}
