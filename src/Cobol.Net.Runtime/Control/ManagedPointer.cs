// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The ONE managed-reference carrier base (COBOLNET_INTERPROGRAM_DESIGN D1). Untyped view for the opaque ABI;
/// the typed accessor lives on <see cref="ManagedPointer{T}"/>. <see cref="Null"/> is the NULL pointer state
/// (an OMITTED argument, an unset data-pointer, a freed BASED item).
/// </summary>
public abstract class ManagedPointer
{
    /// <summary>The NULL carrier (OMITTED argument / NULL pointer — ISO §14.9.4.4 GR11–12).</summary>
    public static readonly ManagedPointer Null = new NullManagedPointer();

    /// <summary>Store <paramref name="value"/> into the referenced storage when the carried type can hold it — the
    /// type-erased face of <see cref="ManagedPointer{T}.Value"/>, for a delivery whose sender and receiver are
    /// described differently but conform (kb/Work PB1164: §14.8.3.3 rule 1 — an object reference of a class conforms
    /// to a receiver of a superclass or a universal one "as if a SET statement were performed", so the sender's
    /// carrier type is a SUBCLASS of the receiver's). False when it cannot — a pair no conformance rule admits.</summary>
    internal virtual bool TryAssign(object? value) => false;

    /// <summary>True for the NULL carrier.</summary>
    public virtual bool IsNull => false;

    /// <summary>Data-pointer equality (ISO §8.8.4.2.16 / §8.8.4.2 :9772 — "equal if they reference the same
    /// address": STRUCTURAL over (storage cell, byte offset) for window pointers; two NULLs are equal; a
    /// legacy accessor carrier compares by instance (the CALL-ABI closures carry no address identity).</summary>
    public static bool SameTarget(ManagedPointer? a, ManagedPointer? b)
    {
        if (a is null || a.IsNull) return b is null || b.IsNull;
        if (b is null || b.IsNull) return false;
        if (a is CellPointer wa && b is CellPointer wb)
            return ReferenceEquals(wa.Cell, wb.Cell) && wa.Offset == wb.Offset;
        return ReferenceEquals(a, b);
    }

    /// <summary>A window pointer at <paramref name="offset"/> character positions into <paramref name="cell"/>
    /// (the ADDRESS OF / ALLOCATE value shape — increment 2).</summary>
    public static ManagedPointer At(StorageCell cell, long offset) => new CellPointer(cell, offset);

    private sealed class NullManagedPointer : ManagedPointer
    {
        public override bool IsNull => true;
    }
}

/// <summary>
/// The typed managed-reference carrier (design D1 — internally the typed <c>ManagedRef&lt;T&gt;</c>; the public
/// name <c>ManagedPointer</c> is kept, SSOT §18 #12). Two construction modes: <see cref="OverField"/> — an
/// accessor over the owner's NATIVE field (WORKING-STORAGE stays unboxed; only the genuine alias carries
/// indirection), and <see cref="Cell"/> — a standalone storage cell (BY CONTENT copies, literals, ALLOCATE).
/// </summary>
/// <typeparam name="T">The carried storage type — <see cref="long"/> for a native fixed-point item,
/// <see cref="string"/> for character storage (alphanumeric / edited / zoned image / whole-group image).</typeparam>
public sealed class ManagedPointer<T> : ManagedPointer
{
    private readonly Func<T> _get;
    private readonly Action<T> _set;

    private readonly bool _omittedArgument;

    private ManagedPointer(Func<T> get, Action<T> set, bool omittedArgument = false)
    {
        _get = get;
        _set = set;
        _omittedArgument = omittedArgument;
    }

    /// <summary>True for the omitted-argument carrier (ISO §14.9.4.4 GR11; kb/Work PB133 wave C): the
    /// §8.8.4.8 omitted-argument condition, <c>CobolArgAdapt.Present</c>, and GR1c's TRANSITIVE omission (an
    /// omitted formal forwarded as an argument) all read presence through this ONE test.</summary>
    public override bool IsNull => _omittedArgument;

    /// <summary>An omitted/absent CALL argument's typed carrier (kb/Work PB133 wave C) — IsNull answers true;
    /// the accessors carry the GR12 checked raise.</summary>
    internal static ManagedPointer<T> OmittedArgument(Func<T> get, Action<T> set) => new(get, set, omittedArgument: true);

    /// <summary>An accessor carrier over a native field: reads/writes go straight through to the owner's storage
    /// — the BY REFERENCE "same storage area" semantics (ISO §14.2.3 GR8) with zero indirection on the owner's
    /// own accesses.</summary>
    public static ManagedPointer<T> OverField(Func<T> get, Action<T> set) => new(get, set);

    /// <summary>A standalone storage cell seeded with <paramref name="initial"/> — the BY CONTENT copy
    /// (ISO §14.2.3 GR9: "a record allocated by the activating element"). NOT the ALLOCATE backing — dynamic
    /// storage lives in <see cref="StorageCell"/> behind a <see cref="CellPointer"/> window (increment 2:
    /// closures carry no address identity for §8.8.4.2 structural equality and no byte offset for F10).</summary>
    public static ManagedPointer<T> Cell(T initial)
    {
        var box = new T[1] { initial };
        return new(() => box[0], v => box[0] = v);
    }

    internal override bool TryAssign(object? value)
    {
        if (value is T typed) { Value = typed; return true; }
        if (value is null && default(T) is null) { Value = default!; return true; }
        return false;
    }

    /// <summary>The referenced storage's current value (get) / store into the referenced storage (set).</summary>
    public T Value
    {
        get => _get();
        set => _set(value);
    }
}
