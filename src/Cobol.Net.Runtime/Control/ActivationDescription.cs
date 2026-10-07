// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace CobolNet.Runtime;

/// <summary>Which shape of data description an <see cref="ActivationDescription"/> describes — the arm of the two
/// relations (<see cref="ActivationRelations"/>) that decides it.</summary>
public enum ActivationShape
{
    /// <summary>A spelled OMITTED argument (ISO §14.9.23.2): it has no description, and §9.3.6 match rule 3 b)
    /// admits it against an OPTIONAL formal only.</summary>
    Omitted,
    /// <summary>An elementary item that is not an object reference (every PICTURE category, the class-pointer
    /// usages).</summary>
    Elementary,
    /// <summary>An object reference (§13.18.60.2) — <see cref="ActivationDescription.ObjectKind"/> and its phrases.</summary>
    ObjectReference,
    /// <summary>A fixed-length alphanumeric group item that is not strongly typed.</summary>
    AlphanumericGroup,
    /// <summary>A bit group or national group, "treated as an elementary item" at the activation boundary (ISO
    /// §14.8.2.1 / §14.8.3.1 NOTE).</summary>
    AsIfElementaryGroup,
    /// <summary>A variable-length group (§8.5.1.12.1) that is not strongly typed.</summary>
    VariableLengthGroup,
    /// <summary>A strongly-typed group item (§8.5.3): its class and category are its type-name (§8.5.2.1).</summary>
    StrongGroup,
}

/// <summary>The §13.18.60.2 kind of an object-reference description.</summary>
public enum ObjectReferenceKind
{
    /// <summary>Not an object reference.</summary>
    None,
    /// <summary>A universal object reference (no class or interface phrase).</summary>
    Universal,
    /// <summary>Described with an interface-name.</summary>
    Interface,
    /// <summary>Described with an object-class-name.</summary>
    ObjectClass,
    /// <summary>Described with the ACTIVE-CLASS phrase.</summary>
    ActiveClass,
}

/// <summary>The ISO §8.5.2 class-and-category names an <see cref="ActivationDescription.Category"/> carries that the
/// relations themselves read (every other category name is compared only for equality). §8.5.2.1: "an alphanumeric
/// group item has class and category alphanumeric", a bit group boolean, a national group national.</summary>
public static class ActivationCategory
{
    /// <summary>Category alphanumeric — the elementary partner of an alphanumeric group (§14.8.2.2 / §14.8.3.2).</summary>
    public const string Alphanumeric = "alphanumeric";
    /// <summary>Class and category boolean.</summary>
    public const string Boolean = "boolean";
    /// <summary>Class and category national.</summary>
    public const string National = "national";
    /// <summary>Class object (an object reference).</summary>
    public const string Object = "object";
    /// <summary>Class pointer, category data-pointer.</summary>
    public const string DataPointer = "data-pointer";
    /// <summary>Class pointer, category program-pointer.</summary>
    public const string ProgramPointer = "program-pointer";
    /// <summary>Class pointer, category function-pointer.</summary>
    public const string FunctionPointer = "function-pointer";
    /// <summary>Class and category index (a USAGE INDEX item).</summary>
    public const string Index = "index";
}

/// <summary>
/// ⛔ THE DESCRIPTION OF ONE OPERAND OF AN ACTIVATION, as it crosses a UNIVERSAL object-reference dispatch (OO deep-dive
/// D10; kb/Work PB480 / PB1780). Through a universal receiver the method is chosen at run time (ISO §9.3.6: "it is the
/// class of the actual object referenced at runtime that is used in resolving a method invocation"), so both sides'
/// descriptions must travel to the generated <c>__CobolInvoke</c> switch, which asks the standard's TWO relations of
/// them — §9.3.6's MATCH (resolution; a failure is EC-OO-METHOD after the INHERITS walk) and §14.8.2 / §14.8.3's
/// CONFORMANCE (asked of the bound method by §14.9.23.4 GR7 c); a failure is EC-OO-UNIVERSAL). Both are in
/// <see cref="ActivationRelations"/>.
/// <para>It replaced a descriptor STRING compared for equality (<c>OoConformance.ConformanceDescriptor</c>), which
/// could answer only "the two strings are equal": that is neither relation. Match rule 3 e) is an identity of a CLAUSE
/// LIST (a group carries none of the clauses, so two groups match whatever their sizes and a group never matches a
/// <c>PIC X(n)</c>), and conformance is not identity (§14.8.2.2 rule 1 admits a SMALLER formal group, §8.5.1.12.3
/// admits a dynamic-capacity table opposite a fixed one). Every field here is read by one of the two relations
/// (<c>ActivationDescriptionFieldDriftTests</c>).</para>
/// <para>The compiler builds every description in ONE place (<c>ActivationDescriptions</c>) from the data
/// description entry; the generated code constructs it with an object initializer. It is a record so that a fact only
/// the run time knows — a reference-modified argument's evaluated length (<see cref="CobolInvokeArg.ReferenceModified"/>)
/// — is set with <c>with</c>, never by a second field-by-field copy.</para>
/// </summary>
public sealed record ActivationDescription
{
    /// <summary>The spelled OMITTED argument's description (kb/Work PB757).</summary>
    public static ActivationDescription Omitted { get; } = new() { Shape = ActivationShape.Omitted };

    /// <summary>Which arm of the relations decides this operand.</summary>
    public ActivationShape Shape { get; init; }

    /// <summary>ISO §9.3.6 match rule 3 c) "is the same class and category": the §8.5.2 class and category of the
    /// item. A strongly-typed group's is its type-name (§8.5.2.1), a bit / national group's is boolean / national,
    /// an alphanumeric group's alphanumeric.</summary>
    public string Category { get; init; } = "";

    /// <summary>ISO §9.3.6 match rule 3 e) — the identity of "the same ALIGNED, ANY LENGTH, BLANK WHEN ZERO, DYNAMIC
    /// LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE clauses", with rule 3 e) 1.'s currency strings and 2.'s
    /// DECIMAL-POINT IS COMMA state folded into the PICTURE part, as one canonical text (empty for a group, which
    /// carries none of the clauses). The class-pointer restriction (§14.8.2.3.2's class-pointer paragraph) is its
    /// USAGE clause's TO phrase and is part of it too. The LOCALE phrase's external identification is not an
    /// identity (<see cref="LocaleExternal"/>).</summary>
    public string Clauses { get; init; } = "";

    /// <summary>ISO §9.3.6 match rule 3 e) 3. b.: the external identification of the PICTURE clause's LOCALE phrase —
    /// the external-locale-name or literal value as written — or null for a LOCALE phrase without a locale-name and
    /// for a picture with no LOCALE phrase (the presence and SIZE of the phrase are in <see cref="Clauses"/>).</summary>
    public string? LocaleExternal { get; init; }

    /// <summary>True when <see cref="LocaleExternal"/> came from a literal (compared character for character) rather
    /// than an external-locale-name word (§8.1.3.2 GR3 a), compared without regard to case).</summary>
    public bool LocaleFromLiteral { get; init; }

    /// <summary>True when the item is described with the ANY LENGTH clause (§13.18.2) — §14.9.23.4 GR7 c) bars it from
    /// a method bound through a universal receiver.</summary>
    public bool AnyLength { get; init; }

    /// <summary>The item's CHARACTER positions: an alphanumeric group's image width (§14.8.2.2 rule 1 / §14.8.3.2 compare
    /// it), a bit / national group's boolean / national positions (§14.8.2.3.2 "Additionally" b) / c)). Zero for every
    /// other shape.</summary>
    public int Positions { get; init; }

    /// <summary>An elementary item's USAGE (the clause's name) — the half of §14.8.3.3 "Additionally" 2) / 3) that pairs
    /// a bit group with an elementary item of usage BIT and a national group with one of usage NATIONAL; for a bit /
    /// national group, the usage its GROUP-USAGE gives its positions.</summary>
    public string Usage { get; init; } = "";

    /// <summary>A strongly-typed group's §8.5.3.1 type identity — its type-name, STRONG and EXTERNAL presence and the
    /// position, length and clauses of every elementary item — so two descriptions name "the same type" exactly when
    /// these are equal (§14.8.2.2 / §14.8.3.2: "both shall be of the same type").</summary>
    public string? StrongType { get; init; }

    /// <summary>A group's ISO §8.5.1.12 layout as positional atoms (<see cref="GroupAtom"/>: REDEFINES subtrees dropped,
    /// subordinate groups flattened, tables whole with their element's atoms) — what the §8.5.1.12 compatibility walk
    /// (<see cref="GroupCompatibility.Walk"/>) compares and what the carrier conversions between two shapes of group read
    /// (<see cref="UniversalGroupCarrier"/>). Null for a non-group, and for a group with a USAGE BIT leaf (§8.5.1.6.3's
    /// shared-byte runs make a character position non-positional), which then stands as one run of
    /// <see cref="Positions"/>.</summary>
    public GroupAtom[]? Atoms { get; init; }

    /// <summary>An object reference's §13.18.60.2 kind.</summary>
    public ObjectReferenceKind ObjectKind { get; init; }

    /// <summary>The class-name or interface-name of an object reference (folded; null for universal and
    /// ACTIVE-CLASS).</summary>
    public string? ObjectName { get; init; }

    /// <summary>The FACTORY phrase of an object reference.</summary>
    public bool Factory { get; init; }

    /// <summary>The ONLY phrase of an object reference.</summary>
    public bool Only { get; init; }

    /// <summary>A FORMAL parameter described with the OPTIONAL phrase (§9.3.6 match rules 1 and 3 b)).</summary>
    public bool Optional { get; init; }

    /// <summary>A FORMAL parameter specified BY VALUE — never a match for a universal invocation, every argument of
    /// which is BY REFERENCE (§14.9.23.3 SR6; §9.3.6 match rule 3 a)).</summary>
    public bool ByValue { get; init; }

    /// <summary>How the description reads in an EC-OO-UNIVERSAL message.</summary>
    public override string ToString() => Shape switch
    {
        ActivationShape.Omitted => "OMITTED",
        ActivationShape.ObjectReference => ObjectKind switch
        {
            ObjectReferenceKind.Universal => "OBJECT REFERENCE",
            ObjectReferenceKind.Interface => $"OBJECT REFERENCE {ObjectName}",
            ObjectReferenceKind.ActiveClass => $"OBJECT REFERENCE {(Factory ? "FACTORY OF " : "")}ACTIVE-CLASS",
            _ => $"OBJECT REFERENCE {(Factory ? "FACTORY OF " : "")}{ObjectName}{(Only ? " ONLY" : "")}",
        },
        ActivationShape.AlphanumericGroup => $"an alphanumeric group of {Positions} character positions",
        ActivationShape.AsIfElementaryGroup => $"a {Category} group of {Positions} positions",
        ActivationShape.VariableLengthGroup => $"a variable-length group ({GroupCompatibility.Describe(Atoms ?? [])})",
        ActivationShape.StrongGroup => $"a strongly-typed group of type {Category}",
        _ => $"{Category} {Clauses}",
    };
}
