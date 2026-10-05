// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Runtime;

namespace CobolNet.Binding;

/// <summary>The ISO §8.5.2.1 Table-2 CLASSES.</summary>
/// <remarks>
/// Distinct from <see cref="PicCategory"/>, which is the CATEGORY axis — several categories map to one class, and
/// §15's argument rules are written against the CLASS. The two rows that matter here and are easy to read the
/// wrong way: <b>numeric-edited (usage display) is class ALPHANUMERIC</b>, not numeric; and a group item takes
/// its class from its own kind rather than from an (absent) PICTURE.
/// <para>
/// ⚠ <see cref="Alphabetic"/> IS a member, and the remark that used to stand here said it could not be — "the
/// distinction is not recoverable here, and inventing a class the model cannot produce would make the §15.3
/// type-1 arm dead code that reads as coverage". <see cref="PicCategory"/> does fold <c>PIC A</c> into
/// <see cref="PicCategory.Alphanumeric"/>, but the CLASS question is answered from
/// <c>PicInfo.IsAlphabetic</c> in <see cref="ClassOfPlace"/>, not from the category — so the member is
/// reachable and the exclusions that name it (§15.95.3 r1, §15.93.3 r1) are enforced (kb/Work PB124 wave 5,
/// PB245). The stale text survived the change that refuted it; it is recorded here so it is not re-argued.
/// </para>
/// </remarks>
internal enum CobolClass
{
    Alphanumeric,

    /// <summary>Class ALPHABETIC — §8.5.2.1 Table 2's own first row ("Alphabetic | Alphabetic"), distinct from
    /// Alphanumeric (kb/Work PB124 wave 5, AR-15.3-1). The STORAGE model folds PIC A into
    /// PicCategory.Alphanumeric (one string carrier); the CLASS question un-folds here via
    /// <c>PicInfo.IsAlphabetic</c>, so a category-worded rule ("shall be of category alphanumeric or
    /// national" — NUMVAL and the 'a' kind's rows) rejects a PIC A item while a class-worded rule
    /// ("class alphabetic, alphanumeric, or national" — UPPER-CASE and the 's' kind's rows) admits it.
    /// Cross rules treat alphabetic and alphanumeric as ONE block — §15.59.3 r2's own exception ("mixing of
    /// arguments of alphabetic and alphanumeric classes is allowed"), the same two-block shape §15.87.3 r2
    /// and §15.96.3 r2 write out — see <see cref="CrossViolation"/>.</summary>
    Alphabetic,

    /// <summary>
    /// NUMERIC-EDITED with usage display — Table 2 class ALPHANUMERIC, modelled apart so the STRING family can
    /// admit it as the alphanumeric item Table 2 says it is, without the screen having to pretend it is numeric.
    /// </summary>
    /// <remarks>
    /// ⚠ Anywhere a rule says "shall be of CLASS alphanumeric", this counts as alphanumeric — that is what
    /// Table 2 says and this member does not change it.
    /// <para>
    /// ⛔ THE NAME IS NOW HISTORICAL AND THE REASONING IT RECORDS WAS OVERTURNED (owner decision 2026-08-02).
    /// This member used to justify itself by "such an item DE-EDITS to a defined numeric value and is therefore a
    /// legal arithmetic operand" — which is FALSE. §8.8.1.1 admits "an identifier referencing a NUMERIC data
    /// item"; §8.5.2.13 calls this a "numeric-edited data item", a distinct defined term; and §15.43.3 r1 shows
    /// that when the standard means to admit one it says "category numeric OR NUMERIC-EDITED" explicitly, which
    /// §15.3's integer and numeric types do not. De-editing is granted by the MOVE rules (§14.9.25.4 GR5/GR6d1)
    /// and nowhere extended to arithmetic. The <c>'i'</c> and <c>'n'</c> arms therefore BOTH exclude it now;
    /// only the <c>'s'</c> string family admits it. See DEVLOG 1142 and <c>pb1-integer-arg-numeric-edited</c>.
    /// </para>
    /// </remarks>
    NumericEditedDeEditing,

    /// <summary>NUMERIC-EDITED with usage NATIONAL — ISO §8.5.2.1 Table 2 class NATIONAL ("National | National,
    /// National-edited, Numeric-edited (if usage is national)"), the national twin of
    /// <see cref="NumericEditedDeEditing"/>. Its absence was kb/Work PB1128: the class was read off the CATEGORY
    /// alone, which cannot see the usage Table 2 splits the row on, so a national numeric-edited item answered
    /// "alphanumeric" and INSPECT's §14.9.22.3 SR4 refused the legal N" " operand beside it while admitting the
    /// illegal " ". Every CLASS-worded rule reads it as national through <see cref="IntrinsicArgumentRules.TableTwoClass"/>.</summary>
    NumericEditedNational,
    Boolean,
    National,
    Numeric,
    Object,
    Pointer,

    /// <summary>Class INDEX (§8.5.2.1 Table 2 — a distinct class, never numeric; §13.18.60's index data items
    /// and INDEXED BY index-names both present it). No §15 argument rule admits it, so classifying it is what
    /// REJECTS an index argument (kb/Work R27 — the lattice previously could not express it: an index DATA
    /// item's <see cref="PicInfo.IndexItem"/> carries category Numeric for storage, so it passed every 'n'
    /// screen, and an index-NAME fell into the computed-operand-is-numeric arm). USAGE MESSAGE-TAG (Table 2's
    /// other unexpressed class) has NO <see cref="Usage"/> member — the MCS facility is unmodeled — so it gets
    /// no dead enum member here; the Usage-inventory drift test forces the classification decision the moment
    /// the usage lands.</summary>
    Index,
}

/// <summary>One argument POSITION's requirement — the §15.x.3 rule for that ordinal, with the clause it was read
/// from. <paramref name="Kind"/> is the code <see cref="IntrinsicArgumentRules.Admissible"/> resolves.</summary>
/// <param name="NoZeroLengthClause">
/// The clause reference for "argument-N shall not be a zero-length literal" when this position carries that
/// rule, else null (fix-queue PB35). SIX §15 clauses state it — §15.59.3 r3 (MAX), §15.63.3 r3 (MIN), §15.66.3
/// r3 (NATIONAL-OF), §15.71.3 r2 (ORD-MAX), §15.72.3 r2 (ORD-MIN) and §15.85.3 r4 (STANDARD-COMPARE, on
/// argument-1 AND argument-2) — and exactly ONE was enforced, hand-written inside <c>CheckRepertoireArgs</c> for
/// NATIONAL-OF alone. Declaring it HERE, beside the position's class rule, makes the seventh clause a one-line
/// table edit instead of a seventh copy of the check (<c>feedback_one_rule_one_place</c>, CLAUDE.md rule 5).
/// <para>
/// ⛔ IT IS A SEPARATE CLAUSE REFERENCE AND NOT A <c>bool</c>, because it is a DIFFERENT RULE NUMBER from the
/// position's class rule in every one of the six: MAX's class rule is §15.59.3 <b>r1</b> and its zero-length
/// rule is <b>r3</b>. A boolean would have made the diagnostic cite r1 for an r3 violation — a citation that
/// passes <c>cite.py --check</c> (the text is real) while pointing at the wrong rule, which is the precise
/// failure mode CLAUDE.md rule 1 exists for.
/// </para>
/// <para>
/// ⚠ It rides the POSITION, not the function, because the clauses genuinely differ in reach: MAX/MIN/ORD-MAX/
/// ORD-MIN print <c>FUNCTION MAX ( { argument-1 } … )</c>, so "argument-1" IS the whole variadic list and the
/// prohibition covers every argument (it lands on the schema's <see cref="ArgSchema.Tail"/>); NATIONAL-OF's
/// argument-1 is one ordinal of two; STANDARD-COMPARE names two ordinals and not its third.
/// </para>
/// </param>
internal readonly record struct ArgRule(char Kind, string Clause, string? NoZeroLengthClause = null)
{
    /// <summary>The position's predicates BEYOND its class kind (kb/Work PB58): each is a rule of a different
    /// SHAPE than "shall be of class X" that the §15.x.3 argument rules state and a class column structurally
    /// could not carry — a minimum or exact WIDTH in character positions, an operand-SHAPE restriction, the
    /// strongly-typed-group exclusion — each with the clause it was read from. Empty for the ordinary row.
    /// <see cref="NoZeroLengthClause"/> predates the list (PB35) and stays as it is; a second boolean-with-clause
    /// would have been the second copy of the same idea, so the list is the general form and the older field
    /// the one instance that was already written down.</summary>
    public ArgPredicate[] Predicates { get; init; } = [];
}

/// <summary>The KINDS of per-position predicate a §15.x.3 argument rule states beyond the operand's class
/// (kb/Work PB58 — the sweep measured five predicate kinds the class column could not carry; four are
/// per-position and live here, the fifth is <see cref="CrossArgRule"/>).</summary>
internal enum ArgPredicateKind
{
    /// <summary>"shall be at least N character positions in length" (§15.57.3 r1 / §15.97.3 r1 / §15.78.3 r1's
    /// second half). Decided only where the operand's width is STATIC (a literal, a fixed item, a literal-length
    /// reference modification); a runtime-width operand fails open.</summary>
    MinWidth,
    /// <summary>"shall be [exactly] N character position(s) in length" (§15.70.3 r1's "one character position",
    /// §15.96.3 r2's "a single character"). Same static-width discipline as <see cref="MinWidth"/>.</summary>
    ExactWidth,
    /// <summary>"shall be an integer data item or integer literal" (§15.37.3 r3) — NARROWER than §15.3 type 6:
    /// an arithmetic expression or a nested function is barred outright, whatever it evaluates to.</summary>
    DataItemOrLiteralOnly,
    /// <summary>"nor shall it be a strongly-typed group item" (§15.59.3 r1 / §15.63.3 r1 / §15.71.3 r1 /
    /// §15.72.3 r1) — a group whose TYPE is STRONG (<c>StrongTypeModel.IsStrongGroup</c>); §15.3 item 2 treats
    /// every OTHER strongly-typed group as an alphanumeric argument, which is why this cannot be a class.</summary>
    NotStrongGroup,
    /// <summary>"shall be of CATEGORY alphanumeric or national" (§15.68.3 r1 and, through §15.94.3 r1,
    /// TEST-NUMVAL-C; §15.67.3 r1 / §15.69.3 r1 say "an alphanumeric or national … data item", which §8.5.2.1's
    /// closing sentence and §8.5.2.3 4) / §8.5.2.10 4) — "Such an item is referred to as an alphanumeric [a
    /// national] data item", each item listed being of that CATEGORY — read as the category) — the
    /// FINER axis under the position's class kind (kb/Work PB658). Table 2 puts the edited categories
    /// (numeric-edited, alphanumeric-edited, national-edited) in class alphanumeric or national, so the class kind
    /// admits them; a CATEGORY-worded rule does not. Declared per POSITION beside the class kind, never as a
    /// second kind: the class set is the same for the category-worded and the class-worded rows (TEST-NUMVAL's
    /// "a data item of class alphanumeric or national", §15.93.3 r1), and only this predicate tells them apart.</summary>
    CharacterCategory,
}

/// <summary>One per-position predicate: its <see cref="Kind"/>, the clause it was read from, and the width
/// operand for the width kinds.</summary>
internal readonly record struct ArgPredicate(ArgPredicateKind Kind, string Clause, int N = 0)
{
    public static ArgPredicate MinWidth(int n, string clause) => new(ArgPredicateKind.MinWidth, clause, n);
    public static ArgPredicate ExactWidth(int n, string clause) => new(ArgPredicateKind.ExactWidth, clause, n);
    public static ArgPredicate DataItemOrLiteralOnly(string clause) => new(ArgPredicateKind.DataItemOrLiteralOnly, clause);
    public static ArgPredicate NotStrongGroup(string clause) => new(ArgPredicateKind.NotStrongGroup, clause);
    public static ArgPredicate CharacterCategory(string clause) => new(ArgPredicateKind.CharacterCategory, clause);
}

/// <summary>A §15.x.3 rule about the argument list AS A WHOLE, which no per-position code can express.</summary>
/// <remarks>
/// ⛔ THIS EXISTS BECAUSE THE RULES DO, AND A PER-POSITION SCREEN WAS SILENTLY IGNORING THEM (fix-queue PB31,
/// and the residue PB12/PB52 recorded three separate times before it was modelled). §15.59.3 r2 and §15.63.3 r2
/// are not decoration: without them <c>FUNCTION MAX(1 "A")</c> is accepted, and every argument passes its own
/// position's check while the LIST is illegal.
/// </remarks>
internal enum CrossArgRule
{
    /// <summary>No cross-argument constraint beyond the per-position kinds.</summary>
    None,

    /// <summary>§15.59.3 r2 / §15.63.3 r2 — "All arguments shall be of the same class with the exception that
    /// mixing of arguments of alphabetic and alphanumeric classes is allowed." (The exception is free here:
    /// <see cref="PicCategory"/> folds alphabetic into alphanumeric, so the two are one class in this model.)</summary>
    AllSameClass,

    /// <summary>§15.37.3 r2 / §15.68.3 r2 / §15.48.3 r3 / §15.79.3 r3 — the positions that share argument-1's
    /// declared kind shall agree in class with argument-1. Positions declared with a DIFFERENT kind (FIND-STRING's
    /// integer argument-3) are governed by their own rule and are not part of this one.</summary>
    MatchArgument1,
}

/// <summary>
/// A catalogued function's §15.x.3 ARGUMENT SCHEMA: what each POSITION requires, what a variadic tail requires,
/// and the cross-argument rule the clause carries.
/// </summary>
/// <remarks>
/// ⛔ THIS REPLACED ONE KIND PER FUNCTION, AND THE OLD SHAPE IS WHY SIX FUNCTIONS COULD NOT BE SCREENED AT ALL
/// (fix-queue PB12). FIND-STRING and the four FORMATTED-* functions take MIXED argument classes — an
/// alphanumeric/national format string PLUS integer operands — so a single-kind row would have screened every
/// position by the first one's class and REJECTED LEGAL SOURCE. That is not a gap in the table, it is a gap in
/// the table's SHAPE, and CLAUDE.md rule 5 says the answer is the restructuring rather than the smallest diff
/// (<c>feedback_model_the_rule_shape_not_one_case</c>).
/// <para>
/// ⚠ The uniform case stays a one-liner (<see cref="Uniform"/>) and every pre-existing row migrated to it
/// unchanged — a schema whose <see cref="Positions"/> is empty and whose <see cref="Tail"/> carries the one kind
/// is EXACTLY the old behaviour, position for position, which is what made the restructuring provable rather
/// than merely tested.
/// </para>
/// </remarks>
internal sealed record ArgSchema(ArgRule[] Positions, ArgRule? Tail, CrossArgRule Cross, string CrossClause)
{
    /// <summary>The rule governing argument position <paramref name="i"/> (0-based), or null when the schema
    /// says nothing about it — a position past the declared ones with no variadic tail is UNSCREENED, never
    /// screened by the previous position's kind.</summary>
    public ArgRule? At(int i) => i < Positions.Length ? Positions[i] : Tail;

    /// <summary>The positions <see cref="CrossArgRule.MatchArgument1"/> governs: those declaring the SAME kind as
    /// argument-1. Derived from the table rather than listed beside it, so a new row cannot forget to say which
    /// positions participate — and it is the honest reading, since "argument-2 shall be of the same class as
    /// argument-1" is only meaningful where both were declared in the same family.</summary>
    public IEnumerable<int> MatchedPositions(int argCount)
    {
        if (Positions.Length == 0) yield break;
        char first = Positions[0].Kind;
        for (int i = 0; i < Positions.Length; i++)
            if (Positions[i].Kind == first) yield return i;
        // ⛔ The variadic TAIL is governed too (kb/Work PB118): §15.87.3 r2's class agreement covers EVERY
        // (argument-2, argument-3) pair — §15.87.2 repeats the pair — but this enumerator stopped at the three
        // DECLARED positions, so from the second pair onward a national/alphanumeric mismatch bound clean.
        // Any MatchArgument1 schema whose tail kind matches position 0's is extended the same way.
        if (Tail is { } t && t.Kind == first)
            for (int i = Positions.Length; i < argCount; i++)
                yield return i;
    }

    /// <summary>This schema with <paramref name="p"/> added to position <paramref name="position"/> (0-based) —
    /// the per-position predicate attach (kb/Work PB58). A position the schema does not declare cannot carry a
    /// predicate: that is a table error, and it throws at static-initialization so the drift tests see it.</summary>
    public ArgSchema WithPredicate(int position, ArgPredicate p)
    {
        if (position >= Positions.Length)
            throw new InvalidOperationException($"IntrinsicArgumentRules: predicate {p.Kind} attached to undeclared position {position + 1}");
        var copy = (ArgRule[])Positions.Clone();
        copy[position] = copy[position] with { Predicates = [.. copy[position].Predicates, p] };
        return this with { Positions = copy };
    }

    /// <summary>This schema with the "argument-N shall not be a zero-length literal" rule attached to the given
    /// 0-based POSITIONS, citing <paramref name="clause"/> — the per-position twin of <see cref="Uniform"/>'s
    /// <c>noZeroLen</c> tail argument (kb/Work PB101). §15.85.3 r4 names TWO ordinals of four and not the rest,
    /// which a tail cannot express and a constructor argument would have expressed only for position 0.</summary>
    public ArgSchema WithNoZeroLength(string clause, params int[] positions)
    {
        var copy = (ArgRule[])Positions.Clone();
        foreach (int position in positions)
        {
            if (position >= copy.Length)
                throw new InvalidOperationException(
                    $"IntrinsicArgumentRules: zero-length rule {clause} attached to undeclared position {position + 1}");
            copy[position] = copy[position] with { NoZeroLengthClause = clause };
        }
        return this with { Positions = copy };
    }

    /// <summary>This schema with <paramref name="p"/> added to the variadic TAIL (every position past the
    /// declared ones — for a Uniform row, every argument).</summary>
    public ArgSchema WithTailPredicate(ArgPredicate p)
    {
        if (Tail is not { } t)
            throw new InvalidOperationException($"IntrinsicArgumentRules: tail predicate {p.Kind} on a schema with no tail");
        return this with { Tail = t with { Predicates = [.. t.Predicates, p] } };
    }
}

/// <summary>
/// The ISO §15.3 ARGUMENT-TYPE screen — the one place a catalogued intrinsic's declared per-argument class is
/// checked against the operand actually written.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ WHY THIS EXISTS. <c>IntrinsicSig.ArgKinds</c> has always declared each argument's required class on all 79
/// catalog rows, and <c>IntrinsicSig.ArgKind</c> was written to read it — but it had ZERO callers. The only
/// consumer of <c>ArgKinds</c> anywhere was <c>IntrinsicBinder</c>'s <c>== "p"</c> MAX/MIN polymorphism test, so
/// the table built to enforce §15's argument rules enforced nothing, and class checking survived only as
/// hand-written arms for a handful of functions (the repertoire pair, CONVERT, CHAR, the ALGEBRAIC family). The
/// Phase-B traceability review surfaced it as 11 separate DIVERGES rows over 11 functions; it is ONE defect
/// (fix-queue <c>PB1</c>). Two reproductions, both silently accepted before this screen existed:
/// <c>MOVE FUNCTION REVERSE(N) TO R</c> with <c>N PIC 9(4)</c> printed <c>4321</c>, and
/// <c>COMPUTE R = FUNCTION ABS(A)</c> with <c>A PIC X(4) VALUE "ABCD"</c> printed <c>0000000{</c>.
/// </para>
/// <para>
/// ⚠ <c>OperandContext.FunctionArgument</c> is NOT the bug and is unchanged. Its doc comment always said "the
/// function's own §15.x argument rule governs, so an alphanumeric operand may be perfectly legal here" — which is
/// correct, and is exactly why the §8.8.1.1 arithmetic screen must stay suppressed for arguments. The design was
/// right; the per-function rule it promised was the missing half. This is that half.
/// </para>
/// <para>
/// <b>ERROR, WITH THE LENIENCY DIALECT-GATED.</b> ISO §4.2.2 paragraph 3: "There are rules in standard COBOL that
/// are not identified as general formats or syntax rules, but nevertheless specify elements that are syntactically
/// distinguishable. This warning mechanism shall indicate violations of such rules."
/// <para>
/// ⚠ Read that paragraph to its END, because it is a DISCRETION clause wrapped around an obligation and the first
/// draft of this comment stated only the obligation: it closes "For elements not specified in general formats or
/// in explicit syntax rules, it is left to the implementor's discretion to determine what is syntactically
/// distinguishable." So the standard does not decree that an argument rule is compile-time checkable — WE
/// determine that, and having determined it, the mechanism "shall indicate violations of such rules". An
/// argument rule qualifies on its merits: it constrains the CLASS of an operand, which is a static property of
/// the source. The obligation is real; it is just downstream of a judgement that is ours to make and to defend.
/// (Caught by an adversarial reviewer reading the clause past the sentence this comment had quoted.)
/// </para>
/// <para>
/// The disposition follows DA6, which settled the
/// sibling §8.8.1.1 question (a wrong-class ARITHMETIC operand) one wave earlier: reject under strict conformance,
/// keep the leniency behind <c>--permissive</c>. Two mechanisms for one question would be the anti-pattern.
/// </para>
/// <para>
/// <b>CLASS, NOT CATEGORY.</b> §15's rules say "shall be of CLASS numeric", and §8.5.2.1 closes with "Use of the
/// name of a data class or data category in the rules of COBOL refers to the category unless class is
/// specifically indicated" — so they resolve through Table 2's CLASS column.
/// </para>
/// <para>
/// <b>FAIL-OPEN BY CONSTRUCTION.</b> An operand whose class cannot be determined statically is never rejected.
/// A false reject turns legal COBOL away, which CLAUDE.md rule 4 forbids outright; a missed one leaves an open
/// inventory row that a later pass revisits. The asymmetry is deliberate.
/// </para>
/// </remarks>
internal static class IntrinsicArgumentRules
{
    /// <summary>The ISO §8.5.2.1 Table-2 class of an operand, or <see langword="null"/> when it is not statically
    /// decidable (an index item and other PIC-less leaves included — those are simply not screened).</summary>
    /// <remarks>
    /// ⛔ THIS IS A VIEW OF <see cref="CandidateClasses"/>, NOT A SECOND CLASSIFIER. "The class of this operand"
    /// is the right question only for an operand that HAS one; a figurative constant does not (§8.3.3.6.4 GR4 —
    /// ZERO is "the numeric value '0' … or … the character '0' … DEPENDING ON CONTEXT"). Modelling the answer as
    /// a SET and reading the singleton off it keeps both questions on one table
    /// (<c>feedback_model_the_rule_shape_not_one_case</c>); the previous scalar-only shape is why a figurative
    /// had to fall out of the screen entirely as "not statically decidable".
    /// <para>⚠ THE TWO ARMS BELOW ARE ONE TABLE, NOT TWO. Everything except a figurative constant and an ALL
    /// literal reaches <see cref="CandidateClasses"/>'s own <c>_ =&gt; ClassOf1(op)</c> arm, so calling
    /// <see cref="ClassOf1"/> directly for those shapes is the SAME lookup with the one-element array elided —
    /// the two multi-candidate shapes still go through the set. This matters because the §8.8.4.2.2 Format 3
    /// relation screen asks this of BOTH operands of EVERY relation in the program (kb/Work PB399), and a
    /// throwaway array per operand per relation is a bind-path allocation with nothing to show for it. If a
    /// third multi-candidate shape is ever added to <see cref="CandidateClasses"/>, it must be added to the
    /// first arm here as well — hence the type test names the shapes rather than defaulting to them.</para>
    /// </remarks>
    public static CobolClass? ClassOf(BoundOperand op) =>
        op is BoundFigurative or BoundAllLiteral or BoundPredefinedNull
            ? (CandidateClasses(op) is [var only] ? only : null)
            : ClassOf1(op);

    /// <summary>⛔ THE ONE ISO §8.8.1.1 OPERAND-CLASS ANSWER (kb/Work PB169–PB172 — the burn-down cluster's
    /// spine): "An arithmetic expression may be an identifier referencing a numeric data item, a numeric literal,
    /// the figurative constant ZERO …", so an operand is admissible in an arithmetic-expression position iff its
    /// class is (or, for a figurative, may be) NUMERIC.
    /// <para>
    /// Before this, that question was answered by FOUR independent mechanisms — <c>ExpressionBinder</c>'s private
    /// <c>NonNumericOperandKind</c> category switch (which had no index-data-item arm, so
    /// <c>COMPUTE N = IDX + 1</c> compiled clean under STRICT and returned the occurrence number), its
    /// <c>BindPrimary</c> <c>ResultCategory</c> test (which folds §15.2 item 6's INDEX functions into numeric),
    /// the receiving-side <c>ScreenResultant</c> (which DID have the index arm), and this table, which nothing in
    /// the arithmetic funnel used. Expressing the rule over <see cref="CandidateClasses"/> closes the index hole,
    /// the pointer/object hole and the bit/national-group hole in one stroke, and preserves the 2026-08-02 owner
    /// decision on numeric-edited BY CONSTRUCTION (Table 2 puts category numeric-edited in class alphanumeric,
    /// which this table reports as <see cref="CobolClass.NumericEditedDeEditing"/> — never Numeric).
    /// </para>
    /// <para>An EMPTY candidate set means "not statically decidable", and fails OPEN: the screen exists to reject
    /// what the standard names, never to reject what this compiler cannot classify.</para></summary>
    public static bool IsArithmeticOperandClass(BoundOperand op) =>
        CandidateClasses(op) is var set && (set.Length == 0 || set.Contains(CobolClass.Numeric));

    /// <summary>Whether an operand §8.8.1.1 refuses can still be read by the <c>--permissive</c> digit-decoding
    /// extension (decision 2026-07-29: an alphanumeric item's digit characters decoded as an unsigned integer). An
    /// item of class object or class pointer (§8.5.2.1 Table 2) holds a REFERENCE, not characters, so there is
    /// nothing to decode and the extension cannot reach it: `COMPUTE N = SELF` or `= O` with O an object reference
    /// went on to a C# conversion error in the generated program (kb/Work PB1425's sweep).</summary>
    public static bool IsDigitDecodable(BoundOperand op) => ClassOf(op) is not (CobolClass.Object or CobolClass.Pointer);

    /// <summary>The §8.8.1.1 answer for a resolved data item — the <see cref="ClassOfItem"/> lane, for the
    /// <c>ReferenceResolver</c> fast path that never builds a <see cref="Place"/> (kb/Work PB170).</summary>
    public static bool IsArithmeticOperandClass(DataItem item) =>
        ClassOfItem(item) is not { } cls || cls == CobolClass.Numeric;

    /// <summary>
    /// Every ISO §8.5.2.1 Table-2 class this operand is capable of presenting — a singleton for everything with a
    /// fixed class, EMPTY when the class is not statically decidable, and genuinely plural only for a figurative
    /// constant, whose class is chosen by the context it appears in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>§8.3.3.6.4 GR1 + GR4–GR8, read as the table they are.</b> GR1: "When a figurative constant is used in a
    /// context requiring national characters, the figurative constant represents a national character value.
    /// Otherwise, when a figurative constant represents a character value, the figurative constant represents an
    /// alphanumeric character value." GR4 (Format 1) then makes ZERO the exceptional one: it "represents the
    /// numeric value '0', one or more of the boolean character '0', or one or more of the character '0' in the
    /// computer's runtime coded character set, depending on context." GR5/GR6/GR7/GR8 give SPACE, HIGH-VALUE,
    /// LOW-VALUE and QUOTE a CHARACTER reading only — none of them names a numeric or boolean value.
    /// </para>
    /// <para>
    /// ⛔ SO THE ASYMMETRY IS THE POINT, and it is what a scalar column could not express: <b>ZERO is admissible
    /// in a numeric argument position and in a string one; SPACE and its siblings are admissible only in a string
    /// one.</b> §8.3.3.6.3 SR1a says the same thing from the other side — "If the literal is restricted to a
    /// numeric literal, the only figurative constant permitted is ZERO (ZEROS, ZEROES) without the ALL phrase" —
    /// and §8.8.1.1 lists exactly one figurative among the operands an arithmetic expression may be built from.
    /// </para>
    /// <para>
    /// ⚠ <c>ALL literal-1</c> is NOT class-neutral and is deliberately screened here: SR1a's "without the ALL
    /// phrase" bars even <c>ALL "5"</c> from a numeric position, so it carries its literal's own class. Before
    /// this, <c>FUNCTION ABS(ALL "5")</c> compiled clean and aborted at run time on <c>bound operand
    /// 'BoundAllLiteral'</c> — the wrong-stage family again, on source the standard rejects.
    /// </para>
    /// </remarks>
    public static CobolClass[] CandidateClasses(BoundOperand op) => op switch
    {
        // §8.3.3.6.4 GR4 — the zero format is the numeric value '0', the boolean character '0', OR the character
        // '0'; GR1 makes that character national in a national context. Four candidates, and the context picks.
        BoundFigurative { Kind: 'Z' } =>
            [CobolClass.Numeric, CobolClass.Alphanumeric, CobolClass.National, CobolClass.Boolean],
        // GR5/GR6/GR7/GR8 — SPACE, HIGH-VALUE, LOW-VALUE, QUOTE are CHARACTER values, alphanumeric by GR1 or
        // national in a national context. Never numeric, never boolean.
        BoundFigurative { Kind: 'S' or 'H' or 'L' or 'Q' } => [CobolClass.Alphanumeric, CobolClass.National],
        // NULL is NOT a figurative constant — §8.3.3.6.2 lists Formats 1–7 and NULL is none of them (kb/Work
        // PB148). It is the predefined address (§8.4.3.10.1, class pointer) / object reference (§8.4.3.7), an
        // identifier with its own grammar rule and bound node (kb/Work PB1427): class pointer, so every
        // class-closed slot of an SR1 context (an INITIALIZE MOVE-form category, a non-slot formal) rejects it.
        BoundPredefinedNull => [CobolClass.Pointer],
        BoundFigurative => [],                                   // an unmodelled kind screens as before: fail open
        BoundAllLiteral all => ClassOfCategory(all.Category) is { } ac ? [ac] : [],
        _ => ClassOf1(op) is { } c ? [c] : [],
    };

    /// <summary>The single-class classifier for every operand whose class is fixed — the original body of
    /// <see cref="ClassOf"/>, now reached through <see cref="CandidateClasses"/> so there is one table.</summary>
    private static CobolClass? ClassOf1(BoundOperand op) => op switch
    {
        // ⛔ A LITERAL HAS A CATEGORY, AND THIS IGNORED IT — the PB1 trap, sprung a second time and caught by the
        // corpus, not by reading. `BoundStringLiteral` carries `Category` (Alphanumeric by default, but National
        // for N"…" and BOOLEAN for B"…"), and returning a flat Alphanumeric made the brand-new §15.45.3 r1
        // boolean screen REJECT `FUNCTION INTEGER-OF-BOOLEAN(B"00000101")` — legal source, written in
        // `2002/intrinsics_boolean_conv` and in a spec-derived unit test. A screen is only as good as its
        // classifier, and a classifier that flattens a category is how a correct rule rejects a correct program.
        BoundStringLiteral sl => ClassOfCategory(sl.Category) ?? CobolClass.Alphanumeric,
        BoundNumericLiteral => CobolClass.Numeric,
        BoundFieldOperand f => ClassOfPlace(f.Place),
        // An arithmetic expression IS a numeric argument (§15.3 types 6 and 10 admit one outright); a nested
        // intrinsic contributes the class of ITS result category — resolved through the SAME
        // IntrinsicResultType.Resolve the binder types results with (kb/Work PB124 wave 5b): §15.2 item 6
        // makes MAX/MIN over index arguments an INDEX function ("these are of the class and category index"),
        // and the storage model's ResultCategory folds Index into Numeric, so reading the category alone let
        // FUNCTION SQRT(FUNCTION MAX(IX1 IX2)) pass a class-numeric screen (GR-15.2-6 / AR-15.3-10).
        BoundComputedOperand { Expr: BoundIntrinsicCall ic } =>
            IntrinsicResultType.Resolve(ic.Sig, ic.Args) is IntrinsicType.Index
                ? CobolClass.Index : ClassOfCategory(ic.ResultCategory),
        // An INDEX-NAME operand (kb/Work R27): class index (§8.5.2.1 Table 2), which no §15 argument rule
        // admits — BEFORE the generic computed arm, whose is-numeric answer let `FUNCTION INTEGER(IX)`
        // compute the occurrence number silently.
        BoundComputedOperand { Expr: BoundIndexRef } => CobolClass.Index,
        BoundComputedOperand => CobolClass.Numeric,
        BoundBoolOperand => CobolClass.Boolean,   // a boolean EXPRESSION argument (§8.4.3.2.3 SR8; kb/Work PB65)
        BoundAddressOperand => CobolClass.Pointer,   // §8.4.3.11.4 GR1 / §8.4.3.13.4 GR1 — "a unique data item of class pointer" (kb/Work PB1021)
        _ => null,
    };

    internal static CobolClass? ClassOfPlace(Place p)
    {
        // ISO §8.4.3.3.4 GR6 — the unique data item reference modification creates has the SAME class and
        // category as identifier-1 except for the three lettered rewrites (PB20). This said "class alphanumeric,
        // always", on the authority of a clause number that DOES NOT EXIST (the old text cited "8.4.2.4";
        // cite.py --check reports no such clause). The rule now lives in ONE
        // place; a ref-modified boolean item is class BOOLEAN and a ref-modified national item class NATIONAL.
        if (p is RefModPlace rmp)
            return ClassOfCategory(rmp.Category);   // a ref-modded GROUP is an elementary alphanumeric item (GR6; kb/Work PB70)
        return ClassOfItem(p.Item);
    }

    /// <summary>The ISO §8.5.2.1 Table-2 class of a DATA ITEM — the half of <see cref="ClassOfPlace"/> that does
    /// not need a <see cref="Place"/>, so a caller holding only the resolved item asks the SAME table.
    /// <para>⛔ IT IS A SPLIT, NOT A SECOND CLASSIFIER (kb/Work PB170). <c>ReferenceResolver</c> resolves a
    /// subscript / reference-modification operand to a <see cref="DataItem"/> and renders its position read
    /// WITHOUT ever building a <c>Place</c> or entering the expression binder, so the §8.8.1.1 screen could not
    /// reach it: <c>T(XE)</c> with <c>XE PIC X(4)</c> compiled clean and digit-decoded under STRICT. Giving that
    /// path its own category switch would have been the FIFTH place answering "is this operand class numeric";
    /// this entry is what lets it ask the first.</para></summary>
    internal static CobolClass? ClassOfItem(DataItem item)
    {
        // §8.5.2.1 — an alphanumeric group item has class alphanumeric, a bit group boolean, a national group
        // national. A group has no PICTURE of its own, so it cannot fall through to the category table.
        // ⚠ A STRONGLY-TYPED group answers Alphanumeric TOO, and that is the §15.3 reading, not a missing arm (kb/Work
        // PB1901, refuted): §8.5.2.1 gives it the class and category of its TYPE-NAME, but §15.3 item 2 says "Strongly-typed
        // group items are treated as though they were of class and category alphanumeric, unless they are prohibited as
        // arguments of a function" — this classifier answers ARGUMENT positions, where that sentence governs, and the
        // functions that do prohibit one (MAX, MIN, ORD-MAX, ORD-MIN) carry ArgPredicateKind.NotStrongGroup. Every
        // other consumer that must tell a strong group apart (STRING / UNSTRING, the MOVE table, a relation) asks
        // StrongTypeModel itself, so a new CobolClass member would change none of them and would reject
        // UPPER-CASE(G), which §15.3 admits.
        if (item.IsGroup)
        {
            return item.AsIfPic?.Category switch   // D20/PB79 — a bit / national group's as-if picture
            {
                PicCategory.National => CobolClass.National,
                PicCategory.Boolean => CobolClass.Boolean,
                _ => CobolClass.Alphanumeric,
            };
        }
        // The USAGE-keyed arm rides BEFORE the category table (kb/Work R27): an index DATA item's PicInfo
        // carries category NUMERIC for the storage model (PicInfo.IndexItem), but §8.5.2.1 Table 2 puts it in
        // class INDEX — a distinct class no §15 argument rule admits. Keying the usage first is what stops the
        // storage category from answering the CLASS question.
        if (item.Pic is { Usage: Usage.Index }) return CobolClass.Index;
        // Class ALPHABETIC before the category table (kb/Work PB124 wave 5): the storage model folds PIC A
        // into PicCategory.Alphanumeric, so the category cannot answer — IsAlphabetic can, exactly as the
        // Usage.Index arm above un-folds the index item's storage category.
        if (item.Pic is { IsAlphabetic: true }) return CobolClass.Alphabetic;
        // Table 2 splits the numeric-edited row on USAGE — class alphanumeric "if usage is display", national "if
        // usage is national" — so the category table below cannot answer it alone (kb/Work PB1128).
        if (item.Pic is { AnalyzedCategory: PicCategory.NumericEdited, Usage: Usage.National })
            return CobolClass.NumericEditedNational;
        // ⛔ THE ANALYZED category, never the storage one (kb/Work PB960): a recovery profile — the placeholder
        // for an entry whose PICTURE was already rejected — has NO class, so it reads "not statically decidable"
        // and every screen over this table fails OPEN instead of re-diagnosing the item as alphanumeric.
        return item.Pic?.AnalyzedCategory is { } category ? ClassOfCategory(category) : null;
    }

    /// <summary>ISO §8.5.2.1 Table 2, read as written.</summary>
    private static CobolClass? ClassOfCategory(PicCategory category) => category switch
    {
        PicCategory.Alphanumeric => CobolClass.Alphanumeric,
        // ⛔ NumericEdited sits under ALPHANUMERIC in Table 2 when its usage is display — which is why it is not
        // a legal argument to a rule demanding CLASS NUMERIC (ABS). It is reported as its own REFINED member so
        // that a CATEGORY-worded rule can still see the category Table 2 folds away (§15.68.3 r1, "shall be of
        // category alphanumeric or national"); every CLASS-worded rule reads it through TableTwoClass.
        // ⚠ THE JUSTIFICATION THAT STOOD HERE WAS THE OVERTURNED ONE ("so a rule demanding §15.3's INTEGER TYPE
        // can still admit it, since it de-edits to a numeric value") — owner decision 2026-08-02 refuted exactly
        // that reading, and the member's own doc comment has said so since; this was the second copy that did
        // not get the correction (kb/Work PB305).
        PicCategory.NumericEdited => CobolClass.NumericEditedDeEditing,
        PicCategory.National => CobolClass.National,
        PicCategory.Numeric => CobolClass.Numeric,
        PicCategory.Boolean => CobolClass.Boolean,
        PicCategory.ObjectReference => CobolClass.Object,
        // ISO §8.5.2.1 Table 2 collapses all THREE pointer categories — data-pointer, function-pointer and
        // program-pointer — into class Pointer, and all three are now declarable, so all three are named
        // here. The FunctionPointer member arrived with kb/Work PB452 + PB817, which made USAGE
        // FUNCTION-POINTER a real declaration instead of a COBOLNET0899 staged-loud refusal; the comment
        // that stood here recorded the absence as an OPEN GAP rather than a disposition, because the
        // justification BEFORE that ("documented non-support, COBOLNET1564, Annex A.3") was false in every
        // clause — A.3 does not contain function-pointer, A.4's entire SET inventory is three items and
        // contains neither format 8 nor format 15, and docs/CONFORMANCE.md §4 carries no such item.
        PicCategory.Pointer or PicCategory.ProgramPointer or PicCategory.FunctionPointer => CobolClass.Pointer,
        _ => null,                                          // Group is handled by the caller
    };

    /// <summary>
    /// ISO §8.5.2.1 Table 2's CLASS column, read over THIS LATTICE'S REFINED MEMBERS — the ONE answer to
    /// "what class is this operand, for a rule that says CLASS".
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⛔ <see cref="CobolClass"/> IS NOT THE CLASS COLUMN. One of its members is FINER than the class it
    /// belongs to, kept apart on purpose so a CATEGORY-worded rule can see the category Table 2 folds away:
    /// <see cref="CobolClass.NumericEditedDeEditing"/> is category numeric-edited, whose CLASS is alphanumeric.
    /// §8.4.3.3.4 GR6 c) says it in words — "the categories numeric and numeric-edited are considered class and
    /// category national if the usage is national; otherwise they are considered class and category
    /// alphanumeric" — and §8.5.2.1's own closing sentence is why the distinction has to exist at all: "Use of
    /// the name of a data class or data category in the rules of COBOL refers to the category unless class is
    /// specifically indicated." §15.68.3 is the proof that BOTH readings are needed of the same operand: r1 is
    /// category-worded ("shall be of category alphanumeric or national") and r2 is class-worded ("shall be of
    /// the same class as argument-1").
    /// <para>⚠ <see cref="CobolClass.Alphabetic"/> is NOT such a member and must not be folded here — Table 2
    /// gives category alphabetic its own CLASS row. Cross rules merge it with alphanumeric by a rule-level
    /// exception they each state (see <see cref="CrossBlock"/>), which is a different fact.</para>
    /// </para>
    /// <para>
    /// ⛔ IT IS A PROJECTION AND NOT A FOLD, and that distinction is the whole of kb/Work PB305.
    /// <see cref="CrossViolation"/> carried a hand-written ONE-CASE fold (Alphabetic → Alphanumeric) in the place
    /// this projection belongs, so <c>FUNCTION FIND-STRING(&lt;PIC ZZ9 item&gt; "5")</c> — legal under §15.37.3
    /// r2, which admits "a data item or literal of either class alphabetic or alphanumeric" — was rejected
    /// COBOLNET1627 by a message that NAMED the operand's class "alphanumeric (numeric-edited)" in the same
    /// sentence in which it refused it for not being alphanumeric. TEN catalogued functions inherited it, because
    /// every cross rule intersects these same candidate sets. A class the lattice refines is un-refined ONCE,
    /// here, never remembered at each screen.
    /// </para>
    /// <para>
    /// The national half of GR6 c) is <see cref="CobolClass.NumericEditedNational"/> (a <c>PIC ZZ9 USAGE NATIONAL</c>
    /// item, buildable since kb/Work PB646 / PB492 and witnessed by <c>2002/pb876_national_value_no_editing_bwz_inert</c>);
    /// this projection un-refines it to <see cref="CobolClass.National"/> exactly as it un-refines its display twin.
    /// </para>
    /// </remarks>
    public static CobolClass TableTwoClass(CobolClass c) => c switch
    {
        CobolClass.NumericEditedDeEditing => CobolClass.Alphanumeric,
        CobolClass.NumericEditedNational => CobolClass.National,
        _ => c,
    };

    /// <summary>Every lattice member whose §8.5.2.1 Table-2 CLASS is one of <paramref name="classes"/>, in the
    /// rule's own wording order with each class's refinements following it — how a CLASS-worded §15 argument rule
    /// states its admissible set, so a refined member is admitted BY ITS CLASS and never by being remembered in
    /// the list.</summary>
    /// <remarks>⛔ THE LITERAL LISTS IT REPLACES WERE THE HAND-MAINTAINED FORM OF <see cref="TableTwoClass"/>
    /// (kb/Work PB305; CLAUDE.md rule 5 — "never a hand-maintained list where a structure belongs"). The 's',
    /// 'c', 'n', 'i' and 'b' arms each spelled out which refined members their class admits, so the NEXT refined
    /// member would have to be remembered at five sites plus <see cref="CrossViolation"/> — and the sixth site
    /// had already been forgotten. A CATEGORY-worded rule is not a set here at all: it is a class set plus the
    /// <see cref="ArgPredicateKind.CharacterCategory"/> predicate (kb/Work PB658), because the class column cannot
    /// derive a category set.</remarks>
    private static CobolClass[] ByClass(params CobolClass[] classes) =>
        [.. classes.SelectMany(cls => Enum.GetValues<CobolClass>().Where(m => TableTwoClass(m) == cls)).Distinct()];

    /// <summary>The static storage REPRESENTATION of an intrinsic argument, as a <see cref="Usage"/> — the axis
    /// the keyword-keyed §15.19.3 argument rules key on, and the one <see cref="IntrinsicArgumentRules.ClassOfCategory"/>
    /// cannot answer twice over: Table 2 collapses pointer and program-pointer into ONE class, and class NUMERIC
    /// spans display digits and binary words. §15.19.3 r4 names the axis outright ("hexadecimal digits <b>of
    /// display or national usage</b>"); r5's NOTE ("distinct from simply requiring the string to be of class
    /// alphanumeric") cuts by representation, not class — a numeric or edited DISPLAY item holds alphanumeric
    /// characters and qualifies, a COMP item's storage is not characters at all and does not; r7's exclusion
    /// list is six usages. §15.12.3 r1 (BASECONVERT, display-or-national) waits on the same axis. A literal
    /// answers the usage its category implies (§8.3.3.5 / §8.3.3.2 — an N"…" literal is national characters, any other
    /// string literal display characters), as does a nested intrinsic's STRING result; null = no statically
    /// fixed representation (a group, a figurative, an ALL literal, a numeric value) — every caller reads null
    /// as "leave it to the runtime value screen".</summary>
    public static Usage? StaticUsageOf(BoundOperand op) => op switch
    {
        // ⛔ OperandPic, NOT `IsGroup: false` + raw Pic (D20's ONE category/usage reader; kb/Work PB173's
        // sibling sweep). §13.18.29.4 GR1b/GR2b make a bit / national GROUP "an elementary data item of usage
        // bit" / "of usage national", so it HAS a static representation and the keyword-keyed screens must see
        // it. Measured before the change: `FUNCTION CONVERT(<PIC 1(8) USAGE BIT leaf> ANUM NAT)` was correctly
        // rejected COBOLNET1514 by §15.19.3 r5, while the byte-identical GROUP-USAGE BIT group was ADMITTED —
        // one rule, one usage, two verdicts, because the group's raw `Pic` is null. An ordinary ALPHANUMERIC
        // group still answers null (OperandPic is null for it), which is the "leave it to the runtime value
        // screen" answer this contract documents.
        // ⚠ DELIBERATELY NOT ItemCategory.UsageOf, which answers the OTHER usage question
        // (kb/Work PB411): that reader gives an alphanumeric group §8.5.2.1's "treated as though it had a usage
        // of display" because the rules it serves — §14.9.18.3 SR6, §8.4.3.3.4 GR6 — ask what usage the item
        // HAS. This one asks whether the argument has a statically fixed REPRESENTATION the §15.19.3 keyword
        // screens may key on, and a group's storage is its leaves', not a character string of its own. The two
        // answers differ for exactly one shape and the divergence is stated here so neither is mistaken for a
        // duplicate of the other.
        BoundFieldOperand { Place.Item.OperandPic: { } pic } => pic.Usage,
        BoundStringLiteral { Category: PicCategory.National } => Usage.National,
        BoundStringLiteral => Usage.Display,
        BoundComputedOperand { Expr: BoundIntrinsicCall ic } => ic.ResultCategory switch
        {
            PicCategory.National => Usage.National,
            PicCategory.Alphanumeric => Usage.Display,
            // §15.2 item 2 (kb/Work PB124 wave 5c, GR-15.2-2): "Boolean functions … have an implicit usage
            // bit." The old Display answer let a boolean FUNCTION pass CONCAT's §15.18.3 r2 usage screen as a
            // display argument, though §15.18.1's own result table enumerates only display and national
            // boolean rows — usage bit satisfies neither of r2's families, so the screen now rejects it.
            PicCategory.Boolean => Usage.Bit,
            _ => null,   // a numeric result is a VALUE, not a stored character string
        },
        _ => null,
    };

    /// <summary>
    /// ⛔ THE SCREEN IS DRIVEN FROM HERE, NOT FROM <c>ArgKinds</c> — and the difference is the whole lesson.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The obvious fix for PB1 was "consult <c>sig.ArgKind(i)</c>". Implementing it proved the table is not
    /// merely UNREAD but UNVERIFIED: <c>BYTE-LENGTH</c> is declared <c>"s"</c> while §15.14.3 admits an argument
    /// "of any class", and the nine rows with an EMPTY <c>ArgKinds</c> default to <c>'n'</c>, which would have
    /// screened <c>LENGTH</c> and its family as numeric-only. Those declarations were written as dispatch hints
    /// and drifted freely for years for exactly the reason PB1 exists — nothing read them, so nothing could
    /// contradict them. The comprehensive gate caught 12 corpus programs; every one was legal COBOL.
    /// </para>
    /// <para>
    /// So a function is screened only when its §15 argument rule has been READ AND CITED. An entry here is a
    /// spec-derived fact carrying its clause; a function absent from this table behaves exactly as it did before
    /// PB1, which is why landing this cannot regress anything. The table grows as the Phase-B traceability review
    /// adjudicates each clause — the eleven below are §15.7 and §15.70–15.79, the review's first batch — and
    /// <c>IntrinsicArgumentClassDriftTests</c> holds the two halves in agreement.
    /// </para>
    /// <para>
    /// ⚠ This is deliberately NOT a wider guess. Asserting 68 more argument rules from an unaudited hint column
    /// would not be completeness, it would be fabrication — and every wrong entry rejects legal source, the one
    /// outcome CLAUDE.md rule 4 forbids outright.
    /// </para>
    /// </remarks>
    /// <summary>ONE kind for every position — the shape most §15.x.3 argument rules actually have ("argument-1
    /// shall be of class numeric", with argument-1 repeating for a variadic list). Expressed as a variadic TAIL
    /// with no fixed positions, which is precisely what the pre-schema table did for every row.</summary>
    private static ArgSchema Uniform(char kind, string clause, CrossArgRule cross = CrossArgRule.None,
        string crossClause = "", string? noZeroLen = null) =>
        new([], new ArgRule(kind, clause, noZeroLen), cross, crossClause);

    /// <summary>Per-POSITION kinds, for a rule that constrains its ordinals differently — FIND-STRING's two
    /// string operands plus an integer, the FORMATTED-* family's format literal plus its numeric values.
    /// <paramref name="tail"/> is the kind for positions past the declared ones (a variadic tail), or
    /// <c>'\0'</c> for none — and NONE is the honest default: a position the clause does not describe is
    /// UNSCREENED, never screened by whatever the last declared position happened to be.</summary>
    private static ArgSchema Schema(string clause, char[] kinds, char tail = '\0',
        CrossArgRule cross = CrossArgRule.None, string crossClause = "") =>
        new([.. kinds.Select(k => new ArgRule(k, clause))],
            tail == '\0' ? null : new ArgRule(tail, clause), cross, crossClause);

    public static readonly IReadOnlyDictionary<string, ArgSchema> Verified =
        new Dictionary<string, ArgSchema>(CobolNames.Comparer)
        {
            ["ABS"] = Uniform('n', "§15.7.3 r1"),                            // shall be of class numeric
            // §15.70.3 r1 is ONE sentence with TWO halves — "shall be ONE CHARACTER POSITION IN LENGTH and shall
            // be of category alphabetic, alphanumeric, or national" (PB58 — the width half was unscreened).
            // …and "of CATEGORY alphabetic, alphanumeric, or national" — a category wording, so the edited categories
            // (a one-position PIC Z item is class alphanumeric and category numeric-edited) are out (kb/Work PB658).
            ["ORD"] = Uniform('s', "§15.70.3 r1").WithTailPredicate(ArgPredicate.ExactWidth(1, "§15.70.3 r1"))
                .WithTailPredicate(ArgPredicate.CharacterCategory("§15.70.3 r1")),
            // r1's negative class list ('p') + "nor shall it be a strongly-typed group item" (the predicate);
            // r2 no zero-length literal; r3 all-same-class (PB58 — ORD-MAX/ORD-MIN never had the cross rule MAX
            // and MIN had: the two-arm-dispatch scar, one arm fixed).
            ["ORD-MAX"] = Uniform('p', "§15.71.3 r1", CrossArgRule.AllSameClass, "§15.71.3 r3", "§15.71.3 r2")
                .WithTailPredicate(ArgPredicate.NotStrongGroup("§15.71.3 r1")),
            ["ORD-MIN"] = Uniform('p', "§15.72.3 r1", CrossArgRule.AllSameClass, "§15.72.3 r3", "§15.72.3 r2")
                .WithTailPredicate(ArgPredicate.NotStrongGroup("§15.72.3 r1")),
            ["PRESENT-VALUE"] = Uniform('n', "§15.74.3 r1"),                 // argument-1 and argument-2 class numeric
            // ⛔ RANDOM's argument is §15.3 type 6, INTEGER — Table 21 prints "Int1" — and r1 ("class numeric") is
            // only the class half of it, with r2 ("zero or a positive integer") the value half (kb/Work PB1420: the
            // row said 'n', so `FUNCTION RANDOM(FUNCTION SQRT(4))` compiled although §8.4.3.2.3 SR11 bars a numeric
            // function from an integer operand position). The Table 21 drift test holds every Int position to 'i'.
            ["RANDOM"] = Uniform('i', "§15.75.3 r1/r2"),
            ["RANGE"] = Uniform('n', "§15.76.3 r1"),                         // shall be of class numeric
            ["REM"] = Uniform('n', "§15.77.3 r1"),                           // argument-1 and argument-2 class numeric
            // §15.78.3 r1 — class alphabetic/alphanumeric/national AND "at least one character position in
            // length" (the width half, PB58).
            ["REVERSE"] = Uniform('s', "§15.78.3 r1").WithTailPredicate(ArgPredicate.MinWidth(1, "§15.78.3 r1")),
            // §15.79.3 r1/r3 — argument-1 is a national or alphanumeric LITERAL (its literal-ness is enforced by
            // the existing COBOLNET1517 arm); argument-2 "shall have the same type as argument-1" — the class
            // reading of "type", the cross-argument agreement (PB58; MatchArgument1 was already the shape).
            ["SECONDS-FROM-FORMATTED-TIME"] = Schema("§15.79.3 r1/r3", ['a', 'a'],
                cross: CrossArgRule.MatchArgument1, crossClause: "§15.79.3 r3"),
            // PI takes no arguments (§15.73.2) — present so the drift test can hold this table and the Phase-B
            // batch's function list in agreement rather than silently tolerating a gap.
            ["PI"] = Uniform(' ', "§15.73.2 — no arguments"),

            // ── Phase-B batch 2 (§15.8–15.19). Eight of its twelve functions; the other four are listed under
            //    DELIBERATELY UNSCREENED below, because their argument rule is not a class constraint at all.
            ["ACOS"] = Uniform('n', "§15.8.3 r1"),                           // shall be of class numeric
            ["ASIN"] = Uniform('n', "§15.10.3 r1"),                          // shall be of class numeric
            ["ATAN"] = Uniform('n', "§15.11.3 r1"),                          // shall be of class numeric
            // ⛔ FIVE MEMBERS OF THIS SAME FAMILY WERE ABSENT, AND THE QUEUE ENTRY NAMED ONE (fix-queue PB52
            // cause 1). SQRT, SIGN, COS, SIN and TAN each carry the IDENTICAL rule the three rows above carry —
            // `FUNCTION SQRT(SPACE)` reached run time as "figurative 'S' in a numeric context" while
            // `FUNCTION ACOS(SPACE)` had been a bind diagnostic all along. Each clause below was read and cited
            // INDIVIDUALLY rather than inferred from its neighbours: this table is fail-open on purpose, and
            // PB1 is what asserting an unaudited column costs (12 legal corpus programs rejected).
            // ⚠ SQRT IS §15.84, NOT §15.81 — §15.81 is SIGN. A first pass read "§15.81.3 r1: Argument-1 shall
            // be of class numeric" and took it for SQRT's; the text was real, the clause was another function's
            // (feedback_a_real_clause_can_answer_a_different_question). Both rows below are now the measured ones.
            ["COS"] = Uniform('n', "§15.20.3 r1"),                           // shall be of class numeric
            ["SIN"] = Uniform('n', "§15.82.3 r1"),                           // shall be of class numeric
            ["TAN"] = Uniform('n', "§15.89.3 r1"),                           // shall be of class numeric
            ["SQRT"] = Uniform('n', "§15.84.3 r1"),                          // shall be of class numeric
            ["SIGN"] = Uniform('n', "§15.81.3 r1"),                          // shall be of class numeric
            // §15.9.3 r1 — argument-1 class numeric; r3 — "Argument-2 shall be a positive integer": a per-position
            // schema (PB58 — the Uniform 'n' row let ANNUITY(0.05, 12.50) through, a non-integer period count).
            ["ANNUITY"] = Schema("§15.9.3 r1/r3", ['n', 'i']),
            ["CHAR"] = Uniform('i', "§15.15.3 r1"),                          // shall be an integer
            ["CHAR-NATIONAL"] = Uniform('i', "§15.16.3 r1"),                 // shall be an integer
            ["BOOLEAN-OF-INTEGER"] = Uniform('i', "§15.13.3 r1/r2"),         // both arguments positive integers
            // §15.17.3 r1/r2 — argument-1 "in integer date form", argument-2 "in standard numeric time form".
            // Table 21 types them Int1 and Num2 (§15.3 type 6 and type 10): an integer date, and a time that may
            // carry fractional seconds (§15.3.3), so position 1 is 'i' and position 2 'n' (kb/Work PB1420 — the
            // row was Uniform 'n', which let a numeric function stand where the integer date form is required).
            ["COMBINED-DATETIME"] = Schema("§15.17.3 r1/r2", ['i', 'n']),

            // ── §15.32–15.44, the review's fourth batch (fix-queue PB12) ────────────────────────────────────
            // Each row is the function's OWN §15.x.3 argument rule, read and mechanically cited. Six of the
            // thirteen functions the batch adjudicated are NOT here, and the omissions are the point:
            //   · FIND-STRING and the four FORMATTED-* functions take MIXED argument classes (an alphanumeric or
            //     national format string plus integer operands), and this table carries ONE kind for the whole
            //     function. A row would screen every position by the first one's class and reject legal source.
            //     That needs a per-POSITION schema, not an entry — it is the half of PB12 that is a design change.
            //   · HIGHEST-ALGEBRAIC is the sharper omission: §15.43.3 r1 admits "a data item of category numeric
            //     OR NUMERIC-EDITED", and §8.5.2.1 Table 2 puts numeric-edited under class ALPHANUMERIC when its
            //     usage is display — so an 'n' row would REJECT an argument the standard admits. That is exactly
            //     the PB1 trap (12 legal corpus programs rejected by asserting an unaudited kind), so it stays
            //     unscreened until a kind exists that can say "category numeric or numeric-edited".
            ["EXP"] = Uniform('n', "§15.34.3 r1"),                           // shall be of class numeric
            ["EXP10"] = Uniform('n', "§15.35.3 r1"),                         // shall be of class numeric
            ["FRACTION-PART"] = Uniform('n', "§15.42.3 r1"),                 // shall be of the class numeric
            ["INTEGER"] = Uniform('n', "§15.44.3 r1"),                       // shall be of class numeric
            // ⚠ PARTIAL by construction: §15.36.3 r1 is "an integer GREATER THAN OR EQUAL TO ZERO". The 'i' kind
            // screens the CLASS half (§15.3 type 6); the VALUE half is a run-time property this compile-time
            // screen cannot see, so FACTORIAL(-1) still passes here and is EC-ARGUMENT-FUNCTION's business.
            ["FACTORIAL"] = Uniform('i', "§15.36.3 r1"),                     // shall be an integer (>= 0 is a value rule)
            // Zero-argument functions: the general format admits no argument at all, so any argument is an arity
            // error (COBOLNET1504) before class screening is reached. Recorded so the review can close the row.
            ["EXCEPTION-STATEMENT"] = Uniform(' ', "§15.32.2 — no arguments"),
            ["EXCEPTION-STATUS"] = Uniform(' ', "§15.33.2 — no arguments"),

            // ── §15.45–15.57, the review's fifth batch (fix-queue PB19) ─────────────────────────────────────
            // Every rule below was read at its own clause and `cite.py --check`ed. The batch reported these as
            // "entirely unscreened", which they were: CheckArgumentClasses returns at its TryGetValue guard for
            // any function absent from this table, so §15.49.3 r1 was enforced NOWHERE while the adjacent INTEGER
            // and FRACTION-PART — whose rule text is the same sentence — were enforced. That asymmetry is what
            // "the table grows as the review adjudicates each clause" looks like from inside.
            ["INTEGER-OF-DATE"] = Uniform('i', "§15.46.3 r1"),               // an integer of the form YYYYMMDD
            ["INTEGER-OF-DAY"] = Uniform('i', "§15.47.3 r1"),                // an integer of the form YYYYDDD
            ["INTEGER-PART"] = Uniform('n', "§15.49.3 r1"),                  // shall be of class numeric
            ["LOG"] = Uniform('n', "§15.55.3 r1"),                           // shall be of class numeric
            ["LOG10"] = Uniform('n', "§15.56.3 r1"),                         // shall be of class numeric
            // ⚠ PARTIAL, exactly like REVERSE (`AR-15.78.3-1`): §15.57.3 r1 / §15.97.3 r1 are ONE sentence with
            // TWO halves — "of class alphabetic, alphanumeric, or national" AND "at least one character position
            // in length". This screens the class half only. Half a rule enforced is PARTIAL, never CONFORMS.
            // …and now the width half too (PB58): "shall be at least one character position in length".
            ["LOWER-CASE"] = Uniform('s', "§15.57.3 r1").WithTailPredicate(ArgPredicate.MinWidth(1, "§15.57.3 r1")),
            ["UPPER-CASE"] = Uniform('s', "§15.97.3 r1").WithTailPredicate(ArgPredicate.MinWidth(1, "§15.97.3 r1")),
            // §15.48.3 r1 makes argument-1 a national or alphanumeric LITERAL (its literal-ness is the existing
            // COBOLNET1517 arm, its CONTENT the COBOLNET1631 format screen); r3 makes argument-2 "a data item of
            // the same type as argument-1". Both positions are therefore in the string family, so one kind serves
            // both — the same shape SECONDS-FROM-FORMATTED-TIME already uses.
            // ⚠ RESIDUE: r3 is a CROSS-ARGUMENT rule ("the SAME type as argument-1"), which a per-position screen
            // cannot express — the sibling of `AR-15.79.3-3`. This closes the class half, which is the half that
            // was silently accepting a class-numeric argument-2; the cross-argument half stays PARTIAL.
            // …r3's cross-argument half now rides MatchArgument1 (the class reading of "the same type as
            // argument-1"), and its "shall be a DATA ITEM" half the DataItemOrLiteralOnly predicate's data-item
            // arm cannot express (it admits literals) — argument-2 as a literal is screened in BindIntrinsicCore's
            // format arm (PB58).
            ["INTEGER-OF-FORMATTED-DATE"] = Schema("§15.48.3 r1/r3", ['a', 'a'],
                cross: CrossArgRule.MatchArgument1, crossClause: "§15.48.3 r3"),
            // ⛔ THE ONE THAT NEEDED PB20 FIRST. §15.45.3 r1 is "Argument-1 shall be of class BOOLEAN" — the only
            // rule in the catalogue that names that class, and `Admissible` had no arm able to say it. It is
            // listed last because it was BLOCKED, not forgotten: until PB20, every reference-modified operand was
            // typed class ALPHANUMERIC on the authority of a clause that does not exist, so this screen would
            // have rejected the standard's OWN Annex D worked example, `FUNCTION INTEGER-OF-BOOLEAN (bit (1:6))`.
            // §8.4.3.3.4 GR6 preserves the category, so a ref-modified boolean item is class boolean and the
            // example is admitted. Ordering was the whole risk here.
            ["INTEGER-OF-BOOLEAN"] = Uniform('b', "§15.45.3 r1"),            // shall be of class boolean

            // ── fix-queue PB30 · eight functions whose §15.x.3 rule no code consulted ────────────────────────
            // Each read at its own clause and `cite.py --check`ed. MEAN/MEDIAN/MIDRANGE are VARIADIC and their
            // rule names argument-1 only, which is the tail form: "argument-1 shall be of class numeric" governs
            // every occurrence of the repeated argument, so Uniform is the rule's own shape, not a widening.
            ["MEAN"] = Uniform('n', "§15.60.3 r1"),                          // shall be of class numeric
            ["MEDIAN"] = Uniform('n', "§15.61.3 r1"),                        // shall be of class numeric
            ["MIDRANGE"] = Uniform('n', "§15.62.3 r1"),                      // shall be of class numeric
            ["MOD"] = Uniform('i', "§15.64.3 r1"),                           // argument-1 and argument-2 integers

            // ── fix-queue PB31 · the CROSS-ARGUMENT rule, which no per-position screen could express ─────────
            // §15.59.3 / §15.63.3 r1 is the NEGATIVE class list ('p'); r2 is the list-wide agreement. Both halves
            // now fire: `FUNCTION MAX(1 "A")` was accepted because every argument passed its own position's
            // check while the LIST was illegal.
            // ⚠ RESIDUE, recorded rather than silently dropped: r1's "nor shall it be a strongly-typed group
            // item" and r3's "argument-1 shall not be a zero-length literal" are not class constraints, so this
            // row does not enforce them. Half a rule enforced is PARTIAL, never CONFORMS.
            // …and r1's strongly-typed-group leg is now the NotStrongGroup predicate (PB58); the zero-length
            // literal leg was already NoZeroLengthClause. Both halves of both sentences fire.
            ["MAX"] = Uniform('p', "§15.59.3 r1", CrossArgRule.AllSameClass, "§15.59.3 r2", "§15.59.3 r3")
                .WithTailPredicate(ArgPredicate.NotStrongGroup("§15.59.3 r1")),
            ["MIN"] = Uniform('p', "§15.63.3 r1", CrossArgRule.AllSameClass, "§15.63.3 r2", "§15.63.3 r3")
                .WithTailPredicate(ArgPredicate.NotStrongGroup("§15.63.3 r1")),

            // ── fix-queue PB12 · the MIXED-CLASS functions, which are why the schema exists ──────────────────
            // §15.68.3 r1 makes argument-1 "of category alphanumeric or national"; r2 makes argument-2 "of the
            // same class as argument-1" — a cross-argument rule, so the currency string cannot be national while
            // the subject is alphanumeric. ⚠ The screen runs BEFORE the §15.68.3 r3 default-currency injection,
            // deliberately: that operand is compiler-supplied, not written by the user, and screening it would
            // report a class disagreement on source that contains no argument-2 at all.
            ["NUMVAL-C"] = Schema("§15.68.3 r1/r2", ['a', 'a'], cross: CrossArgRule.MatchArgument1,
                crossClause: "§15.68.3 r2")
                .WithPredicate(0, ArgPredicate.CharacterCategory("§15.68.3 r1")),
            // §15.37.3 — r1: argument-1 of class alphabetic, alphanumeric, or national; r2: argument-2 in the
            // SAME family as argument-1; r3: "argument-3 shall be an integer data item or integer literal".
            // ⛔ THIS ROW IS THE WHOLE ARGUMENT FOR THE SCHEMA. Under one-kind-per-function it would have
            // screened argument-3 as a string and REJECTED the legal `FUNCTION FIND-STRING(a b 2)`.
            // …r3 is NARROWER than §15.3 type 6 — "an integer DATA ITEM or integer LITERAL" — so argument-3 also
            // carries the operand-shape predicate: an arithmetic expression or a nested function is barred
            // outright (PB58).
            ["FIND-STRING"] = Schema("§15.37.3 r1/r2/r3", ['s', 's', 'i'],
                cross: CrossArgRule.MatchArgument1, crossClause: "§15.37.3 r2")
                .WithPredicate(2, ArgPredicate.DataItemOrLiteralOnly("§15.37.3 r3")),
            // §15.85.3 — STANDARD-COMPARE (kb/Work PB101 T7). r1/r2: argument-1 and argument-2 "shall be of class
            // alphabetic, alphanumeric, or national"; r3 says they "may be of different classes", so there is
            // deliberately NO cross-argument rule here — adding MatchArgument1 by analogy with TRIM would reject
            // the exact case r3 permits. r4 prohibits a zero-length LITERAL on both, and r6 makes the ordering
            // LEVEL a positive nonzero integer — the value half is BindStandardCompare's (a class kind cannot say
            // "positive nonzero"), the class half is position 2's 'i'.
            // ⚠ THE POSITIONS ARE OPERAND POSITIONS, NOT WRITTEN ONES — the same convention FIND-STRING's row
            // uses. §15.85.2's ordering-name-1 sits between argument-2 and argument-4 as WRITTEN, but it is a NAME
            // (§15.3 argument type 12); BindStandardCompare consumes it and screens the operand list that remains.
            ["STANDARD-COMPARE"] = Schema("§15.85.3 r1/r2/r6", ['s', 's', 'i'])
                .WithNoZeroLength("§15.85.3 r4", 0, 1),
            // The LOCALE functions (kb/Work PB64 T4): the trailing locale-name-1 is a NAME ('l' — NonOperandArgumentKinds)
            // that IntrinsicBinder.BindLocaleFunction consumes; the schemas screen the operands that remain.
            // §15.51.3 r1/r2 (class alphabetic/alphanumeric/national, r3 the two may differ — no cross rule);
            // §15.52.3 r1 "class alphanumeric or national and shall be 8 character positions in length";
            // §15.53.3 r1 "… 6 character positions in length"; §15.54.3 r1 "a numeric value in standard numeric time
            // form" — 'n', not 'i' (a fractional seconds value is legal; its RANGE is the runtime's screen).
            ["LOCALE-COMPARE"] = Schema("§15.51.3 r1/r2", ['s', 's']),
            ["LOCALE-DATE"] = Schema("§15.52.3 r1", ['a']).WithPredicate(0, ArgPredicate.ExactWidth(8, "§15.52.3 r1")),
            ["LOCALE-TIME"] = Schema("§15.53.3 r1", ['a']).WithPredicate(0, ArgPredicate.ExactWidth(6, "§15.53.3 r1")),
            ["LOCALE-TIME-FROM-SECONDS"] = Schema("§15.54.3 r1", ['n']),
            // The FORMATTED-* family (§15.38–15.41): argument-1 is "a national or alphanumeric literal" (its
            // LITERAL-ness is the existing COBOLNET1517 arm, its CONTENT the format screen); the remaining
            // positions are date/time VALUES — integer date form (§15.39.3 r3, §15.40.3 r3), standard numeric
            // time form (§15.40.3 r4, §15.41.3 r3), and the integer UTC offset (§15.40.3 r5, §15.41.3 r4).
            // ⚠ Standard numeric time form is 'n', not 'i': §15.3.3 admits a fractional-seconds representation,
            // so screening it as an integer would reject a legal fractional time.
            ["FORMATTED-CURRENT-DATE"] = Schema("§15.38.3 r1", ['a']),
            ["FORMATTED-DATE"] = Schema("§15.39.3 r1/r3", ['a', 'i']),
            ["FORMATTED-DATETIME"] = Schema("§15.40.3 r1/r3/r4/r5", ['a', 'i', 'n', 'i']),
            ["FORMATTED-TIME"] = Schema("§15.41.3 r1/r3/r4", ['a', 'n', 'i']),

            // ── kb/Work PB58 · the ABSENT rows. Every catalogued function now has a row here or a reason in
            //    DeliberatelyUnscreened, and IntrinsicArgumentClassDriftTests.EveryCataloguedFunction_HasARow
            //    holds it — the "no row, so CheckArgumentClasses returns at the TryGetValue guard" class of gap
            //    dies structurally. Each clause read at its own section and `cite.py --check`ed.
            // The date family — §15.6 Table 21 types every argument Int (§15.3 type 6):
            ["DATE-OF-INTEGER"] = Uniform('i', "§15.22.3 r1"),               // a value in integer date form
            ["DAY-OF-INTEGER"] = Uniform('i', "§15.24.3 r1"),                // a value in integer date form
            ["DATE-TO-YYYYMMDD"] = Schema("§15.23.3 r1/r2/r4", ['i', 'i', 'i']),   // positive integer < 1000000; integer; integer 1600<x<10000
            ["DAY-TO-YYYYDDD"] = Schema("§15.25.3 r1/r2/r4", ['i', 'i', 'i']),     // positive integer < 100000; integer; integer 1600<x<10000
            ["YEAR-TO-YYYY"] = Schema("§15.100.3 r1/r2/r4", ['i', 'i', 'i']),      // nonnegative integer < 100; integer; integer 1600<x<10000
            ["TEST-DATE-YYYYMMDD"] = Uniform('i', "§15.90.3 r1"),            // shall be an integer
            ["TEST-DAY-YYYYDDD"] = Uniform('i', "§15.91.3 r1"),              // shall be an integer
            // The NUMVAL family — argument-1 "an alphanumeric or national literal or … data item":
            // ⛔ CATEGORY vs CLASS, ROW BY ROW (kb/Work PB658). §15.67.3 r1 / §15.69.3 r1 say "an alphanumeric or
            // national … data item" and §15.68.3 r1 says "of category alphanumeric or national": the CATEGORY
            // column (§8.5.2.1's closing sentence; §8.5.2.3 4) / §8.5.2.10 4) name the data item by category), so
            // the edited categories are out — the CharacterCategory predicate. §15.93.3 r1 and §15.95.3 r1 say "a
            // data item of CLASS alphanumeric or national", which Table 2 gives numeric-edited (display), so the
            // two TEST- twins of the class-worded kind carry NO predicate; the golden
            // `l1_test_numval_f_class_screen` pins that admission.
            ["NUMVAL"] = Uniform('a', "§15.67.3 r1").WithTailPredicate(ArgPredicate.CharacterCategory("§15.67.3 r1")),
            ["NUMVAL-F"] = Uniform('a', "§15.69.3 r1").WithTailPredicate(ArgPredicate.CharacterCategory("§15.69.3 r1")),
            ["TEST-NUMVAL"] = Uniform('a', "§15.93.3 r1"),
            ["TEST-NUMVAL-F"] = Uniform('a', "§15.95.3 r1"),
            // §15.94.3 r1 imports §15.68's argument rules whole — the NUMVAL-C row's shape, verbatim.
            ["TEST-NUMVAL-C"] = Schema("§15.94.3 r1 → §15.68.3 r1/r2", ['a', 'a'], cross: CrossArgRule.MatchArgument1,
                crossClause: "§15.68.3 r2 (via §15.94.3 r1)")
                .WithPredicate(0, ArgPredicate.CharacterCategory("§15.94.3 r1 → §15.68.3 r1")),
            // The statistical trio — "Argument-1 shall be of class numeric" (variadic tail form):
            ["STANDARD-DEVIATION"] = Uniform('n', "§15.86.3 r1"),
            ["SUM"] = Uniform('n', "§15.88.3 r1"),
            ["VARIANCE"] = Uniform('n', "§15.98.3 r1"),
            // §15.96.3 r1 — argument-1 a data item of class alphabetic/alphanumeric/national; r2 — argument-2 "a
            // single character" of the SAME family as argument-1 (the width predicate + the cross rule).
            // ⛔ ARGUMENT-2 REPEATS, so it needs a TAIL (kb/Work PB249). §15.96.2 prints `[ argument-2 ] …` and
            // the catalog row is Variadic 1..∞, but this schema declared only TWO positions with no `tail:`, so
            // ArgSchema.At returned null for every position past the second and MatchedPositions stopped at the
            // declared ones: `FUNCTION TRIM(X "A" "BC")` was class-unscreened, width-unscreened AND
            // cross-unscreened, and the runtime silently kept 'B'. r2 governs EVERY argument-2 identically —
            // it is one sentence about "argument-2", not about the first one — so the tail carries the same
            // kind, the same ExactWidth(1) and (through MatchedPositions' PB118 tail arm) the same cross rule.
            ["TRIM"] = Schema("§15.96.3 r1/r2", ['s', 's'], tail: 's',
                cross: CrossArgRule.MatchArgument1, crossClause: "§15.96.3 r2")
                .WithPredicate(1, ArgPredicate.ExactWidth(1, "§15.96.3 r2"))
                .WithTailPredicate(ArgPredicate.ExactWidth(1, "§15.96.3 r2")),
            // §15.87.3 r1 — argument-1 class alphabetic/alphanumeric/national or a string literal; r2 — every
            // argument-2/argument-3 pair in argument-1's family (the variadic pairs are the tail); r3 — neither
            // argument-1 nor argument-2 of zero LENGTH: argument-1's half is the MinWidth predicate here, and
            // EVERY pair's argument-2 (the odd tail positions, which one tail kind cannot single out) is
            // BindSubstitute's own walk.
            ["SUBSTITUTE"] = Schema("§15.87.3 r1/r2", ['s', 's', 's'], tail: 's',
                cross: CrossArgRule.MatchArgument1, crossClause: "§15.87.3 r2")
                .WithPredicate(0, ArgPredicate.MinWidth(1, "§15.87.3 r3")),
            // §15.92.3 r1/r2 — a format LITERAL (the COBOLNET1517 arm) and argument-2 "of the same type as
            // argument-1" (the class reading, MatchArgument1).
            ["TEST-FORMATTED-DATETIME"] = Schema("§15.92.3 r1/r2", ['a', 'a'], cross: CrossArgRule.MatchArgument1,
                crossClause: "§15.92.3 r2"),
            // §15.18.3 r1 — CONCAT admits "class alphabetic, alphanumeric, boolean, numeric or national" (the
            // 'c' kind: everything but index/object/pointer); r2's usage agreement and r3's usage-display +
            // unsigned-integer conditions are the StaticUsageOf-axis screen in IntrinsicBinder.CheckConcatArgs.
            ["CONCAT"] = Uniform('c', "§15.18.3 r1"),
            // Zero-argument functions — present so the drift test can hold this table and the catalog in
            // agreement (the general format admits no argument; any argument is an arity error first).
            ["CURRENT-DATE"] = Uniform(' ', "§15.21.2 — no arguments"),
            ["E"] = Uniform(' ', "§15.27.2 — no arguments"),
            ["EXCEPTION-LOCATION"] = Uniform(' ', "§15.30.2 — no arguments"),
            ["EXCEPTION-LOCATION-N"] = Uniform(' ', "§15.31.2 — no arguments"),
            ["SECONDS-PAST-MIDNIGHT"] = Uniform(' ', "§15.80.2 — no arguments"),
            ["WHEN-COMPILED"] = Uniform(' ', "§15.99.2 — no arguments"),
        };

    /// <summary>
    /// Functions whose §15 argument rule was READ during the Phase-B review and deliberately left UNSCREENED,
    /// with the reason. Not an oversight, and recorded so it cannot be mistaken for one.
    /// </summary>
    /// <remarks>
    /// A class screen can only express "the argument shall be of class X". These four rules are something else,
    /// and forcing them into the table would reject legal COBOL — which is exactly how the first attempt at PB1
    /// failed its gate on twelve corpus programs.
    /// <list type="bullet">
    ///   <item><b>BYTE-LENGTH</b> §15.14.3 r1 — "a data item of any class or category". There is nothing to
    ///   screen; the catalog's <c>"s"</c> hint is simply wrong, and screening from it rejected legal source.</item>
    ///   <item><b>BASECONVERT</b> §15.12.3 r1 — constrains USAGE ("a usage display or national data item or
    ///   literal"), not class. A usage screen is a different mechanism and does not exist here yet.</item>
    ///   <item><b>CONCAT</b> §15.18.3 r1 — admits "class alphabetic, alphanumeric, boolean, numeric or
    ///   national", i.e. everything except index/object/pointer. Expressible in principle, but its r2/r3 add
    ///   cross-argument USAGE agreement and an unsigned-integer condition, so a class-only screen would give a
    ///   false sense that the rule is enforced.</item>
    ///   <item><b>CONVERT</b> §15.19.3 — the admissible argument depends on the source-format KEYWORD (HEX /
    ///   ANUM / NAT / ANY), so there is no single class for argument-1. Its own arm in the binder owns it.</item>
    /// </list>
    /// </remarks>
    public static readonly IReadOnlyDictionary<string, string> DeliberatelyUnscreened =
        new Dictionary<string, string>(CobolNames.Comparer)
        {
            ["BYTE-LENGTH"] = "§15.14.3 r1 admits an argument of ANY class or category",
            ["BASECONVERT"] = "§15.12.3 r1 constrains USAGE (display or national), not class — an axis this "
                + "ordinal table cannot express; enforced at bind by CheckBaseConvertArgs over StaticUsageOf "
                + "(the usage half, the sub-11 unsigned-integer half, and every static-literal base violation "
                + "per §4.2.2 — range, integer-ness, equality), with the runtime twins (the r2 digit screen, "
                + "dynamic base range/equality) in CobolIntrinsics.BaseConvert. The RENDERER admits a numeric "
                + "LITERAL argument-1 (IntrinsicRenderer.StrNum, PB59)",
            ["CONCAT"] = "§15.18.3 r1 admits all classes but index/object/pointer; r2/r3 are cross-argument "
                + "USAGE rules. The RENDERER admits numeric literals (r1 lists class numeric — the admitting "
                + "StrArgList, PB59); STILL unscreened at bind: r1's exclusions and r3's usage-display + "
                + "unsigned-integer conditions (a signed/fractional literal renders verbatim today)",
            ["CONVERT"] = "§15.19.3 keys the admissible argument-1 on the source-format KEYWORD, an axis this "
                + "ordinal table cannot express; enforced IN FULL at bind by BindConvert's own positional walk "
                + "plus its r4/r5/r6/r7 screens over StaticUsageOf. The value halves (r4's digit validity, "
                + "r5/r6 membership of the coded set) are the runtime screens' territory",
            // ⛔ HIGHEST-ALGEBRAIC / LOWEST-ALGEBRAIC — and the PB12 note that put them here was WRONG about WHY.
            // It recorded them as "left unscreened until a kind can express 'category numeric or numeric-edited'",
            // which reads as a missing capability. Measured, the truth is the opposite: `BindAlgebraicFold`
            // already enforces §15.43.3 r1 / §15.58.3 r1 IN FULL — the category test, the index exclusion, and
            // the half a class screen structurally cannot express ("shall be a data item … and shall not be an
            // integer function or numeric function", so a literal, expression, group, ref-mod or nested function
            // is refused). Adding an admissible-set kind here would be a SECOND mechanism for a rule already
            // enforced more completely by the first (feedback_one_mechanism_per_job), and the two would drift.
            // ⚠ The finding was "unscreened"; the measurement is "screened by a better mechanism" —
            // validate_the_premise_not_only_the_rule.
            ["HIGHEST-ALGEBRAIC"] = "§15.43.3 r1 is enforced IN FULL by BindAlgebraicFold, including its data-item half",
            ["LOWEST-ALGEBRAIC"] = "§15.58.3 r1 is enforced IN FULL by BindAlgebraicFold, including its data-item half",
            ["SMALLEST-ALGEBRAIC"] = "§15.83.3 r1 is enforced IN FULL by BindAlgebraicFold (the same arm as HIGHEST/LOWEST), including its data-item half",
            // kb/Work PB58 — the remaining catalogued functions, each owned by a bespoke arm or a documented posture:
            ["LENGTH"] = "§15.50.3 r1 admits an alphanumeric/national/boolean literal, a data item of ANY class or category, a based entry, or a type-name — nothing a class screen could reject; BindLengthFamily owns it",
            ["DISPLAY-OF"] = "§15.26.3 r1/r2 (national argument-1; a one-character alphabetic/alphanumeric argument-2) are enforced IN FULL by CheckRepertoireArgs, width included",
            ["NATIONAL-OF"] = "§15.66.3 r1/r2/r3 (alphabetic/alphanumeric argument-1; a one-character national argument-2; no zero-length literal) are enforced IN FULL by CheckRepertoireArgs, width included",
            ["MODULE-NAME"] = "§15.65.3 takes ONE phrase keyword (ACTIVATING/CURRENT/NESTED/STACK/TOP-LEVEL), never an operand — BindModuleName owns it, incl. r1's NESTED-in-a-nested-program rule",
            ["EXCEPTION-FILE"] = "§15.28.3 r1 — the optional argument is a FILE-CONNECTOR NAME (a word, not an operand); BindExceptionFile resolves it against the FDs",
            ["EXCEPTION-FILE-N"] = "§15.29.3 r1 — the optional argument is a FILE-CONNECTOR NAME (a word, not an operand); BindExceptionFile resolves it against the FDs",
            // ⛔ THE FOUR LOCALE FUNCTIONS LEFT THIS TABLE at kb/Work PB64 T4, the way STANDARD-COMPARE did at T7: their
            // entries read "rejected BY NAME before any argument is screened", which stopped being true the moment their
            // Bind became Runtime. They now carry Verified schemas above.
            // ⛔ STANDARD-COMPARE LEFT THIS TABLE WHEN ITS SUPPORT WAS CLAIMED (kb/Work PB101 T7). Its entry read
            // "documented non-support … rejected BY NAME before any argument is screened", which stopped being
            // true the moment its Bind became Runtime — and a stale UNSCREENED reason is worse than none, because
            // EveryCataloguedFunction_HasARow is satisfied by it and the §15.85.3 rules would have gone
            // unenforced with a sentence explaining why that was fine. It now carries a Verified schema above.
        };

    /// <summary>
    /// The <c>IntrinsicCatalog</c> <c>ArgKinds</c> codes that are NOT operand kinds at all — a §15.3 argument
    /// TYPE that is a NAME or a KEYWORD, consumed by the function's own bespoke binder and never present in the
    /// operand list <c>CheckArgumentClasses</c> screens — each with the type it models and the binder that
    /// resolves it.
    /// </summary>
    /// <remarks>
    /// ⛔ THIS EXISTS SO A NEW CODE CANNOT BE A DEAD COLUMN (kb/Work PB101; the PB1 trap in its general form).
    /// §15.3 enumerates fourteen argument types and four of them — Keyword (7), Locale-name (8), Ordering-name
    /// (12), Type declaration (14) — are not operands, so <see cref="Admissible"/> can never have an arm for
    /// them: it answers "which CLASSES", and a name has none. Without a disposition table, adding such a code to
    /// <c>ArgKinds</c> would be a declaration nothing reads and nothing can contradict — exactly the state PB1
    /// found on all 79 rows. <c>IntrinsicArgumentClassDriftTests</c> requires every code used in the catalog to
    /// have either an <see cref="Admissible"/> arm or an entry HERE, and requires the named binder to exist.
    /// <para>⚠ It is keyed on the CODE, not on the function: the locale increments (DESIGN-locale-facility §12
    /// T1/T4) add a locale-name code across five functions, and one row here covers all of them.</para>
    /// </remarks>
    public static readonly IReadOnlyDictionary<char, string> NonOperandArgumentKinds =
        new Dictionary<char, string>
        {
            ['o'] = "§15.3 argument type 12 (Ordering-name) — \"An ordering-name defined in the SPECIAL-NAMES "
                + "paragraph shall be specified\": a NAME, resolved by IntrinsicBinder.BindStandardCompare "
                + "against DataBinder.OrderTables (§15.85.3 r5; §12.3.7.3 SR9 makes STANDARD-COMPARE its only "
                + "legal reference site). It never reaches the operand list, so no class screen applies to it",
            ['l'] = "§15.3 argument type 8 (Locale-name) — \"A locale-name defined in the SPECIAL-NAMES paragraph shall "
                + "be specified\": a NAME, resolved by IntrinsicBinder.BindLocaleFunction against DataBinder.Locales "
                + "(through DataBinder.ResolveLocaleName — the ONE undeclared-locale-name diagnostic, COBOLNET1664) "
                + "for LOCALE-COMPARE (§15.51.3 r4), LOCALE-DATE (§15.52.3 r3), LOCALE-TIME (§15.53.3 r4) and "
                + "LOCALE-TIME-FROM-SECONDS (§15.54.3 r2) — and by IntrinsicBinder.BindCaseFunctionWithLocale (the T5 case-function phrase) and IntrinsicBinder.BindNumvalCFamily (the T6 NUMVAL-C/TEST-NUMVAL-C LOCALE keyword). It never reaches the "
                + "operand list, so no class screen applies to it",
        };

    /// <summary>
    /// Is EVERY operand position of <paramref name="functionName"/>'s argument run — <paramref name="argCount"/>
    /// operands — a §15.3 <b>numeric</b> position, i.e. one whose declared argument type is 6 (Integer) or 10
    /// (Numeric)? This is the precondition for rendering a WHOLE call in binary64, and it is a question about
    /// the POSITION, never about the operand that was written.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⛔ WHY THE RENDERER ASKS THE ARGUMENT-RULE TABLE (kb/Work PB635). <c>IntrinsicRenderer.RenderNum</c>'s
    /// float dispatch asked only "is ANY argument floating", and its lane
    /// (<c>IntrinsicRenderer.FloatBody</c>) renders EVERY argument through one binary64 conversion. Those two
    /// facts are only compatible when every position HAS a binary64 value. §15.3 says which do: type 6
    /// ("An arithmetic expression that will always result in an integer value or an integer data item shall be
    /// specified") and type 10 ("Numeric. An arithmetic expression or a numeric data item shall be specified")
    /// are the numeric types; types 1/2/9 (alphabetic / alphanumeric / national), 3 (boolean), 5 (index),
    /// 11 (object) and 13 (pointer) name character, bit and reference operands, which have no numeric value at
    /// all. A function mixing the two — FIND-STRING, whose §15.37.3 r1 argument-1 is "a data item or literal of
    /// class alphabetic, alphanumeric, or national" and whose r3 argument-3 "shall be an integer data item or
    /// integer literal" — was therefore moved WHOLE to the float lane by a single float argument-3: the two
    /// string operands were converted to <c>double</c>, the LAST/ANYCASE phrases were dropped, and the emitted
    /// call named a <c>FindStringReal</c> body that does not and should not exist (Roslyn CS0117).
    /// </para>
    /// <para>
    /// ⛔ IT IS ALSO WHY THE ANSWER IS NOT "are the OPERANDS class numeric". Under <c>--permissive</c> the DA6
    /// leniency admits an ALPHANUMERIC operand at a NUMERIC position and digit-decodes it, so
    /// <c>FUNCTION MOD(&lt;PIC X item&gt; &lt;COMP-2&gt;)</c> has a non-numeric OPERAND at a position that is
    /// numeric and must keep riding the float lane. Reading the operand's class would have routed it to the
    /// exact <c>Int128</c> arm and handed Roslyn a <c>double</c> — the CS1503 fix-queue PB2 closed, reopened.
    /// </para>
    /// <para>
    /// <b>Fail OPEN, in the same direction and for the same reason as the screen above.</b> A function absent
    /// from <see cref="Verified"/> (the <see cref="DeliberatelyUnscreened"/> rows), a position the schema does
    /// not describe, and a kind whose <see cref="Admissible"/> set is <see langword="null"/> (<c>'p'</c> — the
    /// MAX/MIN family's NEGATIVE list) all answer <see langword="true"/>, so this can only ever REMOVE a call
    /// from the float lane on the authority of a cited §15.x.3 rule, never add one. That is what makes landing
    /// it incapable of regressing a function whose rule has not been read.
    /// </para>
    /// </remarks>
    public static bool ArgumentRunIsAllNumeric(string functionName, int argCount)
    {
        if (!Verified.TryGetValue(functionName, out var schema)) return true;    // unscreened — fail open
        for (int i = 0; i < argCount; i++)
            if (schema.At(i) is { } rule && Admissible(rule.Kind) is { } classes
                && classes is not [CobolClass.Numeric])
                return false;
        return true;
    }

    /// <summary>§15's "shall be of class NUMERIC" rows ('n' and 'i') — <see cref="ByClass"/>-derived, so
    /// numeric-edited stays excluded BY ITS CLASS (Table 2 puts it under alphanumeric) rather than by absence,
    /// which is what preserves the 2026-08-02 owner decision by construction.</summary>
    private static readonly CobolClass[] ClassNumeric = ByClass(CobolClass.Numeric);

    /// <summary>§15.45.3 r1's "shall be of class BOOLEAN" row ('b').</summary>
    private static readonly CobolClass[] ClassBoolean = ByClass(CobolClass.Boolean);

    /// <summary>The CLASS-worded string rows ('s') — "class alphabetic, alphanumeric, or national".</summary>
    private static readonly CobolClass[] ClassStringRules =
        ByClass(CobolClass.Alphabetic, CobolClass.Alphanumeric, CobolClass.National);

    /// <summary>CONCAT's row ('c', §15.18.3 r1) — "class alphabetic, alphanumeric, boolean, numeric or
    /// national": everything Table 2 names but index, object and pointer.</summary>
    private static readonly CobolClass[] ClassConcat = ByClass(
        CobolClass.Alphabetic, CobolClass.Alphanumeric, CobolClass.National, CobolClass.Numeric,
        CobolClass.Boolean);

    /// <summary>The 'a' rows — "class alphanumeric or national", with class ALPHABETIC left out (Table 2 gives it
    /// its own row, and §15.93.3 r1 / §15.52.3 r1 write "alphanumeric or national" where §15.96.3 r1 writes
    /// "alphabetic, alphanumeric, or national"). <see cref="ByClass"/>-derived, so a refined member (the
    /// numeric-edited ones) is admitted by its class. The CATEGORY-worded rows (NUMVAL, NUMVAL-F, NUMVAL-C) share
    /// this set and add the <see cref="ArgPredicateKind.CharacterCategory"/> predicate on top (kb/Work PB658).</summary>
    private static readonly CobolClass[] ClassAlphanumericOrNational =
        ByClass(CobolClass.Alphanumeric, CobolClass.National);

    /// <summary>The classes a verified class code admits, or <see langword="null"/> for "no general screen" —
    /// the function's rule is a NEGATIVE list and its own arm owns it.</summary>
    /// <remarks>⚠ Every arm returns a SHARED, PRE-COMPUTED array: a <see cref="ByClass"/>-derived set must be
    /// computed once, and the screen runs per argument of every intrinsic call in the compilation unit.</remarks>
    public static CobolClass[]? Admissible(char kind) => kind switch
    {
        // ⛔ 'n' is for a rule that says CLASS NUMERIC in those words — §15.7.3 r1 (ABS), §15.9.3 r1, §15.74.3 r1,
        // §15.75.3 r1, §15.76.3 r1, §15.77.3 r1 and the trig family. Table 2 puts NUMERIC-EDITED under class
        // ALPHANUMERIC, so such an operand is excluded, however numeric it looks.
        // ⚠ THIS WAS CHALLENGED AND THE CHALLENGE WAS WRONG — recorded so it is not re-argued a third time.
        // The argument for widening it to match 'i' runs: §15.3's type 10 "Numeric" admits "an ARITHMETIC
        // EXPRESSION or a numeric data item" (--check verified), which is verbatim what type 6 admits for 'i',
        // so a numeric-edited item should de-edit and be admissible here too. It does not follow, because
        // §8.8.1.1 defines what an arithmetic expression may BE: "an identifier referencing a NUMERIC DATA ITEM,
        // a numeric literal, the figurative constant ZERO …" (--check verified). A numeric-edited item is
        // category numeric-edited and class alphanumeric — not a numeric data item — so it is neither of type
        // 10's two alternatives. The exclusion is correct and `pb1-numeric-arg-numeric-edited` pins it.
        'n' => ClassNumeric,
        // ⛔ 'b' — class BOOLEAN, and §15.45.3 r1 (INTEGER-OF-BOOLEAN) is the only rule in the catalogue that
        // names it. There was no arm able to express this at all before PB19, which is why the function was
        // unscreened rather than merely unlisted: a screen cannot reject what its vocabulary cannot describe.
        // ⚠ IT DEPENDS ON PB20. §8.4.3.3.4 GR6 preserves a reference-modified item's class, so
        // `FUNCTION INTEGER-OF-BOOLEAN (bit-item (1:6))` — the standard's own Annex D worked example — passes.
        // Under the pre-PB20 rule, which typed EVERY ref-mod result class alphanumeric on the authority of a
        // clause that does not exist, this arm would have rejected that example. Adding the row before fixing
        // the class rule would have shipped a screen that rejects the specification's own sample program.
        'b' => ClassBoolean,
        // ⛔ 'i' SCREENS EXACTLY AS 'n' DOES (owner decision 2026-08-02). The comment that stood here argued the
        // opposite — that §15.3 type 6 "admits an ARITHMETIC EXPRESSION — and a numeric-edited item is a legal
        // arithmetic operand, because it DE-EDITS to a defined numeric value" — and cited DA6's screen, a corpus
        // golden and an AssertSpec test as encoding it. ⚠ THAT WAS THE SAME READING THE 'n' ARM BELOW HAD
        // ALREADY REFUTED, so the two arms rested on readings of §8.8.1.1 that could not both be right; the
        // agreement of a golden and a test is not independent evidence when both were written from the same
        // wrong premise. Re-derived, all three citations `--check`ed:
        //   · §8.8.1.1 admits "an identifier referencing a NUMERIC DATA ITEM"; §8.5.2.13 says a numeric-edited
        //     item "is referred to as a NUMERIC-EDITED data item" — a distinct defined term — and §8.5.2.1
        //     Table 2 puts category numeric-edited in class ALPHANUMERIC or NATIONAL, never numeric;
        //   · type 6's only other alternative is "an INTEGER DATA ITEM", which it also is not;
        //   · de-editing is granted by the MOVE rules (§14.9.25.4 GR6d1) and nowhere extended to arithmetic — a
        //     grant that would be unnecessary if de-editing were generally available.
        // So `FUNCTION CHAR(WS-ED)` over a `PIC Z9` is NOT conforming. The distinction between a CLASS rule and
        // §15.3's integer TYPE is still real — it is just not a distinction about numeric-edited.
        'i' => ClassNumeric,
        // The CLASS-worded string rows (kb/Work PB124 wave 5 split the old union): "class alphabetic,
        // alphanumeric, or national" — UPPER-CASE §15.97.3 r1, LOWER-CASE §15.57.3 r1, TRIM §15.96.3 r1,
        // SUBSTITUTE §15.87.3 r1, ORD §15.70.3 r1, REVERSE §15.78.3 r1, FIND-STRING §15.37.3, STANDARD-COMPARE
        // §15.85.3, LOCALE-COMPARE §15.51.3 — Table 21's cells all print Alph1. Table 2 makes numeric-edited
        // (display) class ALPHANUMERIC, so a class rule admits it as such — and it is ByClass that says so now,
        // not a remembered member (kb/Work PB305).
        's' => ClassStringRules,
        // 'a' — "class alphanumeric or national" (LOCALE-DATE §15.52.3 r1, LOCALE-TIME §15.53.3 r1, TEST-NUMVAL
        // §15.93.3 r1, TEST-NUMVAL-F §15.95.3 r1) and the string positions of the NUMVAL family and the
        // FORMATTED-* / format-literal functions. Class ALPHABETIC is out (Table 21 prints no Alph in any of their
        // cells; the old single union admitted PIC A at every one of these positions — AR-15.3-1's measured
        // over-admission). ⛔ THERE IS NO CATEGORY-WORDED KIND (kb/Work PB658): this row used to be the one hand-listed
        // CATEGORY set ('t'), which admitted the edited items its own category-worded clauses exclude while a
        // bespoke IntrinsicBinder arm rejected them for NUMVAL-C alone — two mechanisms for one rule, and the
        // other category-worded rows (NUMVAL, NUMVAL-F) had only the over-admitting one. A category-worded
        // position is this class set PLUS the CharacterCategory predicate, so the finer axis is declared in the
        // row beside the clause that states it and screened in ONE place (PredicateViolation).
        'a' => ClassAlphanumericOrNational,
        // 'c' — CONCAT (§15.18.3 r1): "class alphabetic, alphanumeric, boolean, numeric or national" — everything
        // Table 2 names but index, object and pointer (kb/Work PB58; the r2/r3 USAGE halves are
        // IntrinsicBinder.CheckConcatArgs' — a class kind cannot carry them). Alphabetic joined when the class
        // gained its member (PB124 wave 5) — the rule always named it; the fold made it unreachable.
        'c' => ClassConcat,
        // 'p' — MAX/MIN/ORD-MAX/ORD-MIN, whose rule (§15.71.3 r1 and siblings) is a NEGATIVE list. An
        // admissible-set cannot express it without also excluding classes the rule permits.
        _ => null,
    };

    /// <summary>The classes §15.59.3 / §15.63.3 / §15.71.3 r1 / §15.72.3 r1 EXCLUDE from a polymorphic
    /// (MAX/MIN/ORD-MAX/ORD-MIN) argument: "shall not be of class boolean, message-tag, object, or pointer".</summary>
    public static readonly CobolClass[] PolymorphicExcluded =
        [CobolClass.Boolean, CobolClass.Object, CobolClass.Pointer];

    /// <summary>Why this operand is inadmissible for an argument declaring <paramref name="kind"/>, or
    /// <see langword="null"/> when it is admissible or not statically decidable.</summary>
    /// <remarks>
    /// ⛔ THE TEST IS AN INTERSECTION, NOT AN EQUALITY, and that is what admits a figurative constant. An operand
    /// is inadmissible only when NONE of the classes it is capable of presenting
    /// (<see cref="CandidateClasses"/>) is one the rule permits — so ZERO passes both a class-numeric rule and a
    /// class-alphanumeric one (§8.3.3.6.4 GR4, "depending on context"), while SPACE passes only the second.
    /// For every operand with a fixed class the set is a singleton and the intersection IS the old equality, so
    /// no previously-screened shape changes verdict.
    /// </remarks>
    public static string? Violation(ArgRule rule, BoundOperand op)
    {
        char kind = rule.Kind;
        // §15.3 TYPE 6 IS "INTEGER", AND INTEGER IS NOT A CLASS (fix-queue PB40) — so the class test below
        // cannot express it and never could. Both an integer function and a numeric function are class NUMERIC
        // (§15.2 items 5 and 6, "these are of the class and category numeric"), so `'i'` admitted both and
        // `FUNCTION CHAR(FUNCTION ABS(<scaled item>))` compiled clean. This arm is the missing half.
        if (kind == 'i' && IntegerViolation(op) is { } notInteger) return notInteger;

        var candidates = CandidateClasses(op);
        if (candidates.Length == 0) return null;                          // not statically decidable — fail open

        if (kind == 'p')
        {
            // §15.59.3 r1 and siblings are a NEGATIVE list, so the operand is rejected only when EVERY class it
            // could present is excluded. A figurative that can be alphanumeric is therefore fine here, which is
            // right: `FUNCTION MAX(SPACE "A")` is two alphanumeric arguments (§15.59.3 r2 — the same class).
            // The clause is the ROW's (PB58 — the message used to say "§15.71.3" for MAX's §15.59.3 r1).
            return candidates.All(PolymorphicExcluded.Contains)
                ? $"is of class {Describe(candidates)}, which ISO {rule.Clause} excludes from a MAX/MIN-family argument list"
                : null;
        }

        if (Admissible(kind) is not { } ok || candidates.Any(ok.Contains)) return null;

        string wanted = ok.Length == 1
            ? Name(ok[0])
            : string.Join(", ", ok[..^1].Select(Name)) + " or " + Name(ok[^1]);
        return $"is of class {Describe(candidates)}; ISO §15.3 requires class {wanted}";
    }

    /// <summary>Why this operand violates a per-position <see cref="ArgPredicate"/>, or <see langword="null"/>
    /// (kb/Work PB58). <paramref name="knownWidth"/> is the operand's STATIC width in character positions when
    /// the caller can determine one (<c>IntrinsicBinder.KnownWidth</c>), else null — the width kinds fail OPEN on
    /// a runtime width, like every arm here.</summary>
    public static string? PredicateViolation(ArgPredicate p, BoundOperand op, int? knownWidth) => p.Kind switch
    {
        ArgPredicateKind.MinWidth when knownWidth is { } w && w < p.N =>
            $"is {w} character position(s) in length; ISO {p.Clause} requires at least {p.N}",
        ArgPredicateKind.ExactWidth when knownWidth is { } w && w != p.N =>
            $"is {w} character position(s) in length; ISO {p.Clause} requires exactly {p.N}",
        // "an integer data item or integer literal" — a computed operand is neither, whatever it evaluates to
        // (a parenthesised bare reference is still written as an expression and §15.37.3 r3 does not admit one).
        ArgPredicateKind.DataItemOrLiteralOnly when op is BoundComputedOperand { Expr: BoundIntrinsicCall ic } =>
            $"is FUNCTION {ic.Sig.Name}, not an integer data item or integer literal, which ISO {p.Clause} requires",
        ArgPredicateKind.DataItemOrLiteralOnly when op is BoundComputedOperand =>
            $"is an arithmetic expression, not an integer data item or integer literal, which ISO {p.Clause} requires",
        ArgPredicateKind.NotStrongGroup when op is BoundFieldOperand { Place.DenotedItem: not null, Place.Item: { } it }
                                             && StrongTypeModel.IsStrongGroup(it) =>
            $"is a strongly-typed group item, which ISO {p.Clause} does not admit",
        ArgPredicateKind.CharacterCategory when EditedCategoryOf(op) is { } edited =>
            $"is of an EDITED category ({edited}), which the CATEGORY wording of ISO {p.Clause} does not admit",
        _ => null,
    };

    /// <summary>The EDITED category of a data-item operand — numeric-edited, alphanumeric-edited or national-edited
    /// — or <see langword="null"/> for every operand whose category is not one of the three (kb/Work PB658).
    /// Table 2 folds all three into class alphanumeric or national, so the class lattice cannot see them; this is
    /// the category axis the <see cref="ArgPredicateKind.CharacterCategory"/> predicate reads. ⚠ A reference-modified
    /// view is NOT edited (<c>Place.DenotedItem</c> is null for it, so the pattern below never reaches its item):
    /// §8.4.3.3.4 GR6 makes the unique data item reference modification creates an elementary
    /// item of category alphanumeric (national stays national), whatever the underlying item was — so
    /// <c>NUMVAL(ED (1:3))</c> over a numeric-edited <c>ED</c> is legal.
    /// <para>⛔ BOTH EDITED CHARACTER CATEGORIES THROUGH THE ONE PREDICATE (kb/Work PB492):
    /// <see cref="PicInfo.IsCharacterEdited"/> answers alphanumeric-edited and national-edited alike (§8.5.2.11 makes
    /// a national-edited item category NATIONAL-EDITED, which "alphanumeric or national" does not name any more than
    /// it names alphanumeric-edited), and the numeric-edited category is read from the ANALYZED category, so a
    /// recovery profile (no class, PB960) fails open.</para></summary>
    private static string? EditedCategoryOf(BoundOperand op) =>
        op is BoundFieldOperand { Place: { DenotedItem: not null, Item: { IsGroup: false, Pic: { } pic } } }
            ? pic.AnalyzedCategory is PicCategory.NumericEdited ? "numeric-edited"
            : pic.IsCharacterEdited ? (pic.Category is PicCategory.National ? "national-edited" : "alphanumeric-edited")
            : null
            : null;

    /// <summary>
    /// Why this operand cannot satisfy a §15.3 <b>type 6 (Integer)</b> position, or <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// §15.3 type 6: "An arithmetic expression that will always result in an integer value or an integer data
    /// item shall be specified." (`--check`ed.) Two shapes are PROVABLY outside that, and only those two are
    /// rejected here — everything else fails open, because the position also admits any arithmetic expression
    /// whose result is always integral and no screen can enumerate those.
    /// </para>
    /// <para>
    /// ⛔ <b>THE FUNCTION ARM IS NOT A VALUE JUDGEMENT, AND THAT IS WHY IT CANNOT OVER-REJECT.</b>
    /// §8.4.3.2.3 SR11: "A numeric function shall not be specified where an integer operand is required, EVEN
    /// THOUGH A PARTICULAR REFERENCE OF THE NUMERIC FUNCTION MIGHT YIELD AN INTEGER VALUE." The standard settles
    /// it on the function's TYPE — so a nested call resolving to §15.x.1's Numeric row is inadmissible outright,
    /// with no question of "but this one is integral". That is the opposite of the fail-open reasoning the class
    /// arms need, and it is the standard's own choice, not this screen's.
    /// </para>
    /// <para>
    /// ⚠ WHAT MUST KEEP COMPILING, and the trap that makes the narrow form necessary:
    /// <c>FUNCTION CHAR(W-I + 1)</c> over an unscaled item is an arithmetic expression that always results in an
    /// integer, which type 6 admits in as many words. A screen written as "reject unless PROVABLY an integer"
    /// would refuse it — the PB1 failure mode, arrived at from the opposite direction.
    /// </para>
    /// </remarks>
    /// <summary>
    /// ⛔ THE ONE PRIMITIVE behind every §15.3 type 6 screen: the reason this ITEM's DESCRIPTION admits a value
    /// that is not an integer, or <see langword="null"/> when the description admits only integers (kb/Work
    /// PB248). Every §15.3 type-6 site reads THIS — the bare-operand arm of <see cref="IntegerViolation"/>, the
    /// additive-spine walk in <see cref="CollectAdditive"/> and the opaque-marking walk in
    /// <see cref="NonIntegralItemsBeneath"/> — so a shape none of them can see cannot be fixed in one of the three.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⛔ THE GOVERNING TEXT IS §15.3 TYPE 6 ITSELF, and the argument is PB40's, applied to the arm PB40's own
    /// primitive could not see. Type 6 admits exactly two things: "an arithmetic expression that will ALWAYS
    /// result in an integer value <b>or</b> an integer data item" (--check verified). A floating-point item is
    /// neither. It is not an always-integral arithmetic expression because §14.6.8.3 sets a floating-point
    /// item's content to "the algebraic value of the sending operand" (--check verified) — its DECLARED value
    /// set contains non-integers, so no reference to it is provably integral. That a particular run happens to
    /// store 7.0 in it is the same irrelevance as a <c>PIC 9V9</c> happening to hold 7.0, which the scale arm
    /// below has rejected since PB40. IT IS A TYPE TEST, NOT A VALUE TEST — both arms, for the same reason.
    /// </para>
    /// <para>
    /// ⚠ §5.5 2)b)2. CORROBORATES AND DOES NOT GOVERN — the distinction matters, because the tempting move is
    /// to lead with it. It defines the term for an identifier operand as "a <b>fixed-point</b> numeric data
    /// item, other than an intrinsic function, whose description does not include any digit positions to the
    /// right of the radix point" (--check verified), which is exactly this arm plus the scale arm in one
    /// sentence — but §5.5 2) scopes itself to "a syntax rule", and §5.3.1 says "Except for intrinsic
    /// functions, rules are categorized as syntax rules and general rules. Intrinsic functions have argument
    /// rules and returned value rules INSTEAD". §15.15.3 r1 ("Argument-1 shall be an integer") is an ARGUMENT
    /// rule, so §5.5 reaches it only as the standard's own usage of the word, never as its definition here.
    /// </para>
    /// <para>
    /// ⛔ WHY THE OMISSION WAS INVISIBLE BY CONSTRUCTION. A floating-point item — §14.6.8.3, "a data item
    /// described with the FLOAT-SHORT usage, the FLOAT-LONG usage, the FLOAT-EXTENDED usage, or any standard
    /// floating-point usage" (--check verified), plus this implementation's COMP-1/COMP-2 synonyms — is
    /// PICTURE-less, so <see cref="PicInfo.FloatItem"/> builds it with <c>Scale: 0</c>. It therefore passed
    /// <c>Admissible('i') = [CobolClass.Numeric]</c> AND missed a scale-only arm, and
    /// <c>FUNCTION TEST-DATE-YYYYMMDD(F)</c> over 20240229.9 truncated at the runtime seam and answered
    /// "valid date". Nothing consulted <see cref="PicInfo.IsFloat"/> here.
    /// </para>
    /// </remarks>
    internal static string? NonIntegralItemReason(PicInfo? pic) =>
        // ⛔ THE PREDICATE IS PicInfo.IsIntegerDescription, NOT A SECOND COPY OF IT. This method adds only the
        // PROSE; the decision is §5.5 2)b)2.'s conjunction, written down once. Usage INDEX cannot reach here
        // anyway (class index, §13.18.60 — the 'i' class screen rejects it first), and the primitive excludes it.
        pic is not { Category: PicCategory.Numeric, Usage: not Usage.Index } || pic.IsIntegerDescription ? null
        : pic.IsFloat
            ? "is a floating-point numeric data item (ISO §14.6.8.3), which ISO §15.3 type 6 does not admit — "
              + "it requires an integer data item or an always-integral arithmetic expression, and a "
              + "floating-point item's declared value set contains non-integers whatever this reference holds"
            // §15.3 type 6 — "an integer data item". A numeric item with digits to the right of the decimal
            // point is not one, and as an arithmetic expression it does not always result in an integer either.
            : "is a numeric data item with digits to the right of the decimal point, which ISO §15.3 type 6 does "
              + "not admit — it requires an integer data item or an always-integral arithmetic expression";

    /// <summary>Does this item's DESCRIPTION admit a non-integral value? The boolean face of
    /// <see cref="NonIntegralItemReason"/>, for the expression walks that need the predicate without the prose.</summary>
    private static bool AdmitsNonIntegralValue(PicInfo? pic) => NonIntegralItemReason(pic) is not null;

    public static string? IntegerViolation(BoundOperand op) => op switch
    {
        // §8.4.3.2.3 SR11 — a NUMERIC function, whatever this reference would evaluate to.
        BoundComputedOperand { Expr: BoundIntrinsicCall ic }
            when IntrinsicResultType.Resolve(ic.Sig, ic.Args) is IntrinsicType.Numeric =>
            $"is FUNCTION {ic.Sig.Name}, a numeric function, which ISO §8.4.3.2.3 SR11 bars from an integer "
            + "operand position even though a particular reference might yield an integer value",
        // §15.3 type 6 — "an integer data item". The item shapes that are NOT one live in the ONE primitive
        // (§5.5 2)b)2.'s two conditions: FIXED-POINT, and no digit positions right of the radix point).
        BoundFieldOperand { Place.DenotedItem: not null, Place.Item: { IsGroup: false } item }
            when NonIntegralItemReason(item.Pic) is { } why => why,
        // A numeric LITERAL whose VALUE is not an integer (kb/Work PB58; the floating form, kb/Work PB248):
        // neither an integer literal (§5.5 2)a) — "an integer literal, as defined in 8.3.3.3.2, Fixed-point
        // numeric literals", which no §8.3.3.3.3 floating-point literal is) nor an arithmetic expression that
        // always results in an integer value. "1.0" and "1.0E2" are left alone — their VALUES are integral and
        // type 6's expression alternative admits them.
        BoundNumericLiteral lit when HasNonIntegralValue(lit.Text) =>
            $"is the numeric literal {lit.Text}, whose value is not an integer — ISO §15.3 type 6 requires an "
            + "integer data item, an integer literal, or an always-integral arithmetic expression",
        // §15.3 type 6's EXPRESSION alternative — the two PROVABLY not-always-integral shapes (kb/Work PB124,
        // AR-15.3-6's remaining half). Soundness is the whole design: an arm may fire only when a witness
        // VALUATION yielding a non-integer provably exists, and the counterexample zoo that keeps everything
        // else fail-open is real legal source — `(I / 2) * 2` and `I / I` are always-integral, `362880 / D9`
        // is integral for every 1-digit divisor, `SCALED * 100` cancels its scale, `SCALED - SCALED` nets to
        // zero. Hence: only a BARE-item/LITERAL quotient at the ROOT (arm in NotAlwaysIntegral), and only a
        // scale>0 item appearing PURELY as additive leaves with an uncancelled net coefficient.
        BoundComputedOperand { Expr: { } expr } when NotAlwaysIntegral(expr) is { } why => why,
        _ => null,
    };

    /// <summary>
    /// ⛔ §8.4.3.2.3 SR12 — the function half of an UNSIGNED-INTEGER position (kb/Work PB1420): "An integer
    /// function other than the integer form of the ABS function shall not be specified where an unsigned integer is
    /// required" (cite.py --check 8.4.3.2.3 OK, rule 12). A nested intrinsic is judged by its TYPE, never by the
    /// value this reference would yield — the same standard-set choice SR11 makes for an integer position — so an
    /// integer function (INTEGER, INTEGER-PART, MOD, ORD …) is barred outright and the ONE exempt shape is the
    /// integer form of ABS, whose value is never negative (§15.7.4 r1: the argument itself when zero or positive,
    /// its negation otherwise). A NUMERIC function is not an integer at all,
    /// so SR11 bars it from this position as it does from any integer operand; the two arms are one screen
    /// because every position that requires an unsigned integer requires an integer first.
    /// <para>The positions that ASK it are the intrinsic argument rules that say "unsigned integer" and admit a
    /// function-identifier — CONCAT §15.18.3 r3 and BASECONVERT §15.12.3 r1's below-base-11 half
    /// (<c>IntrinsicBinder.CheckConcatArgs</c> / <c>CheckBaseConvertArgs</c>). The statement operands that print
    /// "unsigned integer data item" (§14.9.1.3 SR5, §14.9.11.3 SR3 — ACCEPT / DISPLAY LINE and COLUMN) describe a
    /// data item and sit in the screen facility (A.4.2), which docs/CONFORMANCE.md marks not claimed (kb/Work PB260).</para>
    /// </summary>
    public static string? UnsignedIntegerViolation(BoundOperand op) =>
        op is BoundComputedOperand { Expr: BoundIntrinsicCall ic }
            ? IntrinsicResultType.Resolve(ic.Sig, ic.Args) switch
            {
                IntrinsicType.Numeric =>
                    $"is FUNCTION {ic.Sig.Name}, a numeric function, which ISO §8.4.3.2.3 SR11 bars from an integer "
                    + "operand position and so from one that requires an unsigned integer",
                IntrinsicType.Integer when !CobolNames.Same(ic.Sig.Name, "ABS") =>
                    $"is FUNCTION {ic.Sig.Name}, an integer function, which ISO §8.4.3.2.3 SR12 bars (all but the "
                    + "integer form of FUNCTION ABS) from a position that requires an unsigned integer",
                _ => null,
            }
            : null;

    /// <summary>The provably-not-always-integral screen for §15.3 type 6's expression alternative (kb/Work
    /// PB124). Returns the violation text, or null to FAIL OPEN — see the arm's comment for the soundness
    /// obligations and the counterexamples that bound it.</summary>
    private static string? NotAlwaysIntegral(BoundExpr expr)
    {
        // Root quotient over a BARE data item and a literal (either side): decidable from the item's picture
        // granularity. An item of scale s ranges over every multiple of 10^(−s) its digits allow, so
        // item/d is always-integral iff d divides that granularity — for s > 0 never (d ≠ ±1), for
        // P-scaled/integer items iff d | 10^(−s)⁺. literal/item is sound only when NO possible divisor value
        // divides the numerator, which a picture-ranged divisor defeats (362880 / PIC 9) — fail open there.
        if (expr is BoundBinary { Op: '/', Left: BoundNumRef { Place.DenotedItem: not null, Place.Item: { IsGroup: false, Pic: { Category: PicCategory.Numeric } np } }, Right: BoundNumLiteral d }
            && LiteralIntegerMagnitude(d.Text) is { } dv && dv > 1   // 1 always divides; 0 is the zero-divide diagnostic's business
            // A FLOATING-POINT numerator is finer than any fixed granularity (§14.6.8.3 — its content is the
            // algebraic value moved into it), so no divisor > 1 always divides it (kb/Work PB248). The
            // fixed-point arm is the granularity argument the comment above states.
            && (np.IsFloat || np.Scale > 0 || System.Numerics.BigInteger.Pow(10, -np.Scale >= 0 ? -np.Scale : 0) % dv != 0))
            return $"divides {(np.IsFloat ? "a floating-point numeric data item" : $"a data item of scale {np.Scale}")} "
                + $"by the literal {d.Text}, which does not always "
                + "result in an integer value — ISO §15.3 type 6 admits an arithmetic expression only when it "
                + "ALWAYS results in one";
        // An additive spine (+, −, unary −) carrying a scale>0 item PURELY as its own leaves, with a net
        // coefficient the item's granularity does not cancel (10^scale ∤ net): value every other leaf at any
        // constant and step the item by 10^(−scale) — consecutive results are less than 1 apart, so a
        // non-integer value exists. An occurrence inside a NON-additive subtree voids the witness (the subtree
        // moves with the item — `SCALED - (SCALED * 1)` nets to zero through it), so such items are skipped.
        var spine = new AdditiveSpine();
        CollectAdditive(expr, 1, spine);
        foreach (var (item, (coeff, scale, isFloat, opaque)) in spine.Net)
        {
            if (opaque || coeff == 0) continue;
            // ⛔ THE FLOAT WITNESS NEEDS NO GRANULARITY TEST, and applying the fixed-point one to it would
            // disarm the arm entirely (kb/Work PB248): a float item's PicInfo carries Scale 0, so
            // `|coeff| % 10^0` is ZERO for every coefficient and the arm could never fire. Its value set is
            // finer than any decimal granularity — for a net coefficient c ≠ 0 pick the leaf value 10^−k (or
            // 2^−k for a binary float) with 10^k > |c|, hold every other leaf at zero, and c·10^−k is not an
            // integer — so a nonzero net coefficient IS the witness.
            bool witness = isFloat
                || (scale > 0
                    && System.Numerics.BigInteger.Abs(coeff) % System.Numerics.BigInteger.Pow(10, scale) != 0);
            if (!witness) continue;
            return $"is an arithmetic expression whose term '{item.CobolName}' "
                + (isFloat ? "is a floating-point numeric data item" : "has digits to the right of the decimal point")
                + ", so the sum does not always result in an integer value — ISO §15.3 "
                + "type 6 admits an arithmetic expression only when it ALWAYS results in one";
        }
        // THE CONSTANT WITNESS (kb/Work PB617). Every surviving item term is integral — an integer item, or a
        // scaled item whose net coefficient its granularity cancels, or a float netting to zero — so when every
        // OTHER term is provably integral too (RestIntegral), the expression's value is the literal constant plus
        // an integer for EVERY valuation: it is never an integer when the constant is not one. `1.5 + 1` (2.5)
        // and `W-I + 0.5` are refused on that VALUE, while `1.5 + 0.5` (2) and `3 - 1` are admitted — the
        // question is the sum, never the presence of a fraction in some literal. A term the fold cannot value
        // and cannot prove integral (a numeric function, a quotient over an item) clears RestIntegral: FAIL OPEN.
        if (spine.RestIntegral && !spine.Constant.IsInteger)
            return $"is an arithmetic expression whose literal terms sum to {spine.Constant} and whose every other "
                + "term is an integer, so its value is never an integer — ISO §15.3 type 6 admits an arithmetic "
                + "expression only when it ALWAYS results in an integer value";
        return null;
    }

    /// <summary>What <see cref="CollectAdditive"/> learns about an additive spine (+, −, unary −): the signed leaf
    /// count of every item whose DESCRIPTION admits a non-integral value, the exact signed sum of the terms the
    /// literal fold can value (<see cref="ExactValue"/>), and whether every remaining term is provably an
    /// integer.</summary>
    private sealed class AdditiveSpine
    {
        public readonly Dictionary<DataItem, (int Coeff, int Scale, bool Float, bool Opaque)> Net = new();
        public ExactRational Constant = ExactRational.Zero;
        public bool RestIntegral = true;
    }

    /// <summary>Walk an additive spine, accumulating the signed leaf count of every item whose DESCRIPTION
    /// admits a non-integral value (<see cref="NonIntegralItemReason"/> — a scale&gt;0 item OR a floating-point
    /// one) and the exact sum of its literal-only terms; any occurrence under a non-additive node marks the item
    /// OPAQUE (the witness argument no longer holds for it) and — unless that node is provably integral — voids
    /// the constant witness.</summary>
    private static void CollectAdditive(BoundExpr e, int sign, AdditiveSpine spine)
    {
        switch (e)
        {
            case BoundBinary { Op: '+' } b:
                CollectAdditive(b.Left, sign, spine); CollectAdditive(b.Right, sign, spine); break;
            case BoundBinary { Op: '-' } b:
                CollectAdditive(b.Left, sign, spine); CollectAdditive(b.Right, -sign, spine); break;
            case BoundNegate n:
                CollectAdditive(n.Operand, -sign, spine); break;
            case BoundNumRef { Place.DenotedItem: not null, Place.Item: { IsGroup: false, Pic: { } p } and { } it }
                when AdmitsNonIntegralValue(p):
                spine.Net[it] = spine.Net.TryGetValue(it, out var v)
                    ? (v.Coeff + sign, p.Scale, p.IsFloat, v.Opaque) : (sign, p.Scale, p.IsFloat, false);
                break;
            default:
                if (ExactValue(e) is { } constant)
                    spine.Constant += sign < 0 ? -constant : constant;
                else if (!ProvablyIntegral(e))
                {
                    spine.RestIntegral = false;
                    foreach (var it in NonIntegralItemsBeneath(e))
                        spine.Net[it.Item] = spine.Net.TryGetValue(it.Item, out var prior)
                            ? (prior.Coeff, it.Scale, it.Float, true) : (0, it.Scale, it.Float, true);
                }
                break;
        }
    }

    /// <summary>Is <paramref name="e"/> an integer for EVERY valuation of the items it references? Sound, not
    /// complete: true only for a literal fold with an integral value, an item whose description admits only
    /// integers, and +, −, ×, unary − and a non-negative integral literal power over such terms. A quotient over
    /// an item, a function and every other shape answer false — the caller then fails open.
    /// <para>⛔ It must agree with <see cref="NonIntegralItemsBeneath"/>: a subtree that returns true here has no
    /// item beneath it that admits a non-integral value, which is why <see cref="CollectAdditive"/> may skip the
    /// opaque marking for it.</para></summary>
    private static bool ProvablyIntegral(BoundExpr e) => e switch
    {
        _ when ExactValue(e) is { } v => v.IsInteger,
        // Category NUMERIC only: any other operand an extension admits (a de-edited numeric-edited item, say) is
        // not provably integral, and the screen fails open over it.
        BoundNumRef { Place.DenotedItem: not null, Place.Item: { IsGroup: false, Pic: { Category: PicCategory.Numeric } p } }
            => !AdmitsNonIntegralValue(p),
        BoundBinary { Op: '+' or '-' or '*' } b => ProvablyIntegral(b.Left) && ProvablyIntegral(b.Right),
        BoundNegate n => ProvablyIntegral(n.Operand),
        BoundPower { Exp: var x } pw => ProvablyIntegral(pw.Base) && ExactValue(x) is { IsInteger: true, Sign: >= 0 },
        _ => false,
    };

    /// <summary>The EXACT algebraic value of a literal-only subtree (numeric literals under +, −, ×, ÷, unary −
    /// and an integral literal exponent of modest magnitude), or null when the subtree references anything else,
    /// divides by zero (the zero-divide screen's business) or raises to a power this fold declines.
    /// <para>⚠ DETERMINATION — the value is the ALGEBRAIC one (§8.3.3.3.2 / §8.3.3.3.3 GR5's literal values
    /// combined exactly), not a value rounded to an arithmetic mode's intermediate: §15.3 type 6 asks whether the
    /// expression "will always result in an integer value", a property of the expression, and an exact fold is the
    /// only reading under which the screen's verdict cannot depend on the ARITHMETIC clause.</para></summary>
    private static ExactRational? ExactValue(BoundExpr e)
    {
        switch (e)
        {
            case BoundNumLiteral lit:
                return TryLiteralValue(lit.Text, out var sig, out int exp10) ? ExactRational.FromScaled(sig, exp10) : null;
            case BoundNegate n:
                return ExactValue(n.Operand) is { } v ? -v : null;
            case BoundBinary b when ExactValue(b.Left) is { } l && ExactValue(b.Right) is { } r:
                return b.Op switch
                {
                    '+' => l + r,
                    '-' => l - r,
                    '*' => l * r,
                    '/' when r.Sign != 0 => l / r,
                    _ => null,
                };
            case BoundPower pw when ExactValue(pw.Base) is { } bv && ExactValue(pw.Exp) is { IsInteger: true } ev
                && System.Numerics.BigInteger.Abs(ev.Numerator) <= MaxFoldedExponent
                && (bv.Sign != 0 || ev.Sign > 0):
                return bv.Pow((int)ev.Numerator);
            default:
                return null;
        }
    }

    /// <summary>The largest literal exponent <see cref="ExactValue"/> folds — a bound on the fold's work, never on
    /// what the source may say: a larger exponent is left unfolded and the screen fails open.</summary>
    private const int MaxFoldedExponent = 64;

    /// <summary>An exact rational in lowest terms with a positive denominator — the carrier of
    /// <see cref="ExactValue"/>'s fold. A decimal literal is num/10^k; a quotient of literals may leave any
    /// denominator, which is why this is not a scaled decimal.</summary>
    private readonly record struct ExactRational
    {
        public System.Numerics.BigInteger Numerator { get; }
        public System.Numerics.BigInteger Denominator { get; }

        private ExactRational(System.Numerics.BigInteger num, System.Numerics.BigInteger den)
        {
            if (den.Sign < 0) { num = -num; den = -den; }
            var g = System.Numerics.BigInteger.GreatestCommonDivisor(num, den);
            if (!g.IsZero && !g.IsOne) { num /= g; den /= g; }
            Numerator = num;
            Denominator = num.IsZero ? System.Numerics.BigInteger.One : den;
        }

        public static ExactRational Zero => new(0, 1);

        /// <summary>significand × 10^exp10 — <see cref="TryLiteralValue"/>'s decomposition of a literal.</summary>
        public static ExactRational FromScaled(System.Numerics.BigInteger significand, int exp10) => exp10 >= 0
            ? new(significand * System.Numerics.BigInteger.Pow(10, exp10), 1)
            : new(significand, System.Numerics.BigInteger.Pow(10, -exp10));

        public bool IsInteger => Denominator.IsOne;
        public int Sign => Numerator.Sign;

        public static ExactRational operator -(ExactRational a) => new(-a.Numerator, a.Denominator);
        public static ExactRational operator +(ExactRational a, ExactRational b) =>
            new(a.Numerator * b.Denominator + b.Numerator * a.Denominator, a.Denominator * b.Denominator);
        public static ExactRational operator -(ExactRational a, ExactRational b) => a + -b;
        public static ExactRational operator *(ExactRational a, ExactRational b) =>
            new(a.Numerator * b.Numerator, a.Denominator * b.Denominator);
        /// <summary>Division; the caller guarantees a nonzero divisor.</summary>
        public static ExactRational operator /(ExactRational a, ExactRational b) =>
            new(a.Numerator * b.Denominator, a.Denominator * b.Numerator);

        /// <summary>this^n for |n| ≤ <see cref="MaxFoldedExponent"/>; the caller guarantees a nonzero base when
        /// n &lt; 0.</summary>
        public ExactRational Pow(int n) => n >= 0
            ? new(System.Numerics.BigInteger.Pow(Numerator, n), System.Numerics.BigInteger.Pow(Denominator, n))
            : new(System.Numerics.BigInteger.Pow(Denominator, -n), System.Numerics.BigInteger.Pow(Numerator, -n));

        /// <summary>The value as the diagnostic prints it: a decimal numeral when the denominator is a product of
        /// 2s and 5s (every sum of decimal literals), otherwise the fraction.</summary>
        public override string ToString()
        {
            var den = Denominator;
            int twos = 0, fives = 0;
            while (den.IsEven) { den /= 2; twos++; }
            while (den % 5 == 0) { den /= 5; fives++; }
            if (!den.IsOne) return $"{Numerator}/{Denominator}";
            int k = Math.Max(twos, fives);
            var scaled = System.Numerics.BigInteger.Abs(Numerator * System.Numerics.BigInteger.Pow(10, k) / Denominator);
            string digits = scaled.ToString().PadLeft(k + 1, '0');
            string text = k == 0 ? digits : $"{digits[..^k]}.{digits[^k..]}";
            return Numerator.Sign < 0 ? "-" + text : text;
        }
    }

    /// <summary>Every numeric item whose description admits a non-integral value referenced anywhere beneath a
    /// non-additive subtree. ⛔ It MUST see exactly what <see cref="CollectAdditive"/>'s leaf arm sees — the two
    /// halves are one rule (the same <see cref="NonIntegralItemReason"/> primitive), and a shape visible to the
    /// leaf arm but not to this one would fire SPURIOUSLY on legal source: <c>F - (F * 1)</c> nets to zero only
    /// because this walk marks <c>F</c> opaque (kb/Work PB248 — the float widening had to land in both).</summary>
    private static IEnumerable<(DataItem Item, int Scale, bool Float)> NonIntegralItemsBeneath(BoundExpr e)
    {
        switch (e)
        {
            case BoundNumRef { Place.Item: { IsGroup: false, Pic: { } p } and { } it } when AdmitsNonIntegralValue(p):
                yield return (it, p.Scale, p.IsFloat); break;
            case BoundBinary b:
                foreach (var x in NonIntegralItemsBeneath(b.Left)) yield return x;
                foreach (var x in NonIntegralItemsBeneath(b.Right)) yield return x;
                break;
            case BoundNegate n:
                foreach (var x in NonIntegralItemsBeneath(n.Operand)) yield return x;
                break;
            case BoundPower p2:
                foreach (var x in NonIntegralItemsBeneath(p2.Base)) yield return x;
                foreach (var x in NonIntegralItemsBeneath(p2.Exp)) yield return x;
                break;
        }
    }

    /// <summary>
    /// ⛔ THE ONE EXACT DECODE both literal screens read — <see cref="CobolNet.Common.NumericLiteral.TryParseExact"/>,
    /// "the ONE exact parser the binder's range checks, the standard-decimal literal operand and the VALUE
    /// initializer share", which handles the FIXED-POINT form (§8.3.3.3.2) and the FLOATING-POINT form
    /// (§8.3.3.3.3, whose GR5 value is "the algebraic product of the value of its significand and the quantity
    /// derived by raising ten to the power of the exponent") alike. Returns the literal's value as an unscaled
    /// significand and a power of ten, or <see langword="false"/> for text that is not a numeric literal.
    /// </summary>
    /// <remarks>⚠ The DECIMAL-POINT IS COMMA radix separator (§12.3.7.4 GR14a — "the character written in
    /// numeric literals to represent the decimal separator shall be the comma") is folded to '.' first: the two
    /// screens below have always treated ',' as the radix point, and §8.3.3.3.2 gives a numeric literal no
    /// grouping separator at all ("digits '0' through '9', the plus sign, the minus sign, and the decimal
    /// point") and at most one decimal point (r3), so the substitution cannot change a literal's value.</remarks>
    private static bool TryLiteralValue(string text, out System.Numerics.BigInteger unscaled, out int exp10)
    {
        unscaled = 0;
        bool ok = CobolNet.Common.NumericLiteral.TryParseExact(text.Replace(',', '.'), out var sig, out exp10);
        if (ok) unscaled = sig;
        return ok;
    }

    /// <summary>The literal's magnitude as an integer, or null when its VALUE is not an integer (those are
    /// not the quotient screen's business — the item/d granularity argument needs an integral d).</summary>
    /// <remarks>⚠ An E-form literal used to return null here unconditionally, which failed the quotient screen
    /// OPEN for every floating-point divisor (kb/Work PB248). <c>1.0E2</c> is the integer 100 and divides
    /// exactly as <c>100</c> does; the form of the literal was never the question, its VALUE was.</remarks>
    private static System.Numerics.BigInteger? LiteralIntegerMagnitude(string text)
    {
        if (!TryLiteralValue(text, out var sig, out int exp10)) return null;
        var mag = System.Numerics.BigInteger.Abs(sig);
        if (exp10 >= 0) return mag * System.Numerics.BigInteger.Pow(10, exp10);
        var unit = System.Numerics.BigInteger.Pow(10, -exp10);
        return mag % unit == 0 ? mag / unit : null;
    }

    /// <summary>
    /// Is this numeric literal's VALUE not an integer? §15.3 type 6 admits an integer literal (§5.5 2)a) — "an
    /// integer literal, as defined in 8.3.3.3.2, Fixed-point numeric literals") or an arithmetic expression
    /// that always results in an integer value, and a literal IS an arithmetic expression (§8.8.1.1), so the
    /// question the screen must ask is about the VALUE, not the written form: <c>1.0</c> and <c>1.0E2</c> pass,
    /// <c>1.5</c> and <c>2.02402299E7</c> do not.
    /// </summary>
    /// <remarks>⛔ IT USED TO OPEN <c>if (text.Contains('E')) return false;</c> — "an E-form literal is left to
    /// the runtime" — which disarmed the arm for EVERY floating-point literal (kb/Work PB248), and the grammar
    /// admits one directly in the argument region. <c>FUNCTION TEST-DATE-YYYYMMDD(2.02402299E7)</c> reached the
    /// runtime's <c>TryIntegerArg</c>, was truncated to 20240229, and answered "valid date".</remarks>
    private static bool HasNonIntegralValue(string text) =>
        TryLiteralValue(text, out var sig, out int exp10)
        && exp10 < 0
        && System.Numerics.BigInteger.Abs(sig) % System.Numerics.BigInteger.Pow(10, -exp10) != 0;

    /// <summary>The operand's class for a diagnostic — the one it has, or the choice it offers.</summary>
    private static string Describe(CobolClass[] candidates) => candidates.Length == 1
        ? Name(candidates[0])
        : string.Join(" or ", candidates.Select(Name));

    /// <summary>Why this argument LIST violates the schema's cross-argument rule, or <see langword="null"/>.</summary>
    /// <remarks>
    /// ⛔ THE CANDIDATE-SET MODEL MAKES THIS AN INTERSECTION, and that is the whole reason it is three lines
    /// rather than a special case per figurative. "All arguments shall be of the same class" is exactly "some
    /// class is available to every argument": <c>MAX(ZERO 5)</c> intersects to {numeric} because §8.3.3.6.4 GR4
    /// lets ZERO be either; <c>MAX(SPACE "A")</c> intersects to {alphanumeric}; <c>MAX(SPACE 5)</c> and
    /// <c>MAX(1 "A")</c> intersect to EMPTY and are rejected. The PB48 set model was built for a different
    /// question and answers this one for free (CLAUDE.md rule 5 — the shape that makes the next case automatic).
    /// <para>
    /// ⚠ FAIL-OPEN, like every other arm here: an operand whose class is not statically decidable contributes no
    /// constraint rather than an empty intersection, so an undecidable argument can never turn a legal list into
    /// a rejected one.
    /// </para>
    /// </remarks>
    public static string? CrossViolation(ArgSchema schema, IReadOnlyList<BoundOperand> args)
    {
        if (schema.Cross == CrossArgRule.None || args.Count < 2) return null;

        IEnumerable<int> governed = schema.Cross == CrossArgRule.AllSameClass
            ? Enumerable.Range(0, args.Count)
            : schema.MatchedPositions(args.Count);

        CobolClass[]? common = null;
        foreach (int i in governed)
        {
            if (i >= args.Count) continue;
            var cs = CandidateClasses(args[i]);
            if (cs.Length == 0) continue;                       // not statically decidable — contributes nothing
            // ⛔ TWO NORMALIZATIONS, AND THEY ARE DIFFERENT THINGS — conflating them into the one-case fold that
            // stood here is the whole of kb/Work PB305, and ten catalogued functions inherited it. See
            // CrossBlock: the §8.5.2.1 Table-2 CLASS projection runs FIRST, the cross rules' own
            // alphabetic|alphanumeric merge SECOND. Normalizing the candidate keeps the intersection model intact.
            cs = [.. cs.Select(CrossBlock).Distinct()];
            common = common is null ? cs : [.. common.Intersect(cs)];
            if (common.Length == 0)
                return schema.Cross == CrossArgRule.AllSameClass
                    ? $"argument-{i + 1} is of class {Describe(cs)}, which cannot agree with the earlier "
                      + $"arguments; ISO {schema.CrossClause} requires all arguments to be of the same class"
                    : $"argument-{i + 1} is of class {Describe(cs)}, which cannot agree with argument-1; "
                      + $"ISO {schema.CrossClause} requires it to be of the same class as argument-1";
        }
        return null;
    }

    /// <summary>
    /// The BLOCK a candidate class occupies for a CROSS-argument rule: its §8.5.2.1 Table-2 class
    /// (<see cref="TableTwoClass"/>), with class alphabetic and class alphanumeric then merged.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⛔ THE ORDER IS LOAD-BEARING — project FIRST, merge SECOND — because the two steps rest on different
    /// authorities. The projection is a CLASS FACT: EVERY cross clause in the catalogue is class-worded, so the
    /// operand must be read through Table 2's class column before anything else happens to it. §15.37.3 r2 makes
    /// argument-2 "a data item or literal of either class alphabetic or alphanumeric"; §15.96.3 r2 makes it "a
    /// single character that is either class alphabetic or class alphanumeric"; §15.87.3 r2 says it per
    /// argument-2/argument-3 pair; §15.59.3 r2, §15.63.3 r2, §15.71.3 r3 and §15.72.3 r3 say "All arguments
    /// shall be of the same class"; §15.68.3 r2 says "of the same class as argument-1" even though its own r1 is
    /// category-worded; and §15.48.3 r3 / §15.79.3 r3 / §15.92.3 r2's "the same type as argument-1" is read as
    /// the class (each schema row cites it). The merge is a RULE-LEVEL EXCEPTION, not a class fact: §15.59.3 r2
    /// writes it outright — "with the exception that mixing of arguments of alphabetic and alphanumeric classes
    /// is allowed" — and §15.87.3 r2 / §15.96.3 r2 spell the same two blocks per pair. Alphabetic IS its own
    /// Table-2 class; numeric-edited is NOT.
    /// </para>
    /// <para>
    /// ⚠ A merge that ran on the UNPROJECTED member — the one-case fold this replaced — left
    /// <see cref="CobolClass.NumericEditedDeEditing"/> alone in a block of its own, which is how a screen that
    /// had already been told "Table 2 makes numeric-edited (display) class ALPHANUMERIC" (the 's' arm's own
    /// comment, and the member's own doc) came to reject an alphanumeric literal standing beside it. Written the
    /// other way round the message even said so: "argument-2 is of class alphanumeric (numeric-edited), which
    /// cannot agree with argument-1" (kb/Work PB305 — <c>feedback_two_arm_dispatch</c>).
    /// </para>
    /// </remarks>
    private static CobolClass CrossBlock(CobolClass c) =>
        TableTwoClass(c) is var cls && cls == CobolClass.Alphabetic ? CobolClass.Alphanumeric : cls;

    internal static string Name(CobolClass c) => c switch
    {
        CobolClass.Alphabetic => "alphabetic",
        CobolClass.Alphanumeric => "alphanumeric",
        CobolClass.NumericEditedDeEditing => "alphanumeric (numeric-edited)",
        CobolClass.Boolean => "boolean",
        CobolClass.National => "national",
        CobolClass.NumericEditedNational => "national (numeric-edited)",
        CobolClass.Numeric => "numeric",
        CobolClass.Object => "object",
        CobolClass.Pointer => "pointer",
        CobolClass.Index => "index",
        _ => c.ToString().ToLowerInvariant(),
    };
}
