// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>
/// ⛔ THE ONE READER of a described data item's ISO/IEC 1989:2023 §8.5.2 CLASS and CATEGORY for a syntax rule
/// that is WORDED in one of them, and the ONE English face for it.
/// <para>WHY IT EXISTS. Three rules of the file control entry are worded the same way and were each about to grow
/// their own copy of "is this item category alphanumeric?": §12.4.5.2 SR7 (<i>"Data-name-1 shall reference an
/// alphanumeric data item"</i>, ASSIGN … USING), §12.4.5.12.3 SR2 and §12.4.5.6.3 SR2 (<i>"category alphanumeric
/// or category national"</i>, the record keys). The first was written inline in <c>DataBinder</c>; the second pair
/// had no site at all (kb/Work PB743). One rule written down in more than one place is this repository's most
/// reproducible defect shape, so the predicate lives here and the rules cite it.</para>
/// <para>⛔ IT HAPPENED ANYWAY, AND HERE (kb/Work PB337). The GROUP arm's own definition — §13.18.29.4 GR3, what
/// an <i>alphanumeric group item</i> IS — was written down TWICE: as <c>item.IsGroup</c> here (the structural
/// conjunct alone) and completely, but privately, in <c>DataBinder.GroupValue</c>. The incomplete copy is the
/// one every rule above reads, so each of them admitted a strongly-typed group and a variable-length group,
/// which GR3 excludes by name. <see cref="IsAlphanumericGroup"/> is now the ONE spelling and
/// <c>DataBinder.GroupValue</c> calls it. The INTO-receiver rules of §14.9.30.3 SR1 b) and §14.9.34.3 SR2 b),
/// which name the same operand set, were given their first implementation through it rather than through a
/// fourth copy.</para>
/// <para>THE GROUP ARM IS §13.18.29.4 GR3 — <i>"If a GROUP-USAGE clause is not specified or implied for a group
/// item that is not strongly typed and is not a variable-length group, that group item is an alphanumeric group
/// item"</i> — so a plain group key is category alphanumeric and a <c>GROUP-USAGE NATIONAL</c> group is category
/// national. Both are read through <see cref="Table16Operand.Of(DataItem)"/>, the compiler's ONE classifier of an
/// item's category, which already resolves a bit / national group's as-if PICTURE (§13.18.29.4 GR1b/GR2b) and
/// answers <see cref="PicCategory.Group"/> for the plain group. Never a second walk of <c>Pic</c> /
/// <c>AsIfPic</c> / <c>GroupUsage</c>.</para>
/// <para>⚠ WHAT THIS SCREEN DELIBERATELY DOES NOT SPLIT, said out loud: <see cref="PicCategory"/> has no
/// <c>AlphanumericEdited</c> or <c>NationalEdited</c> member — an alphanumeric-edited item carries category
/// <see cref="PicCategory.Alphanumeric"/> and a national-edited one category <see cref="PicCategory.National"/>,
/// each distinguished only by <see cref="PicInfo.EditMask"/> (data-model design D-N6; kb/Work PB492) — so a
/// screen written here admits BOTH edited categories where the standard's category list names only the plain
/// ones. Since PB492 that is a CHOICE rather than a blindness (<c>PicInfo.IsCharacterEdited</c> would decide it),
/// and the choice is to keep erring toward accepting legal source. That is the established posture for a
/// category screen in this compiler
/// (<c>RecordLayout.CategoryOfItem</c>, §14.9.41.3 SR6 b) 2.: <i>"where the model cannot tell two categories
/// apart the test passes — this screen exists to reject what the rule NAMES, never what this compiler cannot
/// classify"</i>), and it errs toward accepting legal source rather than rejecting it. Category ALPHABETIC is
/// visible (<c>PicInfo.IsAlphabetic</c>) and is excluded: §8.8.4.2.4's <i>"A class alphabetic operand shall be
/// treated as though it were an operand of class alphanumeric"</i> is a COMPARISON rule, not a category
/// identity.</para>
/// </summary>
public static class ItemCategory
{
    /// <summary>Category ALPHANUMERIC (ISO §8.5.2.4): an elementary <c>PIC X</c> item, or an alphanumeric group
    /// item (§13.18.29.4 GR3). Alphabetic (<c>PIC A</c>) is its own category and is excluded, as are numeric,
    /// numeric-edited, national, boolean and the PICTURE-less pointer / object-reference usages.</summary>
    public static bool IsAlphanumeric(DataItem item) => Admits(item, national: false);

    /// <summary>Category ALPHANUMERIC or category NATIONAL — the operand set §12.4.5.12.3 SR2 and §12.4.5.6.3 SR2
    /// name for a record key, and the elementary half of §14.9.30.3 SR1 b) / §14.9.34.3 SR2 b)'s INTO-receiver
    /// set. A <c>GROUP-USAGE NATIONAL</c> group qualifies through its as-if <c>PIC N(m)</c> — §13.18.29.4 GR2 b)
    /// makes a national group "treated as though it were an elementary data item of usage national … described
    /// with PICTURE N(m)", which is exactly the shape those rules name; a <c>GROUP-USAGE BIT</c> group does not
    /// (GR1 makes it category boolean).</summary>
    public static bool IsAlphanumericOrNational(DataItem item) => Admits(item, national: true);

    /// <summary>
    /// ⛔ ISO §13.18.29.4 GR3 — <i>"If a GROUP-USAGE clause is not specified or implied for a group item that is
    /// not strongly typed and is not a variable-length group, that group item is an alphanumeric group item."</i>
    /// THE ONE spelling of that definition; the rules worded <i>"an alphanumeric group item"</i> ask it here.
    /// <para>⛔ ALL THREE conjuncts, and the third and fourth are why this is not inlined anywhere. The copy
    /// inside <see cref="Admits"/> carried only the structural half (<c>item.IsGroup</c>) and therefore answered
    /// TRUE for a strongly-typed group and for a variable-length group — two shapes GR3 excludes by name — so
    /// every rule reading this predicate under-rejected them (kb/Work PB337: the DYNAMIC LENGTH <c>LIMIT</c>
    /// operand of §12.4.5.2 SR7 and the record keys of §12.4.5.12.3 SR2 / §12.4.5.6.3 SR2 each admitted a strong
    /// group). A second, COMPLETE copy lived privately in <c>DataBinder.GroupValue</c> for §13.18.63.3 SR14;
    /// ONE RULE WRITTEN DOWN IN TWO PLACES had already drifted, which is the whole reason it now lives here.</para>
    /// <para><c>IsGroup</c> (no PICTURE **and** it has subordinates) is the structural half no classifier
    /// carries: a PICTURE-less entry with no subordinates is not a group item and has no category at all, and
    /// admitting it would let a rule about data items answer about something that is not one.</para>
    /// </summary>
    public static bool IsAlphanumericGroup(DataItem item) => GroupKindsOf(item).HasFlag(GroupKinds.Alphanumeric);

    /// <summary>⛔ THE CATEGORY QUESTION "IS THIS A GROUP ITEM?" — any of the five kinds — as a rule worded
    /// <i>"group item"</i> asks it. It differs from the STRUCTURAL <see cref="DataItem.IsGroup"/> ("no PICTURE and
    /// has subordinates") in exactly one shape: the level-66 THROUGH alias, which §13.18.45.4 GR2 makes an
    /// alphanumeric group item although it has no subordinate entries (kb/Work PB907 — §14.9.25.4 GR9's
    /// antecedent "both the sending operand and the receiving data item are group items" was asked as
    /// <c>IsGroup</c> and told the user their alias was "not a group item").</summary>
    public static bool IsGroupItem(DataItem item) => GroupKindsOf(item) != GroupKinds.None;

    /// <summary>
    /// ⭐ <b>THE ONE ANSWER TO "WHICH KIND OF GROUP ITEM IS THIS?"</b> — the axis every rule worded as a LIST of
    /// group kinds asks about, and <see cref="GroupKinds.None"/> for anything that is not a group item.
    /// <para>⛔ WHY IT IS A SET AND NOT A BOOLEAN (kb/Work PB392). ADD §14.9.2.3 SR6 and SUBTRACT §14.9.44.3 SR6
    /// read <i>"Identifier-4 and identifier-5 shall be alphanumeric group items, national group items,
    /// variable-length groups, or strongly-typed group items and shall not be described with level-number
    /// 66"</i> — a rule that names FOUR of the five kinds. Asked as <c>item.IsGroup</c> it admitted the fifth: a
    /// <c>GROUP-USAGE BIT</c> group passed, and the statement then executed as a silent no-op, because
    /// §14.7.6 rule 3 requires both items of an implied ADD/SUBTRACT pair to be numeric and every child of a bit
    /// group is category boolean — which is exactly WHY SR6 leaves bit groups out. A scalar predicate where the
    /// rule names a set is <c>feedback_model_the_rule_shape_not_one_case</c>, and it cost in both directions.</para>
    /// <para>⛔ AND IT IS FLAGS, NOT AN ORDERED CLASSIFICATION. Four of the five kinds are mutually exclusive by
    /// construction — §13.18.29.3 SR1, <i>"The GROUP-USAGE clause may be specified only if the subject of the
    /// entry is a group item that is not strongly typed and not a variable-length group"</i>, keeps BIT and
    /// NATIONAL apart from the other two, and §3.11 defines an <i>alphanumeric group item</i> as the complement
    /// ("group item except for a bit group item, a national group item, a strongly-typed group item, or a
    /// variable-length group item"). STRONGLY-TYPED and VARIABLE-LENGTH, however, are NOT exclusive: nothing in
    /// §13.18.58.3 stops a type declaration from containing a dynamic-length elementary item. A scalar enum
    /// would have to pick one of the two and would then answer a rule that admits only the OTHER incorrectly, so
    /// the model is the set of kinds the item IS and a rule is <c>(kinds &amp; admitted) != 0</c>.</para>
    /// <para>The three hand-written copies of this classification that stood in this file — <c>Admits</c>'s
    /// <c>item.IsGroup</c>, <see cref="IsAlphanumericGroup"/>'s four conjuncts and <see cref="Face"/>'s ordered
    /// group arms — now all read this one. <c>GroupKindDriftTests</c> pins the §3.11 complement and the
    /// §13.18.29.3 SR1 disjointness.</para>
    /// </summary>
    public static GroupKinds GroupKindsOf(DataItem item)
    {
        // ⛔ A level-66 THROUGH alias IS A GROUP ITEM, and it is the one group item with no subordinates
        // (kb/Work PB907). §13.18.45.4 GR2: "When the THROUGH phrase is specified, data-name-1 defines an
        // alphanumeric group item that includes all elementary items starting with data-name-2 …" — so the
        // answer is a CATEGORY fact, not the STRUCTURAL `IsGroup` below (the entry rides Renames66 and carries
        // a composed alphanumeric PICTURE for its carrier). It is ALWAYS the alphanumeric kind: §13.18.45.3
        // SR8 bars "a strongly-typed group item … a variable-length data item, or an occurs-depending table"
        // from the range, and a level-66 entry writes no GROUP-USAGE clause. The no-THROUGH alias (GR1) takes
        // data-name-2's own attributes and is never asked here — ReferenceResolver forwards it to that item.
        if (item.Renames is { IsAlias: false }) return GroupKinds.Alphanumeric;
        // The structural half no classifier carries. §8.5.1.3.1 — "The most basic subdivisions of a record,
        // that is, those not further subdivided, are called elementary items" — so an entry with NO subordinates
        // is not a group item whatever else it is, and here it is an error-recovery artifact (a refused
        // MESSAGE-TAG entry, a picture-less USAGE NATIONAL awaiting COBOLNET0881). It has no kind at all;
        // admitting it would let a rule about group items answer about something that is not one.
        if (!item.IsGroup) return GroupKinds.None;
        // §13.18.29.4 GR1/GR2 — the GROUP-USAGE clause names the kind outright, and SR1 above guarantees such a
        // group is neither strongly typed nor variable-length, so these two arms are terminal.
        var groupUsage = GroupUsageInEffect(item);
        if (groupUsage is GroupUsage.Bit) return GroupKinds.Bit;
        if (groupUsage is GroupUsage.National) return GroupKinds.National;
        GroupKinds kinds = GroupKinds.None;
        if (StrongTypeModel.IsStronglyTyped(item)) kinds |= GroupKinds.StronglyTyped;
        if (VariableLengthCompatibility.IsVariableLength(item)) kinds |= GroupKinds.VariableLength;
        // §13.18.29.4 GR3 — no GROUP-USAGE clause, not strongly typed, not variable-length ⇒ alphanumeric.
        return kinds is GroupKinds.None ? GroupKinds.Alphanumeric : kinds;
    }

    /// <summary>The GROUP-USAGE a group is "explicitly or implicitly described as" (ISO §13.18.29.3 SR2/SR3: "All
    /// subordinate group items shall be explicitly or implicitly described as GROUP-USAGE BIT [NATIONAL]") — its
    /// own clause, else the nearest enclosing group's. The usage-inheritance walk WRITES the implied clause down
    /// onto every subordinate group, so after it has run this is the item's own <see cref="DataItem.GroupUsage"/>;
    /// asked PULL-style here so the answer is the same BEFORE that walk too — the SAME AS expansion
    /// (§13.18.49.4 GR3/GR5, kb/Work PB1300) classifies data-name-1's ancestors in the first bind pass, when a
    /// group nested in a <c>GROUP-USAGE NATIONAL</c> group has not been marked yet and would otherwise read as
    /// an alphanumeric group.</summary>
    private static GroupUsage GroupUsageInEffect(DataItem item)
    {
        for (DataItem? g = item; g is not null; g = g.Parent)
            if (g.GroupUsage is not GroupUsage.None) return g.GroupUsage;
        return GroupUsage.None;
    }

    /// <summary>The group kinds <paramref name="kinds"/> names, spelled the way the standard spells them, joined
    /// as a syntax rule joins them — so a diagnostic quoting an admitted SET is generated from the SET rather
    /// than hand-copied beside it (a second copy is how the message and the check come to disagree). The
    /// declaration order of <see cref="GroupKinds"/> is §14.9.2.3 / §14.9.44.3 SR6's own order, so those two
    /// rules' admitted set renders word for word as the rule reads.</summary>
    public static string Spell(GroupKinds kinds)
    {
        // A rule that admits every kind is not written as an enumeration — §14.9.25.3 SR12 says "group data
        // items" — so the whole set is spelled the way the standard spells it rather than as all five names.
        if (kinds is GroupKinds.Any) return "group data items";
        var parts = new List<string>(5);
        if (kinds.HasFlag(GroupKinds.Alphanumeric)) parts.Add("alphanumeric group items");
        if (kinds.HasFlag(GroupKinds.National)) parts.Add("national group items");
        if (kinds.HasFlag(GroupKinds.VariableLength)) parts.Add("variable-length groups");
        if (kinds.HasFlag(GroupKinds.StronglyTyped)) parts.Add("strongly-typed group items");
        if (kinds.HasFlag(GroupKinds.Bit)) parts.Add("bit group items");
        return parts.Count switch
        {
            0 => "no group items",
            1 => parts[0],
            2 => parts[0] + " or " + parts[1],
            _ => string.Join(", ", parts.Take(parts.Count - 1)) + ", or " + parts[^1],
        };
    }

    /// <summary>
    /// ⛔ THE ONE READER of the USAGE a data item OPERATES with — the axis every rule worded <i>"a data item
    /// with usage display or usage national"</i> asks about (ISO §14.9.18.3 SR6 / §14.9.42.3 SR2, the
    /// termination-status operand; §8.4.3.3.4 GR6's unique data item; §14.9.41.3 SR6 b) 2.'s key comparison).
    /// <para>⛔ THE GROUP ANSWER IS §8.5.2.1's OWN SENTENCE — <i>"An alphanumeric group item is treated as
    /// though it had a usage of display."</i> Every site that instead asked <see cref="DataItem.OperandPic"/>
    /// alone read a NULL for such a group and concluded "no usage", which is not what the standard says: the
    /// group HAS a usage, and it is display. That conclusion was a measured rejects-legal-source defect at the
    /// status operand of both STOP RUN and GOBACK (kb/Work PB411), and a latent one at
    /// <c>SendingValueTemp.UsageOf</c>, where a reference-modified national or bit group's §14.9.25.4 GR1
    /// intermediate was built with category NATIONAL / BOOLEAN beside usage DISPLAY — one item, two usages.
    /// (Probed: the intermediate's VALUE is unchanged by the correction, so that half is an inconsistency
    /// removed rather than a wrong answer fixed — said plainly rather than claimed as a second repro.)</para>
    /// <para>⚠ §8.5.2.1's sentence is about an ALPHANUMERIC GROUP ITEM, which §3.11 defines by exclusion —
    /// <i>"group item except for a bit group item, a national group item, a strongly-typed group item, or a
    /// variable-length group item"</i> — so it is asked through <see cref="IsAlphanumericGroup"/>, the ONE
    /// spelling of that definition, never through <c>IsGroup</c>. A bit / national group answers from its
    /// §13.18.29.4 GR1b/GR2b as-if PICTURE (usage bit / usage national) like any elementary item. A STRONGLY-TYPED
    /// group and a VARIABLE-LENGTH group answer <see langword="null"/>: the standard states a usage for neither
    /// (§8.5.2.1 gives the first its type-name as class and category and no usage at all, and §8.5.1.12.1 says
    /// the second "is not equivalent to an alphanumeric data item"), and a caller that must have an answer says
    /// so at its own site rather than having one invented here.</para>
    /// </summary>
    public static Usage? UsageOf(DataItem item) =>
        item.OperandPic?.Usage ?? (IsAlphanumericGroup(item) ? Usage.Display : null);

    /// <summary>ONE walk for both predicates — the two rules differ only in whether category national is in the
    /// admitted set, and writing the walk twice is how the alphabetic and group arms would come to disagree.</summary>
    private static bool Admits(DataItem item, bool national) => Table16Operand.Of(item) switch
    {
        // §13.18.29.4 GR3's group arm, in its ONE spelling — never `item.IsGroup` alone (see IsAlphanumericGroup).
        { Category: PicCategory.Group } => IsAlphanumericGroup(item),
        { Category: PicCategory.Alphanumeric, IsAlphabetic: false } => true,
        { Category: PicCategory.National } => national,
        _ => false,
    };

    /// <summary>
    /// ⛔ CLASS index, message-tag, object or pointer (ISO §8.5.2) — the four-class set that THREE rules name
    /// with word-for-word identical text, and which therefore gets ONE predicate:
    /// <list type="bullet">
    /// <item>§13.16.3 SR24 e) — <i>"A data item of the class index, message-tag, object, or pointer"</i> may not
    /// be a conditional variable;</item>
    /// <item>§13.18.60.3 SR11 — <i>"An elementary data item of class index, message-tag, object, or pointer shall
    /// not be a conditional variable"</i>, the same prohibition stated the other way round;</item>
    /// <item>§14.7.6 rule 4 — <i>"Neither data item contains an OCCURS, REDEFINES, or RENAMES clause or is of
    /// class index, message-tag, object, or pointer"</i>, the CORRESPONDING-phrase exclusion (kb/Work PB391,
    /// which found the third asker holding a private one-usage copy: <c>Pic?.Usage is not Usage.Index</c>, so a
    /// POINTER namesake pair was excluded only by the accident of a private Table-16 copy's
    /// <c>_ =&gt; false</c> default, and deleting that copy would have turned the accident into a silent
    /// pointer copy).</item>
    /// </list>
    /// <para>⛔ IT IS §13.18.60.3 SR4'S POPULATION, resolved through SR4's own phrase reader rather than a
    /// hand-written class list. SR4 names "The INDEX, MESSAGE-TAG, OBJECT REFERENCE, POINTER, FUNCTION-POINTER,
    /// and PROGRAM-POINTER phrases" — exactly the six USAGE phrases that produce exactly these four classes
    /// (§8.5.2) — and <see cref="Sr4PhraseOf"/> states that list as <see cref="Sr14PhraseOf"/>'s five plus
    /// INDEX. A second copy would drift the moment MESSAGE-TAG or FUNCTION-POINTER gains a bound model;
    /// <c>ConditionNameAssociationDriftTests</c> asserts the two populations are the same set.</para>
    /// <para>The WRITTEN clause is read as well as the resolved one, and both arms are load-bearing: a
    /// MESSAGE-TAG entry is refused non-support by <c>PictureAnalyzer.ParseUsage</c> (COBOLNET1943) and a
    /// FUNCTION-POINTER entry is staged there, so neither gains a <see cref="PicInfo"/> at all and only
    /// <see cref="DataItem.OwnUsage"/> sees them — while a usage acquired by §13.18.60.4 GR1 inheritance, a TYPE
    /// clone or a SAME AS copy writes no clause of its own and is visible only in the resolved
    /// <see cref="DataItem.Pic"/>.</para></summary>
    public static bool IsIndexMessageTagObjectOrPointer(DataItem item) =>
        Sr4PhraseOf(item.OwnUsage) is not null || Sr4PhraseOf(item.Pic?.Usage) is not null;

    /// <summary>Is this a MESSAGE-TAG data item? ISO §13.18.60.4 GR9 — <i>"The class and category of a
    /// message-tag data item are message-tag"</i> (<c>cite.py --check 13.18.60.4</c> OK).
    /// <para>⛔ BOTH ARMS, for the reason <see cref="IsIndexMessageTagObjectOrPointer"/> states: the WRITTEN
    /// clause is visible only in <see cref="DataItem.OwnUsage"/>, and a usage acquired by a TYPE clone or a
    /// SAME AS copy only in the resolved <see cref="DataItem.Pic"/>. The usage is DECLINED non-support (Annex
    /// A.3 item 4, refused by name with COBOLNET1943 at the entry), so the resolved profile is
    /// <c>PicInfo.MessageTagRecovery()</c> — a recovery placeholder that nonetheless carries the written usage,
    /// precisely so a later screen can ask this question instead of reading the placeholder's CATEGORY as if it
    /// were an analysis (kb/Work PB453).</para></summary>
    public static bool IsMessageTag(DataItem item) =>
        item.OwnUsage is Usage.MessageTag || item.Pic?.Usage is Usage.MessageTag;

    /// <summary>Which of ISO §13.18.60.3 SR14's phrases a <see cref="DataItem.OwnUsage"/> names, or null.
    /// FUNCTION-POINTER is included: <c>PictureAnalyzer.ParseUsage</c> stages it loud (the P13 prototype band)
    /// so its <c>Pic</c> stays null and a resolved-usage arm never sees it, but the written clause is still
    /// visible here and the rule governs it. MESSAGE-TAG (kb/Work PB487) is the SECOND member in that position,
    /// for the same reason by a different route — the usage is declined non-support and refused by name.
    /// <para>SR14: "A USAGE clause with the MESSAGE-TAG, OBJECT REFERENCE, POINTER, FUNCTION-POINTER, or
    /// PROGRAM-POINTER phrase may be specified only for an elementary data item at level 1 or an elementary data
    /// item subordinate to a type declaration that includes the STRONG phrase."</para></summary>
    public static string? Sr14PhraseOf(Usage? u) => u switch
    {
        // MESSAGE-TAG is the FIRST phrase SR14 names. Its arm lands here rather than staying a forward
        // obligation because kb/Work PB487 gave the model a Usage member for it — the usage is DECLINED
        // non-support and refused by name (COBOLNET1943), so like FUNCTION-POINTER it is the WRITTEN
        // clause, never a bound class, that this screen sees.
        Usage.MessageTag => "MESSAGE-TAG",
        Usage.Pointer => "POINTER",
        Usage.ProgramPointer => "PROGRAM-POINTER",
        Usage.FunctionPointer => "FUNCTION-POINTER",
        Usage.ObjectReference => "OBJECT REFERENCE",
        _ => null,
    };

    /// <summary>ISO §13.18.60.3 SR4's list stated as what it IS — <see cref="Sr14PhraseOf"/>'s five phrases PLUS
    /// INDEX. Written as a union rather than a second hand-copied list so the one-phrase difference between the
    /// two rules is the code's own structure and the drift test can assert it directly
    /// (<c>Sr4 \ Sr14 == {INDEX}</c>). An index data item's <see cref="PicInfo"/> is
    /// <c>(Numeric, Usage.Index)</c> — the class is numeric, so the usage, not the category, identifies it.
    /// <para>SR4: "The INDEX, MESSAGE-TAG, OBJECT REFERENCE, POINTER, FUNCTION-POINTER, and PROGRAM-POINTER
    /// phrases shall not be specified in a data item described with the CONSTANT RECORD clause, or in any item
    /// subordinate to a data item described with the CONSTANT RECORD clause."</para></summary>
    public static string? Sr4PhraseOf(Usage? u) => u is Usage.Index ? "INDEX" : Sr14PhraseOf(u);

    /// <summary>A short English face for a diagnostic — WHAT THE OPERAND IS, so a message names the reason
    /// instead of restating the rule it broke.
    /// <para>⛔ The GROUP arms are ordered the way <see cref="IsAlphanumericGroup"/> refuses, and the two
    /// §13.18.29.4 GR3 exclusions are named OUT LOUD: before kb/Work PB337 a PICTURE-less item with
    /// <c>GROUP-USAGE NONE</c> fell straight to <i>"not an elementary or group data item"</i>, which is false of
    /// every plain group — harmless only while no group could fail the predicate, and wrong the moment the
    /// strongly-typed and variable-length exclusions began to bite.</para></summary>
    public static string Face(DataItem item) =>
        item.Pic is { } pic
            ? pic.IsAlphabetic ? "a category-alphabetic item" : $"a category-{pic.Category.ToString().ToLowerInvariant()} item"
        : GroupKindsOf(item) switch
        {
            GroupKinds.None => "not an elementary or group data item",
            GroupKinds.Bit => "a bit group item (ISO §13.18.29.4 GR1)",
            GroupKinds.National => "a national group item (ISO §13.18.29.4 GR2)",
            GroupKinds.Alphanumeric => "an alphanumeric group item",
            // Strongly-typed and variable-length are the one pair §13.18.29.3 SR1 does not keep apart, so the
            // face names BOTH when an item is both rather than reporting whichever arm happened to run first.
            var k => (k.HasFlag(GroupKinds.StronglyTyped)
                        ? "a strongly-typed group item (ISO §8.5.3.3)" : "")
                   + (k == (GroupKinds.StronglyTyped | GroupKinds.VariableLength) ? " that is also " : "")
                   + (k.HasFlag(GroupKinds.VariableLength)
                        ? "a variable-length group (ISO §8.5.1.12.1)" : "")
                   + ", which §13.18.29.4 GR3 excludes from the alphanumeric group items",
        };
}

/// <summary>
/// ⭐ The FIVE kinds of group item ISO/IEC 1989:2023 distinguishes, as a SET — the shape a syntax rule worded
/// <i>"shall be alphanumeric group items, national group items, variable-length groups, or strongly-typed group
/// items"</i> (§14.9.2.3 SR6 / §14.9.44.3 SR6) needs in order to be asked as the rule is written.
/// <para>The declaration order is those two rules' own order, because <see cref="ItemCategory.Spell"/> renders a
/// set in it. Four of the five are mutually exclusive by §13.18.29.3 SR1 and §3.11; STRONGLY-TYPED and
/// VARIABLE-LENGTH are not, which is why this is <c>[Flags]</c> rather than a scalar classification — see
/// <see cref="ItemCategory.GroupKindsOf"/>.</para>
/// </summary>
[Flags]
public enum GroupKinds
{
    /// <summary>Not a group item at all — an elementary item, a level-66 RENAMES entry, or a PICTURE-less entry
    /// with no subordinates (§8.5.1.3.1 reserves "elementary" for a record's undivided subdivisions, and this
    /// compiler's error recovery leaves such an entry neither).</summary>
    None = 0,
    /// <summary>ISO §3.11 — <i>"group item except for a bit group item, a national group item, a strongly-typed
    /// group item, or a variable-length group item"</i>; §13.18.29.4 GR3 states the same thing positively.</summary>
    Alphanumeric = 1 << 0,
    /// <summary><c>GROUP-USAGE NATIONAL</c> (ISO §13.18.29.4 GR2) — class and category national.</summary>
    National = 1 << 1,
    /// <summary>ISO §8.5.1.12.1 — <i>"a group item whose data description has at least one dynamic-length
    /// elementary item or dynamic-capacity table as a subordinate item"</i>.</summary>
    VariableLength = 1 << 2,
    /// <summary>ISO §8.5.3.3 — a group described with, or subordinate to nothing but, a <c>TYPEDEF … STRONG</c>
    /// type declaration.</summary>
    StronglyTyped = 1 << 3,
    /// <summary><c>GROUP-USAGE BIT</c> (ISO §13.18.29.4 GR1) — class and category boolean.</summary>
    Bit = 1 << 4,

    /// <summary>Every kind — what a rule worded simply <i>"group data items"</i> admits (ISO §14.9.25.3 SR12,
    /// MOVE CORRESPONDING). Stated as the union so a sixth kind joins it without an edit.</summary>
    Any = Alphanumeric | National | VariableLength | StronglyTyped | Bit,
}
