// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

/// <summary>
/// THE §13.18.60.3 USAGE DECLARATION-PLACEMENT screen (kb/Work PB183) — the syntax rules of the USAGE clause
/// that restrict WHERE a usage phrase may be written, as opposed to what a PICTURE may accompany it (SR3 / SR5 /
/// SR12 / SR13 / SR20, enforced per entry in <c>PictureAnalyzer</c>) or what may REFERENCE the resulting item
/// (SR10, enforced in <c>ExpressionBinder</c>). Partial-class extension over <c>DataBinder</c>, run as the
/// declared <c>CheckUsageDeclarations</c> pass (see <c>BindPipeline</c>).
///
/// <para><b>Why this exists as its own pass.</b> Three rules of one clause, all with the same subject — a
/// WRITTEN data description entry — and not one of them was written down anywhere in the compiler. Measured on
/// 2acbd842, every one of these compiled and ran:</para>
/// <list type="bullet">
///   <item><c>01 G. 05 P USAGE POINTER.</c> — a pointer member of an ORDINARY group, SR14's headline case.</item>
///   <item><c>01 G USAGE POINTER. 05 A PIC X. 05 B PIC X.</c> — the same violation acquired through
///     §13.18.60.4 GR1, which applies a group's usage "only to each elementary item in the group".</item>
///   <item><c>01 W IS TYPEDEF. 05 WP USAGE POINTER.</c> — a WEAK typedef template; SR14 permits the subordinate
///     form only under a type declaration that includes the STRONG phrase.</item>
///   <item><c>01 P USAGE POINTER. 01 G. 05 Q SAME AS P.</c> — the same level-05 pointer manufactured by a
///     fourth acquisition route.</item>
/// </list>
///
/// <para><b>⛔ The rule was verified against the PRINTED page before the screen was written.</b> SR14's reading
/// is restrictive enough to be exactly the shape the falsely-restrictive-OCR hazard produces, so PDF page 535
/// (printed folio 505) was rendered at 300dpi and read as an image: the printed SR14 is character-for-character
/// the transcription at <c>specs/ISO_COBOL.md</c>. There is no OCR loss here and the rule really is this
/// restrictive.</para>
///
/// <para><b>⛔ SR14 OMITS INDEX; the neighbouring SR4 INCLUDES it.</b> Six usages there, five here — deliberate
/// drafting, not an oversight. <c>05 IX USAGE INDEX.</c> inside an ordinary group is LEGAL COBOL and stays legal;
/// <see cref="Sr14PlacementClass"/> and <see cref="Sr4ConstantRecordUsage"/> are therefore two predicates and
/// must never be "unified". A positive golden (<c>pb183_usage_index_in_group_ok</c>) and a unit drift test are
/// what keep that true, because over-rejection — not under-rejection — is this screen's real hazard.</para>
///
/// <para><b>Level 77 satisfies SR14's first arm — a determination, derived, not assumed.</b> The rule says "at
/// level 1" and the standard elsewhere writes "level 1 or level 77" (§8.5.1.6.3 alignment) and "a level 01 entry
/// or a level 77 entry" (§14.9.4.3 SR1) when it means both, which is a real argument the other way. It loses to
/// two texts. §8.5.1.3.2: "Three types of entries exist for which there is no true concept of level", the second
/// being "entries that specify noncontiguous working-storage, local storage, and linkage data items" — so a 77
/// entry is not AT some level greater than 1; it is outside the level system, and SR14's arm cannot exclude it
/// by level arithmetic. And §13.11.1 declares the two spellings ALTERNATIVES for one thing: "Data elements that
/// bear no hierarchical relationship to any other data item may be described as records that are single
/// elementary items. Alternatively, such data elements ... may be described as separate data description entries
/// having level-number 77." SR14's evident subject is SUBORDINATION to a non-strong group; a 77 entry is defined
/// by having no such relationship. The surveyed implementations agree — GnuCOBOL's testsuite uses
/// <c>77 ptr USAGE POINTER.</c> throughout. The test is <see cref="Sr14PermittedLevel"/>, ONE named predicate,
/// so the determination is a one-line change if the owner reads the letter the other way.</para>
///
/// <para><b>A syntax rule is a "shall", so the conforming response is a compile-time diagnostic</b> — the same
/// posture as SR14's drafting twin §13.18.57.3 SR6 (COBOLNET1532, a plain <c>Edition.Error</c> with no
/// permissive escape). It is NOT dialect-gated: <c>--permissive</c> is the migration mode for constructs an
/// edition REMOVED, which this is not. GnuCOBOL accepts the shape, but the follow-GnuCOBOL rule covers
/// implementor-defined LATITUDE, and a syntax rule is not latitude.</para>
///
/// <para><b>Edition axis.</b> The MESSAGE-TAG phrase is a 2023 addition (VCR row 32) and has no <c>Usage</c>
/// member yet, so SR14's message-tag arm and SR21 (MESSAGE-TAG exclusivity) are forward obligations the drift
/// test holds open rather than dead code. For POINTER / OBJECT REFERENCE / PROGRAM-POINTER / FUNCTION-POINTER
/// Annex E carries no note on §13.18.60.3 — but Annex E's scope is 2014→2023 only, so its silence is a MISSING
/// observation, not evidence of no change, and these arms ship UNGATED (the un-narrowed reading) pending the ISO
/// 2002/2014 texts. Ungated is the safe direction here: it enforces the rule the compiler can actually cite.</para>
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>The §13.18.60.3 declaration-placement rules — SR14 (the five pointer/object usage phrases only
    /// at level 1/77 or under a STRONG type declaration), SR15 (no USAGE OBJECT REFERENCE in the file section)
    /// and SR4 (none of six usages in or under a CONSTANT RECORD). ONE verdict per offending WRITTEN entry.
    ///
    /// <para>⛔ Runs over <see cref="ConformanceForest"/>, NOT <see cref="CompositionForest"/>. These are
    /// properties of the WRITTEN clause list, and the forest choice is what makes the verdict once-per-source:
    /// it walks <see cref="Roots"/> + <see cref="LinkageRoots"/> + the TYPEDEF templates in <see cref="TypeDecls"/>
    /// and PRUNES the TYPE-clone subtrees, so <c>01 W IS TYPEDEF. 05 WP USAGE POINTER.</c> is rejected ONCE at
    /// the template no matter how many <c>TYPE W</c> reference sites exist — the entry the programmer must
    /// change. A SAME AS copy is NOT pruned (<see cref="StrongTypeModel.TypeAnchor"/> keys on a TYPE clause,
    /// which a SAME AS subject does not carry), which is exactly right: <c>05 Q SAME AS P.</c> is its own
    /// written entry, in its own group, at its own level.</para>
    ///
    /// <para>All four usage-acquisition routes fall to this ONE enumeration rather than to a hand-written copy
    /// at each site — a written USAGE clause, §13.18.60.4 GR1 group inheritance, a TYPE clone and a SAME AS
    /// copy. That is deliberate: SR14's drafting twin §13.18.57.3 SR6 is implemented as TWO hand-written parent
    /// walks (<c>ExpandType</c> and <c>ExpandSameAs</c>), and writing a fifth copy at each of four sites would
    /// have reproduced that duplication at twice the scale. A fifth acquisition route added later is screened
    /// here for free.</para></summary>
    internal void CheckUsageDeclarations()
    {
        foreach (var item in ConformanceForest())
        {
            using var _ = Edition.At(item);
            string name = item.CobolName ?? "FILLER";

            // ── §13.18.60.3 SR4 ──────────────────────────────────────────────────────────────────────────
            // SIX usage phrases, INDEX among them, barred from a CONSTANT RECORD entry "or in any item
            // subordinate to" one — IsConstantRecordItem is precisely that subtree walk (the flag lives on
            // the level-01 root). Checked before SR14 so the narrower rule names the entry's real defect: a
            // pointer member of a CONSTANT RECORD group violates both, and SR4 is the one that also governs
            // the level-1 spelling.
            if (Sr4ConstantRecordUsage(item) is { } phrase4 && IsConstantRecordItem(item))
            {
                Edition.Error(DiagnosticCatalog.UsageConstantRecord, $"data item '{name}' is described with "
                    + $"USAGE {phrase4} and is{(item.IsConstantRecord ? "" : " subordinate to")} a data item "
                    + "described with the CONSTANT RECORD clause — the INDEX, MESSAGE-TAG, OBJECT REFERENCE, "
                    + "POINTER, FUNCTION-POINTER, and PROGRAM-POINTER phrases shall not be specified there "
                    + "(ISO §13.18.60.3 SR4)");
                continue;
            }

            // ── §13.18.60.3 SR15 ─────────────────────────────────────────────────────────────────────────
            // The DIRECT declaration arm. Its SAME AS twin (§13.18.49.3 SR6) has been enforced since the SAME
            // AS landing while this one had no screen at all — the two-arm dispatch, with the arm that needs
            // no indirection missing. IsFileSectionItem is the twin's own root test, shared rather than
            // re-written. Reported independently of SR14: an FD record may legally be a level-1 elementary
            // item, so `01 R USAGE OBJECT REFERENCE C.` under an FD passes SR14 and violates only SR15.
            if (item.Pic is { Category: PicCategory.ObjectReference } && IsFileSectionItem(item))
            {
                Edition.Error(DiagnosticCatalog.UsageObjectReferenceFileSection, $"data item '{name}' is "
                    + "described with USAGE OBJECT REFERENCE and belongs to a file section record — the USAGE "
                    + "OBJECT REFERENCE clause shall not be specified in the file section "
                    + "(ISO §13.18.60.3 SR15)");
                continue;
            }

            // ── §13.18.22.3 SR4 ──────────────────────────────────────────────────────────────────────────
            // "The EXTERNAL clause shall not be specified for a data item of class object or pointer."
            // ⛔ WHY IT IS HERE AND WHY NOW (kb/Work PB231, the pointer third). Until the pointer-class storage
            // landing, `01 P USAGE POINTER EXTERNAL.` was rejected — but by the byte-window RESIDUE diagnostic
            // (COBOLNET1695 / the EXTERNAL twin), i.e. "recognized but not yet implemented", which is a
            // different verdict about a different thing. Opening the byte-window gate to pointer-class leaves
            // would have made this NONCONFORMING shape compile clean: an under-rejection created by a fix. The
            // rule that actually bars it is enforced in the same change set, and it is the ITEM's own class the
            // rule names — a strongly-typed EXTERNAL group holding a pointer MEMBER is class alphanumeric and
            // stays legal (SR5 only requires its type declaration to be external too).
            if (ScreenExternalClassItem(item, name)) continue;

            // ── §13.18.5.3 SR1 ───────────────────────────────────────────────────────────────────────────
            // "The subject of the entry shall not be of class object." The EXTERNAL twin's other half, and the
            // asymmetry is the rule's own: the BASED rule names ONLY class object, so `01 P USAGE POINTER
            // BASED.` — class POINTER — is legal source and is exactly what kb/Work PB231's pointer third
            // implements. Same reason for the timing as SR4 above: COBOLNET1695 used to reject both, and this
            // one must keep being rejected after the gate opens.
            if (ScreenBasedClassObjectItem(item, name)) continue;

            // ── §13.18.5.3 SR2 ───────────────────────────────────────────────────────────────────────────
            // "The subject of the entry shall not be a dynamic-length elementary item or a variable-length
            // group." (kb/Work PB1213.) Asked of the SHAPE, through the one screen of that pair — never of the
            // BASED cell's carriage gates (ByteWindowResidueOf / VariableLengthCellResidueOf), which answer what
            // the shared cell can HOLD and are shared with EXTERNAL and ADDRESS OF, where a variable-length group
            // is legal. Until this arm, a BASED group with a DYNAMIC LENGTH leaf compiled and ran because the
            // cell had learned to carry one, and the dynamic-capacity-table spelling was refused only as "not yet
            // implemented". A written BASED clause and one a TYPE / SAME AS copy composed both land on IsBased,
            // so both are judged here. (The elementary spelling is also §13.16.3 SR18's — DYNAMIC LENGTH admits
            // no BASED clause beside it — which clears IsDynamicLength, so it is reported once, there.)
            if (item.IsBased && VariableLengthCompatibility.DynamicLengthOrVariableLengthGroup(item) is { } variableShape)
            {
                Edition.Error(DiagnosticCatalog.BasedSubjectVariableLength, $"data item '{name}' is described with "
                    + $"the BASED clause and is {variableShape}; the subject of a BASED entry shall not be a "
                    + "dynamic-length elementary item or a variable-length group (ISO §13.18.5.3 SR2)");
                // The BASED placement arms' discipline: the refusal clears the flag, so the entry binds as ordinary
                // storage under an already-failed compile and the cell classifier never adds its carriage verdict
                // ("not yet implemented") for a shape the standard forbids outright.
                item.IsBased = false;
                continue;
            }

            // ── §13.18.60.3 SR14 ─────────────────────────────────────────────────────────────────────────
            // ARM A — a GROUP entry that WROTE one of the five phrases. §13.18.60.4 GR1: the clause "applies
            // only to each elementary item in the group", and every such item is subordinate to this group, so
            // each acquires the usage at a level greater than 1 and SR14 is violated for each. The verdict is
            // reported ONCE, at the entry carrying the clause the programmer must change — not once per leaf.
            // (Screened before arm B so the group's own header, which sheds its synthesized profile in
            // ResolveIndexItems, is not silently skipped by a Pic test that no longer sees the usage.)
            if (!Sr14Elementary(item) && ItemCategory.Sr14PhraseOf(item.OwnUsage) is { } phraseA)
            {
                Edition.Error(DiagnosticCatalog.UsageDeclarationPlacement, $"data item '{name}' is a GROUP item "
                    + $"described with USAGE {phraseA} — that usage applies to each elementary item in the "
                    + "group (ISO §13.18.60.4 GR1), and each of those is subordinate to this group rather than "
                    + $"an elementary item at level 1. A USAGE clause with the {Sr14PhraseList} phrase may be "
                    + "specified only for an elementary data item at level 1 or an elementary data item "
                    + "subordinate to a type declaration that includes the STRONG phrase "
                    + "(ISO §13.18.60.3 SR14)");
                continue;
            }

            // ARM B — an ELEMENTARY item of one of SR14's classes, however it acquired the usage.
            if (!Sr14Elementary(item) || !Sr14PlacementClass(item)) continue;
            if (Sr14PermittedLevel(item.Level) || UnderStrongTypeDeclaration(item)) continue;

            Edition.Error(DiagnosticCatalog.UsageDeclarationPlacement, $"data item '{name}' is described with "
                + $"USAGE {Sr14PhraseNameOf(item)} at level {item.Level:00}, subordinate to "
                + $"'{item.Parent?.CobolName ?? "FILLER"}', which is not a type declaration that includes the "
                + $"STRONG phrase — a USAGE clause with the {Sr14PhraseList} phrase may be specified only for "
                + "an elementary data item at level 1 or an elementary data item subordinate to a type "
                + "declaration that includes the STRONG phrase (ISO §13.18.60.3 SR14)");
        }

        // ── THE COMPOSED-ENTRY HALF OF §13.18.22.3 SR4 AND §13.18.5.3 SR1 (kb/Work PB1212) ──────────────────
        // Both rules are about an entry whose CLASS comes from one place and whose clause from another: `01 A TYPE PT
        // EXTERNAL.` writes the EXTERNAL clause at the site and takes class pointer from the type declaration PT
        // (§13.18.57.4 GR1), and the first loop above never sees A — ConformanceForest PRUNES every entry that carries
        // a TYPE clause, because the template was gated once. So the SAME two screens are asked, by the same code, of
        // each entry that DIRECTLY carries a TYPE clause. Only a level-1 entry can write EXTERNAL or BASED, so the
        // entries reached here are the site entries and nothing is reported twice (the templates were judged above).
        foreach (var item in CompositionForest().Where(i => i.TypeName is not null))
        {
            using var _ = Edition.At(item);
            string name = item.CobolName ?? "FILLER";
            if (ScreenExternalClassItem(item, name)) continue;
            ScreenBasedClassObjectItem(item, name);
        }
    }

    /// <summary>§13.18.22.3 SR4 — "The EXTERNAL clause shall not be specified for a data item of class object or
    /// pointer." True when it reported. One screen for the WRITTEN-entry loop and the TYPE-entry loop of
    /// <see cref="CheckUsageDeclarations"/>: the class is the entry's COMPOSED one, however it was acquired.</summary>
    private bool ScreenExternalClassItem(DataItem item, string name)
    {
        if (!(item.HasExternalClause && SlotWindow.CarriedBySlot(item))) return false;
        Edition.Error(DiagnosticCatalog.ExternalPointerOrObjectItem, $"data item '{name}' is described "
            + $"with USAGE {Sr14PhraseNameOf(item)} — of class "
            + $"{(item.Pic!.Category is PicCategory.ObjectReference ? "object" : "pointer")} — and with "
            + "the EXTERNAL clause; the EXTERNAL clause shall not be specified for a data item of class "
            + "object or pointer (ISO §13.18.22.3 SR4)");
        return true;
    }

    /// <summary>§13.18.5.3 SR1 — "The subject of the entry shall not be of class object." True when it reported;
    /// the twin of <see cref="ScreenExternalClassItem"/>, shared by the same two loops.</summary>
    private bool ScreenBasedClassObjectItem(DataItem item, string name)
    {
        if (!(item.IsBased && item.Pic is { Category: PicCategory.ObjectReference })) return false;
        Edition.Error(DiagnosticCatalog.BasedSubjectOfClassObject, $"data item '{name}' is described "
            + "with USAGE OBJECT REFERENCE — of class object — and with the BASED clause; the subject "
            + "of a BASED entry shall not be of class object (ISO §13.18.5.3 SR1)");
        return true;
    }

    /// <summary>SR14's subject test — "an ELEMENTARY data item", which §8.5.1.3.2 settles structurally: an entry
    /// with subordinates is a group, one without is elementary.
    ///
    /// <para>⛔ NOT <see cref="DataItem.IsGroup"/>, and the difference is load-bearing rather than stylistic.
    /// <c>IsGroup</c> is <c>Pic is null &amp;&amp; Children.Count > 0</c>, and <c>ParseUsage</c> synthesizes a
    /// pointer profile onto an entry BEFORE its subordinates are known — so <c>01 G USAGE POINTER. 05 A PIC X.</c>
    /// binds a header that is neither <c>IsGroup</c> nor honestly elementary, and an <c>IsGroup</c>-keyed screen
    /// waves it through arm A and then blesses it in arm B as "a level-1 elementary pointer". Measured exactly
    /// that way on the first build of this screen: every other shape rejected and the group-usage shape ran
    /// clean. (<c>ResolveIndexItems</c> sheds that synthesized profile for a group header whose usage is INDEX or
    /// OBJECT REFERENCE, but not for POINTER / PROGRAM-POINTER — a real modelling asymmetry, made unreachable on
    /// conforming source by arm A rather than papered over, since the shape it describes is nonconforming.)</para>
    /// </summary>
    private static bool Sr14Elementary(DataItem d) => d.Children.Count == 0;

    /// <summary>SR14's five usage phrases, as the rule spells them — used in every message so the diagnostic
    /// quotes the rule rather than a paraphrase of it.</summary>
    private const string Sr14PhraseList = "MESSAGE-TAG, OBJECT REFERENCE, POINTER, FUNCTION-POINTER, or "
        + "PROGRAM-POINTER";

    /// <summary>The classes ISO §13.18.60.3 SR14's five USAGE phrases produce (§8.5.2): class object
    /// (OBJECT REFERENCE), class pointer (POINTER / PROGRAM-POINTER / FUNCTION-POINTER) and class message-tag
    /// (MESSAGE-TAG). ⛔ CLASS INDEX IS NOT AMONG THEM — SR14 names five phrases where the neighbouring SR4
    /// names six, and the omission of INDEX is the difference; see <see cref="Sr4ConstantRecordUsage"/>.
    ///
    /// <para>This is deliberately the SAME population as <c>PointerObjectClass</c>, the §13.18.44.3 SR12/SR14
    /// REDEFINES class test (kb/Work PB179): that rule names the classes ("class object, message-tag, or
    /// pointer") and this one names the phrases that produce exactly those classes, so the two screens resolve
    /// their class question through ONE predicate and cannot drift apart as MESSAGE-TAG and FUNCTION-POINTER
    /// gain models. A unit pin asserts the identity.</para></summary>
    private static bool Sr14PlacementClass(DataItem d) => PointerObjectClass(d);

    /// <summary>The phrase to NAME in arm B's message: the item's own written clause when it wrote one, else
    /// the phrase its resolved class implies — a usage acquired by §13.18.60.4 GR1 inheritance, a TYPE clone or
    /// a SAME AS copy has no written clause of its own, and a message that said "described with USAGE &lt;null&gt;"
    /// would name a clause the user cannot find.
    /// <para>⛔ The fall-through says so rather than GUESSING a phrase. It is unreachable while
    /// <see cref="Sr14PlacementClass"/> is the caller's guard — every class it admits is named above — and a
    /// default of "POINTER" would make the ONE symptom of the predicate having been widened wrongly (an INDEX
    /// item reported as a pointer) look like an ordinary correct verdict. Measured: with the predicate
    /// deliberately widened to SR4's six-usage list, that default reported <c>05 IX USAGE INDEX.</c> as
    /// "described with USAGE POINTER". A message that cannot be false is worth one arm.</para></summary>
    private static string Sr14PhraseNameOf(DataItem d) => ItemCategory.Sr14PhraseOf(d.OwnUsage) ?? d.Pic?.Category switch
    {
        PicCategory.Pointer => "POINTER",
        PicCategory.ProgramPointer => "PROGRAM-POINTER",
        PicCategory.FunctionPointer => "FUNCTION-POINTER",
        PicCategory.ObjectReference => "OBJECT REFERENCE",
        var c => $"an unnamed usage of class {c?.ToString() ?? "(none)"} — this is a compiler defect: the "
            + "§13.18.60.3 SR14 class predicate admitted a category this message does not name",
    };

    /// <summary>SR14's "at level 1" arm. ⛔ 77 SATISFIES IT — see this file's header for the derivation
    /// (§8.5.1.3.2 "no true concept of level"; §13.11.1 makes the level-1 and level-77 spellings alternatives
    /// for one non-hierarchical data element). ONE named predicate so the determination is a one-line change.
    /// A level-66 RENAMES entry and a level-88 condition-name are not nodes in the forest at all — the binder's
    /// level stack attaches an 88 to its conditional variable — so this predicate never sees them.</summary>
    private static bool Sr14PermittedLevel(int level) => level is 1 or 77;

    /// <summary>"Subordinate to a TYPE DECLARATION that includes the STRONG phrase" — SR14's second arm, and
    /// §13.16.3 SR24 g)'s second arm, which are the same words and so are ONE predicate (kb/Work PB488 made it
    /// the second caller and dropped the <c>Sr14</c> prefix; <c>DataBinder.ConditionName.cs</c>).
    /// ⛔ The test is the DECLARATION-side <see cref="DataItem.TypedefStrong"/> on an enclosing TYPEDEF
    /// template root, NOT the post-expansion <c>StrongTypeModel.StrongRoot</c> / <c>DataItem.StrongType</c>:
    /// both rules say "subordinate to a type declaration that includes the STRONG phrase", and screening the
    /// TEMPLATE — which <see cref="ConformanceForest"/> visits once and whose clones it prunes — is what makes
    /// this a once-per-source verdict anchored at the entry the programmer wrote. Reading the post-expansion
    /// flag instead would let a WEAK typedef's pointer member escape at the template and then fire once per
    /// TYPE reference site: wrong site, wrong count, and a diagnostic naming a line that is not the defect.
    /// </summary>
    private static bool UnderStrongTypeDeclaration(DataItem item)
    {
        for (var p = item.Parent; p is not null; p = p.Parent)
            if (p.TypedefStrong) return true;
        return false;
    }

    /// <summary>Which of ISO §13.18.60.3 SR4's SIX usage phrases an entry names, or null. ⛔ INDEX IS IN THAT
    /// LIST and NOT in <see cref="Sr14PlacementClass"/>'s — SR4 reads "The INDEX, MESSAGE-TAG, OBJECT
    /// REFERENCE, POINTER, FUNCTION-POINTER, and PROGRAM-POINTER phrases", SR14 reads the same list without
    /// INDEX. Keeping them as two predicates is the whole guard against a future "unification" that would start
    /// rejecting legal <c>05 IX USAGE INDEX.</c>; the unit drift test asserts the difference is exactly INDEX.
    /// <para>⛔ BOTH PHRASE READERS NOW LIVE ON <see cref="ItemCategory"/> (kb/Work PB391), because SR4's
    /// population is also §14.7.6 rule 4's CLASS exclusion and §13.16.3 SR24 e)'s, and the CORRESPONDING binder
    /// is outside <c>DataBinder</c>. This file keeps only what is about USAGE-CLAUSE PLACEMENT.</para>
    /// <para>Reads the RESOLVED class as well as the written clause, because SR4's reach is "in a data item
    /// described with the CONSTANT RECORD clause, or in any item subordinate to" it — an inherited usage inside
    /// that subtree is as much "specified in" it as a written one (§13.18.60.4 GR1).</para></summary>
    private static string? Sr4ConstantRecordUsage(DataItem d) =>
        ItemCategory.Sr4PhraseOf(d.OwnUsage)
        ?? (Sr14Elementary(d) ? ItemCategory.Sr4PhraseOf(d.Pic?.Usage) : null);
}
