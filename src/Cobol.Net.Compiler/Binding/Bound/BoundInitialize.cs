// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Bound;

// The INITIALIZE bound nodes (P7 Step 10e: the binder half moved to
// Binding/Procedure/Verbs/InitializeBinder.cs; these records/enum STAY here — the source-generated
// visitor, the emitter, BoundStores, and UsageCollectionPass key on this namespace).

/// <summary><c>INITIALIZE</c> (ISO §14.9.20), expanded at BIND time into the spec's series of implicit elementary
/// MOVEs (§14.9.20 GR4) — there is no runtime INITIALIZE: each action is one per-elementary store (the full MOVE
/// conversion/editing/padding/truncation rules apply at emit, through the ONE MOVE path) or a per-occurrence loop
/// over a table dimension (GR5b2 — every occurrence of a table element is a possible receiving operand). Multiple
/// identifier-1 expand in source order as separate statements (GR3); elementary receivers within a group appear in
/// definition order (GR8).</summary>
public sealed record BoundInitialize(IReadOnlyList<InitializeAction> Actions) : BoundStatement
{
    /// <summary>The REPLACING phrase as WRITTEN — each category-name (a §5.2.6.4 SET) with its identifier-2 /
    /// literal-1 — for the rule that is asked of the PHRASE rather than of any receiver: §14.9.20.3 SR4, "a MOVE
    /// statement with identifier-2 or literal-1 as the sending item and an item of the specified category as the
    /// receiving operand shall be valid". Its edition-gated half, §14.9.25.3 SR5's figurative→numeric rows, is
    /// the post-bind <c>VersionConformancePass</c>'s (kb/Work PB879), which is why the phrase rides the node.
    /// Every implicit INITIALIZE statement of §14.9.20.4 GR3 carries the one phrase it was written with. Empty
    /// when no REPLACING phrase is specified.</summary>
    public IReadOnlyList<InitializeReplacingItem> Replacing { get; init; } = [];
}

/// <summary>One REPLACING item: the category-name set and its sending operand (§14.9.20.2).</summary>
public sealed record InitializeReplacingItem(InitializeCategorySet Categories, BoundOperand Sender);

/// <summary>One step of an expanded INITIALIZE.</summary>
public abstract record InitializeAction;

/// <summary>One implicit elementary MOVE (ISO §14.9.20.4 GR4 — "Otherwise, the implicit statement is: MOVE
/// sending-operand TO receiving-operand"): the <see cref="BoundMove"/> itself, stored under the MOVE rules (§14.9.25
/// — conversion, editing, JUSTIFIED/padding, truncation).
/// <para>⛔ THE MOVE IS BOUND, NOT A PAIR THE EMITTER TURNS INTO ONE (kb/Work PB880). This record used to carry
/// (Target, Source) and <c>InitializeEmitter</c> built <c>new BoundMove(s.Source, [s.Target])</c> at EMIT time —
/// downstream of <c>MoveBinder.MarkFillImageStorage</c>, whose <c>StoreAsImage</c> fact <c>StorageFormPass</c>
/// consumes — so <c>INITIALIZE G REPLACING NUMERIC DATA BY SPACE</c> over an ordinary <c>PIC 9(3)</c> aborted the
/// run unit ("without image-backed storage") at every edition where the explicit <c>MOVE SPACE TO N</c> stores
/// three spaces. The move is now built by <c>MoveBinder.BindMoveOf</c> with <see cref="ImplicitMovePhrase.Initialize"/>,
/// exactly as every FROM / INTO phrase's is.</para></summary>
public sealed record InitializeStore(BoundMove Move) : InitializeAction
{
    /// <summary>The one receiving operand (GR4: "each of which has an elementary data item as its receiving operand").</summary>
    public Place Target => Move.Targets[0];

    /// <summary>The sending operand (§14.9.20.4 GR6) — after any §14.9.25.4 GR1 freeze the bind applied.</summary>
    public BoundOperand Source => Move.Source;
}

/// <summary>The per-occurrence expansion of ONE OCCURS dimension (ISO §14.9.20.4 GR5b2 — "if the elementary data
/// item is a table element, each occurrence of the elementary data item is a possible receiving-operand"): the body
/// repeats for <paramref name="Var"/> = 1‥<paramref name="Count"/>; nested dimensions nest loops, outermost first
/// (the loop variable is spliced into each body place's subscript position).
/// <para>⛔ <paramref name="Count"/> is the ONE <see cref="AllCount"/> occurrence-count model (the same one a
/// <c>table(ALL)</c> intrinsic argument carries, §15.3), NOT an integer — because §14.9.20.4 GR8 does not fix the
/// count at the maximum: "For a variable-occurrence data item, the number of occurrences initialized is determined
/// by the rules of the OCCURS clause for a receiving data item", and §13.18.38.4 GR8 then splits on WHERE
/// data-name-1 lives — GR8a (outside the group) uses "the value of the data item referenced by data-name-1 at the
/// start of the operation", GR8b (inside, receiving) "the maximum length of the group". A dynamic-capacity table's
/// count is its CURRENT CAPACITY (GR10). Three counts, one model, one renderer
/// (<c>PlaceRenderer.OccurrenceCount</c>) — kb/Work PB393; before it, the ODO arm silently initialized to the
/// maximum in both quadrants and the dynamic arm aborted the run unit.</para></summary>
public sealed record InitializeLoop(string Var, AllCount Count, IReadOnlyList<InitializeAction> Body) : InitializeAction;

/// <summary>⛔ THE PER-OCCURRENCE ARM SELECTOR — the ONE place a receiver's sending-operand is allowed to differ
/// between occurrences of the same table element (ISO §14.9.20.4 GR5c1c: an item is a receiving-operand when "a
/// table format VALUE clause is specified in the data description entry of the elementary item and that VALUE
/// clause specifies a value for the particular occurrence of the elementary data item"; GR6a3: "if the data item
/// is a table element, the literal in the VALUE clause that corresponds to the occurrence being initialized
/// determines the sending-operand").
/// <para>A Format-2 (table) VALUE keys a DIFFERENT literal to each occurrence, so the single bind-time action an
/// <see cref="InitializeLoop"/> body carries cannot express it — and the occurrences it does NOT key are not
/// receiving-operands under the VALUE phrase at all, which is why <paramref name="Otherwise"/> exists and is
/// nullable: those occurrences fall through GR5c to the REPLACING/DEFAULT arms, or to nothing.
/// <paramref name="Occurrences"/> are the run-time occurrence numbers of the subject's OCCURS chain that the
/// expansion does not pin at bind time — each an enclosing <see cref="InitializeLoop"/> variable, or a subscript
/// identifier-1 itself wrote as an expression (a typed position) — MOST INCLUSIVE FIRST — exactly the order §13.18.63.3 SR20 keys the plan's subscript tuples by, so an
/// arm's <see cref="InitializeOccurrenceArm.When"/> tuples index straight into them. Arms are TESTED IN ORDER and
/// are mutually exclusive by construction (one arm per distinct literal, the occurrences sharing it coalesced —
/// the same folding <c>ValueInitializer.OccurrenceSwitch</c> does, so a literal spanning a thousand occurrences is one
/// branch and not a thousand).</para></summary>
public sealed record InitializeOccurrenceSelect(
    IReadOnlyList<Position> Occurrences,
    IReadOnlyList<InitializeOccurrenceArm> Arms,
    InitializeAction? Otherwise) : InitializeAction;

/// <summary>One arm of an <see cref="InitializeOccurrenceSelect"/>: the occurrence tuples that take
/// <paramref name="Do"/>. Never empty — an arm with no tuple is dropped at bind time.</summary>
public sealed record InitializeOccurrenceArm(IReadOnlyList<Subscripts> When, InitializeAction Do);

/// <summary>An implicit <c>SET</c> … <c>TO NULL</c> (ISO §14.9.20.4 GR4/GR6c): a data-pointer, program-pointer, or
/// object-reference receiver is initialized to its predefined NULL value. This is a SET, NOT a MOVE — it does not
/// route through the <see cref="InitializeStore"/> conversion/editing path; the emitter renders the item's
/// <c>DefaultInitializer</c> (the predefined-NULL idiom) directly into the place.</summary>
public sealed record InitializeSetNull(Place Target) : InitializeAction;

/// <summary>An implicit <c>SET</c> <paramref name="Target"/> <c>TO</c> <paramref name="Source"/> (ISO §14.9.20.4
/// GR4 — "if the category of a receiving-operand is data-pointer, function-pointer, message-tag,
/// object-reference, or program-pointer, the implicit statement is SET receiving-operand TO sending-operand" —
/// with GR6b supplying identifier-2 as that sending-operand: "the sending-operand is the literal-1 or identifier-2
/// associated with the category specified in the REPLACING phrase").
/// <para>⛔ THIS IS THE ARM PB418 STAGED LOUD. GR6a1/GR6a2 and every pointer row of GR6c's fill table give the
/// predefined NULL, which <see cref="InitializeSetNull"/> renders; GR6b does NOT, and writing NULL there would be
/// a wrong answer rather than a missing one. It was unreachable until kb/Work PB415 gave <c>initializeCategory</c>
/// the standard's thirteen category-names — DATA-POINTER / FUNCTION-POINTER / OBJECT-REFERENCE / PROGRAM-POINTER
/// among them — and it is reachable now.</para>
/// <para>The emitter renders exactly what the EXPLICIT SET statement renders for the same operand pair —
/// <c>SetEmitter.EmitSetPointer</c>'s straight handle copy for the pointer family, <c>OoEmitter</c>'s Format-5
/// cast-and-copy (§14.9.39 GR9, "reference copy") for an object-reference receiver — because §14.9.20.4 GR4 does
/// not describe a similar statement, it names THE SET statement.</para></summary>
public sealed record InitializeSetFrom(Place Target, Place Source) : InitializeAction;

/// <summary>A receiver the binder could not materialize as a typed place — the backend emits a loud runtime
/// guard (COBOLNET_DESIGN §1.4), never a silent skip.</summary>
public sealed record InitializeErrorAction(string Feature) : InitializeAction;

/// <summary>The INITIALIZE data categories (ISO §14.9.20.2 category-name, per §8.5.2 class/category) — ALL
/// THIRTEEN of the words the printed general format encloses in its choice-indicator brace, one member each
/// (kb/Work PB415 landed the eight the grammar could not spell).
/// <para>NATIONAL-EDITED is its OWN member even though GR6c's fill table gives it the same "Figurative constant
/// national SPACES" as NATIONAL: GR5c matches a REPLACING/TO VALUE category-name against the receiving operand's
/// §8.5.2 category, and those are two different categories (§8.5.2.10 vs §8.5.2.11), so folding them would make
/// `REPLACING NATIONAL DATA BY …` reach a national-edited item the rule does not name (kb/Work PB492).</para>
/// <para><see cref="MessageTag"/> is spellable as a category-name (§8.9 reserves MESSAGE-TAG from 2023) and can
/// never CLASSIFY a receiver: no <see cref="PicCategory"/> carries message-tag, because the A.4 message-control
/// facility is owner-declined (docs/CONFORMANCE.md §4) and the DATA DIVISION refuses such an item loudly. Naming
/// it in REPLACING is therefore conforming source that matches no receiving-operand — which is the rule's own
/// answer, not a gap.</para>
/// <para>⛔ ORDINALS ARE LOAD-BEARING: <see cref="InitializeCategorySet"/> is a bitmask over them, so the member
/// count must stay under 32. <c>InitializeLaneDriftTests</c> pins that, and pins that every member is reachable
/// from a grammar category-name.</para></summary>
public enum InitializeCategory
{
    Alphabetic, Alphanumeric, AlphanumericEdited, Numeric, NumericEdited, Boolean, National, NationalEdited,
    DataPointer, ProgramPointer, FunctionPointer, ObjectReference, MessageTag,
}

/// <summary>⛔ ONE <c>category-name</c> — WHICH IS A SET, NOT A WORD (ISO §14.9.20.2 + §5.2.6.4). The printed
/// figure encloses the thirteen category names in a brace carrying CHOICE INDICATORS, and §5.2.6.4 reads them
/// "one or more of the alternatives contained within the choice indicators shall be specified, but any single
/// alternative shall be specified only once" — so `REPLACING NUMERIC ALPHANUMERIC DATA BY SPACE` names ONE
/// category-name of two categories, and every rule that consumes a category-name (§14.9.20.4 GR5c1's "one of the
/// categories specified or implied in the VALUE phrase", GR5c2's "one of the categories specified in the
/// REPLACING phrase", GR6b's "associated with the category specified in the REPLACING phrase") is a MEMBERSHIP
/// test. Modelling it as a scalar rejected every multi-category spelling at every edition (kb/Work PB415).
/// <para>A bitmask rather than a set object: the whole thing is one <c>int</c> on the bind path, membership is one
/// AND, and the union that §14.9.20.3 SR6 ("the same category shall not be repeated in a REPLACING phrase") and
/// §5.2.6.4's "only once" both test is one OR — so the fourteenth category-name is a new enum member and nothing
/// else.</para></summary>
public readonly record struct InitializeCategorySet(int Mask)
{
    /// <summary>No category named. In the VALUE phrase this is ALL's representation (§14.9.20.4 GR2 — "if ALL is
    /// specified in the VALUE phrase it is as if all of the categories listed in category-name were specified"),
    /// so the ALL case is the ABSENCE of a restriction rather than a thirteen-bit constant that would have to be
    /// widened by hand whenever the standard adds a word.</summary>
    public bool IsEmpty => Mask == 0;

    /// <summary>The set naming exactly <paramref name="c"/>.</summary>
    public static InitializeCategorySet Of(InitializeCategory c) => new(1 << (int)c);

    /// <summary>§5.2.6.4 membership — is <paramref name="c"/> one of the alternatives this category-name names?</summary>
    public bool Contains(InitializeCategory c) => (Mask & (1 << (int)c)) != 0;

    /// <summary>This set plus <paramref name="c"/> (idempotent — the caller diagnoses the repetition).</summary>
    public InitializeCategorySet With(InitializeCategory c) => new(Mask | (1 << (int)c));

    /// <summary>The union — the running "already named in this REPLACING phrase" accumulator SR6 tests against.</summary>
    public InitializeCategorySet Union(InitializeCategorySet other) => new(Mask | other.Mask);
}

/// <summary>Facts about <see cref="InitializeCategory"/> that the binder and the pure syntax checks BOTH read —
/// kept beside the enum so there is exactly one copy of each (CLAUDE.md rule 5's "one rule, one place").</summary>
public static class InitializeCategories
{
    /// <summary>Every member, once. Cached because <c>Enum.GetValues</c> allocates on every call and these scans
    /// run per REPLACING item.</summary>
    public static readonly InitializeCategory[] All = Enum.GetValues<InitializeCategory>();

    /// <summary>⛔ THE FIVE CATEGORIES WHOSE IMPLICIT STATEMENT IS A <c>SET</c>, NOT A MOVE — ISO §14.9.20.4 GR4,
    /// "if the category of a receiving-operand is data-pointer, function-pointer, message-tag, object-reference,
    /// or program-pointer, the implicit statement is SET receiving-operand TO sending-operand" — which is also
    /// exactly the list §14.9.20.3 SR3 (identifier-2 shall be specified) and SR4 (the SET shall be valid) name.
    /// ONE definition, read by both syntax checks, by the sender choice and by the receiver arm, so a sixth
    /// pointer-ish category is one entry rather than four edits that can drift apart.</summary>
    public static bool IsSetForm(InitializeCategory cat) =>
        cat is InitializeCategory.DataPointer or InitializeCategory.FunctionPointer
            or InitializeCategory.MessageTag or InitializeCategory.ObjectReference
            or InitializeCategory.ProgramPointer;

    /// <summary>⛔ THE CATEGORY-NAME'S POSITION IN ISO §14.9.25.3 TABLE 16, AS A RECEIVING OPERAND — what
    /// §14.9.20.3 SR4's second paragraph asks about: "For each of the other categories specified in the REPLACING
    /// phrase, a MOVE statement with identifier-2 or literal-1 as the sending item and <b>an item of the specified
    /// category</b> as the receiving operand shall be valid." The receiving operand of that hypothetical MOVE is
    /// the CATEGORY, not any actual receiver, so the whole rule is a bind-time screen over the REPLACING phrase
    /// and needs no receiver walk (kb/Work PB416).
    /// <para>The five SET-form categories return <see langword="null"/>: SR4's FIRST paragraph governs them and
    /// asks about a SET statement, which <c>StatementValidation.CheckInitializeReplacingSetCategoryAgrees</c>
    /// answers. Table 16 has no row or column for them at all — §14.9.25.3 SR1 bars class index, message-tag,
    /// object and pointer from a MOVE outright — so returning a position for one would be inventing a cell.</para>
    /// <para>Table 16's receiving COLUMNS pair the edited forms with their plain ones ("Alphanumeric-edited,
    /// Alphanumeric"; "National, National-edited"; "Numeric, Numeric-edited"), which is why
    /// <see cref="Table16Operand.IsEdited"/> is set here for fidelity and read only by the table's ROW arms; the
    /// ALPHABETIC column is its own, carried by <see cref="Table16Operand.IsAlphabetic"/> over the storage model's
    /// PIC A fold.</para></summary>
    public static Table16Operand? Table16Receiver(InitializeCategory cat) => cat switch
    {
        InitializeCategory.Alphabetic => new Table16Operand(PicCategory.Alphanumeric, IsAlphabetic: true),
        InitializeCategory.Alphanumeric => new Table16Operand(PicCategory.Alphanumeric),
        InitializeCategory.AlphanumericEdited => new Table16Operand(PicCategory.Alphanumeric, IsEdited: true),
        InitializeCategory.Boolean => new Table16Operand(PicCategory.Boolean),
        InitializeCategory.National => new Table16Operand(PicCategory.National),
        InitializeCategory.NationalEdited => new Table16Operand(PicCategory.National, IsEdited: true),
        InitializeCategory.Numeric => new Table16Operand(PicCategory.Numeric),
        InitializeCategory.NumericEdited => new Table16Operand(PicCategory.NumericEdited),
        _ => null,   // the five §14.9.20.4 GR4 SET-form categories — SR4's first paragraph, not Table 16
    };

    /// <summary>The category's PRINTED category-name (ISO §14.9.20.2), for diagnostics — a message about COBOL
    /// source names the COBOL word the programmer wrote, never the C# member spelling.</summary>
    public static string Spelling(InitializeCategory cat) => cat switch
    {
        InitializeCategory.Alphabetic => "ALPHABETIC",
        InitializeCategory.Alphanumeric => "ALPHANUMERIC",
        InitializeCategory.AlphanumericEdited => "ALPHANUMERIC-EDITED",
        InitializeCategory.Boolean => "BOOLEAN",
        InitializeCategory.DataPointer => "DATA-POINTER",
        InitializeCategory.FunctionPointer => "FUNCTION-POINTER",
        InitializeCategory.MessageTag => "MESSAGE-TAG",
        InitializeCategory.National => "NATIONAL",
        InitializeCategory.NationalEdited => "NATIONAL-EDITED",
        InitializeCategory.Numeric => "NUMERIC",
        InitializeCategory.NumericEdited => "NUMERIC-EDITED",
        InitializeCategory.ObjectReference => "OBJECT-REFERENCE",
        _ => "PROGRAM-POINTER",
    };
}
