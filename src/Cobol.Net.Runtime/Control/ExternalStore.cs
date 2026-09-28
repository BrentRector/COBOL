// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The static facade over the run unit's <see cref="ExternalTable"/> (the emitted surface — generated programs
/// call <c>ExternalStore.Cell(name, image)</c>; kept name-stable pre-G8). Forwards to
/// <c>RunUnit.Current.External</c>.
/// </summary>
public static class ExternalStore
{
    /// <inheritdoc cref="ExternalTable.Cell(string, string)"/>
    public static StorageCell Cell(string name, string initialImage) => RunUnit.Current.External.Cell(name, initialImage);

    /// <inheritdoc cref="ExternalTable.Cell(string, Func{StorageCell})"/>
    public static StorageCell Cell(string name, Func<StorageCell> create) => RunUnit.Current.External.Cell(name, create);

    /// <summary>Register one external description at an activation entry and run the §14.8.4 conformance check.
    /// The gate realizes §14.8.4.1's both-elements rule: the ACTIVATING element's mask (latched by the activation
    /// boundary into <c>ActivatorExternalMask</c>) ANDed with <paramref name="selfMask"/>, the activated element's
    /// own before-Environment-division TURN mask — each EC-EXTERNAL condition pairs independently, bitwise.</summary>
    public static void Describe(string describer, string name, ExternalDescriptor desc, int selfMask)
        => RunUnit.Current.External.Describe(describer, name, desc,
            (ExternalChecks)(Exceptions.ExceptionState.ActivatorExternalMask & selfMask));

    /// <summary>⛔ THE METHOD ACTIVATION BOUNDARY's external-item check (ISO §14.9.23.4 GR7 d); kb/Work PB1138) — the INVOKE
    /// twin of <c>ProgramTable.CallProgram</c>'s GR3e step, run by a generated method's prologue BEFORE control is
    /// transferred to it (GR7 e). A method has no external items of its own (§13.4.3 SR1 keeps a FILE SECTION out of a
    /// method, and a method WORKING-STORAGE EXTERNAL item is refused), so <paramref name="describe"/> registers the
    /// external items of the factory or instance definition that contains it — the ones its statements reference.
    /// §14.8.4.1's pair: the ACTIVATING half is the INVOKE statement guard's checking flags
    /// (<c>ExceptionEngine.ExternalActivatingMask</c>), latched as the activator mask for the registrations, and the
    /// activated method's half is <paramref name="selfMask"/>, folded at bind time before the method's Environment
    /// division. A detected violation is "the method invocation is not successful" and GR7 g)'s exception processing:
    /// the condition leaves as a <see cref="Exceptions.CobolFatalException"/> carrying its Table 13 name, which the
    /// INVOKE statement's guard selects on exactly as it does EC-OO-METHOD and EC-OO-UNIVERSAL (an INVOKE has no
    /// ON EXCEPTION phrase, so the CALL boundary's <see cref="CobolCallException"/> partition does not apply).</summary>
    public static void DescribeAtMethodActivation(Action<int> describe, int selfMask)
    {
        var exc = RunUnit.Current.Exceptions;
        int saved = exc.ActivatorExternalMask;
        exc.ActivatorExternalMask = exc.ExternalActivatingMask;
        try { describe(selfMask); }
        catch (CobolCallException x) { throw new Exceptions.CobolFatalException(x.EcName, x.Message); }
        finally { exc.ActivatorExternalMask = saved; }
    }
}
