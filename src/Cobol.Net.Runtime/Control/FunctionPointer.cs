// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The function-pointer carrier (ISO §13.18.60.4 GR26 / §8.5.2.x category function-pointer; kb/Work PB452):
/// "A data description entry that specifies the USAGE FUNCTION-POINTER clause specifies a function-pointer data
/// item, also called a function-pointer, that may contain the address of a function." GR26 leaves alignment,
/// size and representation implementor-defined — this implementation's representation is the function's
/// EXTERNALIZED function-name, which is exactly what §8.4.3.12.4 GR2 makes the address of a COBOL function
/// ("For a COBOL function, the address is that of the function identified by the externalized function-name in
/// its FUNCTION-ID paragraph"), resolved and activated through the ONE run-unit <see cref="ProgramTable"/>
/// exactly as a program-pointer is (the singular-pattern rule; never a second lookup path). The default value
/// IS the predefined NULL address (§8.4.3.10.4 GR2 — no function has a null name).
/// <para>⛔ THE PROGRAM-POINTER TWIN, DELIBERATELY A SEPARATE TYPE. §8.4.3.10.4 keeps the four predefined NULL
/// addresses apart by category, §14.9.39.3 SR20/SR21 forbid mixing the two categories in one SET, and
/// §8.8.4.2.16 compares pointers only within a category — so one shared struct would make a program-pointer and
/// a function-pointer assignment-compatible in the GENERATED C#, which is the one place the binder's category
/// screens cannot reach. The behaviour is the ProgramPointer behaviour and the shape is deliberately identical.</para>
/// <para>The DOCUMENTED representation (Annex A.1 item 210 — a REQUIRED implementor definition; see
/// <c>docs/CONFORMANCE.md</c> §7 row DOC-A.1-210) is: alignment — none, the item rides a managed slot and
/// occupies no character positions; size — the 8-byte managed reference width the storage model reserves for
/// every class-pointer leaf; representation — this externalized-name identity; allowable languages — COBOL only
/// (a non-COBOL function has no run-unit registration to resolve, so §8.4.3.12.4 GR4's EC-FUNCTION-NOT-FOUND is
/// the answer for one).</para>
/// </summary>
public readonly record struct FunctionPointer(string? Name)
{
    /// <summary>The NULL function address (ISO §8.4.3.10.4 GR2 — GR3 is the PROGRAM twin).</summary>
    public static readonly FunctionPointer Null = default;

    /// <summary>True when this pointer holds the NULL function address.</summary>
    public bool IsNull => Name is null;

    /// <summary>Pointer equality (ISO §8.8.4.2.16 — two function-pointers are equal when they identify the same
    /// function, or are both NULL). Function-name identity is case-insensitive (§8.3.2.2).</summary>
    public static bool SameTarget(FunctionPointer a, FunctionPointer b) =>
        ExternalizedNames.Same(a.Name, b.Name);
}
