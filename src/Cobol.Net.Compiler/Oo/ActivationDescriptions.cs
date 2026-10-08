// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using CobolNet.Binding;
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
                Table16 = Table16Operand.Of(item).Heading,
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
            // An index item is class index (§8.5.2.1 Table 2), which SR1 keeps out of every MOVE, so it has no heading.
            Table16 = p.Usage is Usage.Index ? Table16Category.None : Table16Operand.Of(item).Heading,
            BinaryWidth = MoveTable16.IsBinaryWidth(item),
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
            // GR6's category, with the alphabetic rider the ONE ref-mod Table-16 reader keeps (kb/Work PB73).
            Table16 = Table16Operand.Of(r).Heading,
        };
    }

    /// <summary>The description of a PLACE an activation names — an argument or a RETURNING item: a reference-modified
    /// operand is the unique data item §8.4.3.3.4 GR5 creates (<see cref="OfReferenceModification"/>), any other place
    /// the item it denotes (<see cref="Of"/>). Null for a place that denotes no item and is not reference-modified.</summary>
    public static ActivationDescription? OfPlace(Place p) =>
        p is RefModPlace r ? OfReferenceModification(r) : p.DenotedItem is { } item ? Of(item) : null;

    /// <summary>⛔ THE DESCRIPTION OF ONE CALL ARGUMENT whose activated program is located by name at run time (kb/Work
    /// PB165) — what §14.9.4.4 GR3 d) compares with the activated program's registered formal
    /// (<c>ActivationRelations.CallArgumentViolation</c>). An identifier is described as the place it is
    /// (<see cref="OfPlace"/>), an address-identifier as the unique pointer item it creates (<see cref="OfAddress"/>;
    /// <paramref name="programRestriction"/> is a program-address-identifier's prototype's signature class), and a BY
    /// CONTENT / BY VALUE argument that has no storage as the sending VALUE §14.8.2.3.3 rule 2 names
    /// (<see cref="OfValue"/>). Null for an omitted argument (§14.9.4.4 GR11), which has nothing to describe.</summary>
    public static ActivationDescription? OfCallArgument(BoundCallArg a, string? programRestriction)
    {
        if (a.Omitted) return null;
        // §14.9.4.2 Format 2's boolean-expression-1: a boolean value (§8.8.2), with the character image of its bits.
        if (a.ContentBool is not null) return Value(ActivationShape.Literal, ActivationCategory.Boolean, Table16Category.Boolean);
        if (a.DataAddress is { } data) return OfAddress(new BoundAddressOperand(data, null), null);
        if (a.ProgramAddress is { } program) return OfAddress(new BoundAddressOperand(null, program), programRestriction);
        if (a.Place is { } p) return OfPlace(p);
        return a.Value is { } v ? OfValue(v) : null;
    }

    /// <summary>The description of a BY CONTENT / BY VALUE argument that has NO STORAGE, as the sending operand ISO
    /// §14.8.2.3.3 rule 2 names — the same readings the compiler's own screen gives each shape when it knows the formal
    /// (<c>ParameterConformance.ContentConformanceReason</c>): a nonnumeric literal is its category's Table 16 row; a
    /// numeric literal (and a BY VALUE literal bound as an expression, §14.9.4.4 GR8) the numeric row of its own digits;
    /// a figurative constant or an ALL literal an alphanumeric value of any category, ZERO also a numeric COMPUTE sender
    /// (§8.8.1.1); a character-valued intrinsic function its §15.2 type's row; a numeric function or an arithmetic
    /// expression a numeric value with no character image, of its function's row or — for an expression, which carries
    /// no compile-time integer guarantee — the noninteger row (kb/Work PB1946); NULL a SET sender only. Null for a shape
    /// none of these names.</summary>
    public static ActivationDescription? OfValue(BoundOperand value) => value switch
    {
        BoundPredefinedNull => new ActivationDescription { Shape = ActivationShape.PredefinedNull },
        BoundFigurative f => Value(ActivationShape.Literal,
            f.Kind == 'Z' ? ActivationCategory.FigurativeZero : ActivationCategory.Figurative, Table16Category.None),
        BoundAllLiteral => Value(ActivationShape.Literal, ActivationCategory.Figurative, Table16Category.None),
        BoundStringLiteral s => Value(ActivationShape.Literal, CategoryName(s.Category), MoveTable16.SenderPosition(s).Heading),
        BoundNumericLiteral n => Value(ActivationShape.Literal, ActivationCategory.Numeric, MoveTable16.SenderPosition(n).Heading),
        BoundComputedOperand ce when Gr8ArgumentLiteral.NumericText(ce.Expr) is { } text =>
            Value(ActivationShape.Literal, ActivationCategory.Numeric,
                MoveTable16.SenderPosition(new BoundNumericLiteral(text)).Heading),
        BoundComputedOperand { Expr: BoundIntrinsicCall { ResultCategory: not PicCategory.Numeric } ic } character =>
            Value(ActivationShape.Literal, CategoryName(ic.ResultCategory), MoveTable16.SenderPosition(character).Heading),
        BoundComputedOperand { Expr: BoundIntrinsicCall } function =>
            Value(ActivationShape.Expression, ActivationCategory.Numeric, MoveTable16.SenderPosition(function).Heading),
        BoundComputedOperand => Value(ActivationShape.Expression, ActivationCategory.Numeric, Table16Category.NumericNoninteger),
        _ => null,
    };

    private static ActivationDescription Value(ActivationShape shape, string category, Table16Category table16) =>
        new() { Shape = shape, Category = category, Table16 = table16 };

    /// <summary>The §8.5.2 category name of a character-valued operand's <see cref="PicCategory"/>.</summary>
    private static string CategoryName(PicCategory c) => c switch
    {
        PicCategory.National => ActivationCategory.National,
        PicCategory.Boolean => ActivationCategory.Boolean,
        _ => ActivationCategory.Alphanumeric,
    };

    /// <summary>The description of a method's formal parameter: its item's, with the OPTIONAL phrase (§9.3.6 match
    /// rules 1 and 3 b)) and the BY VALUE phrase (rule 3 a)). Null when the item has no crossing form.</summary>
    public static ActivationDescription? OfFormal(OoFormal formal) =>
        Of(formal.Item) is { } d ? With(d, formal.Optional, formal.ByValue) : null;

    /// <summary>The description of an ADDRESS-IDENTIFIER argument (kb/Work PB1137): §8.4.3.11.4 GR1 — "a unique data
    /// item of class pointer and category data-pointer", restricted to its operand's type when that is a
    /// strongly-typed group or a restricted data-pointer (GR2, <see cref="StrongTypeModel.AddressOfRestriction"/>) —
    /// and §8.4.3.13.4 GR1 a program-address-identifier one of category program-pointer, restricted by its prototype
    /// (GR3). Described exactly as <see cref="Of"/> describes a pointer ITEM of the same restriction.
    /// <paramref name="programRestriction"/> is the program-address-identifier's prototype's signature class
    /// (<see cref="PrototypeSignatureClasses"/>, resolved by the invoking unit's prototype table), null when it names none.</summary>
    public static ActivationDescription OfAddress(BoundAddressOperand address, string? programRestriction) => address.Data is { } data
        ? Pointer(ActivationCategory.DataPointer, Restriction(StrongTypeModel.AddressOfRestriction(data.Item)))
        : Pointer(ActivationCategory.ProgramPointer, programRestriction ?? ActivationDescription.Unrestricted);

    private static ActivationDescription Pointer(string category, string restriction) => new()
    {
        Shape = ActivationShape.Elementary, Category = category, Usage = category, Clauses = restriction,
    };

    /// <summary>A data-pointer's restriction (§14.8.2.3.2's class-pointer paragraph: "if either is a restricted
    /// pointer, both shall be restricted and of the same type"): the whole TYPE identity it is restricted to,
    /// <see cref="StrongTypeModel.TypeRestriction.Key"/> — the §8.5.3.1 identity of the declaration (so two
    /// non-equivalent declarations of one type-name in two source elements are two restrictions), and for the address
    /// of a strongly-typed group SUBORDINATE to a type declaration its position and length (§8.4.3.11.4 GR2 "restricted to the
    /// type of identifier-1", kb/Work PB1408) — so that address never matches a formal restricted to the whole
    /// record's type. A program- or function-pointer's restriction is its prototype's signature class
    /// (<see cref="PrototypeSignatures.RestrictionIdentity"/>).</summary>
    private static string Restriction(StrongTypeModel.TypeRestriction type) =>
        type.IsRestricted ? "TO " + type.Key : ActivationDescription.Unrestricted;

    private static ActivationDescription With(ActivationDescription d, bool optional, bool byValue) =>
        d with { Optional = optional, ByValue = byValue };

    /// <summary>The ISO §8.5.2 class-and-category name of an elementary item (§8.5.2.1 Table 2). The PICTURE identity in
    /// <see cref="ElementaryClauses"/> separates every finer distinction; this is match rule 3 c)'s coarse half.</summary>
    private static string ElementaryCategory(PicInfo p) => p.Category switch
    {
        PicCategory.Numeric => p.Usage is Usage.Index ? ActivationCategory.Index : ActivationCategory.Numeric,
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
            return PrototypeSignatures.RestrictionIdentity(item) ?? ActivationDescription.Unrestricted;
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
