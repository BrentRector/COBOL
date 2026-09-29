// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The program-pointer carrier (ISO §13.18.60 GR24 / §8.5.2.15; P10 Step 7): "may contain the address of a
/// program … For a COBOL program, the address is for an outermost program." GR24 leaves alignment, size, and
/// representation implementor-defined — this implementation's representation is the OUTERMOST program's
/// externalized identity (its per-run-unit-unique PROGRAM-ID name, §8.4.6.3 rule 4), resolved and activated
/// through the ONE run-unit <see cref="ProgramTable"/> exactly as a CALL-by-name is (the singular-pattern
/// rule; never a second lookup path). The default value IS the predefined NULL program address (§8.4.3.10
/// GR3 — "guaranteed not to represent the address of any program": no program has a null name).
/// </summary>
public readonly record struct ProgramPointer(string? Name)
{
    /// <summary>The NULL program address (ISO §8.4.3.10 GR3).</summary>
    public static readonly ProgramPointer Null = default;

    /// <summary>True when this pointer holds the NULL program address.</summary>
    public bool IsNull => Name is null;

    /// <summary>Pointer equality (ISO §8.8.4.2.16 — two program-pointers are equal when they identify the same
    /// program, or are both NULL). Program-name identity is case-insensitive (§8.3.2.2).</summary>
    public static bool SameTarget(ProgramPointer a, ProgramPointer b) =>
        ExternalizedNames.Same(a.Name, b.Name);
}
