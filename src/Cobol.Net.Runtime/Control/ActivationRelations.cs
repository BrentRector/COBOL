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
/// <para>A CALL whose activated program is located by name at run time asks the CONFORMANCE relations too
/// (<see cref="CallArgumentViolation"/>, <see cref="CallReturningViolation"/>; kb/Work PB165), through
/// <see cref="BoundaryItem"/> at <c>ProgramTable.CallProgram</c>. The §14.8.2.2 group rules, the §14.8.2.3.2
/// object-reference identity and the §14.8.3 returning rules are ONE copy each, shared by both lanes.</para>
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
            // 3 d) 4./5.: a condition on the VALUE, not on the argument's description — "the corresponding parameter
            // shall evaluate to an object reference of the same class specified in the invocation" / "to the factory of
            // the class specified in the invocation" (kb/Work PB1112's universal leg). 3 d) 1.–3.: the same description.
            return formal.ObjectKind is ObjectReferenceKind.ActiveClass
                ? ActiveClassValueMatches(value, formal.Factory, receiver)
                : SameObjectReferenceDescription(argument, formal);
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
        // The match already required the same type-NAME of a strongly-typed group (§8.5.2.1: it is the class and
        // category) and the same class of every other pair, so what §14.8.2.2 still asks is the type's §8.5.3.1
        // equivalence, the §8.5.1.12 compatibility and rule 1's sizes — the ONE group arm the CALL lane asks too.
        if (ReferenceGroupViolation(argument, formal) is { } groupWhy) return groupWhy;
        // §14.8.2.3.2 "Additionally" b) / c): a bit / national group matches the same number of positions.
        return formal.Shape is ActivationShape.AsIfElementaryGroup && formal.Positions != argument.Positions
            ? $"the formal parameter ({formal}) and the argument ({argument}) have different numbers of positions "
              + "(ISO §14.8.2.3.2 \"Additionally\" b) / c))"
            : null;
    }

    /// <summary>§14.8.2.3.2's object-reference rules 1–3 (and §9.3.6 match rule 3 d) 1.–3., their twin): "If either …
    /// is a universal object reference, the corresponding … shall be a universal object reference"; "… described with an
    /// interface-name, … the same interface-name"; "… described with an object-class-name, … the same object-class-name,
    /// and the FACTORY and ONLY phrases shall be the same". Two ACTIVE-CLASS descriptions are the same description when
    /// their FACTORY phrases agree (rule 4 asks the invocation, which only a method has).</summary>
    private static bool SameObjectReferenceDescription(ActivationDescription argument, ActivationDescription formal) =>
        formal.ObjectKind switch
        {
            ObjectReferenceKind.Universal => argument.ObjectKind is ObjectReferenceKind.Universal,
            ObjectReferenceKind.Interface => argument.ObjectKind is ObjectReferenceKind.Interface
                && CobolNames.Same(argument.ObjectName, formal.ObjectName),
            ObjectReferenceKind.ObjectClass => argument.ObjectKind is ObjectReferenceKind.ObjectClass
                && CobolNames.Same(argument.ObjectName, formal.ObjectName)
                && argument.Factory == formal.Factory && argument.Only == formal.Only,
            ObjectReferenceKind.ActiveClass => argument.ObjectKind is ObjectReferenceKind.ActiveClass
                && argument.Factory == formal.Factory,
            _ => false,
        };

    /// <summary>⛔ ISO §14.8.2.2 FOR A PARAMETER PASSED BY REFERENCE, the one copy both lanes ask (a universal INVOKE's
    /// bound method, <see cref="ParameterViolation"/>, and a CALL, <see cref="CallArgumentViolation"/>): "If either the
    /// formal parameter or the corresponding argument is a strongly-typed group item, both shall be of the same type"
    /// (§8.5.3.1's equivalence, <see cref="ActivationDescription.StrongType"/>); "If either … is a variable length group,
    /// … shall be compatible, as described in 8.5.1.12"; and rule 1 for an alphanumeric group: "that argument or the
    /// formal parameter … shall be an alphanumeric group item or an elementary item of category alphanumeric, and the
    /// formal parameter shall be described with the same number or a smaller number of bytes as the corresponding
    /// argument". Null when neither side is such a group, or when the pair conforms.</summary>
    private static string? ReferenceGroupViolation(ActivationDescription argument, ActivationDescription formal)
    {
        if (formal.Shape is ActivationShape.StrongGroup || argument.Shape is ActivationShape.StrongGroup)
            return formal.StrongType is not null
                   && string.Equals(formal.StrongType, argument.StrongType, StringComparison.Ordinal)
                ? null
                : $"the formal parameter ({formal}) and the argument ({argument}) are not of the same type — two type "
                  + "declarations of one type-name are the same type only when equivalent (ISO §14.8.2.2; §8.5.3.1)";
        if (formal.Shape is ActivationShape.VariableLengthGroup || argument.Shape is ActivationShape.VariableLengthGroup)
            return VariableLengthViolation(argument, formal, "argument", "formal parameter");
        if (formal.Shape is not ActivationShape.AlphanumericGroup && argument.Shape is not ActivationShape.AlphanumericGroup)
            return null;
        if (!IsAlphanumericGroupPartner(formal) || !IsAlphanumericGroupPartner(argument))
            return $"the formal parameter ({formal}) and the argument ({argument}): opposite an alphanumeric group the other "
                + "item shall be an alphanumeric group item or an elementary item of category alphanumeric (ISO §14.8.2.2 rule 1)";
        return formal.Positions <= argument.Positions ? null
            : $"the formal parameter ({formal.Positions} character positions) is larger than the argument "
              + $"({argument.Positions}) (ISO §14.8.2.2 rule 1)";
    }

    /// <summary>⛔ ISO §14.8.2 FOR ONE ARGUMENT OF A CALL whose activated program is located by name at run time (kb/Work
    /// PB165): a dynamic Format-1 CALL, or a program-prototype whose details §12.3.8.4 GR10 c) takes from the external
    /// repository, so no formal was known when the activating element was compiled. §14.9.4.4 GR3 d): "the rules for
    /// conformance specified in 14.8.2, Parameters and 14.8.3, Returning items apply. If a violation of these rules is
    /// detected, the EC-PROGRAM-ARG-MISMATCH exception condition is set to exist". Null when the pair conforms, else the
    /// reason.
    /// <para><paramref name="specifiedProgram"/> selects the elementary rule both §14.8.2.3.2 and §14.8.2.3.3 split on: rule
    /// 1 governs "a program for which there is no program-specifier in the REPOSITORY paragraph of the activating element
    /// and there is no NESTED phrase", and there "the formal parameter shall be of the same length as the corresponding
    /// argument" — a LENGTH, which this relation does not hold (<see cref="BoundaryItem.Length"/> does, and
    /// <see cref="BoundaryItem.ArgumentViolation"/> asks it when this answers null); rule 2 governs a program "for which
    /// there is a program-specifier", where BY REFERENCE "the definition of the formal parameter and the definition of the
    /// argument shall have the same ALIGN, BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE clauses"
    /// and BY CONTENT / BY VALUE a COMPUTE, a SET or a MOVE shall be valid. Every other paragraph — the object-reference
    /// and class-pointer rules, §14.8.2.2's group rules, the SET paragraph of §14.8.2.3.3 — governs both lanes alike.</para></summary>
    public static string? CallArgumentViolation(CobolPassMode mode, ActivationDescription argument,
        ActivationDescription formal, bool specifiedProgram)
    {
        if (argument.Shape is ActivationShape.Omitted) return null;
        return mode is CobolPassMode.Reference
            ? ByReferenceViolation(argument, formal, specifiedProgram)
            : ByContentViolation(argument, formal, specifiedProgram);
    }

    /// <summary>Whether §14.8.2.3.2 / §14.8.2.3.3 rule 1's LENGTH sentence governs a pair: two items treated as
    /// elementary (§14.8.2.1: a bit or national group is one) neither of which is of class object or pointer, and no
    /// value without storage, whose length rule 1 cannot measure.</summary>
    public static bool IsLengthRulePair(ActivationDescription argument, ActivationDescription formal) =>
        IsElementaryData(argument) && IsElementaryData(formal);

    private static bool IsElementaryData(ActivationDescription d) =>
        d.Shape is ActivationShape.Elementary or ActivationShape.AsIfElementaryGroup && SetClass(d) is null;

    /// <summary>§14.8.2.3.2, Elementary items passed by reference — and §14.8.2.2 rule 1 through
    /// <see cref="ReferenceGroupViolation"/>.</summary>
    private static string? ByReferenceViolation(ActivationDescription argument, ActivationDescription formal,
        bool specifiedProgram)
    {
        // "If either the formal parameter or the corresponding argument is an object reference, the corresponding
        // argument or formal parameter shall be an object reference following these rules" (rules 1–3).
        if (formal.Shape is ActivationShape.ObjectReference || argument.Shape is ActivationShape.ObjectReference)
            return formal.Shape is ActivationShape.ObjectReference && argument.Shape is ActivationShape.ObjectReference
                   && SameObjectReferenceDescription(argument, formal)
                ? null
                : $"the formal parameter ({formal}) and the argument ({argument}) are not object references of the same "
                  + "description (ISO §14.8.2.3.2 rules 1-3)";
        // "If either the argument or the formal parameter is of class pointer, the corresponding formal parameter or
        // argument shall be of class pointer and the corresponding items shall be of the same category. If either is a
        // restricted pointer, both shall be restricted and of the same type" — the restriction is the USAGE clause's TO
        // phrase, which Clauses carries (Unrestricted for none).
        if (IsPointerClass(formal) || IsPointerClass(argument))
            return IsPointerClass(formal) && IsPointerClass(argument)
                   && string.Equals(formal.Category, argument.Category, StringComparison.Ordinal)
                   && string.Equals(formal.Clauses, argument.Clauses, StringComparison.Ordinal)
                ? null
                : $"the formal parameter ({formal}) and the argument ({argument}) are not pointers of the same category and "
                  + "restriction (ISO §14.8.2.3.2)";
        if (ReferenceGroupViolation(argument, formal) is { } groupWhy) return groupWhy;
        if (!IsElementaryData(argument) || !IsElementaryData(formal) || !specifiedProgram) return null;
        // Rule 2. "Additionally" b) / c): a bit group matches an elementary bit item, a national group an elementary
        // national item, of the same number of positions.
        if (argument.Shape is ActivationShape.AsIfElementaryGroup || formal.Shape is ActivationShape.AsIfElementaryGroup)
            return AsIfElementaryPartners(argument, formal) ? null
                : $"the formal parameter ({formal}) and the argument ({argument}) do not match (ISO §14.8.2.3.2 "
                  + "\"Additionally\" b) / c))";
        // "Additionally" e): "If the argument is described with the ANY LENGTH clause, the corresponding formal parameter
        // shall be described with the ANY LENGTH clause."
        if (argument.AnyLength && !formal.AnyLength)
            return $"the argument ({argument}) is described with the ANY LENGTH clause and the formal parameter is not "
                + "(ISO §14.8.2.3.2 \"Additionally\" e))";
        // "Additionally" d): an ANY LENGTH formal's "length is considered to match the length of the corresponding
        // argument" — its one-symbol PICTURE has no length of its own (§13.18.2), so what remains comparable is the class
        // and category and the usage. (§13.18.2.3 SR2 confines ANY LENGTH to a function, a contained program or a method,
        // none of which a CALL reaches through a program-specifier: this arm is completeness, not a common path.)
        if (formal.AnyLength)
            return string.Equals(formal.Category, argument.Category, StringComparison.Ordinal)
                   && string.Equals(formal.Usage, argument.Usage, StringComparison.Ordinal)
                ? null
                : $"the formal parameter ({formal}) and the argument ({argument}) are not of the same category and usage "
                  + "(ISO §14.8.2.3.2 rule 2, \"Additionally\" d))";
        return SameElementaryClauses(argument, formal) ? null
            : $"the formal parameter ({formal}) and the argument ({argument}) are not described with the same ALIGN, "
              + "BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN and USAGE clauses (ISO §14.8.2.3.2 rule 2)";
    }

    /// <summary>§14.8.2.3.3, Elementary items passed by content or by value — and §14.8.2.2 rule 2 for a group.</summary>
    private static string? ByContentViolation(ActivationDescription argument, ActivationDescription formal,
        bool specifiedProgram)
    {
        // "If the formal parameter is of class pointer or an object reference described without the ACTIVE-CLASS phrase,
        // the conformance rules shall be the same as if a SET statement were performed in the activating runtime element
        // with the argument as the sending operand and the corresponding formal parameter as the receiving operand." The
        // predefined NULL is the one value a SET sends there (§14.9.39.3 Formats 5, 7, 8, 9); it is no MOVE operand.
        if (SetClass(formal) is { } setClass and not ActivationCategory.Index)
            return argument.Shape is ActivationShape.PredefinedNull || MaySet(setClass, formal, argument) ? null
                : $"the argument ({argument}) is not a valid sending operand of a SET into the formal parameter ({formal}) "
                  + "(ISO §14.8.2.3.3)";
        if (argument.Shape is ActivationShape.PredefinedNull)
            return "NULL is a sending operand only of a SET into a data item of class pointer or object reference, and the "
                + $"formal parameter ({formal}) is neither (ISO §14.8.2.3.3; §8.4.3.10.3)";
        // §14.8.2.2 rule 2: "If the argument is passed by content, the conformance rules are the same as for a MOVE
        // statement with the argument as the sending operand and the corresponding formal parameter as the receiving
        // operand" — a strongly-typed group and a variable-length group included (their sentences are MOVE's SR2 / SR9
        // too), and an arithmetic expression has no character image for a group move to copy (§14.9.25.4 GR4).
        if (IsGroup(formal) || IsGroup(argument))
            return argument.Shape is ActivationShape.Expression
                ? $"an arithmetic expression has no character image for the group move into the formal parameter ({formal}) "
                  + "to copy (ISO §14.8.2.2 rule 2; §14.9.25.4 GR4)"
                : ContentMoveViolation(argument, formal);
        if (!specifiedProgram) return null;   // rule 1 — the length sentence, BoundaryItem's
        // Rule 2 b): "If the formal parameter is an index data item, … the same as for a SET statement": identifier-2 "shall
        // reference a data item of class index" (§14.9.39.3 SR2), and SR3 refuses a literal or an expression.
        if (formal.Category is ActivationCategory.Index)
            return argument.Category is ActivationCategory.Index && argument.Shape is ActivationShape.Elementary ? null
                : $"the argument ({argument}) is not a data item of class index, which a SET into an index data item "
                  + "formal parameter requires (ISO §14.8.2.3.3 rule 2 b); §14.9.39.3 SR2 / SR3)";
        // Rule 2 a): "If the formal parameter is numeric, the conformance rules are the same as for a COMPUTE statement" —
        // a numeric sending operand (§8.8.1.1: an identifier of class numeric, a numeric literal, ZERO, an expression).
        if (formal.Category is ActivationCategory.Numeric && !formal.AnyLength)
            return argument.Category is ActivationCategory.Numeric or ActivationCategory.FigurativeZero ? null
                : $"the argument ({argument}) is not a numeric sending operand of the COMPUTE into the numeric formal "
                  + $"parameter ({formal}) (ISO §14.8.2.3.3 rule 2 a); §8.8.1.1)";
        // Rule 2 c): an ANY LENGTH formal's "length is considered to match" — LENGTH only, so rule 2 d)'s MOVE asks the
        // category pair. Rule 2 d): "Otherwise, the conformance rules are the same as for a MOVE statement".
        return ContentMoveViolation(argument, formal);
    }

    /// <summary>The MOVE question of §14.8.2.2 rule 2 / §14.8.2.3.3 rule 2 d) with the argument as the sending operand:
    /// <see cref="MayMove"/> for an argument with storage or a literal of a known category, and for a figurative constant
    /// or an ALL literal, whose category the receiving context chooses (§8.3.3.6.4 GR1), a MOVE valid under ANY of the three
    /// categories it can take — the reading the compiler's own screen gives the same argument when it knows the formal
    /// (<c>OoConformance.ContentAlphanumericLiteralMismatch</c>).</summary>
    private static string? ContentMoveViolation(ActivationDescription argument, ActivationDescription formal)
    {
        bool valid = argument.Category is ActivationCategory.Figurative or ActivationCategory.FigurativeZero
            && argument.Shape is ActivationShape.Literal
            ? FigurativeCategories.Any(c => MayMove(argument with { Table16 = c }, formal))
            : MayMove(argument, formal);
        return valid ? null
            : $"the argument ({argument}) is not a valid sending operand of a MOVE to the formal parameter ({formal}) "
              + "(ISO §14.8.2.3.3 rule 2 d) / §14.8.2.2 rule 2; §14.9.25.3)";
    }

    /// <summary>The Table 16 rows a figurative constant or an ALL literal can stand in (§8.3.3.6.4 GR1).</summary>
    private static readonly Table16Category[] FigurativeCategories =
        [Table16Category.Alphanumeric, Table16Category.National, Table16Category.Boolean];

    /// <summary>ISO §14.8.3 for the RETURNING items of a CALL whose activated program is located by name at run time (kb/Work
    /// PB165, PB1040): <paramref name="sending"/> is the activated program's returning item, <paramref name="receiving"/>
    /// the activating element's. §14.8.3 does not split on the program-specifier as §14.8.2 does, so every lane asks the
    /// one rule — <see cref="ReturningConformance"/>, with §14.8.3.3 "Additionally" 4) and 5) for ANY LENGTH. Null when
    /// the pair conforms, else the reason.</summary>
    public static string? CallReturningViolation(ActivationDescription receiving, ActivationDescription sending)
    {
        // 4) "If the receiving operand is described with the ANY LENGTH clause, the sending operand shall also be
        // described with the ANY LENGTH clause."
        if (receiving.AnyLength && !sending.AnyLength)
            return $"the receiving item ({receiving}) is described with the ANY LENGTH clause and the returning item "
                + $"({sending}) is not (ISO §14.8.3.3 \"Additionally\" 4))";
        // 5) "If the sending operand is described with the ANY LENGTH clause, the length of the sending operand is
        // considered to match the length of the receiving operand" — what remains comparable is the category and usage.
        if (sending.AnyLength && !receiving.AnyLength)
            return string.Equals(receiving.Category, sending.Category, StringComparison.Ordinal)
                   && string.Equals(receiving.Usage, sending.Usage, StringComparison.Ordinal)
                ? null
                : $"the returning item ({sending}) and the receiving item ({receiving}) are not of the same category and "
                  + "usage (ISO §14.8.3.3 \"Additionally\" 5))";
        return ReturningConformance(receiving, sending);
    }

    private static bool IsPointerClass(ActivationDescription d) =>
        d.Shape is ActivationShape.Elementary && d.Category is ActivationCategory.DataPointer
            or ActivationCategory.ProgramPointer or ActivationCategory.FunctionPointer;

    private static bool IsGroup(ActivationDescription d) =>
        d.Shape is ActivationShape.AlphanumericGroup or ActivationShape.StrongGroup or ActivationShape.VariableLengthGroup;

    /// <summary>§14.8.2.3.2 rule 2 / §14.8.3.3 / §9.3.6 match rule 3 e): the same class and category, the same clause
    /// identity (<see cref="ActivationDescription.Clauses"/> — ALIGN, BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE
    /// with its currency and DECIMAL-POINT IS COMMA facets, SIGN, USAGE and the LOCALE phrase's SIZE) and the same LOCALE
    /// external identification ("Additionally" a)).</summary>
    private static bool SameElementaryClauses(ActivationDescription a, ActivationDescription b) =>
        string.Equals(a.Category, b.Category, StringComparison.Ordinal)
        && string.Equals(a.Clauses, b.Clauses, StringComparison.Ordinal)
        && SameLocaleIdentification(a, b);

    /// <summary>ISO §9.3.6 match rules 6 and 7, split on the INVOCATION's returning item <paramref name="receiving"/>:
    /// when it is "usage OBJECT REFERENCE, POINTER or INDEX" the method's returning item <paramref name="sending"/> "may
    /// be a sending item in a SET statement with the returning item as the receiving item" (rule 6,
    /// <see cref="MaySet"/>), otherwise it "may be a sending item in a MOVE statement" (rule 7, <see cref="MayMove"/>).
    /// "May be" is the statement's VALIDITY — its syntax rules over the two descriptions — not only which statement
    /// applies: a method returning <c>PIC 9V99</c> does not match an invocation returning <c>PIC X(4)</c> (Table 16:
    /// Numeric Noninteger → Alphanumeric is "No"), so the search goes on up the INHERITS chain and ends in EC-OO-METHOD
    /// (§9.3.6 6)) rather than binding the method and failing its §14.8.3 conformance (kb/Work PB2076).</summary>
    public static bool ReturningMatches(ActivationDescription receiving, ActivationDescription sending) =>
        SetClass(receiving) is { } setClass ? MaySet(setClass, receiving, sending) : MayMove(sending, receiving);

    /// <summary>The class a SET statement receives a description into (§9.3.6 rule 6's "usage OBJECT REFERENCE,
    /// POINTER or INDEX"; POINTER read as the class, §8.5.2.1 Table 2's three pointer categories, since a
    /// program-pointer or function-pointer receiver is no MOVE operand either, §14.9.25.3 SR1): "object", the pointer
    /// category, "index" — or null for a description a MOVE receives.</summary>
    private static string? SetClass(ActivationDescription d) =>
        d.Shape is ActivationShape.ObjectReference ? ActivationCategory.Object
        : d.Shape is ActivationShape.Elementary && d.Category is ActivationCategory.DataPointer
            or ActivationCategory.ProgramPointer or ActivationCategory.FunctionPointer or ActivationCategory.Index
            ? d.Category
            : null;

    /// <summary>§9.3.6 rule 6: is a SET of <paramref name="sending"/> into <paramref name="receiving"/> (of SET class
    /// <paramref name="setClass"/>) valid by ISO §14.9.39.3?
    /// <list type="bullet">
    /// <item>object — Format 5 SR9: "Identifier-4 shall be an object reference". ⚠ SR10–SR14's CLASS conditions (the
    /// sender's class the receiver's class or a subclass, an interface it implements, the ONLY and FACTORY phrases) are
    /// NOT asked here: a description carries a class NAME and the run time has no class hierarchy to read it against,
    /// so an unrelated class matches and the delivery refuses the object (<see cref="CobolObject.NarrowUniversal{T}"/>,
    /// EC-OO-UNIVERSAL where rule 6 makes it a non-match, EC-OO-METHOD) — kb/Work PB2463.</item>
    /// <item>index — Format 1 SR2: "Identifier-2 shall reference a data item of class index".</item>
    /// <item>data-pointer — Format 7 SR17 / SR19: a data-pointer, and a restricted one on either side needs the other
    /// "restricted to the same type".</item>
    /// <item>program-pointer — Format 9 SR21 / SR22: a program-pointer, and a RESTRICTED receiver needs a sender whose
    /// program-prototype has the same signature; an unrestricted receiver takes any.</item>
    /// <item>function-pointer — Format 8 SR20: "The function-prototypes associated with identifier-12 and
    /// identifier-13 shall have the same signature".</item>
    /// </list>
    /// A pointer's restriction is its USAGE clause's TO phrase as <see cref="ActivationDescription.Clauses"/> carries
    /// it (<see cref="ActivationDescription.Unrestricted"/> for none): for a prototype pointer the SIGNATURE class of its
    /// prototype (§13.18.60.4 GR25 / GR26), so two differently named prototypes of one signature share one restriction
    /// and the pair is admitted — which is what "the same signature" of SR20 / SR22 asks (kb/Work PB2464); for a
    /// data-pointer its TYPE identity.</summary>
    private static bool MaySet(string setClass, ActivationDescription receiving, ActivationDescription sending)
    {
        if (!string.Equals(SetClass(sending), setClass, StringComparison.Ordinal)) return false;
        return setClass switch
        {
            ActivationCategory.ProgramPointer => receiving.Clauses == ActivationDescription.Unrestricted
                                                 || string.Equals(receiving.Clauses, sending.Clauses, StringComparison.Ordinal),
            ActivationCategory.DataPointer or ActivationCategory.FunctionPointer =>
                string.Equals(receiving.Clauses, sending.Clauses, StringComparison.Ordinal),
            _ => true,
        };
    }

    /// <summary>§9.3.6 rule 7: is a MOVE of <paramref name="sending"/> to <paramref name="receiving"/> valid by ISO
    /// §14.9.25.3? Its syntax rules over two data items, in SR order: SR1 (neither of class index, object or pointer —
    /// the receiving side is not, or rule 6 would apply), SR2 (a strongly-typed group receiver takes only a group of the
    /// same type), SR8 (a binary-char/-short/-long/-double sender needs a numeric or numeric-edited receiver), SR9
    /// (a variable-length group only to or from a compatible group, §8.5.1.12) and SR10 (Table 16). SR8 and SR10 are
    /// the ONE copy the compiler's MOVE screens read (<see cref="MoveValidity"/>).</summary>
    private static bool MayMove(ActivationDescription sending, ActivationDescription receiving)
    {
        if (SetClass(sending) is not null) return false;                                                    // SR1
        if (receiving.Shape is ActivationShape.StrongGroup
            && (sending.Shape is not ActivationShape.StrongGroup
                || !string.Equals(receiving.StrongType, sending.StrongType, StringComparison.Ordinal)))
            return false;                                                                                     // SR2
        if (MoveValidity.BinaryWidthRefusal(sending.BinaryWidth, receiving.Table16) is not null) return false; // SR8
        if ((sending.Shape is ActivationShape.VariableLengthGroup || receiving.Shape is ActivationShape.VariableLengthGroup)
            && VariableLengthViolation(sending, receiving, "sending item", "receiving item") is not null)
            return false;                                                                                     // SR9
        return MoveValidity.Table16Refusal(sending.Table16, receiving.Table16) is null;                       // SR10
    }

    /// <summary>ISO §14.8.3 for the returning item of a method §9.3.6 has BOUND (§14.9.23.4 GR7 c)):
    /// <paramref name="sending"/> is the method's returning item, <paramref name="receiving"/> the invocation's. Null when
    /// the pair conforms, else the reason.</summary>
    public static string? ReturningViolation(ActivationDescription receiving, ActivationDescription sending)
    {
        if (sending.AnyLength)
            return "the returning item is described with the ANY LENGTH clause, which a method invoked through a "
                + "universal object reference shall not have (ISO §14.9.23.4 GR7 c))";
        return ReturningConformance(receiving, sending);
    }

    /// <summary>⛔ ISO §14.8.3's conformance of a pair of returning items, the one copy a universal INVOKE's bound method
    /// (<see cref="ReturningViolation"/>) and a CALL (<see cref="CallReturningViolation"/>) both ask; each lane asks its
    /// own ANY LENGTH rule first. Null when the pair conforms, else the reason.</summary>
    private static string? ReturningConformance(ActivationDescription receiving, ActivationDescription sending)
    {
        // §14.8.3.3: "If either of the operands is an object reference, the corresponding item shall be an object
        // reference", and rule 1 / rule 2: "the conformance rules are the same as if a SET statement were performed" —
        // which classes a SET admits is the object's run-time class, which the delivery checks
        // (CobolObject.NarrowUniversal; rule 2 b) 4.: through a universal receiver the sending operand has the universal
        // description).
        if (receiving.Shape is ActivationShape.ObjectReference || sending.Shape is ActivationShape.ObjectReference)
            return receiving.Shape is ActivationShape.ObjectReference && sending.Shape is ActivationShape.ObjectReference
                ? null
                : $"the returning item ({sending}) and the receiving item ({receiving}) are not both object references "
                  + "(ISO §14.8.3.3)";
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
    /// (§8.5.1.12.1: "unless the other operand is a compatible group"). A fixed-length partner is converted over the two
    /// shapes by the same walk (<see cref="CobolVarGroup.Reshape"/>, kb/Work PB2690), so the walk is the whole question.
    /// Null when compatible.</summary>
    private static string? VariableLengthViolation(ActivationDescription a, ActivationDescription b, string aRole,
        string bRole)
    {
        string Refusal(string why) => $"the {aRole} ({a}) and the {bRole} ({b}) are not compatible: {why}";
        if (!IsGroupPartner(a) || !IsGroupPartner(b))
            return Refusal("a variable-length group is compatible only with a group (ISO §8.5.1.12.1)");
        if (GroupCompatibility.Walk(AtomsOf(a), AtomsOf(b)) is { } why)
            return Refusal($"{GroupMismatchText(why)} (ISO §8.5.1.12)");
        return null;
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
