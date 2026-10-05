// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

/// <summary>
/// ⛔ <b>THE PICTURE-PLACEMENT INVARIANT — both directions of it, in ONE pass over the finished forest.</b>
///
/// <para><b>ISO §13.16.3 SR8</b>, its last sentence: "For any other entry describing an elementary item, a
/// PICTURE clause shall be specified except as indicated in Syntax rule 9." — <i>elementary ⇒ has a
/// PICTURE</i>, the CLOSING GUARD that guarantees no elementary item ever reaches code generation without a
/// <see cref="DataItem.Pic"/>.</para>
///
/// <para><b>ISO §13.18.40.3 SR1</b>, its whole text: "The PICTURE clause may be specified only at the
/// elementary level." — <i>has a PICTURE ⇒ elementary</i>, the converse, added by kb/Work PB527. The two are
/// one rule about one thing (WHERE a PICTURE clause may stand) and they need the same fact to be decidable —
/// whether the entry has subordinates — which is why they live in one pass and share
/// <see cref="HasNoSubordinates"/>. Neither can be asked at entry bind: the subordinate entries are not parsed
/// when <c>BindEntry</c> runs, and <c>PictureAnalyzer.Analyze</c> has no view of the tree at all.</para>
///
/// <para><b>Why this exists (kb/Work PB487).</b> The rule was enforced only for the two usages that had a
/// dedicated <see cref="PicPending"/> mark, NATIONAL and BIT. Every other picture-less elementary entry escaped
/// with <c>Pic == null</c> and CRASHED the compiler — an unhandled <c>System.NullReferenceException</c> in
/// <c>MoveEmitter.ConvertSource</c>, or a raw Roslyn <c>CS0103</c> the user cannot act on. Measured on plain
/// COBOL with no exotic clause in it at all:
/// <code>
///   01 M.
///   01 N PIC X(4).
///   …  MOVE "ABCD" TO M
/// </code>
/// M is elementary (nothing is subordinate to it) and has no PICTURE, so it is SR8-nonconforming; the compiler
/// answered with a stack trace and no diagnostic. It is the same hole the PB487 catch-all reached through
/// <c>01 M MESSAGE-TAG.</c>, but it is reachable WITHOUT the catch-all, which is why closing the §13.16.2 clause
/// list does not close it.
///
/// <para><b>Where the picture-less usages are handled instead.</b> §13.16.3 SR8's own first sentence exempts
/// binary-char/short/long/double, float-short/long/extended, index, message-tag, object reference, pointer,
/// function-pointer and program-pointer — for those <c>BindEntry</c> SYNTHESIZES a <see cref="PicInfo"/>, and
/// SR9's VALUE-implied PICTURE arrives with one too, synthesized by <c>SynthesizeImpliedPictures</c> two passes
/// earlier (kb/Work PB504/PB831; <c>DataBinder.ImpliedPicture.cs</c>). So the test here is simply "elementary and
/// still no Pic after every synthesis and inheritance pass has run", which is exactly the population SR8 governs
/// and needs no second copy of the exemption list — and no carve-out of any kind — to maintain.</para>
///
/// <para><b>Two forests, deliberately.</b> The DIAGNOSTIC is reported over <see cref="ConformanceForest"/> so a
/// TYPEDEF template is named once rather than once per <c>TYPE</c> reference site — the entry the programmer
/// must change. The RECOVERY shape is then applied over <see cref="CompositionForest"/>, because
/// <c>ExpandTypes</c> has already cloned the broken template and a clone would otherwise carry the null into
/// codegen with its own diagnostic suppressed. Same rule, one message, no escape.</para>
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>§13.16.3 SR8 — every elementary item has a PICTURE or a synthesized profile by now.
    /// <para>Runs immediately after <c>UsageInheritancePass</c>, which is the last pass that can legitimately
    /// FILL a null <c>Pic</c> (a group header shedding its usage to leaves, §13.18.60.4 GR1; a PICTURE-less
    /// INDEX or OBJECT REFERENCE leaf taking the inherited profile). Anything still null at this point is a
    /// declaration defect, not a pending adjudication.</para></summary>
    internal void CheckPictureRequired()
    {
        CheckElementaryOnlyClauses();   // §13.18.40.3 SR1 + §13.16.3 SR11 — the table's ElementaryOnly rows
        foreach (var item in ConformanceForest())
        {
            if (!IsPictureLessLeaf(item)) continue;
            // ⛔ NO CARVE-OUT, AND THE ABSENCE IS THE POINT (kb/Work PB504/PB831). SR8 defers to SR9 in its own
            // words — "except as indicated in Syntax rule 9" — and SR9 is APPLIED, not excused: the
            // SynthesizeImpliedPictures pass gives every entry inside SR9's grant the PICTURE the rule implies
            // (DataBinder.ImpliedPicture.cs), so such an entry is no longer a picture-less leaf and this guard
            // never sees it. The FIGURATIVE case rides the same synthesis (§8.3.3.1 makes a figurative constant a
            // literal; §8.3.3.6.4 GR3 b/c supply the length SR9 takes "as specified in 8.3.3"), so the former
            // exemption — which let `01 G VALUE ALL "AB".` fall to the ONE-CHARACTER recovery item and store "A",
            // a silent wrong answer — is gone rather than widened. What remains here is exactly SR8's own
            // population: a numeric literal VALUE (`01 F VALUE 42.`), the class-pointer figurative NULL, a
            // zero-length literal (SR9 excludes it by name), a Format-2 table VALUE (SR9 grants only the
            // data-item format), and an entry with no VALUE clause at all.
            using var _ = Edition.At(item);
            Edition.Error(DiagnosticCatalog.UsageClauseCompatibility,
                $"data item '{item.CobolName ?? "FILLER"}': a PICTURE clause shall be specified for an elementary "
                + "item (ISO §13.16.3 SR8) — it may be omitted only for an item whose usage is "
                + "binary-char, binary-short, binary-long, binary-double, float-short, float-long, "
                + "float-extended, index, message-tag, object reference, pointer, function-pointer or "
                + "program-pointer, or when a VALUE clause supplies an alphanumeric, boolean or national "
                + "literal (SR9)");
        }

        // The recovery shape, over the COMPOSED forest so a TYPE/SAME AS clone of a defective template cannot
        // carry a null Pic into the emitter behind an already-reported diagnostic. Errors have been raised, so
        // the compile HALTS before codegen either way — this is the belt to that braces, and it is what makes
        // "no elementary item reaches the emitter without a Pic" an INVARIANT rather than a hope.
        foreach (var item in CompositionForest())
            if (IsPictureLessLeaf(item))
                item.Pic = PicInfo.Recovery();
    }

    /// <summary>An entry that is NEITHER a group NOR elementary: no PICTURE and no subordinate entries.
    /// <para>⛔ The test cannot be <see cref="DataItem.IsElementary"/>, which is DEFINED as
    /// <c>Pic is not null</c> — asking it about a picture-less item is vacuously false and the guard would never
    /// fire (measured: the first cut of this pass was a no-op for exactly that reason). “Elementary” in
    /// §13.16.3 SR8's sense is a property of the SOURCE — §8.5.1.3.1 makes an entry with nothing subordinate to it
    /// elementary whether or not it has a PICTURE — so the predicate is the CHILD COUNT.</para>
    /// <para>Levels 66 (RENAMES) and 88 (condition-name) are not data description entries in Format 1's sense
    /// (§13.16.2 Formats 2–4) and never carry a PICTURE, so they are excluded by level.</para>
    /// <para>An UNEXPANDED <c>TYPE</c> or <c>SAME AS</c> reference is excluded too, and by the rule's own words:
    /// SR8 governs "any other entry DESCRIBING an elementary item", and such an entry describes nothing —
    /// §13.18.57.4 GR1 and §13.18.49.4 GR1/GR2 give it the REFERENCED entry's description. The exclusion bites for
    /// the §13.16.3 SR9 synthesis (<c>SynthesizeImpliedPictures</c>), which runs BEFORE <c>ExpandTypes</c>, so a
    /// TYPE subject's own VALUE never implies a PICTURE of its own (<c>01 W TYPE U VALUE "QRSTUV".</c> takes U's
    /// size). By the time this guard runs, every reference — a TYPEDEF template member's included, since each
    /// declaration is completed as a declaration (kb/Work PB1302) — is expanded, or refused and given a recovery
    /// profile.</para></summary>
    private static bool IsPictureLessLeaf(DataItem item) =>
        item.Pic is null && HasNoSubordinates(item) && item.Level is not (66 or 88)
        && item.TypeRefName is null && item.SameAsName is null;

    /// <summary>⛔ §8.5.1.3.1'S OWN TEST, and the ONE place either direction of the picture-placement rule asks
    /// it: "The most basic subdivisions of a record, that is, those not further subdivided, are called
    /// elementary items." An entry is elementary IN THE SOURCE'S SENSE exactly when nothing is subordinate to
    /// it — a property of the written hierarchy, never of <see cref="DataItem.IsElementary"/>, which is DEFINED
    /// as <c>Pic is not null</c> and would make both guards circular (the SR1 direction is
    /// <c>CheckElementaryOnlyClauses</c>, DataBinder.ClausePlacement.cs, which JUSTIFIED and BLANK WHEN ZERO share).</summary>
    private static bool HasNoSubordinates(DataItem item) => item.Children.Count == 0;
}
