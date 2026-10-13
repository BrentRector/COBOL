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
    /// PICTURE-less elementary item. An object reference carries its class facts from <paramref name="classes"/>, the
    /// group's class table (<see cref="OoClassTable.SetOperand"/>; kb/Work PB2463), which §9.3.6 match rule 6 and the
    /// SET conformance of §14.8.2.3.3 / §14.8.3.3 relate at run time; with no table it carries none.</summary>
    public static ActivationDescription? Of(DataItem item, OoClassTable? classes)
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
        // NOTE), of class and category boolean / national (§8.5.2.1) — and "treated as though it were an elementary data
        // item of usage bit … described with PICTURE 1(m)" / "of usage national … described with PICTURE N(m)"
        // (§13.18.29.4 GR1 b) / GR2 b)), so match rule 3 e) compares THAT picture: a national group matches an
        // elementary PIC N(m) USAGE NATIONAL of its length and no national group of another length (kb/Work PB2077 —
        // the typed lane's answer, §14.8.2.3.2 "Additionally" b) / c); this description carried no clauses, so the
        // universal lane refused the first pair and matched the second).
        if (item.IsAsIfElementary)
        {
            var asIf = item.AsIfPic!;
            string symbol = item.GroupUsage is GroupUsage.Bit ? "1" : "N";
            var picture = PictureClauseIdentity.Of(string.Concat(Enumerable.Repeat(symbol, asIf.Length)), '$', "$",
                decimalPointIsComma: false);
            return new ActivationDescription
            {
                Shape = ActivationShape.AsIfElementaryGroup,
                Category = item.GroupUsage is GroupUsage.Bit ? ActivationCategory.Boolean : ActivationCategory.National,
                Usage = item.GroupUsage is GroupUsage.Bit ? nameof(Usage.Bit) : nameof(Usage.National),
                Clauses = ClausesOf(item, asIf, picture.Key, Invariant(asIf.Length)),
                ClausePhrases = PhrasesOf(item, asIf, picture.Phrases()),
                AnyLengthClauses = ClausesAtAnyLength(item, asIf, symbol),
                Positions = asIf.Length,
                Table16 = Table16Operand.Of(item).Heading,
            };
        }
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
            var facts = classes?.SetOperand(d);
            return new ActivationDescription
            {
                Shape = ActivationShape.ObjectReference,
                Category = ActivationCategory.Object,
                ObjectKind = d.RuntimeKind,
                // The containing class of an ACTIVE-CLASS description is not part of its identity (§9.3.8.2.3 rule 2
                // d)); the SET table reads it from the class facts (ObjectClasses' first entry).
                ObjectName = d.IsActiveClass || d.Name is null ? null : CobolNames.Fold(d.Name),
                Factory = d.Factory,
                Only = d.Only,
                ObjectClasses = Folded(facts?.Classes),
                ObjectInterfaces = Folded(facts?.Interfaces),
            };
        }
        var (clauses, phrases) = ElementaryClauses(item);
        return new ActivationDescription
        {
            Shape = ActivationShape.Elementary,
            // §8.5.2.1 Table 2's category name (match rule 3 c)'s coarse half; the PICTURE identity in
            // ElementaryClauses separates every finer distinction), in the compiler's ONE spelling (kb/Work PB850).
            Category = ItemCategory.CategoryName(p),
            Usage = p.Usage.ToString(),
            Clauses = clauses,
            ClausePhrases = phrases,
            AnyLength = item.IsAnyLength,
            AnyLengthClauses = AnyLengthClausesOf(item),
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
    /// (alphanumeric- and national-edited, numeric and numeric-edited read as alphanumeric or national; alphabetic and
    /// boolean kept — the ONE GR6 reader, <see cref="ItemCategory.CategoryName(Place)"/>). It is described by no PICTURE
    /// clause and none of §9.3.6 match rule 3 e)'s other clauses (kb/Work PB3098), so it matches only a formal that has
    /// none of them either — a group of its class — and §14.8.2.2 rule 1 then compares its length. At run time that is
    /// the EVALUATED length (GR5 c)), which the universal call site sets from the slice it boxes
    /// (<see cref="CobolInvokeArg.ReferenceModified"/>). At compile time it is the length when that is static
    /// (<see cref="RefModPlace.StaticLength"/>), otherwise the slice's MAXIMUM extent — identifier-1's positions from a
    /// static leftmost position on, or all of them — the measure §14.8.2.2 gives the other argument whose length moves
    /// at run time, "the maximum length is used" for an occurs-depending group (docs/CONFORMANCE.md D-RM1 a); kb/Work
    /// PB2077). identifier-1's positions are the ONE reader's (<see cref="RefModPlace.StaticInnerPositions"/>), so a
    /// slice of an alphanumeric group, whose length is static, is measured too. Zero when identifier-1's own length is
    /// not static (an ANY LENGTH or DYNAMIC LENGTH item), which no group formal admits.</summary>
    public static ActivationDescription OfReferenceModification(RefModPlace r)
    {
        int? inner = r.StaticInnerPositions;
        return new ActivationDescription
        {
            Shape = ActivationShape.Elementary,
            Category = ItemCategory.CategoryName(r)!,
            Usage = r.Inner.Item.OperandPic?.Usage.ToString() ?? "",
            Positions = r.StaticLength(inner) ?? MaximumExtent(r, inner) ?? 0,
            // GR6's category, with the alphabetic rider the ONE ref-mod Table-16 reader keeps (kb/Work PB73).
            Table16 = Table16Operand.Of(r).Heading,
        };
    }

    /// <summary>The most positions a slice whose length is a run-time value can have: identifier-1's
    /// <paramref name="inner"/> positions from a static leftmost position to the end (§8.4.3.3.4 GR5 b) / c)), or all of
    /// them when the leftmost position is a run-time value too. Null when identifier-1's length is not static.</summary>
    private static int? MaximumExtent(RefModPlace r, int? inner) =>
        inner is not { } size ? null
        : r.Start is PositionConstant { Value: >= 1 } left && left.Value <= size ? size - (int)left.Value + 1
        : size;

    /// <summary>The class facts of a description as it carries them: folded, as <see cref="ActivationDescription.ObjectName"/>
    /// is, so a rendered description spells one name one way.</summary>
    private static string[]? Folded(IReadOnlyList<string>? names) => names?.Select(CobolNames.Fold).ToArray();

    /// <summary>The description of a PLACE an activation names — an argument or a RETURNING item: a reference-modified
    /// operand is the unique data item §8.4.3.3.4 GR5 creates (<see cref="OfReferenceModification"/>), any other place
    /// the item it denotes (<see cref="Of"/>). Null for a place that denotes no item and is not reference-modified.</summary>
    public static ActivationDescription? OfPlace(Place p, OoClassTable? classes) =>
        p is RefModPlace r ? OfReferenceModification(r) : p.DenotedItem is { } item ? Of(item, classes) : null;

    /// <summary>⛔ THE DESCRIPTION OF ONE CALL ARGUMENT whose activated program is located by name at run time (kb/Work
    /// PB165) — what §14.9.4.4 GR3 d) compares with the activated program's registered formal
    /// (<c>ActivationRelations.CallArgumentViolation</c>). An identifier is described as the place it is
    /// (<see cref="OfPlace"/>), an address-identifier as the unique pointer item it creates (<see cref="OfAddress"/>;
    /// <paramref name="programRestriction"/> is a program-address-identifier's prototype's signature class), and a BY
    /// CONTENT / BY VALUE argument that has no storage as the sending VALUE §14.8.2.3.3 rule 2 names
    /// (<see cref="OfValue"/>). Null for an omitted argument (§14.9.4.4 GR11), which has nothing to describe.</summary>
    public static ActivationDescription? OfCallArgument(BoundCallArg a, string? programRestriction, OoClassTable? classes)
    {
        if (a.Omitted) return null;
        // §14.9.4.2 Format 2's boolean-expression-1: a boolean value (§8.8.2), with the character image of its bits.
        if (a.ContentBool is not null) return Value(ActivationShape.Literal, ActivationCategory.Boolean, Table16Category.Boolean);
        if (a.DataAddress is { } data) return OfAddress(new BoundAddressOperand(data, null), null);
        if (a.ProgramAddress is { } program) return OfAddress(new BoundAddressOperand(null, program), programRestriction);
        if (a.Place is { } p) return OfPlace(p, classes);
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
    public static ActivationDescription? OfFormal(OoFormal formal, OoClassTable? classes) =>
        Of(formal.Item, classes) is { } d ? With(d, formal.Optional, formal.ByValue) : null;

    /// <summary>The description of an ADDRESS-IDENTIFIER argument (kb/Work PB1137): §8.4.3.11.4 GR1 — "a unique data
    /// item of class pointer and category data-pointer", restricted to its operand's type when that is a
    /// strongly-typed group or a restricted data-pointer (GR2, <see cref="StrongTypeModel.AddressOfRestriction"/>) —
    /// and §8.4.3.13.4 GR1 a program-address-identifier one of category program-pointer, restricted by its prototype
    /// (GR3). Described exactly as <see cref="Of"/> describes a pointer ITEM of the same restriction.
    /// <paramref name="programRestriction"/> is the program-address-identifier's prototype's signature class
    /// (<see cref="PrototypeSignatureClasses"/>, resolved by the invoking unit's prototype table), null when it names none.</summary>
    public static ActivationDescription OfAddress(BoundAddressOperand address, string? programRestriction)
    {
        if (address.Data is { } data)
        {
            var type = StrongTypeModel.AddressOfRestriction(data.Item);
            return Pointer(ActivationCategory.DataPointer, Restriction(type), PointerPhrase(Usage.Pointer, TypePhrase(type)));
        }
        return Pointer(ActivationCategory.ProgramPointer, programRestriction ?? ActivationDescription.Unrestricted,
            PointerPhrase(Usage.ProgramPointer, programRestriction));
    }

    private static ActivationDescription Pointer(string category, string restriction, string phrase) => new()
    {
        Shape = ActivationShape.Elementary, Category = category, Usage = category, Clauses = restriction,
        ClausePhrases = [phrase],
    };

    /// <summary>A class pointer's USAGE clause as its entry spells it, with its TO phrase (<paramref name="to"/>, null
    /// for an unrestricted pointer) — the reader's half of <see cref="Restriction"/> and
    /// <see cref="PrototypeSignatures.RestrictionIdentity"/>, which are identities, not spellings.</summary>
    private static string PointerPhrase(Usage usage, string? to) =>
        $"USAGE {UsageFamilies.UsageWord(usage)}{(to is null ? "" : " " + to)}";

    /// <summary>The TO phrase of a data-pointer restricted to <paramref name="type"/> as a reader spells it — the
    /// type-name, and for a subordinate item of a type declaration that item — or null when it is unrestricted.</summary>
    private static string? TypePhrase(StrongTypeModel.TypeRestriction type) => type.IsRestricted ? "TO " + type : null;

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

    /// <summary>⛔ ISO §9.3.6 match rule 3 e)'s identity of an elementary item — "the same ALIGNED, ANY LENGTH, BLANK
    /// WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE clauses", with 3 e) 1. (currency strings) and 2.
    /// (DECIMAL-POINT IS COMMA) carried by the PICTURE clause's ONE identity (<see cref="PictureClauseIdentity"/>) and
    /// 3. a. (the LOCALE phrase's SIZE) here, 3. b. (its external identification) in
    /// <see cref="ActivationDescription.LocaleExternal"/>. The same facts <c>OoConformance.DescriptionMismatch</c>
    /// compares for an elementary pair (its §14.8.2.3.2 rule 2 list is this list without ANY LENGTH), so a pair the
    /// typed lane finds identical has equal clauses here — held by
    /// <c>PictureClauseIdentityDriftTests.ActivationMatch_AgreesWithTheComparator_OverEveryElementaryPair</c>. A
    /// class pointer's restriction is its USAGE clause's TO phrase.</summary>
    public static (string Clauses, string[] Phrases) ElementaryClauses(DataItem item)
    {
        var p = item.Pic!;
        if (p.Category is PicCategory.Pointer)
        {
            var type = StrongTypeModel.PointerRestriction(item);
            return (Restriction(type), [PointerPhrase(p.Usage, TypePhrase(type))]);
        }
        if (p.Category is PicCategory.ProgramPointer or PicCategory.FunctionPointer)
            return (PrototypeSignatures.RestrictionIdentity(item) ?? ActivationDescription.Unrestricted,
                [PointerPhrase(p.Usage, p.RestrictedPrototypeName is { } prototype ? "TO " + prototype : null)]);
        string[] phrases = PhrasesOf(item, p, p.Clause?.Phrases() ?? []);
        // An ANY LENGTH item's one-symbol picture has no length of its own (§13.18.2.3 SR1).
        return item.IsAnyLength
            ? (ClausesAtAnyLength(item, p, p.Clause?.CharacterString ?? ""), phrases)
            : (ClausesOf(item, p, p.Clause?.Key ?? "", Invariant(p.Length)), phrases);
    }

    private static string Invariant(int n) => n.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary><see cref="ActivationDescription.AnyLengthClauses"/>: the <see cref="ElementaryClauses"/> of
    /// <paramref name="item"/> as if it were described with the ANY LENGTH clause — §13.18.2.3 SR1's one-symbol PICTURE,
    /// the symbol its expanded character-string repeats, and no length — so that §14.8.2.3.2 "Additionally" d)'s "its
    /// length is considered to match" relaxes the length and nothing else. Null for a PICTURE that repeats no single
    /// symbol, or that carries a currency string, a DECIMAL-POINT IS COMMA state or an EDITING phrase (a one-symbol
    /// ANY LENGTH picture carries none), and for every item without a PICTURE clause.</summary>
    private static string? AnyLengthClausesOf(DataItem item) =>
        !item.IsAnyLength && item.Pic is { Clause: { CharacterString: { Length: > 0 } cs } clause, LocaleEdit: null } pic
        && clause is { CurrencyString: null, DecimalPointIsComma: null, Editing: null } && cs.All(c => c == cs[0])
            ? ClausesAtAnyLength(item, pic, cs[..1])
            : null;

    private static string ClausesAtAnyLength(DataItem item, PicInfo p, string symbol) => ClausesOf(item, p, "*" + symbol, "*");

    /// <summary>The one spelling of ISO §9.3.6 match rule 3 e)'s clause identity of <paramref name="item"/> described
    /// by <paramref name="p"/> (its own PICTURE, or a bit / national group's as-if one), given the PICTURE and LENGTH
    /// texts.</summary>
    private static string ClausesOf(DataItem item, PicInfo p, string picture, string length)
    {
        string locale = p.LocaleEdit is { } le ? $"|LOCALE {(le.Locale.IsCurrent ? "-" : "N")} SIZE {le.Size} {le.Picture}" : "";
        string dynamic = item.IsDynamicLength
            ? $"|DYNAMIC LENGTH {item.DynMaxSize} {CobolNames.Fold(item.DynStructure?.Name ?? "")}" : "";
        return $"USAGE {p.Usage}|SIGN {(p.Signed ? "S" : "U")} {p.SignKind}|DIGITS {p.Digits}.{p.Scale}"
            + $"|LENGTH {length}"
            + $"|{(item.BlankWhenZero ? "BLANK WHEN ZERO" : "-")}|{(item.Justified ? "JUSTIFIED" : "-")}"
            + $"|{(item.IsAligned ? "ALIGNED" : "-")}{dynamic}|PICTURE {picture}{locale}";
    }

    /// <summary>⛔ The reader's spelling of the clauses <see cref="ClausesOf"/> keys, one phrase per clause
    /// (<see cref="ActivationDescription.ClausePhrases"/>): the PICTURE clause's phrases (<paramref name="picture"/>,
    /// <see cref="PictureClauseIdentity.Phrases"/>), then ANY LENGTH, USAGE, a SIGN clause that is not the default
    /// trailing embedded sign, BLANK WHEN ZERO, JUSTIFIED, ALIGNED, DYNAMIC LENGTH and the PICTURE clause's LOCALE
    /// phrase — every clause the key holds, so two descriptions whose keys differ name the clause they differ in. Two
    /// facts are named by presence only: an EDITING phrase's rules and a LOCALE phrase's own picture, so a pair that
    /// differs only there names no clause and its refusal cites the rule alone. Built beside the key from the same
    /// item, never parsed back out of it (kb/Work PB2077 review).</summary>
    private static string[] PhrasesOf(DataItem item, PicInfo p, IEnumerable<string> picture)
    {
        List<string> phrases = [.. picture];
        if (item.IsAnyLength) phrases.Add("ANY LENGTH");
        phrases.Add("USAGE " + UsageFamilies.UsageWord(p.Usage));
        if (p.Signed && SignPhrase(p.SignKind) is { } sign) phrases.Add(sign);
        if (item.BlankWhenZero) phrases.Add("BLANK WHEN ZERO");
        if (item.Justified) phrases.Add("JUSTIFIED");
        if (item.IsAligned) phrases.Add("ALIGNED");
        if (item.IsDynamicLength)
            phrases.Add("DYNAMIC LENGTH" + (item.DynStructure?.Name is { } structure ? " " + structure : "")
                + (item.DynMaxSize != CobolDynString.MaxLength ? " LIMIT " + Invariant(item.DynMaxSize) : ""));
        if (p.LocaleEdit is { } le)
            phrases.Add($"LOCALE{(le.Locale.Named is { } named ? " " + named.External : "")} SIZE {le.Size}");
        return [.. phrases];
    }

    /// <summary>The SIGN clause a signed item's <see cref="PicInfo.SignKind"/> was analyzed from
    /// (<see cref="PicInfo.SignKindFor"/>), or null for the default trailing embedded sign and a binary or
    /// packed sign, which no SIGN clause states.</summary>
    private static string? SignPhrase(string signKind) => signKind switch
    {
        "LeadingSeparate" => "SIGN LEADING SEPARATE",
        "TrailingSeparate" => "SIGN TRAILING SEPARATE",
        "LeadingOverpunch" => "SIGN LEADING",
        _ => null,
    };
}
