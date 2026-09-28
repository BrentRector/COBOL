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
}
