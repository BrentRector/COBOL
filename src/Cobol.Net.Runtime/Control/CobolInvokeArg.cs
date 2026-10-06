// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace CobolNet.Runtime;

/// <summary>
/// One argument (or the RETURNING slot) crossing a UNIVERSAL object-reference dispatch
/// (<see cref="CobolObject.__CobolInvoke"/>; OO deep-dive D10/D-U2). The <see cref="Description"/> is the caller's
/// description of the operand (<see cref="ActivationDescription"/>, built by the compiler's ONE builder): the
/// generated callee switch asks <see cref="ActivationRelations"/> whether it MATCHES the method's formal (ISO §9.3.6 —
/// resolution continues and ends in EC-OO-METHOD when it does not) and then whether the bound method CONFORMS
/// (§14.9.23.4 GR7 c) — EC-OO-UNIVERSAL when it does not; conformance through a universal receiver is checked AT
/// RUNTIME, §9.3.8.2.1 NOTE). <see cref="Value"/> is the argument in its canonical box form — everything crossing a
/// universal dispatch is implicitly BY REFERENCE (§14.9.23.3 SR6), and ISO §14.2.3 GR8 makes the formal occupy "the same
/// storage area as the argument", so an identifier argument's box is LIVE (kb/Work PB2087): its accessors read and write
/// the argument's own storage on every access, the callee's formal is a converting view over it, and its
/// <see cref="Area"/> is the cell an area formal is laid over. A box built from a value (a RETURNING slot) holds it.
/// </summary>
public sealed class CobolInvokeArg(ActivationDescription description, object? value = null, bool omitted = false)
{
    private object? _value = value;
    private readonly Func<object?>? _get;
    private readonly Action<object?>? _set;

    /// <summary>A LIVE identifier argument (kb/Work PB2087): <paramref name="get"/> / <paramref name="set"/> read and write
    /// the argument's own storage in its canonical box form, and <paramref name="area"/> is the cell it lives in.</summary>
    public CobolInvokeArg(ActivationDescription description, Func<object?> get, Action<object?> set, CellPointer? area)
        : this(description)
    {
        _get = get;
        _set = set;
        Area = area;
    }

    /// <summary>The storage area the argument occupies when it lives in a cell (ISO §14.2.3 GR8 — an area formal is laid
    /// over it), else null.</summary>
    public CellPointer? Area { get; }

    /// <summary>A spelled OMITTED argument (kb/Work PB757): it has no description (§14.9.23.2), and §9.3.6 match rule
    /// 3 b) admits it only against an OPTIONAL formal, "and this parameter is considered to match exactly".</summary>
    public static CobolInvokeArg OmittedArgument() => new(ActivationDescription.Omitted, null, omitted: true);

    /// <summary>A REFERENCE-MODIFIED argument: the unique data item ISO §8.4.3.3.4 GR5 creates, whose length is the
    /// EVALUATED length (GR5 c) "The evaluation of length specifies the number of bit positions or character positions
    /// of the data item"), so its description carries the evaluated slice's own positions — a non-literal reference
    /// modifier's length is known only here, and §14.8.2.2 rule 1 compares it with the formal's.</summary>
    public static CobolInvokeArg ReferenceModified(ActivationDescription description, Func<object?> get, Action<object?> set,
        CellPointer? area) =>
        new(description with { Positions = ((string?)get())?.Length ?? 0 }, get, set, area);

    /// <summary>The caller's description of the operand.</summary>
    public ActivationDescription Description { get; } = description;

    /// <summary>The argument in its canonical box form — read from and written to the argument's own storage when the
    /// box is live.</summary>
    public object? Value
    {
        get => _get is null ? _value : _get();
        set
        {
            if (_set is null) _value = value;
            else _set(value);
        }
    }

    /// <summary>True when the omitted-argument condition for the corresponding formal shall be true in the
    /// invoked method (ISO §14.9.23.4 GR9; §8.8.4.8.4 GR1) — a spelled OMITTED argument, or an identifier that
    /// is itself a formal parameter for which that condition is true (GR1 c), whose description still conforms.</summary>
    public bool Omitted { get; } = omitted;
}

/// <summary>
/// The callee side of a GROUP crossing a universal dispatch whose two descriptions are not the same shape — the
/// carriers ISO §14.8.2.2 admits once <see cref="ActivationRelations"/> has decided the pair conforms (kb/Work PB480):
/// <list type="bullet">
/// <item>rule 1's PREFIX: an alphanumeric group formal no larger than the argument sees the argument's leading
/// character positions, and its stores reach only those (§14.2.3 GR8 — "as if the formal parameter occupies the same
/// storage area as the argument"), so the write-back splices the formal's image over the argument's leading
/// positions and keeps the tail;</item>
/// <item>§8.5.1.12's fixed / variable-length pair: one side crosses as the variable-length carrier
/// (<see cref="CobolVarGroup"/>), the other as a fixed record image, converted through the pair's positional
/// correspondence (<see cref="CobolVarGroup.CorrespondingSpans"/>) exactly as the CALL boundary converts them.</item>
/// </list>
/// The caller's box is always in the ARGUMENT's own form, so a caller needs nothing of the method it reaches.
/// </summary>
public static class UniversalGroupCarrier
{
    /// <summary>The fixed record image a FIXED-length group formal of <paramref name="formal"/> sees of the argument
    /// box <paramref name="box"/> (described by <paramref name="argument"/>).</summary>
    public static string FixedImage(object? box, ActivationDescription argument, ActivationDescription formal) =>
        box is CobolVarGroup v
            ? CobolVarGroup.ToFixedImage(v, formal.Positions, Spans(formal, argument))
            : CobolString.Store((string)box!, formal.Positions);

    /// <summary>The argument box after the FIXED-length group formal's image <paramref name="image"/> is stored back
    /// over it: the formal's positions replace the argument's leading ones and the rest survives.</summary>
    public static object WriteBackFixed(object? box, ActivationDescription argument, ActivationDescription formal,
        string image)
    {
        if (box is CobolVarGroup v) return CobolVarGroup.OverlayFixedImage(v, image, Spans(formal, argument));
        string current = (string)box!;
        return image.Length >= current.Length ? image[..current.Length] : image + current[image.Length..];
    }

    /// <summary>The variable-length carrier a VARIABLE-length group formal sees of the argument box.</summary>
    public static CobolVarGroup VariableCarrier(object? box, ActivationDescription argument,
        ActivationDescription formal) =>
        box as CobolVarGroup ?? CobolVarGroup.FromFixedImage((string)box!, Spans(argument, formal));

    /// <summary>The argument box after the VARIABLE-length group formal's carrier <paramref name="carrier"/> is stored
    /// back: a variable-length argument takes the carrier, a fixed one the record image it rebuilds.</summary>
    public static object WriteBackVariable(object? box, ActivationDescription argument, ActivationDescription formal,
        CobolVarGroup carrier) =>
        box is CobolVarGroup ? carrier
        : CobolVarGroup.ToFixedImage(carrier, ((string)box!).Length, Spans(argument, formal));

    /// <summary>A group RETURNING value <paramref name="value"/> (the method's item, described by
    /// <paramref name="sending"/>) in the form of the invocation's receiving item (<paramref name="receiving"/>) — the
    /// fixed / variable-length pair §14.8.3.2 admits subject to §8.5.1.12 compatibility; every other pair already has the
    /// receiving item's form.</summary>
    public static object? Deliver(object? value, ActivationDescription receiving, ActivationDescription sending) =>
        value switch
        {
            CobolVarGroup v when receiving.Shape is ActivationShape.AlphanumericGroup =>
                CobolVarGroup.ToFixedImage(v, receiving.Positions, Spans(receiving, sending)),
            string s when receiving.Shape is ActivationShape.VariableLengthGroup =>
                CobolVarGroup.FromFixedImage(s, Spans(sending, receiving)),
            _ => value,
        };

    /// <summary>The spans of the FIXED side's tables that correspond to the VARIABLE side's dynamic items — never
    /// null here: the conformance relation proved the correspondence before any carrier is built.</summary>
    private static int[] Spans(ActivationDescription fixedSide, ActivationDescription varSide) =>
        CobolVarGroup.CorrespondingSpans(fixedSide.Layout ?? CobolVarGroup.FixedRun(fixedSide.Positions), varSide.Layout!)
        ?? [];
}
