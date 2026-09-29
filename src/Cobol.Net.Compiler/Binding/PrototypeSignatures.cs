// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Compiler.Oo;

namespace CobolNet.Binding;

/// <summary>
/// ⛔ THE ONE "same signature" TEST over two prototypes (kb/Work PB817). The standard writes that sentence
/// TWICE, in adjacent syntax rules of one statement, over two carriers:
/// <list type="bullet">
/// <item>ISO §14.9.39.3 SR20 — "The function-prototypes associated with identifier-12 and identifier-13 shall
/// have the same signature." (SET Format 8, function-pointer-assignment.)</item>
/// <item>ISO §14.9.39.3 SR22 — "If identifier-7 references a restricted program-pointer, identifier-8 shall be
/// the predefined address NULL or shall reference a program-pointer and the program-prototypes associated with
/// identifier-7 and identifier-8 shall have the same signature." (SET Format 9.)</item>
/// </list>
/// The only difference is which namespace resolves the prototype NAME, which the caller has already done, so
/// the RULE is written here once. It is also the rule §13.18.60.4 GR25/GR26 state as the carriers' standing
/// invariant, and §14.8.2.3.2's "if either is a restricted pointer, both shall be restricted and of the same
/// type" screens the argument-passing edge of it.
/// <para><b>What "the same signature" means here, derived.</b> The standard's §3 terms do not define the word;
/// Annex D.9.3.2 does, informatively — "all explicit or implicit prototype information that exists for the
/// specified function is applicable to the function identified by the address". The prototype information a
/// COBOL prototype carries is its PROCEDURE DIVISION header: the positional USING formals (their descriptions
/// and their passing modes, §14.8.2) and the RETURNING item (§14.8.3, whose §14.8.3.1 makes its very presence a
/// conformance fact — "if and only if"). So this compares exactly those, position by position, through
/// <see cref="OoConformance.DescriptionMismatch"/> — the ONE description-equality check the whole compiler
/// already shares with the INVOKE / CALL conformance screens, so a description rule cannot mean one thing here
/// and another there — and, for a group, through its subordinate entries as well (<c>SameDescription</c>).</para>
/// <para><b>An UNRESOLVED prototype conforms.</b> A prototype whose signature is null took ISO §12.3.8.4 GR10 c)
/// — "the details are taken from the external repository" — and this implementation's external repository is the
/// RUN UNIT's registry, resolved at execution. There is nothing to compare at compile time, and answering
/// "mismatch" would reject legal source for a separately-compiled target; the run-time screen
/// (§14.9.39.4 GR14 → <c>ProgramTable.FunctionSignatureMatches</c>) is what covers that case.</para>
/// </summary>
internal static class PrototypeSignatures
{
    /// <summary>True when the two prototypes have the same signature (see the type remarks for the derivation).
    /// Either side being null — an unresolved / separately-compiled prototype — conforms.</summary>
    public static bool Same(CalleeSignature? a, CalleeSignature? b)
    {
        if (a is null || b is null) return true;   // §12.3.8.4 GR10 c) — resolved by the run unit, not here
        if (a.Formals.Count != b.Formals.Count) return false;
        for (int i = 0; i < a.Formals.Count; i++)
        {
            var (fa, fb) = (a.Formals[i], b.Formals[i]);
            // §14.8.2's positional correspondence carries the MODE as well as the description: a BY VALUE formal
            // and a BY REFERENCE formal of identical description are different prototype information (§14.2.3
            // GR10 copies one and aliases the other), and OPTIONAL changes §14.8.2.1's required count.
            if (fa.ByValue != fb.ByValue || fa.Optional != fb.Optional) return false;
            if (!SameDescription(fa.Item, fb.Item)) return false;
        }
        // §14.8.3.1 — a returning item is present "if and only if"; presence is half the fact, description the other.
        if ((a.Returning is null) != (b.Returning is null)) return false;
        return a.Returning is null || SameDescription(a.Returning, b.Returning!);
    }

    /// <summary>ISO §13.7.3 SR2 — a prototype's formal parameter or returning item "shall match the description of
    /// the formal parameters and the returning item in the corresponding function definition or program
    /// definition", the description §8.13 stores as the signature's "description of the parameters". Two halves:
    /// <list type="bullet">
    /// <item>the item's OWN description — <see cref="OoConformance.DescriptionMismatch"/> in pair mode, the ONE
    /// identical-description check (category, clauses, PICTURE identity, strong type, variable-length shape,
    /// width);</item>
    /// <item>for a GROUP, its SUBORDINATE entries (kb/Work PB1115, row SR-13.7.3-2): a group's data description
    /// entry carries no PICTURE, USAGE or length of its own, so what describes it is the entries subordinate to
    /// it. Before this half, <c>01 L-G. 05 PIC 9(2). 05 PIC X(2).</c> in a prototype and
    /// <c>01 L-G. 05 PIC X(2). 05 PIC 9(2).</c> in its definition compared equal by WIDTH alone, and the caller —
    /// checked against the prototype — handed the definition a group it reads with the fields swapped. The
    /// structure is compared by §8.5.3.1's essential characteristics
    /// (<see cref="StrongTypeModel.SameElementaryLayout"/>: each elementary item at the same relative position,
    /// of the same length, with the same eight clauses), the standard's own statement of when two group
    /// descriptions describe the same data — so a group matches only a group, never an elementary item of equal
    /// width.</item>
    /// </list></summary>
    private static bool SameDescription(DataItem a, DataItem b) =>
        OoConformance.DescriptionMismatch(a, b) is null
        && (a.IsGroup == b.IsGroup)
        && (!a.IsGroup || StrongTypeModel.SameElementaryLayout(a, b));

    /// <summary>The number of positional USING formals a prototype declares — the RUN-TIME screen's granularity
    /// (ISO §14.9.39.4 GR14 via <c>ProgramTable.FunctionSignatureMatches</c>, which can see only the run-unit
    /// registry's <c>FormalCount</c>). Zero when the prototype is unresolved, which is also what a zero-formal
    /// unit registers.</summary>
    public static int FormalCount(CalleeSignature? s) => s?.Formals.Count ?? 0;
}
