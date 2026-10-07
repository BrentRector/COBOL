// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Runtime;

namespace CobolNet.Compiler.Oo;

/// <summary>
/// ⛔ THE ONE BUILDER OF <see cref="ActivationDescription"/> (kb/Work PB480 / PB1780): the description of an operand
/// crossing a UNIVERSAL object-reference dispatch, built from its data description entry for both sides — the caller's
/// argument and RETURNING item (<c>OoBinder.OoBindUniversalInvoke</c>) and the method's formals and returning item
/// (<c>OoEmitter.EmitCobolInvokeCase</c>) — so the two descriptions a run-time relation compares were derived by the
/// same code. The relations themselves (§9.3.6 MATCH, §14.8.2 / §14.8.3 CONFORMANCE) are
/// <see cref="ActivationRelations"/>, in the runtime.
/// <para>It replaced <c>OoConformance.ConformanceDescriptor</c>, a string compared for equality that could not express
/// either relation: a group keyed like a <c>PIC X(n)</c> of its width matched one (§9.3.6 3 e) says it does not), a
/// smaller formal group did not match a larger argument (§14.8.2.2 rule 1 admits it), a national, boolean or
/// numeric-edited operand had no key at all and was refused at bind (COBOLNET0866), and an ACTIVE-CLASS formal was keyed
/// by its containing class where §9.3.6 3 d) 4. asks a question of the argument's run-time object.</para>
/// </summary>
public static class ActivationDescriptions
{
    /// <summary>The description of <paramref name="item"/>, or null when the item has no universal crossing form — a
    /// group with neither a character image, a variable-length carrier nor (strongly typed) a leaf vector, or a
    /// PICTURE-less elementary item.</summary>
    public static ActivationDescription? Of(DataItem item)
    {
        if (StrongTypeModel.IsStrongGroup(item))
            // §8.5.2.1: "Both the class and the category of a strongly-typed group item are the type-name specified in
            // the TYPE clause" — so match rule 3 c) needs the same type-name, and §14.8.2.2 / §14.8.3.2's "both shall be
            // of the same type" is the §8.5.3.1 identity (StrongTypeModel.TypeIdentityKey).
            return item.IsImageCapable || item.CurrentExtentImageCapable || OoClassTable.LeafCarried(item)
                ? new ActivationDescription
                {
                    Shape = ActivationShape.StrongGroup,
                    Category = "type:" + CobolNames.Fold(StrongTypeModel.TypedItemType(item).Name ?? ""),
                    StrongType = StrongTypeModel.TypeIdentityKey(item),
                    Positions = item.IsImageCapable ? item.ImageWidth : 0,
                }
                : null;
        // A bit / national group is "treated as an elementary item" at the activation boundary (§14.8.2.1 / §14.8.3.1
        // NOTE), of class and category boolean / national (§8.5.2.1), and carries no clause of match rule 3 e)'s list.
        if (item.IsAsIfElementary)
            return new ActivationDescription
            {
                Shape = ActivationShape.AsIfElementaryGroup,
                Category = item.GroupUsage is GroupUsage.Bit ? ActivationCategory.Boolean : ActivationCategory.National,
                Usage = item.GroupUsage is GroupUsage.Bit ? nameof(Usage.Bit) : nameof(Usage.National),
                Positions = item.AsIfPic!.Length,
            };
        if (item.IsGroup)
        {
            // §8.5.2.1: "an alphanumeric group item has class and category alphanumeric". A group carries none of the
            // §9.3.6 3 e) clauses, so its Clauses are empty: two groups match whatever their sizes, and a group never
            // matches an elementary item (whose PICTURE is a clause the group lacks).
            if (item.CurrentExtentImageCapable)
                return new ActivationDescription
                {
                    Shape = ActivationShape.VariableLengthGroup,
                    Category = ActivationCategory.Alphanumeric,
                    Atoms = VariableLengthCompatibility.GroupAtoms(item),
                };
            return item.IsImageCapable
                ? new ActivationDescription
                {
                    Shape = ActivationShape.AlphanumericGroup,
                    Category = ActivationCategory.Alphanumeric,
                    Positions = item.ImageWidth,
                    Atoms = VariableLengthCompatibility.GroupAtoms(item),
                }
                : null;
        }
        if (item.Pic is not { } p) return null;
        if (p.Category is PicCategory.ObjectReference)
        {
            var d = p.ObjectRef ?? ObjectRefDescriptor.Universal;
            return new ActivationDescription
            {
                Shape = ActivationShape.ObjectReference,
                Category = ActivationCategory.Object,
                ObjectKind = d.Kind switch
                {
                    ObjectRefKind.Universal => ObjectReferenceKind.Universal,
                    ObjectRefKind.Interface => ObjectReferenceKind.Interface,
                    ObjectRefKind.ActiveClass => ObjectReferenceKind.ActiveClass,
                    _ => ObjectReferenceKind.ObjectClass,
                },
                // The containing class of an ACTIVE-CLASS description is not part of it (§9.3.8.2.3 rule 2 d)).
                ObjectName = d.IsActiveClass || d.Name is null ? null : CobolNames.Fold(d.Name),
                Factory = d.Factory,
                Only = d.Only,
            };
        }
        return new ActivationDescription
        {
            Shape = ActivationShape.Elementary,
            Category = ElementaryCategory(p),
            Usage = p.Usage.ToString(),
            Clauses = ElementaryClauses(item),
            AnyLength = item.IsAnyLength,
            Positions = p.Length,
            LocaleExternal = p.LocaleEdit?.Locale.Named?.External,
            LocaleFromLiteral = p.LocaleEdit?.Locale.Named?.FromLiteral ?? false,
        };
    }

    /// <summary>The description of a REFERENCE-MODIFIED argument — the unique data item ISO §8.4.3.3.4 GR5 creates, which
    /// GR6 makes "an elementary data item without the JUSTIFIED clause" of identifier-1's class, category and usage
    /// (alphanumeric- and national-edited, numeric and numeric-edited read as alphanumeric or national). It is
    /// described by no PICTURE clause and none of §9.3.6 match rule 3 e)'s other clauses, so it matches only a formal
    /// that has none of them either — a group of its class — and §14.8.2.2 rule 1 then compares its length: the
    /// EVALUATED length (GR5 c)), which the call site sets from the slice it boxes
    /// (<see cref="CobolInvokeArg.ReferenceModified"/>), so a non-literal modifier is measured too. The compile-time
    /// length here (<see cref="RefModPlace.StaticLength"/>) is only what the description says before that.</summary>
    public static ActivationDescription OfReferenceModification(RefModPlace r)
    {
        string category = r.Category switch
        {
            PicCategory.National => ActivationCategory.National,
            PicCategory.Boolean => ActivationCategory.Boolean,
            _ => ActivationCategory.Alphanumeric,
        };
        return new ActivationDescription
        {
            Shape = ActivationShape.Elementary,
            Category = category,
            Usage = r.Inner.Item.OperandPic?.Usage.ToString() ?? "",
            Positions = r.StaticLength(r.Inner.Item.OperandPic?.Length) ?? 0,
        };
    }

    /// <summary>The description of a method's formal parameter: its item's, with the OPTIONAL phrase (§9.3.6 match
    /// rules 1 and 3 b)) and the BY VALUE phrase (rule 3 a)). Null when the item has no crossing form.</summary>
    public static ActivationDescription? OfFormal(OoFormal formal) =>
        Of(formal.Item) is { } d ? With(d, formal.Optional, formal.ByValue) : null;

    /// <summary>The description of an ADDRESS-IDENTIFIER argument (kb/Work PB1137): §8.4.3.11.4 GR1 — "a unique data
    /// item of class pointer and category data-pointer", restricted to its operand's type when that is a
    /// strongly-typed group or a restricted data-pointer (GR2, <see cref="StrongTypeModel.AddressOfRestriction"/>) —
    /// and §8.4.3.13.4 GR1 a program-address-identifier one of category program-pointer, restricted by its prototype
    /// (GR3). Described exactly as <see cref="Of"/> describes a pointer ITEM of the same restriction.</summary>
    public static ActivationDescription OfAddress(BoundAddressOperand address) => address.Data is { } data
        ? Pointer(ActivationCategory.DataPointer, Restriction(StrongTypeModel.AddressOfRestriction(data.Item)))
        : Pointer(ActivationCategory.ProgramPointer, Restriction(address.Program!.Prototype));

    private static ActivationDescription Pointer(string category, string restriction) => new()
    {
        Shape = ActivationShape.Elementary, Category = category, Usage = category, Clauses = restriction,
    };

    /// <summary>A program- or function-pointer's restriction — its USAGE clause's TO phrase (§14.8.2.3.2's
    /// class-pointer paragraph: "if either is a restricted pointer, both shall be restricted and of the same type")
    /// — or "*" for none.</summary>
    private static string Restriction(string? prototype) => prototype is null ? "*" : "TO " + CobolNames.Fold(prototype);

    /// <summary>A data-pointer's restriction (the same paragraph): the whole TYPE identity it is restricted to,
    /// <see cref="StrongTypeModel.TypeRestriction.Key"/> — the §8.5.3.1 identity of the declaration (so two
    /// non-equivalent declarations of one type-name in two source elements are two restrictions), and for the address
    /// of a strongly-typed group SUBORDINATE to a type declaration its position and length (§8.4.3.11.4 GR2 "restricted to the
    /// type of identifier-1", kb/Work PB1408) — so that address never matches a formal restricted to the whole
    /// record's type.</summary>
    private static string Restriction(StrongTypeModel.TypeRestriction type) =>
        type.IsRestricted ? "TO " + type.Key : "*";

    private static ActivationDescription With(ActivationDescription d, bool optional, bool byValue) =>
        d with { Optional = optional, ByValue = byValue };

    /// <summary>The ISO §8.5.2 class-and-category name of an elementary item (§8.5.2.1 Table 2). The PICTURE identity in
    /// <see cref="ElementaryClauses"/> separates every finer distinction; this is match rule 3 c)'s coarse half.</summary>
    private static string ElementaryCategory(PicInfo p) => p.Category switch
    {
        PicCategory.Numeric => p.Usage is Usage.Index ? ActivationCategory.Index : "numeric",
        PicCategory.NumericEdited => "numeric-edited",
        PicCategory.Alphanumeric => p.IsAlphabetic ? "alphabetic"
            : p.EditMask is not null ? "alphanumeric-edited" : ActivationCategory.Alphanumeric,
        PicCategory.National => p.EditMask is not null ? "national-edited" : ActivationCategory.National,
        PicCategory.Boolean => ActivationCategory.Boolean,
        PicCategory.Pointer => ActivationCategory.DataPointer,
        PicCategory.ProgramPointer => ActivationCategory.ProgramPointer,
        PicCategory.FunctionPointer => ActivationCategory.FunctionPointer,
        _ => p.Category.ToString(),
    };

    /// <summary>⛔ ISO §9.3.6 match rule 3 e)'s identity of an elementary item — "the same ALIGNED, ANY LENGTH, BLANK
    /// WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE clauses", with 3 e) 1. (currency strings) and 2.
    /// (DECIMAL-POINT IS COMMA) carried by the PICTURE clause's ONE identity (<see cref="PictureClauseIdentity"/>) and
    /// 3. a. (the LOCALE phrase's SIZE) here, 3. b. (its external identification) in
    /// <see cref="ActivationDescription.LocaleExternal"/>. The same facts <c>OoConformance.DescriptionMismatch</c>
    /// compares for an elementary pair (its §14.8.2.3.2 rule 2 list is this list without ANY LENGTH), so a pair the
    /// typed lane finds identical has equal clauses here — held by
    /// <c>PictureClauseIdentityDriftTests.ActivationMatch_AgreesWithTheComparator_OverEveryElementaryPair</c>. A
    /// class pointer's restriction is its USAGE clause's TO phrase.</summary>
    public static string ElementaryClauses(DataItem item)
    {
        var p = item.Pic!;
        if (p.Category is PicCategory.Pointer)
            return Restriction(StrongTypeModel.PointerRestriction(item));
        if (p.Category is PicCategory.ProgramPointer or PicCategory.FunctionPointer)
            return Restriction(p.RestrictedPrototypeName);
        // An ANY LENGTH item's one-symbol picture has no length of its own (§13.18.2.3 SR1).
        string picture = item.IsAnyLength ? "*" + (p.Clause?.CharacterString ?? "") : p.Clause?.Key ?? "";
        string locale = p.LocaleEdit is { } le ? $"|LOCALE {(le.Locale.IsCurrent ? "-" : "N")} SIZE {le.Size} {le.Picture}" : "";
        string dynamic = item.IsDynamicLength
            ? $"|DYNAMIC LENGTH {item.DynMaxSize} {CobolNames.Fold(item.DynStructure?.Name ?? "")}" : "";
        return $"USAGE {p.Usage}|SIGN {(p.Signed ? "S" : "U")} {p.SignKind}|DIGITS {p.Digits}.{p.Scale}"
            + $"|LENGTH {(item.IsAnyLength ? "*" : p.Length.ToString(System.Globalization.CultureInfo.InvariantCulture))}"
            + $"|{(item.BlankWhenZero ? "BLANK WHEN ZERO" : "-")}|{(item.Justified ? "JUSTIFIED" : "-")}"
            + $"|{(item.IsAligned ? "ALIGNED" : "-")}{dynamic}|PICTURE {picture}{locale}";
    }
}
