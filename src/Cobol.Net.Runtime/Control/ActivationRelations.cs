// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace CobolNet.Runtime;

/// <summary>
/// ⛔ THE TWO RELATIONS THE STANDARD DEFINES OVER A PAIR OF ACTIVATION DESCRIPTIONS, asked at run time by the generated
/// universal-dispatch switch (<c>__CobolInvoke</c>; kb/Work PB480 / PB1780):
/// <list type="number">
/// <item><b>MATCH</b> (ISO §9.3.6, method resolution): <see cref="Matches"/> for a parameter (match rule 3 — every
/// universal argument is BY REFERENCE, §14.9.23.3 SR6) and <see cref="ReturningMatches"/> (match rules 6 / 7). A method
/// that does not match is not bound; the search continues up the INHERITS chain and ends in EC-OO-METHOD
/// (§9.3.6 resolution step 6).</item>
/// <item><b>CONFORMANCE</b> (ISO §14.8.2 / §14.8.3, asked of the BOUND method by §14.9.23.4 GR7 c) — "the rules for
/// conformance specified in 14.8.2, Parameters and 14.8.3, Returning items apply"): <see cref="ParameterViolation"/>
/// and <see cref="ReturningViolation"/>. A violation is EC-OO-UNIVERSAL.</item>
/// </list>
/// The two are different questions and neither is the other's equality: match rule 3 e) is an identity of a CLAUSE
/// LIST, so two alphanumeric groups MATCH whatever their sizes (neither carries any of the clauses) and a group never
/// matches a <c>PIC X(n)</c>; conformance then admits a formal group no larger than the argument (§14.8.2.2 rule 1)
/// and refuses a larger one. Every argument here comes from <see cref="ActivationDescription"/>s the compiler built
/// in one place (<c>ActivationDescriptions</c>).
/// </summary>
public static class ActivationRelations
{
    /// <summary>ISO §9.3.6 match rule 3, for one BY REFERENCE parameter of the invocation: does
    /// <paramref name="argument"/> (whose run-time value is <paramref name="value"/>) match <paramref name="formal"/> of
    /// a method of the object <paramref name="receiver"/>?</summary>
    public static bool Matches(ActivationDescription argument, object? value, ActivationDescription formal,
        CobolObject receiver)
    {
        // 3 a) "is specified with the BY REFERENCE phrase" — a BY VALUE formal never matches (kb/Work PB1051).
        if (formal.ByValue) return false;
        // 3 b) "if the passed parameter is specified with the OMITTED phrase, shall be specified with the OPTIONAL
        // phrase. No further checking is performed on this parameter".
        if (argument.Shape is ActivationShape.Omitted) return formal.Optional;
        if (formal.Shape is ActivationShape.ObjectReference)
        {
            // 3 c) "is the same class and category": an object reference opposite an object reference only.
            if (argument.Shape is not ActivationShape.ObjectReference) return false;
            return formal.ObjectKind switch
            {
                // 3 d) 1.–3.: the same description.
                ObjectReferenceKind.Universal => argument.ObjectKind is ObjectReferenceKind.Universal,
                ObjectReferenceKind.Interface => argument.ObjectKind is ObjectReferenceKind.Interface
                    && CobolNames.Same(argument.ObjectName, formal.ObjectName),
                ObjectReferenceKind.ObjectClass => argument.ObjectKind is ObjectReferenceKind.ObjectClass
                    && CobolNames.Same(argument.ObjectName, formal.ObjectName)
                    && argument.Factory == formal.Factory && argument.Only == formal.Only,
                // 3 d) 4./5.: a condition on the VALUE, not on the argument's description — "the corresponding
                // parameter shall evaluate to an object reference of the same class specified in the invocation" /
                // "to the factory of the class specified in the invocation" (kb/Work PB1112's universal leg).
                ObjectReferenceKind.ActiveClass => ActiveClassValueMatches(value, formal.Factory, receiver),
                _ => false,
            };
        }
        if (argument.Shape is ActivationShape.ObjectReference) return false;   // 3 c)
        // 3 c) the same class and category; 3 e) the same ALIGNED, ANY LENGTH, BLANK WHEN ZERO, DYNAMIC LENGTH,
        // JUSTIFIED, PICTURE, SIGN and USAGE clauses (3 e) 1./2. folded into the PICTURE identity, 3. the LOCALE).
        return string.Equals(argument.Category, formal.Category, StringComparison.Ordinal)
            && argument.AnyLength == formal.AnyLength
            && string.Equals(argument.Clauses, formal.Clauses, StringComparison.Ordinal)
            && SameLocaleIdentification(argument, formal);
    }

    /// <summary>ISO §9.3.6 match rule 3 d) 4./5. evaluated on the argument's VALUE: the class "specified in the
    /// invocation" is the class of the object the method is invoked on (§9.3.6: "it is the class of the actual object
    /// referenced at runtime"), so a non-FACTORY ACTIVE-CLASS formal needs an object of exactly that class and a FACTORY
    /// one that class's factory object. A NULL reference references no object of any class; it is admitted, as SET
    /// admits NULL into every object-reference receiver (§14.9.39.3) and the typed lane admits a NULL argument for an
    /// ACTIVE-CLASS formal.</summary>
    private static bool ActiveClassValueMatches(object? value, bool factory, CobolObject receiver)
    {
        if (value is null) return true;
        Type wanted = factory
            ? receiver.__FactoryClassType ?? receiver.GetType()
            : receiver.__InstanceClassType ?? receiver.GetType();
        return value.GetType() == wanted;
    }

    /// <summary>ISO §9.3.6 match rule 3 e) 3. b. (and §14.8.2.3.2 / §14.8.3.3 "Additionally"): "both specify the LOCALE
    /// phrase without a locale-name or both specify the LOCALE phrase with the same external identification".</summary>
    private static bool SameLocaleIdentification(ActivationDescription a, ActivationDescription b) =>
        a.LocaleExternal is null
            ? b.LocaleExternal is null
            : b.LocaleExternal is not null
              && LocaleIdentification.SameExternalIdentification(a.LocaleExternal, a.LocaleFromLiteral,
                  b.LocaleExternal, b.LocaleFromLiteral);

    /// <summary>ISO §14.8.2 for a BY REFERENCE parameter of a method §9.3.6 has BOUND (§14.9.23.4 GR7 c)). Null when the
    /// pair conforms, else the reason. The elementary and object-reference identities of §14.8.2.3.2 rule 2 / rule 4
    /// are what match rule 3 d)/e) already proved; what remains is the group rules of §14.8.2.2 and GR7 c)'s own ANY
    /// LENGTH ban.</summary>
    public static string? ParameterViolation(ActivationDescription argument, ActivationDescription formal)
    {
        // GR7 c) bans the formal's DESCRIPTION, so it is asked before an OMITTED argument's exemption (§9.3.6 3 b)
        // exempts the MATCH from further checking, not the bound method from GR7 c)).
        if (formal.AnyLength)
            return "the formal parameter is described with the ANY LENGTH clause, which a method invoked through a "
                + "universal object reference shall not have (ISO §14.9.23.4 GR7 c))";
        if (argument.Shape is ActivationShape.Omitted) return null;
        // §14.8.2.2: "If either the formal parameter or the corresponding argument is a strongly-typed group item,
        // both shall be of the same type" — the match already required the same type-NAME (§8.5.2.1: it is the
        // class and category); the §8.5.3.1 equivalence of the two declarations is this.
        if (formal.Shape is ActivationShape.StrongGroup || argument.Shape is ActivationShape.StrongGroup)
            return formal.StrongType is not null
                   && string.Equals(formal.StrongType, argument.StrongType, StringComparison.Ordinal)
                ? null
                : $"the formal parameter ({formal}) and the argument ({argument}) are not of the same type — two type "
                  + "declarations of one type-name are the same type only when equivalent (ISO §14.8.2.2; §8.5.3.1)";
        if (formal.Shape is ActivationShape.VariableLengthGroup || argument.Shape is ActivationShape.VariableLengthGroup)
            return VariableLengthViolation(argument, formal, "argument", "formal parameter");
        return formal.Shape switch
        {
            // §14.8.2.2 rule 1: "the formal parameter shall be described with the same number or a smaller number of
            // bytes as the corresponding argument" — the callee sees the argument's leading positions.
            ActivationShape.AlphanumericGroup => formal.Positions <= argument.Positions ? null
                : $"the formal parameter ({formal.Positions} character positions) is larger than the argument "
                  + $"({argument.Positions}) (ISO §14.8.2.2 rule 1)",
            // §14.8.2.3.2 "Additionally" b) / c): a bit / national group matches the same number of positions.
            ActivationShape.AsIfElementaryGroup => formal.Positions == argument.Positions ? null
                : $"the formal parameter ({formal}) and the argument ({argument}) have different numbers of positions "
                  + "(ISO §14.8.2.3.2 \"Additionally\" b) / c))",
            _ => null,
        };
    }

    /// <summary>ISO §9.3.6 match rules 6 and 7: the method's returning item <paramref name="sending"/> "may be a sending
    /// item in a SET statement" (an OBJECT REFERENCE, POINTER or INDEX returning item of the invocation,
    /// <paramref name="receiving"/>) or "in a MOVE statement" (every other one). A SET admits only the same reference
    /// class; a MOVE admits none of them. Which object classes a SET admits is a run-time question through a universal
    /// receiver, decided at the delivery (<see cref="CobolObject.NarrowUniversal{T}"/>).</summary>
    public static bool ReturningMatches(ActivationDescription receiving, ActivationDescription sending) =>
        string.Equals(SetClass(receiving), SetClass(sending), StringComparison.Ordinal);

    /// <summary>The SET class of a returning description (§9.3.6 rules 6 / 7): "object", a pointer category, "index",
    /// or the empty string for a description received by a MOVE.</summary>
    private static string SetClass(ActivationDescription d) =>
        d.Shape is ActivationShape.ObjectReference ? ActivationCategory.Object
        : d.Shape is ActivationShape.Elementary && d.Category is ActivationCategory.DataPointer
            or ActivationCategory.ProgramPointer or ActivationCategory.FunctionPointer or ActivationCategory.Index
            ? d.Category
            : "";

    /// <summary>ISO §14.8.3 for the returning item of a method §9.3.6 has BOUND (§14.9.23.4 GR7 c)):
    /// <paramref name="sending"/> is the method's returning item, <paramref name="receiving"/> the invocation's. Null when
    /// the pair conforms, else the reason.</summary>
    public static string? ReturningViolation(ActivationDescription receiving, ActivationDescription sending)
    {
        if (sending.AnyLength)
            return "the returning item is described with the ANY LENGTH clause, which a method invoked through a "
                + "universal object reference shall not have (ISO §14.9.23.4 GR7 c))";
        // §14.8.3.3 rule 1 / rule 2: "the conformance rules are the same as if a SET statement were performed" — and
        // through a universal receiver which classes a SET admits is the object's run-time class, which the delivery
        // checks (CobolObject.NarrowUniversal; rule 2 b) 4.: the sending operand has the universal description).
        if (receiving.Shape is ActivationShape.ObjectReference) return null;
        if (receiving.Shape is ActivationShape.StrongGroup || sending.Shape is ActivationShape.StrongGroup)
            return receiving.StrongType is not null
                   && string.Equals(receiving.StrongType, sending.StrongType, StringComparison.Ordinal)
                ? null
                : $"the returning items ({sending} / {receiving}) are not of the same type (ISO §14.8.3.2; §8.5.3.1)";
        if (receiving.Shape is ActivationShape.VariableLengthGroup || sending.Shape is ActivationShape.VariableLengthGroup)
            return VariableLengthViolation(receiving, sending, "receiving item", "returning item");
        // §14.8.3.2: an alphanumeric group opposite "an alphanumeric group item or an elementary item of category
        // alphanumeric", and "the receiving operand shall be of the same length as the sending operand".
        if (receiving.Shape is ActivationShape.AlphanumericGroup || sending.Shape is ActivationShape.AlphanumericGroup)
            return IsAlphanumericGroupPartner(receiving) && IsAlphanumericGroupPartner(sending)
                   && receiving.Positions == sending.Positions
                ? null
                : $"the returning item ({sending}) and the receiving item ({receiving}) are not two alphanumeric items "
                  + "of the same length (ISO §14.8.3.2)";
        // §14.8.3.3 "Additionally" 2) / 3): a bit / national group matches a bit / national item of its positions.
        if (receiving.Shape is ActivationShape.AsIfElementaryGroup || sending.Shape is ActivationShape.AsIfElementaryGroup)
            return AsIfElementaryPartners(receiving, sending) ? null
                : $"the returning item ({sending}) and the receiving item ({receiving}) do not match (ISO §14.8.3.3 "
                  + "\"Additionally\" 2) / 3))";
        // §14.8.3.3: "the receiving operand shall have the same ALIGN, BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED,
        // PICTURE, SIGN, and USAGE clauses" (the class-pointer restriction is its USAGE clause's TO phrase).
        return string.Equals(receiving.Category, sending.Category, StringComparison.Ordinal)
               && string.Equals(receiving.Clauses, sending.Clauses, StringComparison.Ordinal)
               && SameLocaleIdentification(receiving, sending)
            ? null
            : $"the returning item ({sending}) and the receiving item ({receiving}) are not described with the same "
              + "clauses (ISO §14.8.3.3)";
    }

    /// <summary>The side of a §14.8.3.2 alphanumeric-group pair: an alphanumeric group, or an elementary item of
    /// category alphanumeric (whose <see cref="ActivationDescription.Positions"/> is its length).</summary>
    private static bool IsAlphanumericGroupPartner(ActivationDescription d) =>
        d.Shape is ActivationShape.AlphanumericGroup
        || d.Shape is ActivationShape.Elementary && d.Category is ActivationCategory.Alphanumeric;

    /// <summary>§14.8.3.3 "Additionally" 2) / 3): a bit group and a bit group or elementary bit item, a national group
    /// and a national group or elementary national item, of the same number of positions.</summary>
    private static bool AsIfElementaryPartners(ActivationDescription a, ActivationDescription b)
    {
        static bool Partner(ActivationDescription group, ActivationDescription other) =>
            other.Positions == group.Positions
            && (other.Shape is ActivationShape.AsIfElementaryGroup
                    ? string.Equals(other.Category, group.Category, StringComparison.Ordinal)
                    : other.Shape is ActivationShape.Elementary
                      && string.Equals(other.Usage, group.Usage, StringComparison.Ordinal));
        return a.Shape is ActivationShape.AsIfElementaryGroup ? Partner(a, b) : Partner(b, a);
    }

    /// <summary>§14.8.2.2 / §14.8.3.2: "If either … is a variable length group, … shall be compatible, as described
    /// in 8.5.1.12" — the ONE §8.5.1.12 walk (<see cref="GroupCompatibility.Walk"/>, the walk the compiler's bind-time
    /// screen asks) over the atoms both descriptions carry. A variable-length group is compatible only with a GROUP
    /// (§8.5.1.12.1: "unless the other operand is a compatible group"). A fixed-length partner is converted through the
    /// carrier's character correspondence (<see cref="CobolVarGroup.CorrespondingSpans"/>), so a pair whose
    /// correspondence that carrier cannot state is not admitted either. Null when compatible.</summary>
    private static string? VariableLengthViolation(ActivationDescription a, ActivationDescription b, string aRole,
        string bRole)
    {
        string Refusal(string why) => $"the {aRole} ({a}) and the {bRole} ({b}) are not compatible: {why}";
        bool aVar = a.Shape is ActivationShape.VariableLengthGroup, bVar = b.Shape is ActivationShape.VariableLengthGroup;
        if (!IsGroupPartner(a) || !IsGroupPartner(b))
            return Refusal("a variable-length group is compatible only with a group (ISO §8.5.1.12.1)");
        if (GroupCompatibility.Walk(AtomsOf(a), AtomsOf(b)) is { } why)
            return Refusal($"{GroupMismatchText(why)} (ISO §8.5.1.12)");
        if (aVar && bVar) return null;
        var (fixedSide, varSide) = aVar ? (b, a) : (a, b);
        return CobolVarGroup.CorrespondingSpans(GroupCompatibility.Layout(AtomsOf(fixedSide)),
                   GroupCompatibility.Layout(AtomsOf(varSide))) is not null
            ? null
            : Refusal("a variable-length group is compatible only with a group whose items correspond to its "
                      + "variable-length items (ISO §8.5.1.12.1 / §8.5.1.12.2)");
    }

    /// <summary>A side §8.5.1.12 can pair: a variable-length group or a fixed-length alphanumeric group.</summary>
    private static bool IsGroupPartner(ActivationDescription d) =>
        d.Shape is ActivationShape.VariableLengthGroup or ActivationShape.AlphanumericGroup;

    /// <summary>A group description's atoms — one run of its positions when it carries none (a group with a USAGE BIT
    /// leaf).</summary>
    internal static GroupAtom[] AtomsOf(ActivationDescription d) => d.Atoms ?? GroupCompatibility.FixedRun(d.Positions);

    /// <summary>A §8.5.1.12 reason in run-time words: the descriptions carry no item names, so it names the rule and the
    /// relative byte position.</summary>
    private static string GroupMismatchText(GroupMismatch m) => m.Kind switch
    {
        GroupMismatchKind.DynamicLengthUnpaired =>
            $"a dynamic-length item at relative byte position {m.Position} has no dynamic-length item opposite it",
        GroupMismatchKind.DynamicLengthBeyondEnd =>
            "a dynamic-length item lies beyond the last byte of the other group",
        GroupMismatchKind.DynamicTableOppositeNonTable or GroupMismatchKind.DynamicTableUnpaired =>
            $"a dynamic-capacity table at relative byte position {m.Position} has no table opposite it",
        GroupMismatchKind.ElementBytesDiffer =>
            $"the corresponding tables at relative byte position {m.Position} have elements of different byte lengths",
        _ => $"the corresponding tables at relative byte position {m.Position} have incompatible elements",
    };
}
