// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Compiler.Oo;
using CobolNet.Runtime;

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
/// <see cref="OoConformance.SignatureDifference"/> — the ONE pair-mode relation over two descriptions
/// (<c>ActivationRelations.SignatureDifference</c>) that the override, IMPLEMENTS, interface and property-accessor
/// checks ask too, so a description rule cannot mean one thing here and another there (kb/Work PB3101) — and, for an
/// alphanumeric group, through its subordinate entries as well (<c>SameDescription</c>).</para>
/// <para><b>An UNRESOLVED prototype conforms.</b> A prototype whose signature is null took ISO §12.3.8.4 GR10 c)
/// — "the details are taken from the external repository" — and this implementation's external repository is the
/// RUN UNIT's registry, resolved at execution. There is nothing to compare at compile time, and answering
/// "mismatch" would reject legal source for a separately-compiled target; the run-time screen
/// (§14.9.39.4 GR14 → <c>ProgramTable.FunctionSignatureMatches</c>) is what covers that case.</para>
/// </summary>
internal static class PrototypeSignatures
{
    /// <summary>True when the two prototypes have the same signature (see the type remarks for the derivation).
    /// Either side being null — an unresolved / separately-compiled prototype — conforms. A formal that is itself a
    /// restricted program- or function-pointer is compared by its TYPE, the signature class its prototype belongs to
    /// (<see cref="RestrictionIdentity"/>), which its description carries; while the classes are being computed that is
    /// the class of the current round of <see cref="PrototypeSignatureClasses"/>' partition refinement, so two prototypes
    /// that name each other or themselves compare by what they describe and never by what they are called (kb/Work
    /// PB2495, PB3101).</summary>
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
    /// <item>the item's OWN description — <see cref="OoConformance.SignatureDifference"/>, the ONE pair-mode relation
    /// (class and category, the clause identity, ANY LENGTH and the LOCALE identification, the object-reference phrases,
    /// the strong type, a variable-length group's matching information; a bit / national group is the elementary
    /// <c>PIC 1(m)</c> / <c>N(m)</c> item §13.18.29.4 GR1 b) / GR2 b) treat it as, and an alphanumeric group is never
    /// an elementary item);</item>
    /// <item>for two groups that are NOT bit / national groups, their SUBORDINATE entries (kb/Work PB1115, row
    /// SR-13.7.3-2): such a group's data description entry carries no PICTURE, USAGE or length of its own, so what
    /// describes it is the entries subordinate to it.
    /// Before this half, <c>01 L-G. 05 PIC 9(2). 05 PIC X(2).</c> in a prototype and
    /// <c>01 L-G. 05 PIC X(2). 05 PIC 9(2).</c> in its definition compared equal by WIDTH alone, and the caller —
    /// checked against the prototype — handed the definition a group it reads with the fields swapped. The
    /// structure is compared by §8.5.3.1's essential characteristics
    /// (<see cref="StrongTypeModel.SameElementaryLayout"/>: each elementary item at the same relative position,
    /// of the same length, with the same eight clauses), the standard's own statement of when two group
    /// descriptions describe the same data. A bit or national group is never asked it, opposite an elementary item OR
    /// another bit / national group: §13.18.29.4 GR1 b) / GR2 b) make it the elementary <c>PIC 1(m)</c> /
    /// <c>N(m)</c> item and §13.7.3 states nothing otherwise, so the first half's elementary pair is its whole
    /// description. ⛔ That is also what keeps this relation an EQUIVALENCE, which "the same signature" is (§13.18.60.4
    /// GR25 types a restricted pointer by it) and which <see cref="PrototypeSignatureClasses"/>' founder-based
    /// refinement relies on: were two bit groups of one length still compared by their subordinate split, the
    /// elementary <c>PIC 1(8)</c> would equal both a 4+4 and a 2+6 bit group while those two differed, and a SET
    /// between the 4+4 and the 2+6 pointer would be refused although two SETs through a <c>PIC 1(8)</c> pointer
    /// carry the same address there (kb/Work PB3101, review finding R1-1).</item>
    /// </list></summary>
    private static bool SameDescription(DataItem a, DataItem b) =>
        OoConformance.SignatureDifference(a, b, "prototype's item", "definition's item") is null
        && (!DescribedBySubordinates(a) || !DescribedBySubordinates(b) || StrongTypeModel.SameElementaryLayout(a, b));

    /// <summary>A group whose description is its subordinate entries' — the "is this a group operation?" test
    /// (<see cref="DataItem.IsAsIfElementary"/>'s remarks): a group that is not a bit / national group.</summary>
    private static bool DescribedBySubordinates(DataItem item) => item.IsGroup && !item.IsAsIfElementary;

    /// <summary>⛔ THE TYPE OF A RESTRICTED PROGRAM- OR FUNCTION-POINTER ITEM (kb/Work PB2464) — the one reader of
    /// <see cref="DataItem.PointerRestrictionIdentity"/>: <c>TO name</c> of the signature class
    /// (<see cref="PrototypeSignatureClasses"/>) the item's prototype belongs to, so two pointers restricted to
    /// differently named prototypes of ONE signature have equal identities (ISO §13.18.60.4 GR25 / GR26 type the pointer
    /// by that signature; §14.8.2.3.2 "of the same type"). For a prototype the unit's tables do not hold, and for one
    /// with no compile-time signature, the identity is the prototype's own name (<see cref="UnclassifiedIdentity"/>).
    /// Null for an unrestricted pointer and for any other item. Every relation that asks whether two restricted
    /// prototype-pointers are of the same type reads THIS: §8.5.3.1's type-declaration equivalence, through
    /// <see cref="SameType"/>, and, through <c>ActivationDescriptions</c>, the §9.3.6 match, the §14.8.2 / §14.8.3
    /// conformance, the pair-mode signature relation and the SET validity of a universal INVOKE.</summary>
    public static string? RestrictionIdentity(DataItem pointer) =>
        pointer.PointerRestrictionIdentity ?? UnclassifiedIdentity(pointer);

    /// <summary>⛔ THE ONE "of the same type" RELATION OVER TWO PROGRAM- OR FUNCTION-POINTER ITEMS (ISO §14.8.2.3.2: "If
    /// either is a restricted pointer, both shall be restricted and of the same type"; §13.18.60.4 GR25 / GR26 make the
    /// type the prototype's SIGNATURE): their <see cref="RestrictionIdentity"/> values are equal. While
    /// <see cref="PrototypeSignatureClasses"/> refines its partition each restricted pointer carries its prototype's
    /// CURRENT class, so this question asked inside the classification is the refinement's own question, and the greatest
    /// fixed point needs no assumption set (kb/Work PB2495, PB3101).</summary>
    public static bool SameType(DataItem a, DataItem b) =>
        string.Equals(RestrictionIdentity(a), RestrictionIdentity(b), StringComparison.Ordinal);

    /// <summary>The identity of a restricted pointer whose prototype is a signature class of its own: <c>TO</c> the
    /// prototype's externalized name when the unit's tables resolved it, else <c>TO</c> the restriction's own word —
    /// the identity <see cref="PrototypeSignatureClasses"/> gives a prototype with no compile-time signature. Null for
    /// an item that is not a restricted program- or function-pointer.</summary>
    private static string? UnclassifiedIdentity(DataItem pointer) =>
        pointer.Pic is { RestrictedPrototypeName: { } name }
            ? OwnIdentity(pointer.RestrictedPrototype?.ExternalizedName ?? name)
            : null;

    /// <summary>The identity of a prototype that is a signature class of its own — <c>TO name</c> (folded), the spelling
    /// of a pointer's USAGE clause.</summary>
    public static string OwnIdentity(string prototypeName) => "TO " + CobolNames.Fold(prototypeName);

    /// <summary>The number of positional USING formals a prototype declares — the RUN-TIME screen's granularity
    /// (ISO §14.9.39.4 GR14 via <c>ProgramTable.FunctionSignatureMatches</c>, which can see only the run-unit
    /// registry's <c>FormalCount</c>). Zero when the prototype is unresolved, which is also what a zero-formal
    /// unit registers.</summary>
    public static int FormalCount(CalleeSignature? s) => s?.Formals.Count ?? 0;
}
