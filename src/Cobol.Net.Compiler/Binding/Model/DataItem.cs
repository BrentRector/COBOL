// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Microsoft.CodeAnalysis.CSharp;

namespace CobolNet.Binding.Model;

/// <summary>The DATA DIVISION section a data-description entry is written in — the argument of
/// <c>DataBinder.BindEntries</c> (the section-scoped placement rules, e.g. CONSTANT RECORD is WS/LS-only, ISO
/// §13.18.15.3 SR1) and, per root, <see cref="DataItem.Section"/> (the section-worded operand rules).</summary>
public enum EntrySection
{
    WorkingStorage,
    LocalStorage,
    Linkage,
    File,
}

/// <summary>The deferred declaration verdict <see cref="DataItem.Pending"/> carries between entry bind and the
/// <c>DataBinder.ResolveIndexItems</c> adjudication (P5.11c — the explicit discriminant replacing the former
/// reference-identity sentinel PicInfos <c>NationalUsagePending</c>/<c>BitUsagePending</c>/<c>RecoveryItem</c>).</summary>
public enum PicPending
{
    /// <summary>No deferred adjudication.</summary>
    None,
    /// <summary>A PICTURE-less <c>USAGE NATIONAL</c> entry (ISO §13.18.60.3 SR12 / §13.18.60.4 GR1).</summary>
    NationalUsage,
    /// <summary>A PICTURE-less <c>USAGE BIT</c> entry (ISO §13.18.60.3 SR5 / §13.18.60.4 GR1).</summary>
    BitUsage,
}

/// <summary>One Format 2 (table) VALUE phrase (ISO §13.18.63.2, COBOL-2002): a literal list keyed to an occurrence
/// range. <see cref="Literals"/> is the RAW operand text (decoded at emit — the Format-1 currency); <see cref="From"/>
/// is subscript-1 (one integer per OCCURS dimension, SR20); <see cref="To"/> is the optional subscript-2 (SR21), null
/// for the no-TO form (GR14 = fill to the table maximum). <see cref="Ordinal"/> is the source order (GR15 last-FROM
/// wins on an overlap). <see cref="FromWritten"/> / <see cref="ToWritten"/> are the subscripts AS WRITTEN, one per
/// entry of <see cref="From"/> / <see cref="To"/>: a subscript beyond the host range is carried SATURATED in the
/// integer list (kb/Work PB1579 — still out of every table's range), and a diagnostic quotes the written text, never
/// the saturated number.</summary>
public sealed record TableValueSpec(
    IReadOnlyList<string> Literals, IReadOnlyList<int> From, IReadOnlyList<int>? To, int Ordinal,
    IReadOnlyList<string> FromWritten, IReadOnlyList<string>? ToWritten)
{
    /// <summary>The FROM subscripts as the source wrote them, space-separated.</summary>
    public string FromText => string.Join(" ", FromWritten);

    /// <summary>The TO subscripts as the source wrote them, space-separated; empty for the no-TO form.</summary>
    public string ToText => ToWritten is { } t ? string.Join(" ", t) : "";
}

/// <summary>
/// A bound DATA DIVISION item: a node in the record tree. Elementary items (<see cref="Pic"/> non-null) become
/// native C# fields; group items (children, no PIC) become nested <c>record struct</c> types. There is no byte
/// offset or storage length — the .NET type IS the storage.
/// </summary>
public sealed class DataItem
{
    /// <summary>The COBOL level number (01, 05, 77, …). Levels 66/88 are not modeled in this slice.</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "both GR-1 exclusion lists exclude the level-number (ISO §13.18.57.4 GR1 / §13.18.49.4 GR1); a cloned subordinate RENUMBERS relative to its new subject (§13.18.49.4 GR2b/GR2c)")]
    public required int Level { get; init; }

    /// <summary>
    /// A globally-unique id assigned at bind time. It backs the collision-free names for an item's
    /// <c>record struct</c> type (<see cref="StructName"/>) and its numeric profile field (<see cref="ProfileName"/>)
    /// — two distinct groups both named <c>REC</c>, or two leaves both named <c>NUM</c> in different groups, would
    /// otherwise collide. (The member/field name, <see cref="CsName"/>, only needs to be unique within its struct.)
    /// </summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "identity — every clone gets a FRESH Uid; StructName/ProfileName ride on it, so a shared one would collide the clone's emitted type with the template's")]
    public int Uid { get; set; }

    /// <summary>The original COBOL data-name, or <see langword="null"/> for <c>FILLER</c>.</summary>
    [DescriptionCopy(DescriptionCopyKind.MemberOnly,
        "both GR-1 exclusion lists exclude the NAME of a subject; a cloned SUBORDINATE keeps its own (ISO §13.18.49.4 GR2a — 'the same names, descriptions, and hierarchy')")]
    public string? CobolName { get; init; }

    /// <summary>The C#-safe member/field identifier for this item (unique within its containing struct scope).</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "identity — re-uniquified among the clone's NEW siblings")]
    public required string CsName { get; set; }

    /// <summary>The analyzed PICTURE/USAGE for an elementary item; <see langword="null"/> for a group.</summary>
    [DescriptionCopy(DescriptionCopyKind.Clause,
        "PICTURE (ISO §13.18.40) — in neither GR-1 exclusion list")]
    public PicInfo? Pic { get; set; }

    /// <summary>GROUP-USAGE (ISO §13.18.29; data-model design D20; kb/Work PB79) — <see cref="Model.GroupUsage.None"/>
    /// for an ordinary (alphanumeric) group (GR3); Bit / National for a declared bit / national group AND for every
    /// group subordinate to one (SR2/SR3 "explicitly or implicitly" — propagated by <c>DataBinder.ResolveIndexItems</c>,
    /// the usage-inheritance walk). Never set on an elementary item.</summary>
    [DescriptionCopy(DescriptionCopyKind.Clause,
        "GROUP-USAGE (ISO §13.18.29) — in neither GR-1 exclusion list. ⛔ kb/Work PB522: the clause BOTH hand lists lost, so a TYPE / SAME AS copy of a bit or national group bound as an ordinary alphanumeric group")]
    public GroupUsage GroupUsage { get; set; }

    /// <summary>The §13.18.29.4 GR1b/GR2b AS-IF picture of a bit / national group — "treated as though it were an
    /// elementary data item of usage bit and class and category boolean described with PICTURE 1(m), where m is
    /// the bit length of the group" / "of usage national … PICTURE N(m), where m is the length of the group" —
    /// null for every other item. m is the group's bit extent (the §8.5.1.6.3 walk WITHOUT the trailing filler,
    /// whose NOTE excludes "a record that is entirely a bit group") or its national positions (<see cref="ImageWidth"/>
    /// of a national group counts national POSITIONS — only an enclosing ALPHANUMERIC group doubles them to the
    /// storage characters they occupy). A pure function of the children, exactly as the width members are.</summary>
    public PicInfo? AsIfPic => GroupUsage switch
    {
        GroupUsage.Bit => new PicInfo(PicCategory.Boolean, Usage.Bit, BitLayout.ExtentBits(this), Digits: 0, Scale: 0, Signed: false),
        GroupUsage.National => new PicInfo(PicCategory.National, Usage.National, ImageWidth, Digits: 0, Scale: 0, Signed: false),
        _ => null,
    };

    /// <summary>⛔ THE ONE READER for an item's OPERAND category (D20): its own PICTURE for an elementary item, the
    /// as-if picture for a bit / national group, null for an alphanumeric group. Every site that decides class,
    /// category, a Table 16 row, LENGTH positions, ref-mod admissibility, comparison class or string-operand-ness
    /// reads THIS — never <see cref="Pic"/> guarded by <see cref="IsGroup"/> — so a GROUP-USAGE group is a boolean /
    /// national operand everywhere at once. The STRUCTURAL predicates (<see cref="IsGroup"/>, <see cref="IsElementary"/>,
    /// <see cref="Children"/>) keep their meaning: the group is still laid out, imaged, initialized and walked as
    /// a group.</summary>
    public PicInfo? OperandPic => Pic ?? AsIfPic;

    /// <summary>A group that OPERATES as an elementary boolean / national item — GR1b/GR2b (a bit / national group);
    /// false for an alphanumeric group and for every elementary item. The MOVE classifier's, the emitter's and the
    /// comparison's "is this a group operation?" test is <c>IsGroup &amp;&amp; !IsAsIfElementary</c>.</summary>
    public bool IsAsIfElementary => Pic is null && GroupUsage is not GroupUsage.None;

    /// <summary>The DEFERRED adjudication mark of a PICTURE-less <c>USAGE NATIONAL</c>/<c>BIT</c> entry
    /// (ISO §13.18.60.4): whether it is a legal GROUP header (the usage sheds to subordinates, GR1) or an illegal
    /// picture-less elementary item (COBOLNET0881) is unknowable until the forest is complete —
    /// <c>DataBinder.MakeItem</c> writes it, <c>DataBinder.ResolveIndexItems</c> adjudicates and CLEARS it
    /// (P5.11c; <see cref="Pic"/> stays null meanwhile, never a sentinel shape).</summary>
    [DescriptionCopy(DescriptionCopyKind.Clause,
        "the deferred ISO §13.18.60.4 GR1 adjudication of the copied USAGE clause — it travels with the clause it defers")]
    public PicPending Pending { get; set; }

    /// <summary>This entry's OWN SIGN clause (ISO §13.18.52), or <see langword="null"/> when none — captured even on
    /// a group item: a group-level SIGN applies to every subordinate signed numeric DISPLAY item, nearest enclosing
    /// clause winning (GR1–3, applied by the binder's post-build inheritance pass). Settable (not init-only) for
    /// ONE additional writer: the <c>ExpandTypes</c> description copy (a TYPE / SAME AS subject assumes the
    /// template's / data-name-1's description, §13.18.58.4 GR3 / §13.18.49 GR1+GR5).</summary>
    [DescriptionCopy(DescriptionCopyKind.Clause,
        "SIGN (ISO §13.18.52) — in neither GR-1 exclusion list")]
    public SignSpec? OwnSign { get; set; }

    /// <summary>This entry's OWN USAGE keyword (ISO §13.18.60), or <see langword="null"/> when none — captured even
    /// on a group item: a group-level USAGE applies to every elementary item subordinate to it (GR1; NC107A's
    /// <c>01 U9 USAGE COMPUTATIONAL</c> makes the PICTURE-only U10 binary). Applied by the binder's post-build
    /// <c>InheritUsageClauses</c> pass. Settable for the <c>ExpandTypes</c> description copy (§13.18.58.4 GR3 /
    /// §13.18.49 GR1+GR3).</summary>
    [DescriptionCopy(DescriptionCopyKind.Clause,
        "USAGE (ISO §13.18.60) — in neither GR-1 exclusion list")]
    public Usage? OwnUsage { get; set; }

    /// <summary>This entry's OWN USAGE clause carried the <c>WITH NO SIGN</c> phrase (ISO §13.18.60.2 —
    /// <c>PACKED-DECIMAL [ WITH NO SIGN ]</c>), or <see langword="false"/> when it did not — captured even on a
    /// group item, for the same reason <see cref="OwnUsage"/> is: the phrase is PART OF the USAGE clause, and
    /// §13.18.60.4 GR1 applies that clause "only to each elementary item in the group".
    /// <para>⛔ THE FIELD EXISTS BECAUSE THE PHRASE USED TO BE A LOCAL. Entry bind read it off the entry's own
    /// <c>usageClause</c> and adjudicated it THERE, where a group header has no PICTURE — so all three arms
    /// fell through and the phrase was dropped WHOLESALE: <c>01 GRP USAGE PACKED-DECIMAL WITH NO SIGN.
    /// 05 EG PIC 9(4).</c> MEASURED 3 bytes (the SIGNED layout) against 2 for the elementary spelling, and
    /// <c>05 W1 PIC S9(4).</c> under the same header compiled clean where §13.18.40.3 SR31 forbids the 'S'
    /// (kb/Work PB570). Carrying the clause fact here lets the ONE §13.18.60.4 GR1 walk adjudicate BOTH
    /// spellings through one site (<c>DataBinder.ApplyEffectiveNoSign</c>).</para></summary>
    [DescriptionCopy(DescriptionCopyKind.Clause,
        "the WITH NO SIGN phrase of the USAGE clause (ISO §13.18.60.2) — it travels with the clause it is part of")]
    public bool OwnNoSign { get; set; }

    /// <summary>This entry's <see cref="Pic"/> was SYNTHESIZED FROM ITS USAGE, not analyzed from a PICTURE
    /// character-string — the entry wrote one of the picture-less usages (<see cref="UsageFamilies.IsPictureless"/>
    /// / ISO §13.16.3 SR8), whose representation the usage alone fixes.
    /// <para>⛔ The flag exists because §13.18.60.4 GR1 gives that representation to the ELEMENTARY ITEMS, "not to
    /// the group itself": entry bind cannot yet tell a picture-less elementary item from a group header (the
    /// subordinates are not parsed), so it synthesizes optimistically and <c>DataBinder.UsageInheritancePass</c>
    /// moves the profile DOWN to each picture-less leaf and sheds it from the header once the forest is complete.
    /// Before kb/Work PB495 that pass tested for the synthesized shapes by reference identity and by
    /// category/usage patterns — two hand-written lists that disagreed, so a group-level BINARY-SHORT or FLOAT-LONG
    /// left a ZERO-LENGTH leaf (or a group emitted as a scalar <c>float</c>). This is the fact itself, so no list
    /// can go stale against it.</para></summary>
    [DescriptionCopy(DescriptionCopyKind.Clause,
        "the provenance of the copied PICTURE (ISO §13.16.3 SR8) — travels with Pic")]
    public bool PicIsUsageSynthesized { get; set; }

    /// <summary>True when <see cref="Pic"/> is the PICTURE clause ISO §13.16.3 SR9 IMPLIES from the entry's VALUE
    /// literal (<c>DataBinder.SynthesizeImpliedPictures</c>) rather than one the source wrote. The provenance
    /// matters to exactly one family of rules: the §13.18.63.3 SR4/SR5/SR10 size sentences bound a VALUE literal
    /// by "the size indicated by an EXPLICIT PICTURE clause", so an implicit picture sets no such bound. It
    /// travels with the PICTURE through a TYPE copy — §13.18.57.4 GR3: "that implicit PICTURE clause becomes part
    /// of the description of the subject of the entry", still implicit — so a TYPE subject's own VALUE over a
    /// VALUE-implied template is screened for its CLASS but not for a size no explicit clause indicated
    /// (kb/Work PB1300).</summary>
    [DescriptionCopy(DescriptionCopyKind.Clause,
        "the provenance of the copied PICTURE (ISO §13.16.3 SR9; §13.18.57.4 GR3 — the implicit PICTURE becomes part of the subject's description) — travels with Pic")]
    public bool PicIsValueImplied { get; set; }

    /// <summary>The PICTURE character-string as WRITTEN (repetition factors already expanded from any integer
    /// constant-name, §13.10.3 SR2), or <see langword="null"/> for an entry with no PICTURE clause. Kept because
    /// a rule about the picture can be violated LATER than entry bind: §13.18.60.4 GR1 gives an elementary item
    /// the usage its group wrote, and the §13.18.60.3 SR3/SR5/SR12/SR20 screen that then applies has to name the
    /// offending picture in its message exactly as the written-clause spelling does. Travels with a TYPE /
    /// SAME AS description copy, like <see cref="Pic"/> itself.</summary>
    [DescriptionCopy(DescriptionCopyKind.Clause,
        "the written PICTURE character-string — travels with Pic; the ISO §13.18.60.3 screens name it")]
    public string? PictureText { get; set; }

    /// <summary>The raw VALUE operand text (e.g. <c>"ABC"</c> or <c>-12.5</c>), or <see langword="null"/> if none.
    /// Settable for the <c>ExpandTypes</c> description copy (a subject's OWN VALUE wins, §13.18.57.4 GR3;
    /// otherwise the copied description's VALUE applies, §13.18.49 GR1 — VALUE is not in the exclusion list).</summary>
    [DescriptionCopy(DescriptionCopyKind.EntryOnly,
        "VALUE format 1 (ISO §13.18.63) — in neither GR-1 exclusion list; §13.18.57.4 GR3 gives the SUBJECT's own VALUE precedence, hence the ??=; a compiler temporary is stored before it is read, so it takes none (kb/Work PB888)")]
    public string? RawValue { get; set; }

    /// <summary>The Format 2 (table) VALUE phrases (ISO §13.18.63.2, COBOL-2002): each keys a literal list to an
    /// occurrence range via FROM (subscript-1 …) [TO (subscript-2 …)]. Mutually exclusive with <see cref="RawValue"/>
    /// (the grammar's two arms cannot both match). Null unless the entry carries a table VALUE. Resolved to the
    /// per-occurrence emitter map after the forest is built (dimensions / dynamic expected capacity are known then).</summary>
    [DescriptionCopy(DescriptionCopyKind.EntryOnly,
        "VALUE format 2 (ISO §13.18.63.2) — the same clause in its other spelling (kb/Work PB505); its FROM subscripts address an OCCURS a compiler temporary's root never reproduces (kb/Work PB888)")]
    public IReadOnlyList<TableValueSpec>? TableValues { get; set; }

    /// <summary>The RESOLVED Format 2 (table) VALUE — the §13.18.63.4 GR12–GR15 subscript-tuple → literal map plus
    /// the OCCURS chain it is keyed by (§13.18.63.3 SR20's order). Set by the <c>DataBinder.ResolveTableValues</c>
    /// pass, which needs the COMPLETE forest: an entry's dimensions include every OCCURS clause SUPERORDINATE to
    /// it (SR18/SR20), and <see cref="Parent"/> is still null while the entry itself binds. Null when the entry
    /// carries no table VALUE or its clause was rejected.</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "resolved post-build by DataBinder.ResolveTableValues, which needs the COMPLETE forest (ISO §13.18.63.3 SR18/SR20 dimensions), in the copy's OWN scope")]
    public TableValuePlan? TableValuePlan { get; set; }

    /// <summary>True for this item OR ANY ITEM IN ITS SUBTREE carrying a resolved <see cref="TableValuePlan"/> —
    /// set by the same pass, on every ancestor of a plan-carrying entry. ⛔ It is the emitters' FAST PATH GUARD,
    /// not a semantic predicate: without a table VALUE below it, an OCCURS entry's occurrences are all identical,
    /// so one element initializer is composed ONCE and repeated (what both lanes always did). With one, each
    /// occurrence is composed against its own subscript tuple.</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "the same pass's emitter fast-path guard, set on every ancestor of a plan-carrying entry in the copy's own scope")]
    public bool ContainsTableValue { get; set; }

    /// <summary>§13.18.63.4 GR16's INITIAL CAPACITY for THIS dynamic-capacity table, computed from every Format-2
    /// VALUE clause that applies to it ("If more than one VALUE clause applies, the maximum value thus calculated
    /// becomes the initial capacity" — this property IS that maximum, accumulated by the resolve pass). Null when
    /// no table VALUE applies, in which case §8.5.1.9.1's FROM (minimum) opens the table.</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "the same pass's ISO §13.18.63.4 GR16 maximum, computed in the copy's own scope")]
    public int? TableValueInitialCapacity { get; set; }

    /// <summary>⛔ THE ONE READER FOR "what literal initializes this item at this occurrence" — the Format-1
    /// <see cref="RawValue"/>, which is the same for every occurrence (§13.18.63.4 GR9), or the Format-2 literal
    /// keyed to <paramref name="subs"/> (GR12–GR15). It lives HERE because the two emit lanes ask it and may not
    /// answer differently: <see cref="CobolNet.CodeGen.ValueInitializer.InitializerFor"/> (the record-struct
    /// fields) and <see cref="CobolNet.CodeGen.GroupImageCodec.ImageInitOf"/> (the character-image backings).
    /// A GROUP subject's answer is its group-level VALUE either way — §13.18.63.3 SR16 carries SR13 onto format 2,
    /// and §13.18.63.4 GR5 initializes the AREA — so <c>GroupValueSlicer.AreaOf</c> reads this too.</summary>
    public string? ValueAt(Subscripts subs) => TableValuePlan is { } plan ? plan.LiteralAt(subs) : RawValue;

    /// <summary>True when this entry's <see cref="RawValue"/> was TRANSPLANTED from another entry's data
    /// description rather than written in this entry's own — a TYPE template's VALUE assumed by its reference
    /// site (ISO §13.18.57.4 GR1; GR3 gives the subject's OWN VALUE precedence, hence <c>??=</c>), a SAME AS
    /// target's assumed by its subject (§13.18.49 GR1), or a cloned template subtree's member VALUE.
    ///
    /// <para>⛔ THE PROVENANCE THE SYNTAX-RULE SCREENS NEED. A rule whose subject is "an entry that specifies a
    /// VALUE clause" (§13.18.63.3 SR13/SR14) speaks about the entry the PROGRAMMER WROTE: the template is
    /// screened once where it is declared, and re-screening each composed copy reports the identical source
    /// entry once per reference site. Measured: `01 A VALUE "ABCD". 05 X PIC 9(4) COMP. 01 B SAME AS A.` emitted
    /// COBOLNET1702 TWICE, both anchored at X's one declaration. Without this flag the fact is unrecoverable at
    /// screen time — the merge has already happened and the text is indistinguishable.</para></summary>
    [DescriptionCopy(DescriptionCopyKind.CopyWritten,
        "the copy itself WRITES it — the ISO §13.18.63.3 SR13/SR14 screens' subject is the entry that WROTE the VALUE, never a composed copy")]
    public bool ValueIsCopied { get; set; }

    /// <summary>True when this entry carries a TYPEDEF clause — it is a TYPE DECLARATION (a named template; ISO
    /// §13.18.58, data-model D17), allocating NO storage. Registered in <c>DataBinder.TypeDecls</c>, kept OFF
    /// <c>Roots</c>/<c>ByName</c>; its subordinate names are not globally referenceable (GR1).</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "ISO §13.18.57.4 GR1 excludes the TYPEDEF clause by name, and §13.18.58.3 SR1 confines it to level 1 so no subordinate carries it")]
    public bool IsTypedef { get; init; }

    /// <summary>True when the TYPEDEF carries STRONG (ISO §13.18.58.2) — the declared type is strongly typed, so its
    /// referencing items may interoperate only with the same type (the compile-time §8.5.3.3 checks).</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "the STRONG phrase OF the TYPEDEF clause (ISO §13.18.58.2) — excluded with it; a subject's carried strength is StrongType")]
    public bool TypedefStrong { get; init; }

    /// <summary>True when this TYPEDEF entry also carries the EXTERNAL clause (ISO §13.18.22 SR1 — a level-1
    /// type declaration may be external; §13.18.58.3 SR3). The type declaration itself has no storage
    /// (§13.18.58.4 GR2); the effect is on its REFERENCES: any record description containing that type is itself
    /// EXTERNAL (§13.18.22.4 GR3) and must be level-1 (GR2) — <c>ExpandType</c> marks the subject
    /// <see cref="ExternalFromType"/>, and <c>CallBindExternalAndGlobal</c> re-bases it onto the run-unit
    /// <c>ExternalStore</c> cell like any explicitly-EXTERNAL record.</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "a fact of the type DECLARATION's own entry; its effect on a referencing subject is ExternalFromType (ISO §13.18.22.4 GR3)")]
    public bool IsExternalTypedef { get; init; }

    /// <summary>True when THIS entry carries an EXTERNAL clause (ISO §13.18.22) that the clause-placement screen
    /// ADMITTED (<c>DataBinder.ScreenClausePlacement</c> — §13.16.3 SR5/SR7, §13.18.22.3 SR1). ⛔ It is the ONE
    /// input of the run-unit re-basing (<c>CallBindExternalAndGlobal</c> reads it, never the parse tree — kb/Work
    /// PB519: the parse-tree scan re-based an EXTERNAL REDEFINES entry's anchor onto the external cell), and it
    /// also backs the §13.18.22 SR5 strong-external pairing check and the double-registration guard for
    /// <see cref="ExternalFromType"/>. A refused clause leaves it false, so an already-reported entry binds as
    /// ordinary storage.</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "ISO §13.18.49.4 GR1 excludes EXTERNAL by name; for a TYPE subject the carrier is §13.18.22.4 GR3 (ExternalFromType), because the effect is on the RECORD rather than a copied clause")]
    public bool HasExternalClause { get; init; }

    /// <summary>True when THIS entry carries a GLOBAL clause (ISO §13.18.27) that the clause-placement screen
    /// ADMITTED (§13.16.3 SR6/SR7, §13.18.27.3 SR1 b) — level 1, data-name format, in the file, working-storage,
    /// local-storage or linkage section). The ONE input of the global-name registration
    /// (<c>CallBindExternalAndGlobal</c> → <c>CallGlobalRoots</c>) — kb/Work PB518: the parse-tree scan it
    /// replaces skipped a misplaced clause with a <c>continue</c> and never read the FILE SECTION at all.</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "ISO §13.18.49.4 GR1 and §13.18.57.4 GR1 both exclude GLOBAL by name — a SAME AS / TYPE subject never inherits it")]
    public bool HasGlobalClause { get; init; }

    /// <summary>The value of the EXTERNAL clause's <c>AS literal-1</c>, when the entry writes one; null when it
    /// does not (kb/Work PB511). ISO §13.18.22.4 GR5: <i>"Literal-1, if specified, is the name of the file
    /// connector or record that is externalized to the operating environment. If literal-1 is not specified,
    /// the externalized name of the file connector or record is the name specified in the file description
    /// entry or the data-name format of the entry-name clause, respectively."</i> So this field IS the rule's
    /// first sentence and the null case IS its second — the default is applied at the ONE cell-keying site
    /// (<c>CallBindExternalAndGlobal</c> → <c>CallMakeExternal</c>), never re-derived per caller. The literal
    /// is NOT case-folded: a COBOL word is case-insensitive and is uppercased for the key, but a literal is
    /// its own character-string and §8.3.2.2 2) leaves the operating-environment mapping to the implementor,
    /// which this compiler defines as the literal verbatim.</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "a fact of THIS entry's own EXTERNAL clause (ISO §13.18.22.4 GR5), exactly as HasExternalClause is")]
    public string? ExternalizedAs { get; init; }

    /// <summary>True when this record became EXTERNAL by referencing an EXTERNAL type declaration
    /// (ISO §13.18.22.4 GR3 — "the record descriptions in which it is specified are also external"). Set by
    /// <c>ExpandType</c>; consumed by <c>CallBindExternalAndGlobal</c> (which cannot see it in the parse tree —
    /// the record's own entry carries no EXTERNAL clause).</summary>
    [DescriptionCopy(DescriptionCopyKind.CopyWritten,
        "ExpandType / ExpandSameAs write it under ISO §13.18.22.4 GR3 with the GR2 level-1 check — a derived effect, never a verbatim copy")]
    public bool ExternalFromType { get; set; }

    /// <summary>True when this level-01 entry carries a CONSTANT RECORD clause (ISO §13.18.15) — a STRUCTURED
    /// CONSTANT: its content is that of an INITIALIZE … WITH FILLER ALL TO VALUE THEN TO DEFAULT (§13.18.15.4 GR1),
    /// which is NOT the ordinary initial state — a VALUE-less numeric-edited item holds its edited zero, no OPTIONS
    /// INITIALIZE background reaches it and a group-level VALUE sends nothing (§14.9.20.4 GR5 c), GR6) — so the
    /// emitters seed a constant record by <c>SeedRecipe.Initialize</c> (<c>ValueInitializer.RecipeFor</c>, kb/Work
    /// PB1233), and neither it nor any subordinate may be a receiving operand
    /// (§13.18.15.3 SR2 → COBOLNET1548 at the receiving chokepoints; <c>DataBinder.IsConstantRecordItem</c>
    /// walks ancestors). Set only on the root; subordinates are covered by the ancestor walk.</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "ISO §13.18.49.4 GR1 excludes CONSTANT RECORD by name (§13.18.49.3 SR10 also forbids it on data-name-1), and §13.18.15.3 SR1 confines it to level 1 so no subordinate carries it")]
    public bool IsConstantRecord { get; init; }

    /// <summary>The type-name of a <c>TYPE IS type-name</c> reference (ISO §13.18.57), or null. The referencing entry
    /// is CLONED from that type declaration's subtree by the post-build <c>DataBinder.ExpandTypes</c> pass (D17), which
    /// clears this once expanded.</summary>
    [DescriptionCopy(DescriptionCopyKind.MemberOnly,
        "a nested TYPE reference inside a template re-expands PER CLONE (ISO §13.18.57); an ENTRY copy's source is already expanded")]
    public string? TypeRefName { get; set; }

    /// <summary>The <c>SAME AS data-name-1</c> target name (ISO §13.18.49), or null. Structurally the TYPE
    /// reference with a DATA-NAME source: <c>DataBinder.ExpandSameAs</c> (inside the ONE <c>ExpandTypes</c> pass)
    /// resolves the target entry and clones its description in via the SAME <c>CloneItem</c> machinery (GR1/GR2),
    /// then clears this. <see cref="SameAsQualifiers"/> carries any OF/IN qualifiers of the reference.</summary>
    [DescriptionCopy(DescriptionCopyKind.MemberOnly,
        "a nested SAME AS inside a copied description re-expands per clone (ISO §13.18.49.4 GR1)")]
    public string? SameAsName { get; set; }

    /// <summary>The OF/IN qualifier names of a <see cref="SameAsName"/> reference, in written order (empty when
    /// unqualified). Each must name an ancestor of the target for the reference to match.</summary>
    [DescriptionCopy(DescriptionCopyKind.MemberOnly,
        "the OF/IN qualifiers of the SameAsName it rides")]
    public List<string> SameAsQualifiers { get; } = [];

    /// <summary>After <c>ExpandTypes</c>: the type-name this item (or its containing subtree root) was cloned from —
    /// backs the §8.5.3.3 STRONG same-type check. Null for a non-typed item.</summary>
    [DescriptionCopy(DescriptionCopyKind.Clause,
        "the carried type identity the ISO §8.5.3 same-type test reads — a subject declared with TYPE keeps it through a further copy")]
    public string? TypeName { get; set; }

    /// <summary>After <c>ExpandTypes</c>: true when this item is the subject of a TYPE clause referencing a STRONG
    /// type declaration (an item is strongly typed if it or any ancestor has this set). Drives the §8.8.4 gates.</summary>
    [DescriptionCopy(DescriptionCopyKind.Clause,
        "the same carried identity's strength; ISO §13.18.57.3 SR6 re-checks placement at the new site")]
    public bool StrongType { get; set; }

    // The strong-typing overlay (StrongRoot / IsStrongGroup / IsStronglyTyped / TypeAnchor / SameType /
    // EquivalentTypeDeclarations) lives in Model/StrongTypeModel.cs (P5.11b, DESIGN-data-model §2.4) — DataItem keeps
    // only the stored facts (StrongType, TypeName) that ExpandTypes writes.

    /// <summary>The ALLOCATED occurrence count — the table's physical capacity — or <see langword="null"/> if the
    /// item is not a table. For a fixed (Format 1) table this is the OCCURS count; for an occurs-depending (Format 2)
    /// table it is the MAXIMUM, integer-2 (ISO §8.5.1.8 — "the physical capacity is fixed at compile time; the
    /// logical capacity may vary"). The variable current count lives in <see cref="OccursSpec"/>.</summary>
    [DescriptionCopy(DescriptionCopyKind.MemberOnly,
        "ISO §13.18.49.3 SR5 forbids OCCURS on data-name-1 and §13.16.3 SR12/SR14 make a SUBJECT's own OCCURS the array-of-description form; a cloned SUBORDINATE's OCCURS is part of the type (§13.18.58.4 GR1)")]
    public int? Occurs { get; init; }

    /// <summary>The structured OCCURS DEPENDING ON / KEY description (ISO §13.18.38 Format 2 + GR3), or
    /// <see langword="null"/> for a non-table or a plain keyless fixed table (which <see cref="Occurs"/> alone
    /// describes). Carries the integer-1..integer-2 bounds, the resolved DEPENDING ON data-name-1, and the
    /// ASCENDING/DESCENDING KEY data-names.</summary>
    [DescriptionCopy(DescriptionCopyKind.MemberOnly,
        "the same clause's structured form — CLONED, never shared: Depending / CapacityRegister resolve per-clone")]
    public OccursSpec? OccursSpec { get; init; }

    /// <summary>True for a Format-4 DYNAMIC-capacity table (ISO §13.18.38, data-model D9): capacity varies at run
    /// time; storage is the out-of-line <c>CobolDynTable&lt;T&gt;</c>, and <see cref="Occurs"/> (the fixed physical
    /// capacity) is null.</summary>
    public bool IsDynamicTable => OccursSpec is { IsDynamic: true };

    /// <summary>True for ANY table — fixed (<see cref="Occurs"/>) OR dynamic (D9). Use at table-RECOGNITION sites
    /// (subscript arity, SEARCH detection); keep <c>Occurs is not null</c> at fixed-capacity-ARITHMETIC sites
    /// (static image width, fixed-array init) where a dynamic table must NOT be treated as a fixed run.</summary>
    public bool IsTable => Occurs is not null || IsDynamicTable;

    /// <summary>⛔ §8.4.2.3.3 SR2'S ADMISSION TEST, AND NOT <see cref="IsTable"/> (kb/Work PB877). SR2: "If a
    /// subscript is specified, the data description entry describing qualified-data-name-1 or the conditional
    /// variable associated with qualified-condition-name-1 shall contain an OCCURS clause <b>or shall be
    /// subordinate to a data description entry that contains an OCCURS clause</b>." <see cref="IsTable"/> answers
    /// the FIRST half only — "does THIS entry carry an OCCURS clause" — so it is the right question at a
    /// table-RECOGNITION site (is this the table SEARCH iterates) and the wrong one at every
    /// REFERENCE-SHAPE site, where the subject is a reference to a table ELEMENT and every level of the
    /// element's hierarchy counts.
    /// <para>The half that was missing had a measurable cost: <c>ReferenceResolver.SplitSubscriptTokens</c>'s
    /// declaration-informed <c>'('</c> rule asked <c>IsTable</c>, so <c>X</c> — <c>PIC 9</c> under
    /// <c>02 G2 OCCURS 3</c> — answered "cannot be subscripted", its own <c>'('</c> opened a NEW subscript, and
    /// <c>Y (X(1))</c> was read as TWO subscripts on a one-dimensional reference. §8.4.2.3.2's <c>subscript</c>
    /// diagram admits <c>arithmetic-expression-1</c>, §8.8.1.1 admits "an identifier referencing a numeric data
    /// item" as one, and §8.4.3.1.2 Format 2 makes that identifier a qualified-data-name-WITH-SUBSCRIPTS — so
    /// the shape is legal, and it was rejected on the receiving side (COBOLNET0899) and aborted at run time on
    /// the sending side.</para></summary>
    public bool IsTableElement => SubscriptArity > 0;

    /// <summary>§8.4.2.3.3 SR3's COUNT — "when a reference is made to a table element, the number of subscripts
    /// shall equal the number of OCCURS clauses in the description of the table element being referenced". THE
    /// one arity answer: every site that compares a written subscript list's length to the item's dimensions
    /// reads this rather than re-walking the ancestor chain. Counts ANY table level, fixed or dynamic
    /// (<see cref="IsTable"/>) — a dynamic-capacity table IS a dimension though its <see cref="Occurs"/> is
    /// null (D9).
    /// <para>⚠ Deliberately a COUNT and not <see cref="SubscriptLevels"/>: the arity comparison is on the
    /// reference-resolution hot path and allocates nothing. Callers that need the levels THEMSELVES — the
    /// §8.4.2.3.3 SR3 "order of successively less inclusive dimensions" half — take the list instead.</para></summary>
    public int SubscriptArity
    {
        get
        {
            int n = 0;
            for (DataItem? x = this; x is not null; x = x.Parent) if (x.IsTable) n++;
            return n;
        }
    }

    /// <summary>§8.4.2.3.3 SR3's ORDER — the table levels in this item's description, OUTERMOST FIRST: "the
    /// subscripts are written in the order of successively less inclusive dimensions of the table", so element
    /// <i>k</i> of this list is the level the <i>k</i>-th written subscript selects. The list form of
    /// <see cref="SubscriptArity"/>: the same walk over the same predicate, so
    /// <c>SubscriptLevels().Count == SubscriptArity</c> holds by construction — and it keeps holding because
    /// <c>SubscriptAdmissionDriftTests.TheSr3ArityWalk_IsWrittenExactlyOnce</c> fails if the walk is written
    /// anywhere but this file, which is how the five copies that existed before kb/Work PB877 got here.</summary>
    public List<DataItem> SubscriptLevels()
    {
        var levels = new List<DataItem>();
        for (DataItem? x = this; x is not null; x = x.Parent) if (x.IsTable) levels.Add(x);
        levels.Reverse();   // outermost first — the order a subscript list is written in
        return levels;
    }

    /// <summary>The INDEXED BY index-names declared on this item's OCCURS clause (empty if none).</summary>
    [DescriptionCopy(DescriptionCopyKind.MemberOnly,
        "the INDEXED BY phrase of that OCCURS clause (ISO §13.18.38), riding it")]
    public List<string> IndexNames { get; } = [];

    /// <summary>The index-name DECLARATIONS this item's INDEXED BY phrase makes — one per <see cref="IndexNames"/>
    /// entry, each with its own cell (kb/Work PB919), minted when the item is registered in its scope
    /// (<c>DataBinder.RegisterIndexes</c>). Empty for an unregistered TYPEDEF template.</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "identity: each copy's INDEXED BY names are re-declared with their OWN cells when the copy is registered")]
    public List<IndexDeclaration> Indexes { get; } = [];

    /// <summary>
    /// True when this numeric fixed-point elementary item is stored as its CHARACTER IMAGE (a C# <see cref="string"/>
    /// of zoned digits) rather than a native <see cref="long"/> — a numeric-DISPLAY leaf under a group used as a
    /// whole operand (ISO/IEC 1989:2023 §14.9.25.4 MOVE GR4 fills such a group "without consideration for the individual
    /// elementary items", so the leaf may receive non-numeric characters a native <c>long</c> cannot represent), a
    /// Tier-B REDEFINES window, a figurative-fill / ref-mod-store receiver, a FILE-record or report print face, or
    /// an OO crossing-form flip. Numeric <i>use</i> then goes through <c>CobolNum.ParseDisplay</c> (read) /
    /// <c>CobolNum.FormatDisplay</c> (write); the common case stays a native <c>long</c> (locked invariant #2).
    /// <para>P5.7: a READ-ONLY projection of <see cref="Storage"/> — the mutable flag and its 9 cross-layer write
    /// sites are deleted; <c>StorageFormPass</c> computes the decision ONCE from the collected facts. NULL-Storage
    /// (i.e. pre-group-tail) reads answer <c>false</c>, exactly the flag's early value for every legal read (the
    /// bind-time early consumers use <see cref="DataBinder.IsImageBackedEarly"/> instead).</para>
    /// </summary>
    public bool StoreAsImage => Storage is Model.StorageForm.CharImage { Category: PicCategory.Numeric };

    /// <summary>JUSTIFIED [RIGHT] (ISO §13.18.32): alphanumeric/alphabetic receives right-justify — space-fill on
    /// the LEFT when the sender is shorter, truncate from the LEFT when longer (§14.9.25.4 GR6c). Settable for
    /// the <c>ExpandTypes</c> description copy (§13.18.58.4 GR3 / §13.18.49 GR1).</summary>
    [DescriptionCopy(DescriptionCopyKind.Clause,
        "JUSTIFIED (ISO §13.18.32) — in neither GR-1 exclusion list")]
    public bool Justified { get; set; }

    /// <summary>BLANK [WHEN] ZERO (ISO §13.18.8): storing a ZERO value fills the item with spaces — applied at
    /// every numeric-edited store (MOVE editing and arithmetic resultants alike). Settable for the
    /// <c>ExpandTypes</c> description copy (§13.18.58.4 GR3 / §13.18.49 GR1).</summary>
    [DescriptionCopy(DescriptionCopyKind.Clause,
        "BLANK WHEN ZERO (ISO §13.18.8) — in neither GR-1 exclusion list")]
    public bool BlankWhenZero { get; set; }

    /// <summary>The entry carried a SYNCHRONIZED / SYNC clause (ISO §13.18.55). A no-op in the typed-native model
    /// (no byte alignment), but recorded so the edition validator can gate SYNCHRONIZED on a GROUP item — a
    /// COBOL-2023 introduction (Annex E.3.2 item 6) — below 2023 (P3 step 10). Not emitted. Settable for the
    /// <c>ExpandTypes</c> description copy (§13.18.49 GR1 — SYNCHRONIZED is not in the exclusion list).</summary>
    [DescriptionCopy(DescriptionCopyKind.Alignment,
        "SYNCHRONIZED (ISO §13.18.55) IS 'alignment': §13.18.57.4 GR1 excludes it from a TYPE subject (GR2d re-aligns that subject 'as though it were a level 1 item'); §13.18.49.4 GR1 does not exclude it")]
    public bool Synchronized { get; set; }

    /// <summary>Subordinate items (group members). Empty for an elementary item.</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "the hierarchy a copier REBUILDS itself by cloning each child (ISO §13.18.49.4 GR2a)")]
    public List<DataItem> Children { get; } = [];

    /// <summary>The level-88 condition-names whose conditional variable is THIS item (ISO §13.16.3 SR24). Normally these
    /// also live in <c>DataBinder.Conditions</c> (the global by-name index), but a TYPEDEF template keeps them ONLY
    /// here — its condition-names are not globally referenceable (§13.18.58.4 GR1) until a <c>TYPE</c> reference
    /// clones the item, at which point the clone's copies ARE registered globally (data-model D17 inc 3).</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "rebuilt by CloneConditionOnto — a clone's condition-names ARE globally registered where a template's are not (ISO §13.18.58.4 GR1)")]
    public List<Condition88> Own88s { get; } = [];

    /// <summary>Where this item's data description entry begins — the diagnostic cursor captured when it was bound
    /// (kb/Work PB82), so a POST-BUILD pass (REDEFINES / RENAMES resolution, ODO, ANY LENGTH, TYPE expansion, …)
    /// can position its diagnostics at the entry: <c>using var _ = Edition.At(item);</c>. Default (unset) for a
    /// synthetic item (a debug register, a subscript-segment temp); a TYPE / SAME AS clone carries its template's.</summary>
    [DescriptionCopy(DescriptionCopyKind.MemberOnly,
        "the diagnostic cursor: a SUBJECT keeps its own entry's, a cloned subordinate carries its template member's")]
    public CobolNet.Editions.DiagnosticCursor DeclaredAt { get; init; }

    /// <summary>The raw REDEFINES target data-name as written (ISO §13.18.44), resolved post-build; null if none.
    /// Settable so an UNRESOLVED redefiner (kb/Work PB93 — diagnosed COBOLNET1654) is demoted to an ordinary entry:
    /// the layout consumers key on this name, and a name without a target was the half-state.</summary>
    [DescriptionCopy(DescriptionCopyKind.MemberOnly,
        "ISO §13.18.49.4 GR1 excludes REDEFINES from a subject; a cloned subordinate's REDEFINES is part of the type (§13.18.58.4 GR1)")]
    public string? RedefinesTargetName { get; set; }

    /// <summary>The resolved REDEFINES target item (the immediately-redefined entry, which may itself be a
    /// redefiner — SR11). Set by the post-build pass; null for a non-redefining entry.</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "resolved post-build by the REDEFINES pass, in the clone's OWN scope")]
    public DataItem? RedefinesTarget { get; private set; }

    /// <summary>Which construct makes this item share <see cref="RedefinesTarget"/>'s storage — a written REDEFINES
    /// clause or one of the two implicit redefinitions of the file section (kb/Work PB836). A rule about the
    /// REDEFINES CLAUSE screens only <see cref="RedefinitionKind.Clause"/>; the storage layout reads the target
    /// whatever the kind. Written ONLY through <see cref="SetRedefinition"/>, together with the target, so the two
    /// facts cannot disagree.</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "set with RedefinesTarget by the REDEFINES pass / the file-section binder, in the item's OWN scope")]
    public RedefinitionKind RedefinesKind { get; private set; }

    /// <summary>Record that this item shares <paramref name="target"/>'s storage area because of
    /// <paramref name="kind"/> — the ONE writer of <see cref="RedefinesTarget"/> and <see cref="RedefinesKind"/>.
    /// A null target clears both (an unresolved REDEFINES clause, kb/Work PB93).</summary>
    public void SetRedefinition(DataItem? target, RedefinitionKind kind)
    {
        RedefinesTarget = target;
        RedefinesKind = target is null ? RedefinitionKind.None : kind;
    }

    /// <summary>True when the item shares its storage area by one of the file section's IMPLICIT redefinitions
    /// (§13.18.33.4 GR3 / §12.4.6.4.4 GR2) rather than by a written REDEFINES clause.</summary>
    public bool RedefinesImplicitly => RedefinesKind is RedefinitionKind.ImplicitFileRecord or RedefinitionKind.SameRecordArea;

    /// <summary>The level-66 RENAMES descriptor (ISO §13.18.45), or null unless this is a level-66 entry.</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "a level-66 entry is not a subordinate (it rides Renames66) and is part of no entry description")]
    public RenamesInfo? Renames { get; set; }

    /// <summary>The redefines class (shared-storage equivalence class) this item belongs to, or null if it stands
    /// alone. Every member of a class — the original + every redefiner — points to the SAME instance.</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "the redefines equivalence class, computed post-build by RedefinesClassifier")]
    public RedefinesClass? Class { get; set; }

    /// <summary>True for the ONE stored member of a redefines class (the non-redefining anchor — SR7); every other
    /// member is a computed view. Defaults true so a standalone item (the whole existing corpus) emits normally.</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "the same classifier's stored-member verdict")]
    public bool IsCanonical { get; set; } = true;

    /// <summary>The reference-time *-ARG-OMITTED fact when this item is a FORMAL PARAMETER of its source element
    /// and the compilation group can enable the kind's condition (kb/Work PB971 — ISO §14.9.4.4 GR12, §8.4.3.2.4
    /// GR8, §14.9.23.4 GR10). Every access path rooted here carries it; null otherwise.</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "a fact of the formal-parameter ROLE (the PROCEDURE DIVISION USING operand), not of the data description")]
    public OmittedFormalGuard? OmittedGuard { get; set; }

    /// <summary>True for a BASED 01/77 entry (ISO §13.18.5 — a storage TEMPLATE with an implicit data-address
    /// pointer, initially NULL; no storage of its own until SET ADDRESS OF / ALLOCATE gives it one). The
    /// post-build pass routes every reference through the pointer (Phase-4b increment 2). Written by
    /// <c>BindEntry</c> for a written clause and by the one description copy for a TYPE / SAME AS subject whose
    /// type-name-1 / data-name-1 is based; a composed BASED is placement-screened by
    /// <c>DataBinder.ScreenComposedBased</c> (§13.16.3 SR5/SR13/SR16).</summary>
    [DescriptionCopy(DescriptionCopyKind.EntryOnly,
        "BASED (§13.18.5) is in NEITHER GR-1 exclusion list (§13.18.57.4 GR1, §13.18.49.4 GR1; §13.18.58.4 GR3 'All other data description clauses … are assumed by data defined using the type-name'), so a TYPE / SAME AS subject becomes based; §13.18.57.4 GR4 only settles WHICH clause applies when the subject writes its own, and both mean 'based' (kb/Work PB1300). Not onto a compiler temporary: it is storage the pre-op creates and stores, never a template awaiting an address")]
    public bool IsBased { get; set; }

    /// <summary>ALIGNED (ISO §13.18.1; kb/Work PB487) — §13.18.1.4 GR1: "An ALIGNED clause causes the subject of
    /// the entry to be aligned on the first bit of the first available byte boundary. Implicit filler bits may be
    /// generated to complete the assignment of bits as described in 8.5.1.6.3". Set by <c>DataBinder.BindEntry</c>
    /// only after §13.18.1.3 SR1 passes (a bit group item or an elementary bit data item — COBOLNET1942), and
    /// CLEARED on a violation so the item lays out by the default §8.5.1.6.3 rules under an already-failed
    /// compile (the <see cref="IsBased"/> discipline).
    /// <para>Read at exactly ONE site — <see cref="BitLayout.SharesByteWith"/>, the placement predicate both the
    /// extent walk and the offset walk call. GR2 ("applies to each occurrence") is honoured there too, by
    /// rounding the per-occurrence stride up to a byte.</para></summary>
    [DescriptionCopy(DescriptionCopyKind.Alignment,
        "ALIGNED (ISO §13.18.1) is the OTHER alignment clause — the same §13.18.57.4 GR1 'alignment' exclusion as SYNCHRONIZED, and §13.18.49.4 GR1 excludes neither. §13.18.1.3 SR1 admits it on a bit group or an elementary bit item, so a TYPEDEF member can carry it")]
    public bool IsAligned { get; set; }

    /// <summary>The subject of a TYPE clause whose type-name describes a GROUP item — ISO §13.18.57.4 GR2 d): "the
    /// subject of the entry is aligned as though it were a level 1 item" (kb/Work PB1569). Set by
    /// <c>DataBinder.ExpandType</c>, the one place the TYPE clause takes effect.
    /// <para>The only placement the standard gives a level-1 item that a subordinate does not have is bit alignment:
    /// §8.5.1.6.3 — "a level 1 bit group are at the first bit of a byte", where a same-level successor of a bit item
    /// would otherwise go "at the next bit position in storage". Every other item already starts on a byte, and
    /// WiseOwl COBOL inserts no word-boundary slack for any level (§8.5.1.6.4 leaves that to the runtime module), so
    /// the flag is read at exactly ONE site — <see cref="BitLayout.SharesByteWith"/>, the placement predicate the
    /// extent walk, the offset walk, the REDEFINES class walk and the image RUNS all derive from. It aligns the
    /// SUBJECT's first bit, not each occurrence: ALIGNED's own rule for a multiple-occurrence item (§13.18.1.4
    /// GR2) has no counterpart in §13.18.57.4 GR2 d), so an array-of-type keeps §8.5.1.6.3's successive-bit
    /// stride.</para>
    /// <para>Classified <see cref="DescriptionCopyKind.Alignment"/>: it is the effect of the TYPE clause in
    /// data-name-1's description, so a SAME AS copy (§13.18.49.4 GR1 — "as though the data description identified
    /// by data-name-1 had been coded in place") and a reproduced TYPEDEF member carry it; a TYPE subject does not
    /// inherit its template's (GR1 excludes alignment) and is given its own by GR2 d) instead.</para></summary>
    [DescriptionCopy(DescriptionCopyKind.Alignment,
        "ISO §13.18.57.4 GR2 d) — the TYPE clause's level-1 alignment of a group-typed subject; SAME AS (§13.18.49.4 GR1) copies data-name-1's description, TYPE clause included, and GR1 of the TYPE clause excludes the template's alignment")]
    public bool AlignedAsLevelOne { get; set; }

    /// <summary>The PROPERTY clause as written on this entry (ISO §13.18.42), or null (kb/Work PB1273). Set by
    /// <c>DataBinder.BindEntry</c>, which also asks §13.18.42.3 SR1's placement; the clause's other syntax rules are
    /// asked of THIS item — never of a name lookup — by the clause-placement table (SR3's elementary half, §13.16.3
    /// SR21) and <c>DataBinder.OoBindPropertyClauses</c> (SR2, SR3's qualification half, SR4, SR5, SR6), which then
    /// synthesizes the accessors (§13.18.42.4 GR1/GR2).</summary>
    [DescriptionCopy(DescriptionCopyKind.EntryOnly,
        "PROPERTY (ISO §13.18.42) is in neither GR-1 exclusion list (§13.18.57.4 GR1, §13.18.49.4 GR1), so a TYPE / SAME AS subject or a reproduced TYPEDEF member written in an object's working storage defines the property its own entry would; §13.18.42.3 then judges the copy at its own site. Not onto a compiler temporary: a temporary is never object data and defines no accessor method")]
    public PropertyClauseSpec? Property { get; set; }

    /// <summary>True for an ANY LENGTH elementary level-1 LINKAGE entry (ISO §13.18.2 — the item's length varies
    /// at runtime and is the length of the corresponding argument of the activating element, GR1; PICTURE is
    /// exactly one 'X', 'N', or '1', SR1). Set by <c>DataBinder.BindEntry</c> after the SR1/§13.16.3-SR17 shape
    /// checks; CLEARED by the placement sweeps (<c>CallBindLinkage</c> / <c>OoBindMethodData</c>) on an SR2/SR3/SR4
    /// violation so the item binds as ordinary storage under an already-failed compile (the IsBased pattern).
    /// Emit-side: every width-sensitive render of the item uses the CARRIER's runtime length, never Pic.Length.</summary>
    [DescriptionCopy(DescriptionCopyKind.EntryOnly,
        "ANY LENGTH (ISO §13.18.2) — in neither GR-1 exclusion list; §13.18.2.3 SR2/SR3/SR4 are re-screened at the copy's own site by the placement sweeps, which CLEAR it on a violation; a compiler temporary is working storage, never the §13.18.2.3 SR2 linkage parameter (kb/Work PB888)")]
    public bool IsAnyLength { get; set; }

    /// <summary>DYNAMIC LENGTH (ISO §8.5.1.10 / §13.18.19; COBOL-2014) — a variable-length, minimum-length-zero
    /// <c>PIC X</c> or <c>PIC N</c> string whose current length varies at runtime (never a fixed-width image). Set by
    /// <c>DataBinder.BindEntry</c> after the §13.18.19.3 SR1 (PICTURE exactly one N or X) and §13.16.3 SR18
    /// (permitted-co-clause) shape checks pass; CLEARED on any violation so the item binds as ordinary storage under
    /// an already-failed compile (the IsBased pattern). The item's field is a native <c>string</c> (init "" or VALUE);
    /// a receiving MOVE stores through <c>CobolDynString.Store</c> (truncate-to-<see cref="DynMaxSize"/>, no pad).</summary>
    [DescriptionCopy(DescriptionCopyKind.Clause,
        "DYNAMIC LENGTH (ISO §13.18.19) — in neither GR-1 exclusion list")]
    public bool IsDynamicLength { get; set; }

    /// <summary>THE MAXIMUM SIZE of this dynamic-length item in characters (ISO §8.5.1.10.1 — "the smallest of"
    /// the LIMIT phrase value, the largest integer storable in the PREFIXED usage, and the maximum permitted by the
    /// implementor). ALWAYS a real bound in <c>[0, CobolDynString.MaxLength]</c>, computed by the ONE producer
    /// <see cref="CobolNet.Runtime.CobolDynString.MaxSizeOf"/>; an absent LIMIT phrase (§13.18.19.4 GR2) yields the
    /// implementor maximum, NOT "no maximum". Meaningful only when <see cref="IsDynamicLength"/> is set.
    /// <para>⛔ It used to be the LIMIT phrase with <c>-1</c> meaning "no phrase", and the receiving/resize helpers
    /// special-cased that sentinel by skipping their clamp — so an unbounded item's SET SIZE request of 2³²+k wrapped
    /// to k and one past <see cref="int.MaxValue"/> threw out of generated code (kb/Work PB463). "No LIMIT clause" and
    /// "no maximum" are different facts; one field never again holds both.</para></summary>
    [DescriptionCopy(DescriptionCopyKind.Clause,
        "the LIMIT phrase of the DYNAMIC LENGTH clause (ISO §13.18.19.4 GR2), riding it")]
    public int DynMaxSize { get; set; } = CobolNet.Runtime.CobolDynString.MaxLength;

    /// <summary>The SPECIAL-NAMES dynamic-length structure this item's DYNAMIC LENGTH clause names (ISO §13.18.19.3
    /// SR2 — dynamic-length-structure-name-1), or null when the clause names none and the structure is the
    /// implementor's (SR3). Its PREFIXED length field is one of <see cref="DynMaxSize"/>'s §8.5.1.10.1 candidates, and
    /// two corresponding items describe the same DYNAMIC LENGTH clause only when they name the same structure
    /// (§8.5.3.1). Meaningful only when <see cref="IsDynamicLength"/> is set (kb/Work PB829).</summary>
    [DescriptionCopy(DescriptionCopyKind.Clause,
        "dynamic-length-structure-name-1 of the DYNAMIC LENGTH clause (ISO §13.18.19.2), riding it")]
    public DynamicLengthStructure? DynStructure { get; set; }

    /// <summary>The start of this view's window within its class's concatenated image (0 for a whole-area redefiner;
    /// &gt;0 for a partial-overlap view or a RENAMES sub-span). Meaningful only when <see cref="Class"/> is set.
    /// ONE writer: <c>DataBinder.AssignClassOffsets</c> — the classifier's offset walk, shared by the cell forcer
    /// (P5.11d; init-only inexpressible, the walk runs after construction — the P5.10 <c>Storage</c> pattern).</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "computed post-build by DataBinder.AssignClassOffsets over the clone's OWN redefines class")]
    public int ClassOffset { get; internal set; }

    /// <summary>This dynamic-length item's ordinal among the dynamic-length items of its CELL-BACKED class, in
    /// storage order — the cell's dynamic slot it rides (<see cref="DynSlotWindow"/>; kb/Work PB1026). -1 when
    /// the item is not such a member. ONE writer: <c>DataBinder.ForceStringCanonical</c>, the cell forcer.</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "computed post-build by DataBinder.ForceStringCanonical over the clone's OWN cell-backed class")]
    public int ClassDynOrdinal { get; internal set; } = -1;

    /// <summary>Every item under <paramref name="group"/>, depth first in declaration (storage) order.</summary>
    public static IEnumerable<DataItem> DescendantsOf(DataItem group)
    {
        foreach (var c in group.Children)
        {
            yield return c;
            foreach (var d in DescendantsOf(c)) yield return d;
        }
    }

    /// <summary>The start of this view's window within its class's shared area expressed in BITS — the unit
    /// §13.18.44.4 GR1 itself uses ("Storage association … starts at the first BIT of the data item referenced by
    /// data-name-2 and continues over an area sufficient to contain the number of BITS required"). For every
    /// byte-aligned item it is simply <c>8 × <see cref="ClassOffset"/></c>; it DIFFERS only for a
    /// <c>USAGE BIT</c> member that §8.5.1.6.3 places at a bit position inside a shared byte (kb/Work PB203).
    /// Same single writer and same meaningfulness condition as <see cref="ClassOffset"/>.</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "the same walk's bit-unit twin (ISO §13.18.44.4 GR1)")]
    public int ClassBitOffset { get; internal set; }

    /// <summary>The canonical storage representation of this ELEMENTARY item (null for a group — a group emits as a
    /// record struct and answers its image facts recursively over children). Computed ONCE by
    /// <see cref="Passes.StorageFormPass"/> after all facts are known (rearchitecture PHASE 05; DESIGN-data-model
    /// §2.1); the pass (classification + the crossing-form harmonize) is the SOLE writer — the internal setter
    /// documents the single-writer discipline. (The design's init-only shape is not expressible with the
    /// pass-assignment pattern; recorded as a P5.10 deviation. Reading before the group tail answers null — the
    /// <see cref="StoreAsImage"/> projection then answers false, the flag's early value.)</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "computed ONCE by StorageFormPass, the LAST data-model pass, over the complete forest")]
    public Model.StorageForm? Storage { get; internal set; }

    /// <summary>The level-66 RENAMES entries attached to this record (a 01/FD/SD owner). They are NOT storage
    /// children (they add no storage, ISO §13.18.45) — kept here so layout / struct emission ignores them while
    /// reference resolution can still find them.</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "level-66 entries add no storage (ISO §13.18.45) and are part of no copied description")]
    public List<DataItem> Renames66 { get; } = [];

    /// <summary>The containing group, or <see langword="null"/> for a top-level (01/77) item.</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "structure — the copier sets it to the NEW parent")]
    public DataItem? Parent { get; set; }

    /// <summary>The DATA DIVISION section this item is DESCRIBED in — the fact the section-worded syntax rules
    /// read (ISO §12.4.5.8.3 SR2: the FILE STATUS item "defined in the working-storage, local-storage, or linkage
    /// section"; §13.18.43.3 SR6 states the same set for RECORD … DEPENDING ON): its ROOT's
    /// <see cref="RootSection"/>, because an entry subordinate to an 01 is described in the 01's section. Null for an
    /// item no section describes (a compiler temporary). ⚠ Before kb/Work PB1080 the model had no such fact, and a
    /// rule could only ask whether a root sat in THIS unit's section lists — which a GLOBAL item made visible from a
    /// containing program never does. Stored on the item, the fact travels with it.</summary>
    public EntrySection? Section
    {
        get
        {
            var root = this;
            while (root.Parent is { } p) root = p;
            return root.RootSection;
        }
    }

    /// <summary>The section a level-1/77 ROOT is written in — written by <c>DataBinder.BindEntries</c> (and on the
    /// synthesized implied FD record); null on every subordinate, which reads its root's through
    /// <see cref="Section"/>.</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "where the SUBJECT entry is written, never where the type or model entry was: a TYPE / SAME AS subject in "
        + "LINKAGE is described in LINKAGE whatever section declared its TYPEDEF")]
    public EntrySection? RootSection { get; set; }

    /// <summary>True for a SYNTHESIZED compiler temp (a user-function result temp or an object-property temp —
    /// <c>DataBinder.CreateCompilerTemp</c>, THE ONE constructor, sets it). A temp wears the same bound shapes
    /// as declared data, so a rule whose text admits "a data item" and excludes functions (§15.43.3 r1's shape —
    /// §8.5.2.12 items 6/7 make FUNCTIONS category numeric too, which is why the exclusion must be written)
    /// needs this positive is-a-declared-item discrimination (kb/Work R26).</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "a synthesized-temp discrimination (kb/Work R26), not a data description clause")]
    public bool IsCompilerTemp { get; internal set; }

    /// <summary>True for the §15.4 TEMPORARY holding a NUMERIC function's returned value ("The evaluation of a function
    /// produces a returned value in a temporary elementary data item") when a statement materializes it once
    /// (<c>SendingValueTemp.Materialize</c> — the MOVE multi-receiver hoist, an EVALUATE subject, an INVOKE argument).
    /// The flag carries DOC-A.1-92's determination onto the item: a numeric function's returned value used as TEXT
    /// is the LITERAL form of its value (§15.4.1 leaves the representation to the implementor), so the temporary's
    /// character image is that literal form — never the zero-padded digits of the implementor's wide description,
    /// which made <c>MOVE FUNCTION INTEGER(N) TO A B</c> store <c>0000</c> where <c>MOVE FUNCTION INTEGER(N) TO A</c>
    /// stores <c>3</c> (kb/Work PB1007). Read by <c>OperandText</c>'s field image, the ONE text reader.</summary>
    [DescriptionCopy(DescriptionCopyKind.None,
        "a synthesized-temp discrimination (kb/Work PB1007), set on the materialized temp, not a data description clause")]
    public bool IsFunctionReturnedValue { get; internal set; }

    /// <summary>True for a group item (has children, no PICTURE).</summary>
    public bool IsGroup => Pic is null && Children.Count > 0;

    /// <summary>True for an elementary item (has a PICTURE).</summary>
    public bool IsElementary => Pic is not null;

    /// <summary>The generated <c>record struct</c> type name for a group item (unique via <see cref="Uid"/>).</summary>
    public string StructName => "_T_" + Uid;

    /// <summary>The generated runtime <c>NumProfile</c> field name for a numeric item (unique via <see cref="Uid"/>).</summary>
    public string ProfileName => "_P_" + Uid;

    /// <summary>
    /// True if this item's storage is a pure character image (a C# <see cref="string"/> at every leaf) — so the group
    /// has a clean <c>AsImage()</c>/<c>FromImage()</c> (COBOLNET_DESIGN §14.4 / Tier-B §4.2). A leaf qualifies when it
    /// is alphanumeric / numeric-edited (string-stored), OR a numeric-DISPLAY leaf flagged <see cref="StoreAsImage"/>
    /// (also string-stored, its zoned image). An OCCURS table of a character-image element qualifies too — its image
    /// is every occurrence's image concatenated (ISO §14.9 group move includes every OCCURS position; the count is the
    /// fixed/max occurrence count, the only OCCURS form the data model tracks today). A group with a
    /// COMP/COMP-3/COMP-5/float leaf (native non-character storage) is the genuine mixed-usage byte-island (Tier-C),
    /// deferred.
    /// </summary>
    public bool IsCharacterImage =>
        // A DYNAMIC-capacity table is out-of-line (non-contiguous, variable size — §8.5.1.9.1) so it has no static
        // character image; a DYNAMIC LENGTH elementary item is a variable-length string (§8.5.1.10) with no fixed
        // width. Either makes a group CONTAINING it a variable-length group — it drops out via Children.All below.
        !IsDynamicTable && !IsDynamicLength && (
        IsElementary
            // National and boolean leaves are string-stored (D-N1/D-B1). An elementary item's ImageWidth is its
            // CARRIER positions (a national item's Length); the group that holds it counts a national position as
            // the storage characters it occupies (RecordLayout.ImageWidth — kb/Work PB1665).
            ? Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited
                or PicCategory.National or PicCategory.Boolean || StoreAsImage
            : IsGroup && Children.All(c => c.IsCharacterImage));

    /// <summary>
    /// True if this item participates in the generated record-image codec (<c>AsImage()</c>/<c>FromImage()</c>,
    /// COBOLNET_DESIGN §14.4): every <see cref="IsCharacterImage"/> item, PLUS any fixed-point numeric leaf —
    /// DISPLAY (native <c>long</c>/<c>Int128</c>, its image is its zoned digit form) and BINARY/PACKED, whose
    /// image is its TRUE BYTES: radix-2 two's complement or BCD of exactly <see cref="Model.PicInfo.StorageWidth"/>
    /// (ISO/IEC 1989:2023 §13.18.60.4 GR4/GR11 leave the representation, including the sign, to the implementor —
    /// see <see cref="NumericByteForm"/>; V59). COMP-5 and BINARY-CHAR..DOUBLE are IN (kb/Work PB164 wave 1 —
    /// their Binary byte form and StorageWidth were pinned by V59; the old exclusion was predicate drift),
    /// the FLOAT family is IN (wave 2 — the <see cref="NumericByteForm.Ieee32"/>/<c>Ieee64</c> big-endian
    /// interchange pin, §13.18.60.4 GR13–GR15), and USAGE INDEX is IN (the R40 owner decision — the 8-byte
    /// big-endian occurrence-number image; §13.18.60.3 SR10 still restricts what may REFERENCE the item).
    /// Still excluded: a POINTER/PROGRAM-POINTER/FUNCTION-POINTER/OBJECT-REFERENCE leaf (class
    /// pointer/object — no character image; <see cref="ElementImageCapable"/>'s category list) and the
    /// DYNAMIC axis above (a variable-length group has no fixed record window; DISPLAY alone renders its
    /// documented current-extent format, A.1 item 57). A group is
    /// image-capable when every child is. Width-wise the codec uses <see cref="ImageWidth"/>, which IS
    /// <see cref="ByteWidth"/> for every item that reaches an image — the ONE-WIDTH invariant
    /// (<c>ImageWidthIsStorageWidthTests</c>).
    /// </summary>
    public bool IsImageCapable =>
        !IsDynamicTable && !IsDynamicLength   // out-of-line dynamic table / variable-length string — not in the static record codec (D9 / §8.5.1.10)
        && ElementImageCapable;

    /// <summary>The image capability of this item's SHAPE, ignoring the dynamic axis — i.e. of ONE occurrence /
    /// the content at any one length. Split out of <see cref="IsImageCapable"/> (kb/Work PB164, the
    /// variable-length DISPLAY half) so the current-extent composer can ask whether a DYNAMIC table's element
    /// has a well-defined image without duplicating the predicate: a dynamic table of image-capable elements
    /// concatenates their images at current capacity (§14.9.11.4 GR7's documented format), while a
    /// pointer/object-class leaf keeps its group imageless under EITHER axis (an INDEX leaf images since
    /// R40).</summary>
    public bool ElementImageCapable =>
        IsElementary
            // P5.7: the leaf arm is defined DIRECTLY on Pic (a pure declared-shape fact, phase-stable at every
            // point of the pipeline — resolve, procedure bind, emit). The numeric arm is PicInfo's ONE derived
            // image predicate (HasImageByteForm — kb/Work PB164), never a hand-rolled usage union.
            ? Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited
                or PicCategory.National or PicCategory.Boolean
                || Pic is { HasImageByteForm: true }
            : IsGroup && Children.All(c => c.IsImageCapable);

    /// <summary>⛔ THE ONE-WAY TRANSFER IMAGE capability (kb/Work PB244): true when this item's STORAGE can be
    /// rendered as characters for a consumer that sends them OUT and never reads them back into the group nor
    /// compares them — the DISPLAY transfer (ISO §14.9.11.4 GR1/GR4/GR6) and a MOVE's sending group (§14.9.25.4
    /// GR4). It is <see cref="IsImageCapable"/> widened by exactly ONE leaf kind: a class pointer/object leaf
    /// (<see cref="SlotWindow.CarriedBySlot"/>), which contributes its <see cref="ByteWidth"/> RESERVED
    /// placeholder positions — the same positions it occupies in a byte-addressed storage area under D-SLOT
    /// (COBOLNET_DATA_MODEL_DESIGN; the managed reference itself has no byte form, so it is not rendered).
    /// <para>⛔ WHY A SEPARATE CAPABILITY AND NOT A WIDER <see cref="IsImageCapable"/>: the placeholder image is
    /// NOT INJECTIVE (two groups differing only in a pointer render alike) and has no inverse (a pointer cannot
    /// be decoded from placeholder characters). So every consumer that COMPARES an image (§8.8.4.2.12 equality
    /// of two strongly-typed groups — image-equal ⟺ element-equal holds only for the two-way codec) or reads one
    /// BACK (FromImage — CALL/INVOKE copy-back, a record READ, a group MOVE receiver) keeps asking
    /// <see cref="IsImageCapable"/>, and would give a silent wrong answer if this were folded into it.</para>
    /// <para>On conforming source such a group is always inside a STRONG type declaration (§13.18.60.3 SR14),
    /// and it is a legal DISPLAY operand: §14.9.11.3 SR1 excludes only an identifier that references a data
    /// item "of class message-tag, object, or pointer", and a strongly-typed group's class is its type-name
    /// (§8.5.2.1).</para></summary>
    public bool TransferImageCapable =>
        IsImageCapable || (!IsDynamicTable && !IsDynamicLength && ElementTransferImageCapable);

    /// <summary>The element-shape half of <see cref="TransferImageCapable"/> — the twin of
    /// <see cref="ElementImageCapable"/>, and the gate on which the record struct emits its one-way
    /// <c>AsImage()</c> (<c>GroupImageCodec.EmitImageMethods</c>).</summary>
    public bool ElementTransferImageCapable =>
        ElementImageCapable
        || (IsElementary ? SlotWindow.CarriedBySlot(this) : IsGroup && Children.All(c => c.TransferImageCapable));

    /// <summary>True when this item is a VARIABLE-LENGTH GROUP (ISO §8.5.1.12.1) whose CURRENT-EXTENT image is
    /// well defined: every member is <see cref="IsImageCapable"/>, a dynamic-length leaf (its current content IS
    /// its image), a dynamic-capacity table of <see cref="ElementImageCapable"/> elements, or a nested SCALAR
    /// variable-length group satisfying the same. This is the §14.9.11.4 GR7 documented DISPLAY format's gate
    /// (kb/Work PB164 — A.1 item 57) AND the §14.8.2.2 / §14.8.3.2 activation-boundary crossing's gate
    /// (kb/Work PB204): ONE capability, because both need exactly the same thing — every component's extent
    /// known to the group's own struct.
    /// <para>⚠ A runtime-length item INSIDE a table element stays OUT — deliberately the SAME boundary as the
    /// §15.50.4 r7 LENGTH sum's named stage (<c>IntrinsicBinder.VariableLengthGroupSum</c>), so DISPLAY,
    /// FUNCTION LENGTH and the boundary crossing agree about which groups have a defined current extent.
    /// (Since the R40 INDEX pin no LEAF KIND excludes a group — only the shapes here do.)</para>
    /// <para>⛔ MOVED HERE FROM <c>GroupImageCodec</c> at the PB204 landing. It was a CODEGEN predicate that a
    /// BIND-time screen now has to ask (§14.8.2.2's compatibility sentence admits the crossing, so the binder
    /// must know whether this compiler can carry it); a bind phase consulting a codegen class is the layering
    /// inversion, and copying the predicate would have been the second copy this repo's two-arm-dispatch rule
    /// forbids. It is a pure declared-shape fact, so the data model is where it belongs.</para></summary>
    public bool CurrentExtentImageCapable =>
        IsGroup && CobolNet.Binding.ReferenceResolver.HasVariableLengthSubordinate(this)
        && Children.Where(c => c.RedefinesTargetName is null && (c.IsGroup || c.IsElementary))
            .All(c =>
                // ⛔ An OCCURS DEPENDING on or beneath a member stays OUT (the PB164 review fleet's repro: the
                // fixed-member lane renders an ODO table at its MAXIMUM occurrences while the §15.50.4 r4b
                // LENGTH sum counts the CURRENT count — and the composer CANNOT take the current extent,
                // because the composing method is a struct instance method while data-name-1 may live outside
                // the group entirely; the LENGTH sum reads it through the operand's access path, a mechanism a
                // struct method does not have). Loud beats a wrong width.
                !HasOdoOnOrBeneath(c)
                && (c.IsImageCapable
                    || (c.IsDynamicLength && c.IsElementary)
                    || (c.IsDynamicTable && c.ElementImageCapable)
                    // A nested variable-length group composes ONLY as a SCALAR member. ⛔ Discriminated by
                    // !IsDynamicTable, NOT by `Occurs is null` alone — a Format-4 DYNAMIC-capacity table
                    // also carries Occurs == null (the PB164 fleet's CRITICAL: the first cut re-admitted
                    // through this arm the very dynamic-table-with-runtime-length-element shape arm 3
                    // rejects, and the emission was uncompilable C# on legal source that never referenced the
                    // group). Under a fixed OCCURS of its own it is the in-element runtime-length shape (the
                    // pb62 corpus case).
                    || (c.IsGroup && !c.IsDynamicTable && c.Occurs is null && c.CurrentExtentImageCapable)));

    /// <summary>⛔ THE ONE "can this item cross an activation boundary as an image?" predicate (kb/Work PB204):
    /// a fixed-length group through its <c>AsImage</c>/<c>FromImage</c> record codec, a variable-length group
    /// through its <c>AsVarImage</c>/<c>FromVarImage</c> current-extent codec. §14.8.2.2 / §14.8.3.2 admit BOTH
    /// (the second subject to §8.5.1.12 compatibility — <see cref="VariableLengthCompatibility"/>), so a screen
    /// that asks only <see cref="IsImageCapable"/> rejects legal source; every CALL / INVOKE / RETURNING screen
    /// and emit guard asks THIS.</summary>
    public bool BoundaryImageCapable => IsImageCapable || CurrentExtentImageCapable;

    /// <summary>An OCCURS DEPENDING clause on <paramref name="c"/> itself or any subordinate — the shape the
    /// current-extent composer must refuse (see <see cref="CurrentExtentImageCapable"/>). The sibling of
    /// <c>IntrinsicBinder.HasOdoBeneath</c>, which tests subordinates only (its callers already hold the
    /// operand); the composer screens MEMBERS, where the clause may sit on the member itself.</summary>
    internal static bool HasOdoOnOrBeneath(DataItem c) =>
        c.OccursSpec?.DependingName is not null
        || (c.IsGroup && c.Children.Any(m => m.RedefinesTargetName is null && HasOdoOnOrBeneath(m)));

    /// <summary>The character width of this item's image — meaningful for an <see cref="IsCharacterImage"/> item. For a
    /// group it is the sum of each child's TOTAL image contribution, i.e. the child's per-occurrence image width × its
    /// fixed-OCCURS count (every OCCURS position is part of the group image, ISO §14.9). A numeric-DISPLAY leaf's image
    /// is its digit count plus a separate-sign character when SIGN IS SEPARATE (ISO §13.18.52); an over-punched sign
    /// occupies no extra position. (This is the per-occurrence width of THIS item; a parent multiplies by THIS item's
    /// own OCCURS count.)
    /// <para>⛔ A group's unit is its class's: an ALPHANUMERIC group counts a national position as the
    /// <c>CobolBits.BytesPerNational</c> storage characters it occupies, a <c>GROUP-USAGE NATIONAL</c> group counts
    /// national positions (§13.18.29.4 GR2b). The rule — redefining children, the bit walk, the national unit — is
    /// written ONCE, in <see cref="RecordLayout.ImageWidth"/> (kb/Work PB1665); this member is that answer.</para></summary>
    public int ImageWidth => RecordLayout.ImageWidth(this);

    /// <summary>⛔ <b>ISO §8.5.4's STEM, AS A STRUCTURE</b> — "A zero-length item is a data item or a literal
    /// whose minimum length is zero and whose length at runtime is zero." This is the FIRST half, the only half
    /// a description can answer; the second is a run-time test the emitter writes (kb/Work PB896).
    /// <para>The clause then enumerates nine shapes, four of which are GROUPS — item 1 "a group data item
    /// containing only an occurs-depending table in which the number of occurrences is zero", item 2 "a group
    /// data item containing only a subordinate zero-length item", item 5 a zero-character-position
    /// variable-length record, item 7 "a variable-length group containing only dynamic-capacity tables each of
    /// whose current capacity is zero". They are not four cases: they are the ONE sum below reaching zero, which
    /// is why this is written as the recurrence and not as four predicates (CLAUDE.md rule 5 — the shape that
    /// makes the next case automatic). <c>ZeroLengthItemDriftTests</c> pins each of the enumerated shapes
    /// against it.</para>
    /// <para>It mirrors <see cref="ImageWidth"/> exactly — the same non-redefining children (§13.18.44: a
    /// redefining child occupies no new storage), the same per-child occurrence multiplier — except that the
    /// multiplier is the MINIMUM occurrence count: integer-1 of an <c>OCCURS … DEPENDING</c> (§13.18.38 SR16
    /// allows 0) and zero for a DYNAMIC-capacity table (§8.5.1.9 — its current capacity starts at zero).</para></summary>
    public bool MinimumLengthIsZero =>
        // §8.5.4 items 3 and 4 — an ANY LENGTH or DYNAMIC LENGTH item has no minimum length of its own
        // (§8.5.1.10; §13.18.2 GR1 makes an ANY LENGTH item the activating argument's length).
        IsDynamicLength || IsAnyLength
        || (IsElementary ? ElementaryImageWidth == 0
            : Children.Where(c => c.RedefinesTargetName is null)
                      .All(c => c.MinimumOccurrences == 0 || c.MinimumLengthIsZero));

    /// <summary>⛔ THE ONE "fewest occurrences this entry can have at run time" — integer-1 of a Format-2
    /// <c>OCCURS … DEPENDING ON</c> (ISO §13.18.38 SR16 permits zero), the MINIMUM CAPACITY of a Format-4
    /// DYNAMIC-capacity table (§13.18.38.4 GR16: "Integer-4 is the minimum capacity of the table. If integer-4
    /// is absent, a value of zero is assumed for it" — the binder stores it as <see cref="OccursSpec.Min"/>), and
    /// the fixed count otherwise. Read by <see cref="MinimumLengthIsZero"/> and by the §13.18.43.4 GR8 a)
    /// minimum record size (<c>FileModel.MinRecordSize</c>): <see cref="ImageWidth"/> deliberately uses the
    /// MAXIMUM, because §8.5.1.8 fixes the allocated physical capacity at compile time.</summary>
    internal int MinimumOccurrences =>
        OccursSpec is { DependingName: not null } or { IsDynamic: true } ? OccursSpec.Min
        : Occurs ?? 1;

    /// <summary>True when this subtree contains a <c>USAGE BIT</c> leaf — the gate that sends a group's width
    /// through the §8.5.1.6.3 bit walk (design D19, fix-queue PB43) instead of the plain character sum. It is the
    /// PROOF that the change is inert for bit-free programs, not an optimization.</summary>
    public bool HasBitDescendant =>
        IsElementary ? BitLayout.IsBitLeaf(this) : Children.Any(c => c.HasBitDescendant);

    /// <summary>The character-image width of an elementary item (digit count + a separate-sign position when present
    /// for a signed numeric; otherwise the PICTURE's character length). A pure DECLARED-shape fact (reads only
    /// <see cref="Pic"/>) — internal so <see cref="Model.RecordLayout"/>'s leaf arm is PHASE-FREE (P5 Step 8:
    /// callable at bind time, before <see cref="Storage"/> exists; the Storage width was built FROM this value and
    /// identity #3 proved them equal corpus-wide).</summary>
    internal int ElementaryImageWidth
    {
        get
        {
            // A DYNAMIC LENGTH item has no fixed character-image width — it contributes no static positions to an
            // enclosing group (§8.5.1.10; its current byte/char length is a runtime value, read via FUNCTION
            // LENGTH/BYTE-LENGTH). Zero keeps the fixed-layout math sound for the standalone-item case.
            if (IsDynamicLength) return 0;
            if (Pic is not { } pic) return 0;
            // D19/PB43 — a USAGE BIT leaf OCCUPIES ceil(n/8) character positions, not n. §13.18.60.4 GR5 makes
            // its representation bits; §8.1.2 leaves bits-per-character to the implementor and WiseOwl COBOL pins 8.
            // ⚠ This is its OCCUPANCY, deliberately NOT what FUNCTION LENGTH returns for it: §15.50.4 r1 gives an
            // elementary bit item its length in BOOLEAN positions (n). The two coincided while USAGE BIT was
            // stored char-per-bit, which is precisely how the defect stayed invisible — they are read from
            // different members now (IntrinsicBinder's r1 arm reads Pic.Length).
            if (pic is { Category: PicCategory.Boolean, Usage: Usage.Bit })
                return BitLayout.Characters(pic.Length);
            if (pic.Category is PicCategory.Numeric)
                // ⛔ ONE WIDTH (V59). An item with a byte form of its own occupies THOSE bytes in the image —
                // BINARY 1-2-4-8-16, PACKED its BCD nibbles — never a byte per decimal digit. Before this, a
                // PIC 9(4) COMP claimed 4 image positions while FUNCTION BYTE-LENGTH reported 2, and §15.14.4
                // GR1 (bytes) and §15.50.4 r3 (alphanumeric character positions) cannot disagree in a
                // single-byte-character model. The zoned form's digit run IS its byte form, so it stays digits
                // plus a SIGN SEPARATE position (§13.18.52; a binary item never carries a separate sign).
                return pic.ByteForm is NumericByteForm.Binary or NumericByteForm.Packed
                        or NumericByteForm.PackedNoSign or NumericByteForm.Ieee32 or NumericByteForm.Ieee64
                    ? pic.StorageWidth
                    : pic.Digits + (pic.Signed && pic.SignKind is "LeadingSeparate" or "TrailingSeparate" ? 1 : 0);
            return pic.Length;
        }
    }

    /// <summary>The number of CHARACTER positions this item's DISPLAY-STATEMENT / DEVICE text occupies — digits
    /// plus a SIGN SEPARATE position for a numeric item, else the PICTURE length. DISTINCT from
    /// <see cref="ImageWidth"/> for exactly the items V59 separated: a <c>PIC 9(4) COMP</c> occupies TWO bytes in
    /// a record and prints FOUR digits, because §14.9.13 DISPLAY transfers the operand's VALUE (a numeric operand
    /// is treated as though moved to an alphanumeric item, §14.9.25.4 GR6) while §15.50.4 r3 counts the
    /// character positions it OCCUPIES. Read by the text-facing surfaces only — ACCEPT's device window, DISPLAY's
    /// fit, and the Report Writer's print columns (§13.18.14.4 GR9); every storage/record surface uses
    /// <see cref="ImageWidth"/>.</summary>
    public int DisplayTextWidth =>
        IsElementary && Pic is { } pic
            ? DisplayTextWidthOf(pic)
            : ImageWidth;   // a group prints its image (its leaves' storage): §8.5.2.1 gives an
                            // alphanumeric group class and category alphanumeric and a usage of display, so its
                            // character positions ARE its leaves' stored representations (kb/Work PB182 — the
                            // §8.8.4.1.1 this used to cite is a phantom clause)

    /// <summary>The ELEMENTARY half of <see cref="DisplayTextWidth"/>, as a function of the PICTURE alone — so a
    /// caller holding a <see cref="PicInfo"/> with no item yet (the report-writer §13.18.38.3 SR26 overlap test,
    /// which measures a repeating entry's width before the synthetic print item exists) asks the ONE rule rather
    /// than restating it.</summary>
    public static int DisplayTextWidthOf(PicInfo pic)
        => pic.Category is PicCategory.Numeric
            ? pic.Digits + (pic.Signed && pic.SignKind is "LeadingSeparate" or "TrailingSeparate" ? 1 : 0)
            : pic.Length;

    /// <summary>The item's size in BYTES — the FUNCTION BYTE-LENGTH (§15.14) authority (the D7 byte-vs-position
    /// distinction). A group sums each non-redefining child's byte contribution × its own fixed-OCCURS count
    /// (mirroring <see cref="ImageWidth"/>; a REDEFINING child overlays its target and adds no storage,
    /// §13.18.44). Per-usage byte widths are IMPLEMENTOR-DEFINED (§13.18.60 GR4/6/7/8/11/12; §8.1.2 even makes
    /// bits-per-byte implementor-specified) — these are WiseOwl COBOL's PINNED, DOCUMENTED widths
    /// (COBOLNET_INTRINSICS_DESIGN §BYTE-LENGTH): DISPLAY = 1 byte per character position; a boolean item of
    /// usage DISPLAY (implied by §13.18.60.3 SR13(b) when no USAGE clause is written) = 1 byte per boolean
    /// position, per §13.18.60.4 GR7's "alphanumeric coded character set"; a boolean item of **USAGE BIT** =
    /// <c>ceil(n / 8)</c>, because §13.18.60.4 GR5 says bits SHALL be used and §8.1.2 leaves bits-per-character to
    /// the implementor (WiseOwl COBOL pins 8) — design D19, fix-queue PB43; NATIONAL = 2 bytes per position (UTF-16,
    /// D-N1/D-N3); BINARY/COMP-5/PACKED/BINARY-CHAR..DOUBLE =
    /// their <see cref="Model.PicInfo.StorageWidth"/>; COMP-1/FLOAT-SHORT = 4, COMP-2/FLOAT-LONG/-EXTENDED = 8
    /// (the .NET Single/Double carriers); INDEX / POINTER / PROGRAM-POINTER / FUNCTION-POINTER / OBJECT REFERENCE
    /// = 8 (the 64-bit managed carrier). WiseOwl COBOL has no SYNCHRONIZED physical padding, so a group carries no
    /// implicit-filler bytes (§15.14.4 r3 is satisfied vacuously).</summary>
    public int ByteWidth =>
        IsElementary ? ElementaryByteWidth
        // D19/PB43 — same reason as ImageWidth: a bit-bearing subtree is LAID OUT, not summed.
        : HasBitDescendant ? BitLayout.Characters(BitLayout.ExtentBits(this))
        : Children.Where(c => c.RedefinesTargetName is null).Sum(c => c.ByteWidth * (c.Occurs ?? 1));

    /// <summary>An elementary item's byte width — the per-usage pinned widths documented on <see cref="ByteWidth"/>.</summary>
    internal int ElementaryByteWidth
    {
        get
        {
            if (Pic is not { } pic) return 0;
            return pic.Usage switch
            {
                // One width authority: PicInfo.StorageWidth carries every pinned representation's width —
                // radix-2/BCD (V59) and the kb/Work PB164 IEEE interchange forms alike (its float arms were
                // duplicated here as 4/8 literals until the wave-2 dedupe).
                Usage.Binary or Usage.Comp5 or Usage.Packed or Usage.BinaryChar or Usage.BinaryShort
                    or Usage.BinaryLong or Usage.BinaryDouble
                    or Usage.Float or Usage.FloatShort or Usage.FloatBinary32
                    or Usage.Double or Usage.FloatLong or Usage.FloatExtended or Usage.FloatBinary64
                    or Usage.Index   // R40: the 8-byte occurrence-number image — one width authority
                    => pic.StorageWidth,
                // The processor-dependent non-support formats (rejected at ParseUsage, COBOLNET1564) — their pinned
                // ISO/IEC 60559 byte widths, so a BYTE-LENGTH fold under an already-errored compile is not off by 1x.
                Usage.FloatBinary128 or Usage.FloatDecimal34 => 16,   // binary128 / decimal128 = 16 bytes
                Usage.FloatDecimal16 => 8,                            // decimal64 = 8 bytes
                Usage.Pointer or Usage.ProgramPointer or Usage.FunctionPointer
                    or Usage.ObjectReference => 8,
                Usage.National => CobolNet.Runtime.CobolBits.BytesPerNational * ElementaryImageWidth,   // UTF-16, D-N1/D-N3
                _ => ElementaryImageWidth,                       // DISPLAY / BIT: 1 byte per character/boolean position
            };
        }
    }

    /// <summary>The C# type of a single occurrence (a record-struct type name for a group, a <see cref="string"/> for
    /// a <see cref="StoreAsImage"/> numeric leaf, else the PIC's CLR type).</summary>
    public string ElementType => IsGroup ? StructName : StoreAsImage ? "string" : Pic?.ClrType ?? "object";

    /// <summary>The C# type name for this item's field — <c>CobolDynTable&lt;T&gt;</c> for a DYNAMIC table (D9), an
    /// array of <see cref="ElementType"/> for a fixed OCCURS table, else the scalar <see cref="ElementType"/>.</summary>
    public string FieldType =>
        IsDynamicTable ? $"CobolDynTable<{ElementType}>"
        : Occurs is not null ? ElementType + "[]"
        : ElementType;
    // (The ClrType back-compat alias is DELETED — P5.10: zero readers, grep-proven by the topology audit.)

    /// <summary>
    /// Convert a COBOL data-name to a valid, collision-safe C# identifier: hyphens → underscores, a leading digit
    /// gets an underscore prefix, and C# keywords are escaped with <c>@</c>.
    /// </summary>
    public static string Sanitize(string cobolName)
    {
        string s = cobolName.Replace('-', '_');
        if (s.Length == 0 || char.IsDigit(s[0])) s = "_" + s;
        return SyntaxFacts.GetKeywordKind(s) != SyntaxKind.None ? "@" + s : s;
    }
}
