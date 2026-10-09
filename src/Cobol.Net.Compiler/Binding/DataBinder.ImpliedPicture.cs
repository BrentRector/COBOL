// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Common;
using CobolNet.Editions;

namespace CobolNet.Binding;

/// <summary>The PICTURE character-string <b>ISO §13.16.3 SR9</b> implies for a picture-less elementary item whose
/// VALUE clause supplies a literal — its CATEGORY (which of SR9's three arms) and its LENGTH (SR9's "the length of
/// the literal as specified in 8.3.3, Literals").
/// <para>SR9 a) "if the literal is alphanumeric, 'PICTURE X(length)'"; b) "if the literal is boolean,
/// 'PICTURE 1(length)'"; c) "if the literal is national, 'PICTURE N(length)'".</para></summary>
internal readonly record struct ImpliedPicture(LiteralClass Class, int Length)
{
    /// <summary>The PICTURE character-string SR9 spells out for this class and length — the exact text a
    /// programmer would have written, handed to the ONE picture analyzer so an implied clause and a written one
    /// cannot describe different items.</summary>
    public string Text => Class switch
    {
        LiteralClass.National => $"N({Length})",   // SR9 c)
        LiteralClass.Boolean => $"1({Length})",    // SR9 b)
        _ => $"X({Length})",                       // SR9 a)
    };
}

/// <summary>
/// ⛔ <b>ISO §13.16.3 SR9 — THE VALUE-IMPLIED PICTURE</b>, the one exception §13.16.3 SR8 defers to in its own
/// words ("except as indicated in Syntax rule 9"):
/// <i>"The PICTURE clause may be omitted for an elementary item when an alphanumeric, boolean, or national literal
/// that is not a zero-length literal is specified in the data-item format of the VALUE clause. A PICTURE clause is
/// implied as follows: a) if the literal is alphanumeric, 'PICTURE X(length)' b) if the literal is boolean,
/// 'PICTURE 1(length)' c) if the literal is national, 'PICTURE N(length)' where length is the length of the literal
/// as specified in 8.3.3, Literals."</i>
///
/// <para><b>Why a pass, and why HERE (kb/Work PB504, inventory row SR-13.16.3-9).</b> The rule had no
/// implementation site at all. <c>BindEntry</c>'s <c>pic</c> chain is keyed on USAGE (index, pointer, the BINARY-*
/// and float families …) and has no arm keyed on the VALUE literal, so <c>01 A VALUE "HELLO".</c> — legal source
/// whose PICTURE is <c>X(5)</c> — ended bind with <c>Pic == null</c>. Before kb/Work PB487 that leaked a raw Roslyn
/// <c>CS0103</c> to the user; after it, the SR8 closing guard rejects the program by name. Both are wrong: SR9 says
/// the clause MAY BE OMITTED. Synthesizing the implied PICTURE is what makes the entry stop being picture-less, and
/// the SR8 guard then never sees it — which is why this is a SYNTHESIS and never a carve-out in that guard.</para>
///
/// <para><b>It runs BEFORE <c>UsageInheritancePass</c>, and that placement is the whole design.</b> The implied
/// clause is analyzed by the SAME <see cref="PictureAnalyzer.Analyze"/> call a written one takes, with the same
/// arguments <c>BindEntry</c> passes (the entry's OWN usage; <c>explicitUsage</c> = "this entry wrote a USAGE
/// clause"). From that moment the item is INDISTINGUISHABLE from one whose entry wrote the clause, so
/// §13.18.60.4 GR1 group-usage inheritance, the §13.18.60.3 SR3/SR5/SR12/SR20 screens, SIGN inheritance, REDEFINES
/// classification, the VALUE initializer and the emitter all apply to it unchanged and UNTOUCHED — one screen, one
/// diagnostic, no second copy of any rule. §13.18.60.3 SR13 a) and SR20 are written for exactly this ("if the
/// explicit or <b>implicit</b> picture character-string contains the symbol 'N', a USAGE NATIONAL clause is
/// implied"), so an implied <c>N(length)</c> acquires usage national by the standard's own words.</para>
///
/// <para><b>It runs BEFORE <c>ExpandTypes</c>, over the WRITTEN entries (<c>ConformanceForest</c>), and that is the
/// other half of the design (kb/Work PB1300).</b> SR9 is a rule about the entry the programmer wrote, and the TYPE
/// clause then carries its result: §13.18.57.4 GR3 — <i>"When the description of type-name-1 includes an implicit
/// PICTURE clause derived from a VALUE clause, that implicit PICTURE clause becomes part of the description of the
/// subject of the entry."</i> So a TYPEDEF template (<c>01 U TYPEDEF VALUE "ABCD".</c>, or a member
/// <c>05 A VALUE "AB".</c>) must OWN its implied PICTURE before its description is copied — and then the one
/// description copy (<c>CopyEntryDescription</c>) hands it to every TYPE subject and every reproduced subordinate
/// exactly as it hands over a written PICTURE; SAME AS (§13.18.49.4 GR1) the same way. This pass used to run AFTER
/// the copy, over the composed forest: the template had no PICTURE yet, the subject's copy took none, and a subject
/// that wrote its OWN VALUE then had its picture implied from THAT literal — <c>01 W TYPE U VALUE "Q".</c> bound as
/// PIC X(1) where GR3 makes it the template's X(4), and <c>VALUE "QRSTUV"</c> over a two-character template
/// silently widened the item instead of drawing the §13.18.63.3 size refusal. An entry whose description is still a
/// pending TYPE / SAME AS reference is not picture-less in SR9's sense (<c>IsPictureLessLeaf</c>): its PICTURE is
/// the referenced description's, so it is left to the copy. Because no clone exists yet, every item visited IS a
/// written entry and the EDITION GATE fires once per source entry by construction; the pass stays idempotent (a
/// non-null <c>Pic</c> is skipped).</para>
///
/// <para><b>THE STANDARD STATES THIS RULE THREE TIMES, once per entry kind, and the sweep is complete</b>
/// (<c>grep -n "PICTURE clause may be omitted" specs/ISO_COBOL.md</c> returns exactly three hits). §13.16.3 SR9
/// is the DATA description entry — this pass. §13.15.3 SR14 is the REPORT GROUP description entry, word for word
/// the same rule, and it is <see cref="ImpliedReportPicture"/>, sharing this classifier and this edition gate;
/// before that it was the <c>feedback_two_arm_dispatch</c> shape at its purest — the report binder REJECTED
/// <c>02 COLUMN 1 VALUE "HELLO".</c> while the identical data-division entry compiled. §13.17.3 SR10 is the
/// SCREEN description entry, and it has no site because the whole SCREEN SECTION is a DECLINED Annex A.4.2
/// module (<c>ScreenFacility</c>, COBOLNET1560, docs/CONFORMANCE.md §5): the entry never binds, so the rule has
/// nothing to apply to. If that module is ever claimed, its implied PICTURE is one more call to
/// <see cref="Sr9ImpliedFor"/>.</para>
///
/// <para><b>The figurative constant is inside SR9's grant</b> (kb/Work PB828, the adjudication; kb/Work PB831, the
/// wrong answer the un-derived half produced). §8.3.3.1 makes a figurative constant a LITERAL — "A literal is
/// defined by a reserved word that references a figurative constant or is a character-string …" — and SR9's own
/// length reference resolves for it, because §8.3.3.6, <i>Figurative constant values</i>, is a subclause OF §8.3.3
/// and §8.3.3.6.4 GR3 supplies the length the context does not: b) "When a figurative constant is other than ALL
/// literal-1, the length of the string is one character", c) "The length of the string is the length of
/// literal-1". §8.3.3.6.3 SR1 gates a figurative constant on a PROHIBITION ("A figurative constant shall not be
/// specified where a syntax rule prohibits it"), and no syntax rule prohibits one here. §7.3.15.4 GR4 k's
/// <c>&gt;&gt;FLAG-14 VALUE-FIG-CON-LENGTH</c> option presupposes the construct COMPILES, which settles it: a
/// compatibility flag is for syntax a conforming compiler accepts.</para>
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>§13.16.3 SR9 — give every picture-less elementary item whose VALUE clause supplies an
    /// alphanumeric, boolean or national literal the PICTURE the rule implies.</summary>
    internal void SynthesizeImpliedPictures()
    {
        // The WRITTEN entries — the pass runs before ExpandTypes, so there are no clones to exclude yet; the forest
        // is named for what the rule is about (see the class remarks).
        foreach (var item in ConformanceForest()) SynthesizeImpliedPicture(item);
    }

    /// <summary>The same synthesis over one subtree's WRITTEN entries — what <see cref="CompleteDescriptionAhead"/>
    /// runs for an operand a constant's length phrase measures before the pipeline reaches it (kb/Work PB2465). The
    /// pass is idempotent (an entry with a PICTURE fails <see cref="IsPictureLessLeaf"/>), so the pipeline's own run
    /// later passes over these entries.</summary>
    private void SynthesizeImpliedPicturesUnder(DataItem root)
    {
        foreach (var item in PreOrder(root))
            if (IsWrittenEntry(item)) SynthesizeImpliedPicture(item);
    }

    /// <summary>§13.16.3 SR9 for one entry: the body of <see cref="SynthesizeImpliedPictures"/>.</summary>
    private void SynthesizeImpliedPicture(DataItem item)
    {
        // ⛔ THE SAME PREDICATE THE SR8 GUARD USES, and deliberately so: SR9's subject is precisely the
        // population SR8 would otherwise reject, so the two rules must never disagree about which entries
        // they are looking at. It also excludes an entry whose description is a still-pending TYPE / SAME AS
        // reference (its PICTURE is the referenced description's — §13.18.57.4 GR3, §13.18.49.4 GR1), and it
        // is what makes this pass IDEMPOTENT: an entry that already has a PICTURE (written or
        // usage-synthesized) fails it.
        if (!IsPictureLessLeaf(item)) return;
        // ⛔ FORMAT 1 ONLY. SR9's grant is worded "specified in the DATA-ITEM FORMAT of the VALUE clause",
        // which is §13.18.63.2 Format 1 (`VALUE IS literal-1`) — DataItem.RawValue. The Format-2 (table)
        // carrier, DataItem.TableValues, is a different format and SR9 does not reach it, so a picture-less
        // `OCCURS … VALUES ARE "AB" FROM (1)` stays SR8's rejection.
        if (item.RawValue is not { } raw) return;
        if (Sr9ImpliedFor(raw, Sr9EffectiveUsage(item)) is not { } implied) return;

        using var _ = Edition.At(item);
        string where = $"data item '{item.CobolName ?? "FILLER"}'";
        // The edition gate, through the ONE canonical funnel (ConstructRegistry.Check), once per SOURCE entry
        // (every entry visited here is one — no clone exists yet): COBOL-85 required a PICTURE for every
        // elementary item bar an index data item and the subject of a RENAMES clause, and had no
        // VALUE-implied PICTURE at all. Its boolean and national arms are gated a second time and
        // independently by `boolean-data-2002` / `national-data-2002`, which is why this row carries the
        // alphanumeric arm's introduction.
        ConstructRegistry.Check(Edition.Edition, Edition.Sink, Constructs.ValueImpliedPicture2002, where);

        // ⛔ THE SAME CALL BindEntry MAKES FOR A WRITTEN PICTURE, argument for argument — the entry's OWN
        // usage and its own SIGN clause, `explicitUsage` = "this entry wrote a USAGE clause". A usage the
        // entry acquires from a GROUP is applied ONCE, by UsageInheritancePass's ApplyEffectiveUsage, exactly
        // as it is for a written PICTURE; passing the inherited usage here as well would screen it twice and
        // report the §13.18.60.3 SR3/SR5/SR12/SR20 violation twice.
        item.PictureText = implied.Text;
        item.PicIsValueImplied = true;   // an IMPLICIT clause: no SR4/SR5/SR10 size bound (see the property)
        item.Pic = PictureAnalyzer.Analyze(implied.Text, item.OwnUsage ?? Usage.Display, Edition, where,
            item.OwnSign, currencies: CurrencySigns, blankWhenZero: item.BlankWhenZero,
            explicitUsage: item.OwnUsage is not null, decimalPointIsComma: DecimalPointIsComma);
        // The entry is no longer picture-less, so the deferred §13.18.60.3 SR5/SR12 adjudication a
        // PICTURE-less USAGE NATIONAL / BIT entry carries is ANSWERED — by the picture SR9 supplies. The
        // screen above is the one that judges it (`01 A USAGE NATIONAL VALUE "AB".` is SR12's alphanumeric
        // refusal; `01 A USAGE NATIONAL VALUE N"AB".` conforms).
        item.Pending = PicPending.None;

        // ⚠ The VALUE literal is NOT re-screened through ScreenValueLiteral, and that is the rule's own
        // doing: §13.18.63.3 SR4/SR5/SR10's size sentences bound the literal by "the size indicated by an
        // EXPLICIT PICTURE clause", and this one is implied; their class sentences cannot bite because the
        // implied picture's category IS the literal's class by construction; and SR2's numeric-range arm is
        // unreachable, SR9 admitting only the three character categories.
    }

    /// <summary>The §13.15.3 SR14 arm of the same synthesis — the REPORT GROUP description entry's VALUE-implied
    /// PICTURE. SR14 is §13.16.3 SR9 restated for the report section, so it shares the classifier and the
    /// edition gate and differs only in where the analyzed picture goes: the report binder builds its printable
    /// item inline, rather than filling a <see cref="DataItem"/> the entry walk already created.
    /// <para>The gate fires unconditionally here — a report group entry is never a TYPE clone, so the
    /// once-per-source-entry discipline the data-division pass gets from its <c>TypeAnchor</c> test is automatic.
    /// </para></summary>
    internal PicInfo ImpliedReportPicture(ImpliedPicture implied, Usage itemUsage, bool explicitUsage,
        SignSpec? ownSign, string where)
    {
        ConstructRegistry.Check(Edition.Edition, Edition.Sink, Constructs.ValueImpliedPicture2002, where);
        return PictureAnalyzer.Analyze(implied.Text, itemUsage, Edition, where, ownSign,
            currencies: CurrencySigns, explicitUsage: explicitUsage, decimalPointIsComma: DecimalPointIsComma);
    }

    /// <summary>The usage that applies to <paramref name="item"/> by §13.18.60.4 GR1 — its own clause (or the one
    /// a GROUP-USAGE clause implies), else the NEAREST ENCLOSING entry's, else §13.18.60.3 SR13 b)'s implied
    /// DISPLAY. It is the CONTEXT §8.3.3.6.4 GR1 and GR4 ask about, and nothing else: a written literal carries
    /// its own class, so only a figurative constant consults it.
    /// <para><see cref="DeclaredUsageOf"/> is the "own or implied" half, shared verbatim with
    /// <c>UsageInheritanceWalk</c>, which PUSHES the same derivation down the forest; the loop is GR1's "nearest
    /// enclosing" half asked PULL-style, because this pass runs before that walk and the walk's own result does
    /// not yet exist. The two agreeing is pinned end-to-end by
    /// <c>ImpliedPictureTests.Sr9_FigurativeContext_TravelsTheWholeAncestorChain</c>, which nests the usage two
    /// levels above the entry so a one-level or self-only reading fails.</para></summary>
    private static Usage Sr9EffectiveUsage(DataItem item)
    {
        for (DataItem? d = item; d is not null; d = d.Parent)
            if (DeclaredUsageOf(d) is { } u) return u;
        return Usage.Display;
    }

    /// <summary>⛔ THE SR9 CLASSIFIER — which of §13.16.3 SR9's three arms a VALUE operand selects, and the length
    /// it carries, or <see langword="null"/> when SR9 grants nothing and §13.16.3 SR8's "a PICTURE clause shall be
    /// specified" governs.
    /// <para>The literal spellings are read through <see cref="CobolLiteral"/>, THE literal codec: its
    /// <see cref="CobolLiteral.ClassOf"/> is the §8.3.3.2 / §8.3.3.4 / §8.3.3.5 class table (both the plain and the
    /// hexadecimal format of each), and <see cref="CobolLiteral.Decode"/> yields the literal's CHARACTERS, which is
    /// SR9's "length … as specified in 8.3.3, Literals" — two hexadecimal digits per alphanumeric character
    /// (§8.3.3.2.3 r6), four per national character (§8.3.3.5.3 r5), four boolean characters per hexadecimal digit
    /// (§8.3.3.4.4 GR5). A ZERO-LENGTH literal is excluded by §13.16.3 SR9 itself — its grant is worded for a
    /// literal "that is not a zero-length literal" — so <c>01 A VALUE "".</c> falls to SR8.</para>
    /// <para>A NUMERIC literal selects no arm — SR9 names the three character categories only — so
    /// <c>01 F VALUE 42.</c> falls to SR8, and so does the class-pointer figurative NULL / NULLS, which is neither
    /// alphanumeric, boolean nor national (§13.16.3 SR10 forbids a VALUE clause on the pointer items it belongs
    /// to).</para>
    /// <param name="context">The usage that applies to the entry (<see cref="Sr9EffectiveUsage"/>) — §8.3.3.6.4
    /// GR1's "context requiring national characters" and GR4's boolean one. Consulted for a FIGURATIVE constant
    /// only; a written literal's class is its own.</param></summary>
    internal static ImpliedPicture? Sr9ImpliedFor(string raw, Usage context)
    {
        // ── §8.3.3.6.2 Format 6 (`ALL literal-1`) and Format 7 (`ALL symbolic-character-1`, which the VALUE
        // reader has already re-quoted as a one-character ALL literal of its class). §8.3.3.6.4 GR3 c) — "The
        // length of the string is the length of literal-1" — so the length is literal-1's and, by §8.3.3.6.3 SR2
        // ("Literal-1 shall be an alphanumeric, boolean, or national literal"), so is the category.
        // ⛔ THIS IS THE ARM kb/Work PB831 MEASURED WRONG: GR3 c) is the rule for an item of unstated size; GR2,
        // which repeats the string to the receiver's size, governs the case where "the length of the string IS
        // specified in the rules for the context", i.e. a SIZED receiver. Taking `ALL "AB"` to be length-less
        // gave it the one-character recovery item and stored "A" — a silent wrong answer on legal source.
        // THE one §8.3.3.6.2 operand classifier (kb/Work PB461). NULL/NULLS is not admitted: it names no
        // character class and so implies no picture (§8.3.3.6.4 GR1 speaks of character values).
        var figOp = CobolNet.CodeGen.FigurativeConstants.Classify(raw, includeNull: false);
        if (figOp.AllLiteral is { } literal1) return LiteralImplied(literal1);
        // ── A literal written in any of the six quoted spellings (§8.3.3.2 / §8.3.3.4 / §8.3.3.5, plain and
        // hexadecimal). SR9's own arms, read straight off the literal.
        if (CobolLiteral.IsStringLiteral(raw)) return LiteralImplied(raw);

        // ── §8.3.3.6.2 Formats 1-5, the keyword figuratives. The optional ALL word is part of each of those
        // formats (only Format 6 underlines ALL as required), so `ALL SPACES` is Format 2 and §8.3.3.6.4 GR3 b)
        // — "When a figurative constant is other than ALL literal-1, the length of the string is one character"
        // — gives it length ONE, exactly as the bare word. The parse-tree GetText concatenates the two words, so
        // the operand arrives spelled "ALLSPACES"; the classifier above accepts both spellings.
        if (figOp.Kind is not { } kind) return null;
        // §8.3.3.6.4 GR1 — "When a figurative constant is used in a context requiring national characters, the
        // figurative constant represents a national character value. Otherwise … an alphanumeric character
        // value." GR4 adds the third form, and ONLY for the ZERO format ('Z'): "The zero format represents the
        // numeric value '0', one or more of the boolean character '0', or one or more of the character '0' …
        // depending on context." §13.18.60.3 SR5 makes usage bit the context whose picture "describes a boolean
        // data item"; Formats 2-5 (SPACE / HIGH-VALUE / LOW-VALUE / QUOTE) have no boolean representation in
        // §8.3.3.6.4 GR5-GR8, so `01 A USAGE BIT VALUE SPACE.` implies X(1) and draws SR5's refusal from the
        // picture screen — the correct verdict, reported once.
        LiteralClass cls = context is Usage.National ? LiteralClass.National
            : context is Usage.Bit && kind is 'Z' ? LiteralClass.Boolean
            : LiteralClass.Alphanumeric;
        return new ImpliedPicture(cls, 1);

        static ImpliedPicture? LiteralImplied(string lit)
        {
            if (CobolLiteral.ClassOf(lit) is not { } cls) return null;
            // §13.16.3 SR9's own exclusion, structural per §8.3.3.1 ("If the opening and closing delimiters are
            // contiguous, the length of the literal is zero"). An ILL-FORMED hexadecimal literal also decodes to
            // zero characters (CobolLiteral.HexGroupViolation has already reported it); a `PICTURE X(0)` is no
            // picture at all, so both fall to SR8 rather than synthesizing a zero-width item.
            if (CobolLiteral.IsZeroLength(lit)) return null;
            int length = CobolLiteral.Decode(lit).Length;
            return length == 0 ? null : new ImpliedPicture(cls, length);
        }
    }
}
