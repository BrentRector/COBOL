// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

/// <summary>One ISO §8.8.4.4.3 OPERAND rule, as a member of the set a class-condition alternative carries.
/// Each member is a rule of the standard's own shape — "shall reference a data item whose usage is …",
/// "shall not be specified if the category … is …" — so the alternative table below states WHICH rules name
/// an alternative and this enum states WHAT each rule says, once.</summary>
internal enum ClassOperandRule
{
    /// <summary>§8.8.4.4.3 SR4 — "ALPHABETIC, ALPHABETIC-LOWER, ALPHABETIC-UPPER, or class-name-1 shall not be
    /// specified if the category of the data item referenced by identifier-1 is boolean, numeric, or
    /// numeric-edited." ⚠ alphabet-name-1 is NOT in this rule's list, which is why the alphabet-name
    /// alternative carries its own <see cref="ClassConditionModel.AlphabetName"/> kind rather than being folded
    /// onto the class-name one (it was: one kind served both, so broadening SR4 from its boolean half to the
    /// rule as written would have rejected `IF NUM-ITEM IS SOME-ALPHABET`, which SR4 does not name).</summary>
    NotBooleanNumericOrNumericEdited,

    /// <summary>§8.8.4.4.3 SR5 — "BOOLEAN shall not be specified if the category of the data item referenced by
    /// identifier-1 is numeric or numeric-edited." One category short of SR4's list, deliberately: a BOOLEAN
    /// class test over a category-boolean item is the ordinary case.</summary>
    NotNumericOrNumericEdited,

    /// <summary>§8.8.4.4.3 SR3 — "If the alphabet-name-1, ALPHABETIC, ALPHABETIC-LOWER, ALPHABETIC-UPPER,
    /// BOOLEAN, or class-name-1 phrase is specified, identifier-1 shall reference a data-item whose usage is
    /// display or national."</summary>
    UsageDisplayOrNational,

    /// <summary>§8.8.4.4.3 SR8 — "If the NUMERIC phrase is specified, identifier-1 shall reference a data item
    /// whose usage is display or national or whose category is numeric." SR3's rule with the category escape,
    /// which is why NUMERIC is absent from SR3's list and present here.</summary>
    NumericUsageOrCategory,

    /// <summary>§8.8.4.4.3 SR6 — "If FARTHEST-FROM-ZERO, IN-ARITHMETIC-RANGE, or NEAREST-TO-ZERO is specified,
    /// identifier-1 shall reference a data item whose category is numeric." A POSITIVE requirement, unlike SR3–SR5
    /// and SR8: an alphanumeric group and a reference-modified slice (category alphanumeric, §8.4.3.3.4 GR6 c)
    /// BREAK it — the operand's CATEGORY is asked, never its base item's.</summary>
    NumericCategory,

    /// <summary>§8.8.4.4.3 SR7 — "If the FLOAT-INFINITY, FLOAT-NOT-A-NUMBER, FLOAT-NOT-A-NUMBER-QUIET, or
    /// FLOAT-NOT-A-NUMBER-SIGNALING phrase is specified, identifier-1 shall reference a data item described with a
    /// standard floating-point usage." STANDARD — §3.166's FLOAT-BINARY-32/-64/-128 and §3.167's
    /// FLOAT-DECIMAL-16/-34, read from <see cref="UsageFamilies"/> — so FLOAT-SHORT / FLOAT-LONG / FLOAT-EXTENDED /
    /// COMP-1 / COMP-2 break it, exactly as they break the SET Format 15 twin, §14.9.39.3 SR32.</summary>
    StandardFloatUsage,
}

/// <summary>One alternative of the ISO §8.8.4.4.2 class-condition general format: the tag the bound node and
/// the renderer key on, the phrase a diagnostic names it by, and the §8.8.4.4.3 rules that name it — in the
/// order they are APPLIED, category rules before usage rules, so an operand that breaks both is reported
/// against the rule that speaks about its category.</summary>
internal readonly record struct ClassAlternative(char Kind, string Spelling, ClassOperandRule[] Rules);

/// <summary>
/// ⛔ THE ISO §8.8.4.4.2 CLASS-CONDITION ALTERNATIVES AND THEIR §8.8.4.4.3 OPERAND RULES, WRITTEN DOWN ONCE.
/// </summary>
/// <remarks>
/// <para>The general format prints FOURTEEN alternatives in one brace group (the printed page was rendered:
/// PDF page 224 / printed 194 — every keyword alternative underlined, alphabet-name-1 and class-name-1 not,
/// no choice indicator, so exactly one is selected). Which of them this compiler OFFERS is a grammar question
/// (<c>classConditionName</c> in <c>Core/CobolExpressions.g4</c>); which OPERAND each admits is this table, and adding
/// an alternative is a row here plus a grammar alternative plus a renderer arm — never a new screen.</para>
/// <para>⛔ IT REPLACED THREE PARTIAL COPIES (kb/Work PB571 + PB590). The alternatives were enumerated in the
/// <c>classConditionName</c> grammar rule AND in a second grammar rule <c>classCondition</c> (whose list carried
/// ALPHANUMERIC — which §8.8.4.4.2 does not offer at all — and omitted BOOLEAN, class-name-1 and
/// alphabet-name-1), and the kind was decoded in <c>ConditionBinder.BindClassConditionOn</c> AND again in
/// <c>EvaluateBinder.SubjectAsCondition</c>. The operand SCREEN was a third partial: it returned early unless
/// the operand's category was boolean, so §8.8.4.4.3 SR1 — "Identifier-1 shall not reference a data item of
/// class index, message-tag, object, or pointer" — was never asked, and <c>IF IX IS NUMERIC</c> over a USAGE
/// INDEX item compiled clean and printed TRUE, answering a class question about a class-INDEX item as though
/// its class were numeric (§13.18.60.4 GR10: "The class and category of an index data item are index").</para>
/// <para>ALL FOURTEEN ARE OFFERED. The seven COBOL-2014 numeric-content alternatives — FARTHEST-FROM-ZERO,
/// FLOAT-INFINITY, FLOAT-NOT-A-NUMBER[-QUIET|-SIGNALING], IN-ARITHMETIC-RANGE and NEAREST-TO-ZERO — landed
/// together as seven rows over two new rules (SR6, SR7), which is what makes them one mechanism and not seven
/// (kb/Work PB225).</para>
/// </remarks>
internal static class ClassConditionModel
{
    /// <summary>NUMERIC (§8.8.4.4.4 GR3 n).</summary>
    public const char Numeric = 'N';
    /// <summary>ALPHABETIC (GR3 b).</summary>
    public const char Alphabetic = 'A';
    /// <summary>ALPHABETIC-UPPER (GR3 d).</summary>
    public const char AlphabeticUpper = 'U';
    /// <summary>ALPHABETIC-LOWER (GR3 c).</summary>
    public const char AlphabeticLower = 'L';
    /// <summary>BOOLEAN (GR3 e) — a COBOL-2002 introduction with boolean data.</summary>
    public const char Boolean = 'B';
    /// <summary>class-name-1, a SPECIAL-NAMES user-defined class (GR3 f).</summary>
    public const char ClassName = 'C';
    /// <summary>alphabet-name-1, the coded character set an alphabet identifies (GR3 a). ⚠ A DISTINCT kind from
    /// <see cref="ClassName"/> because SR4 names class-name-1 and NOT alphabet-name-1.</summary>
    public const char AlphabetName = 'S';
    /// <summary>FARTHEST-FROM-ZERO (GR3 g) — COBOL-2014.</summary>
    public const char FarthestFromZero = 'F';
    /// <summary>FLOAT-INFINITY (GR3 h) — COBOL-2014.</summary>
    public const char FloatInfinity = 'I';
    /// <summary>FLOAT-NOT-A-NUMBER (GR3 i) — COBOL-2014.</summary>
    public const char FloatNotANumber = 'X';
    /// <summary>FLOAT-NOT-A-NUMBER-QUIET (GR3 j) — COBOL-2014.</summary>
    public const char FloatNotANumberQuiet = 'Q';
    /// <summary>FLOAT-NOT-A-NUMBER-SIGNALING (GR3 k) — COBOL-2014.</summary>
    public const char FloatNotANumberSignaling = 'G';
    /// <summary>IN-ARITHMETIC-RANGE (GR3 l) — COBOL-2014.</summary>
    public const char InArithmeticRange = 'R';
    /// <summary>NEAREST-TO-ZERO (GR3 m) — COBOL-2014.</summary>
    public const char NearestToZero = 'Z';

    /// <summary>The offered alternatives, each with the §8.8.4.4.3 rules that name it.</summary>
    public static readonly ClassAlternative[] Alternatives =
    [
        new(Numeric, "NUMERIC", [ClassOperandRule.NumericUsageOrCategory]),
        new(Alphabetic, "ALPHABETIC",
            [ClassOperandRule.NotBooleanNumericOrNumericEdited, ClassOperandRule.UsageDisplayOrNational]),
        new(AlphabeticUpper, "ALPHABETIC-UPPER",
            [ClassOperandRule.NotBooleanNumericOrNumericEdited, ClassOperandRule.UsageDisplayOrNational]),
        new(AlphabeticLower, "ALPHABETIC-LOWER",
            [ClassOperandRule.NotBooleanNumericOrNumericEdited, ClassOperandRule.UsageDisplayOrNational]),
        new(Boolean, "BOOLEAN",
            [ClassOperandRule.NotNumericOrNumericEdited, ClassOperandRule.UsageDisplayOrNational]),
        new(ClassName, "class-name-1",
            [ClassOperandRule.NotBooleanNumericOrNumericEdited, ClassOperandRule.UsageDisplayOrNational]),
        new(AlphabetName, "alphabet-name-1", [ClassOperandRule.UsageDisplayOrNational]),
        new(FarthestFromZero, "FARTHEST-FROM-ZERO", [ClassOperandRule.NumericCategory]),
        new(FloatInfinity, "FLOAT-INFINITY", [ClassOperandRule.StandardFloatUsage]),
        new(FloatNotANumber, "FLOAT-NOT-A-NUMBER", [ClassOperandRule.StandardFloatUsage]),
        new(FloatNotANumberQuiet, "FLOAT-NOT-A-NUMBER-QUIET", [ClassOperandRule.StandardFloatUsage]),
        new(FloatNotANumberSignaling, "FLOAT-NOT-A-NUMBER-SIGNALING", [ClassOperandRule.StandardFloatUsage]),
        new(InArithmeticRange, "IN-ARITHMETIC-RANGE", [ClassOperandRule.NumericCategory]),
        new(NearestToZero, "NEAREST-TO-ZERO", [ClassOperandRule.NumericCategory]),
    ];

    /// <summary>The row for <paramref name="kind"/>, or null when the kind is one this table does not offer
    /// (a recovery path's placeholder — the screen then asks SR1 alone, never nothing).</summary>
    public static ClassAlternative? For(char kind)
    {
        foreach (var a in Alternatives) if (a.Kind == kind) return a;
        return null;
    }

    /// <summary>ISO §8.8.4.4.3 SR1 — "Identifier-1 shall not reference a data item of class index, message-tag,
    /// object, or pointer, nor a strongly-typed group, nor a variable-length group" — over EVERY alternative,
    /// which is why it is not a row above. Returns the phrase naming why, or null when SR1 admits the operand.
    /// <para>⛔ THE CLASS HALF DELEGATES TO THE TWO EXISTING DEFINITIONS AND WRITES NO THIRD LIST.
    /// <see cref="ItemCategory.IsIndexMessageTagObjectOrPointer"/> is §13.18.60.3 SR4's phrase reader — the
    /// population that answers for MESSAGE-TAG and FUNCTION-POINTER, whose entries never gain a
    /// <see cref="PicInfo"/> at all — and <see cref="IntrinsicArgumentRules.ClassOf"/> is the §8.5.2.1 Table-2
    /// classifier, which answers for the operand shapes that are not a plain data-item reference (an index-NAME,
    /// an intrinsic of §15.2 item 6's index class, the NULL predefined address). Neither alone is total, and a
    /// hand-written class list beside them would be the third copy this table exists to prevent.</para>
    /// <para>The strongly-typed-group arm keeps its own catalog entry (the COBOLNET1533 strong-typing family,
    /// which is split by rule); the class and variable-length-group arms share this one. SR1 is therefore ONE
    /// rule reported through two codes, stated here so it is not re-argued.</para></summary>
    public static string? Sr1Refusal(BoundOperand op)
    {
        if (op is BoundFieldOperand f)
        {
            var item = f.Place.Item;
            if (ItemCategory.IsIndexMessageTagObjectOrPointer(item))
                return "a data item of class index, message-tag, object, or pointer";
            if (item.IsGroup && ReferenceResolver.HasVariableLengthSubordinate(item))
                return "a variable-length group";
        }
        return IntrinsicArgumentRules.ClassOf(op) is CobolClass.Index or CobolClass.Object or CobolClass.Pointer
            ? "an operand of class index, message-tag, object, or pointer"
            : null;
    }

    /// <summary>Whether <paramref name="rule"/> is BROKEN by a DATA-ITEM operand whose operand CATEGORY is
    /// <paramref name="category"/> and whose USAGE is <paramref name="usage"/>.
    /// <para>⛔ THE CATEGORY IS THE OPERAND'S, NOT ITS BASE ITEM'S (kb/Work PB823's screen twin): the caller
    /// reads it through THE ONE operand-category reader, so a reference-modified slice is category alphanumeric
    /// (national when its usage is national) per §8.4.3.3.4 GR6 c) while keeping its item's usage per GR6's
    /// opening sentence ("the same class, category, and usage as that defined for identifier-1, except that…").
    /// The screen used to read the BASE item's picture, so <c>IF NUM (1:2) IS ALPHABETIC</c> was refused under SR4
    /// as though the slice were category numeric.</para>
    /// <para>A null argument is a shape the rule cannot classify (an operand that is not a data-item reference,
    /// an ordinary group's usage) and fails OPEN for the NEGATIVE rules — SR3/SR4/SR5/SR8 exist to reject what
    /// they name, never what this compiler cannot classify. The two POSITIVE rules (SR6, SR7) name what the
    /// operand SHALL be, so a data item whose category is known and is not that fails them: an alphanumeric
    /// group is category alphanumeric (§8.5.2.1) and has no standard floating-point usage.</para>
    /// <para><paramref name="function"/> marks an intrinsic FUNCTION-IDENTIFIER operand, for SR3's second sentence —
    /// "If identifier-1 is a function-identifier, it shall reference an alphanumeric or national function" — which
    /// states the function's half of the usage rule by its TYPE (§15.2), not by a usage clause it cannot have
    /// (kb/Work PB1401). A COUNTER REGISTER is not a function-identifier, has no usage clause, and so keeps the
    /// fail-open usage answer; SR4–SR6 reach it through its category.</para></summary>
    public static bool Violates(ClassOperandRule rule, PicCategory? category, Usage? usage, bool function) => rule switch
    {
        ClassOperandRule.NotBooleanNumericOrNumericEdited =>
            category is PicCategory.Boolean or PicCategory.Numeric or PicCategory.NumericEdited,
        ClassOperandRule.NotNumericOrNumericEdited =>
            category is PicCategory.Numeric or PicCategory.NumericEdited,
        ClassOperandRule.UsageDisplayOrNational => function
            ? category is not (PicCategory.Alphanumeric or PicCategory.National)
            : usage is { } u && !DisplayOrNational(u),
        ClassOperandRule.NumericUsageOrCategory =>
            usage is { } u2 && !DisplayOrNational(u2) && category is not PicCategory.Numeric,
        ClassOperandRule.NumericCategory => category is { } c && c is not PicCategory.Numeric,
        ClassOperandRule.StandardFloatUsage => category is not null
            && !(usage is { } u3 && UsageFamilies.IsStandardFloat(u3)),
        _ => false,
    };

    private static bool DisplayOrNational(Usage u) => u is Usage.Display or Usage.National;

    /// <summary>The diagnostic code, the clause reference and the rule's own words for
    /// <paramref name="rule"/> — declared beside the predicate that tests it, so a message can never name a
    /// rule the predicate does not test and a code can never outlive its rule (CLAUDE.md rule 1: a citation
    /// that passes <c>--check</c> while pointing at the wrong rule is the failure mode).
    /// <para>SR4 and SR8 keep COBOLNET0844, which has reported them (in their boolean-operand halves) since the
    /// data increment; SR3 and SR5 had no arm at all and get their own codes.</para></summary>
    public static (string Code, string Clause, string Text) Wording(ClassOperandRule rule) => rule switch
    {
        ClassOperandRule.NotBooleanNumericOrNumericEdited => ("COBOLNET0844", "ISO §8.8.4.4.3 SR4",
            "ALPHABETIC, ALPHABETIC-LOWER, ALPHABETIC-UPPER, or class-name-1 shall not be specified if the "
            + "category of the data item referenced by identifier-1 is boolean, numeric, or numeric-edited"),
        ClassOperandRule.NotNumericOrNumericEdited =>
            (DiagnosticCatalog.ClassConditionBooleanCategory.Code, "ISO §8.8.4.4.3 SR5",
            "BOOLEAN shall not be specified if the category of the data item referenced by identifier-1 is "
            + "numeric or numeric-edited"),
        ClassOperandRule.UsageDisplayOrNational =>
            (DiagnosticCatalog.ClassConditionOperandUsage.Code, "ISO §8.8.4.4.3 SR3",
            "if the alphabet-name-1, ALPHABETIC, ALPHABETIC-LOWER, ALPHABETIC-UPPER, BOOLEAN, or class-name-1 "
            + "phrase is specified, identifier-1 shall reference a data-item whose usage is display or national. If "
            + "identifier-1 is a function-identifier, it shall reference an alphanumeric or national function"),
        ClassOperandRule.NumericCategory =>
            (DiagnosticCatalog.ClassConditionNotNumericCategory.Code, "ISO §8.8.4.4.3 SR6",
            "if FARTHEST-FROM-ZERO, IN-ARITHMETIC-RANGE, or NEAREST-TO-ZERO is specified, identifier-1 shall "
            + "reference a data item whose category is numeric"),
        ClassOperandRule.StandardFloatUsage =>
            (DiagnosticCatalog.ClassConditionNotStandardFloat.Code, "ISO §8.8.4.4.3 SR7",
            "if the FLOAT-INFINITY, FLOAT-NOT-A-NUMBER, FLOAT-NOT-A-NUMBER-QUIET, or FLOAT-NOT-A-NUMBER-SIGNALING "
            + "phrase is specified, identifier-1 shall reference a data item described with a standard "
            + "floating-point usage (FLOAT-BINARY-32/-64/-128, FLOAT-DECIMAL-16/-34 — not FLOAT-SHORT, "
            + "FLOAT-LONG, FLOAT-EXTENDED, COMP-1 or COMP-2)"),
        _ => ("COBOLNET0844", "ISO §8.8.4.4.3 SR8",
            "if the NUMERIC phrase is specified, identifier-1 shall reference a data item whose usage is "
            + "display or national or whose category is numeric"),
    };
}
