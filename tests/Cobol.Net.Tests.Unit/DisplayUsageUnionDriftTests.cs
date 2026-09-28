// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE HAND-ROLLED DISPLAY-ONLY UNION INVENTORY (kb/Work PB164, the Step D review).
/// <para>
/// <c>{ Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display }</c> is the shape a predicate takes
/// when it was written before the byte forms existed. Sometimes it is exactly right — the question really IS
/// "is this a zoned USAGE DISPLAY item?" — and sometimes it is DRIFT: the question is "does this leaf have a
/// character image?", which <see cref="CobolNet.Binding.Model.DataItem.ElementImageCapable"/> and
/// <c>PicInfo.HasImageByteForm</c> answer for every consumer, and which every numeric usage now satisfies
/// (V59 pinned BINARY/PACKED, PB164 wave 1 COMP-5/BINARY-*, wave 2 the IEEE float family, R40 USAGE INDEX).
/// </para>
/// <para>
/// The drift instance that motivated this test: <c>UdfBinder.UdfReturningResidue</c> screened a GROUP RETURNING
/// item's leaves with this union and rejected a COMP-5 leaf as having "no shared character image across the
/// activation boundary" — years after the group codec started emitting exactly that image. It rejected
/// conforming source (ISO §14.2.2 SR5 places NO category restriction on a RETURNING item), and nothing failed,
/// because a hand-rolled union has no drift lock. <c>V59ImagePredicateDriftTests</c> watches
/// <c>IsCharacterImage</c>; NOTHING watched this shape, and it is the more common one.
/// </para>
/// <para>
/// ⛔ THESE SITES ARE NOT AUTOMATICALLY BUGS — most are load-bearing, several are SPEC-REQUIRED, and the test
/// deliberately does not prejudge them. Its job is to make the inventory EXPLICIT so a new copy cannot appear
/// silently and a removed one records a deliberate decision. Each entry's classification is in the comment
/// beside it; the reasons were MEASURED, not assumed (a probe per claim — see the kb note).
/// </para>
/// <para>
/// ⛔ TWO COUNTS PER FILE, NOT ONE (kb/Work PB646). Once §13.18.60.3 SR12's national-form numeric shapes went
/// live there are TWO shapes spelled with this text and they are DIFFERENT QUESTIONS: the DISPLAY-ONLY union,
/// which is the drift-prone one, and the <c>Usage.Display or Usage.National</c> PAIR, which is what the standard
/// itself writes wherever it admits a numeric item by usage (§8.4.3.3.3 SR1, §13.18.52.3 SR2, §14.9.48.3 SR4).
/// A single total would have let a site move between them silently — which is exactly the migration this landing
/// performed. So the inventory states both, and the IMAGE-FORM question has left this text entirely: it is
/// <c>PicInfo.IsCharacterFormNumeric</c>, one definition, watched by its own drift test. A site that means "does
/// this leaf have a run of digit CHARACTERS?" belongs there and should appear in NEITHER column here.
/// </para>
/// </summary>
public sealed class DisplayUsageUnionDriftTests
{
    /// <summary>⛔ THE ONE comment stripper both readers below use — these predicates are discussed at length in
    /// comments, so a raw text count is a false positive. Drops <c>///</c> doc comments and whole-line
    /// <c>//</c> comments, and cuts trailing <c>//</c> tails, exactly as <see cref="V59ImagePredicateDriftTests"/>
    /// does. Two copies of this heuristic in one file would let the INVENTORY and the single-file assertion
    /// disagree about what "code" is, which is the very failure mode the file exists to catch.</summary>
    private static string CodeText(string src)
    {
        var code = new List<string>();
        foreach (string raw in src.Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            string t = line.TrimStart();
            if (t.StartsWith("///") || t.StartsWith("//")) continue;
            int i = line.IndexOf("//", StringComparison.Ordinal);
            code.Add(i >= 0 ? line[..i] : line);
        }
        return string.Join("\n", code);
    }

    private static int CodeOccurrences(string src, string needle) =>
        Regex.Matches(CodeText(src), Regex.Escape(needle)).Count;

    /// <summary>Both spellings, told apart: <c>Usage: Usage.Display</c> followed by <c>or Usage.National</c> is
    /// the PAIR the standard writes; anything else is the DISPLAY-ONLY union.</summary>
    private static readonly Regex UnionShape = new(@"Usage: Usage\.Display(?<pair>\s+or\s+Usage\.National)?",
        RegexOptions.Compiled);

    private static (int DisplayOnly, int Pair) ClassifyOccurrences(string src)
    {
        int only = 0, pair = 0;
        foreach (Match m in UnionShape.Matches(CodeText(src)))
        {
            if (m.Groups["pair"].Success) pair++; else only++;
        }
        return (only, pair);
    }

    [Fact]
    public void HandRolledDisplayUnion_IsTheKnownInventory()
    {
        var expected = new Dictionary<string, (int DisplayOnly, int Pair)>
        {
            // KEEPER — the REPORT SECTION composes a printable CHARACTER image by nature (§13.18.x report
            // items); the question really is "is this a zoned DISPLAY field?". ⚠ Its own screen four lines
            // above refuses any non-DISPLAY printable item outright, and §13.18.60.3 SR7 — "Only the DISPLAY or
            // NATIONAL phrase may be specified in any USAGE clause associated with a report group item" — says
            // the pair; that refusal predates the national-form landing (it refuses a PIC N(3) report item too)
            // and is a REPORT-WRITER finding, not a predicate one. It is recorded in the PB646 report.
            ["Binding/DataBinder.Reports.cs"] = (1, 0),

            // DISPLAY-ONLY (2) — both are the CARRIAGE question, and deliberately NOT
            // PicInfo.IsCharacterFormNumeric's IMAGE-FORM one (kb/Work PB646; each site says so):
            //  MarkImageLeaves — which leaves must be STORED AS their image. Promotion pins the carrier at
            //        ImageWidth CHARACTER positions, a DISPLAY leaf's whole storage and HALF a national one's
            //        (D-N1), and promoting a national-form numeric leaf was MEASURED to corrupt a plain
            //        group-to-group MOVE between two of them.
            //  the Tier-B StringCanonical split — the DISPLAY arm marks through the CHARACTER pipeline and the
            //        very next arm takes the national-form leaf through Place.NationalWindow. Both arms
            //        present: the deliberate two-lane split, not a missing lane.
            // PAIR (1) — ApplyEffectiveSign, the ONE site at which an item acquires an ancestor's SIGN clause
            //        (§13.18.52.4 GR1 inheritance and the §13.18.49.4 GR5 SAME-AS transform both call it), and
            //        §13.18.52.3 SR2 names exactly that pair: "The usage of an elementary item for which the
            //        SIGN clause is specified shall be display or national".
            //
            // ⛔ WAS 7, THEN 4. THREE WERE DELETED by kb/Work PB495 — the USAGE half of §13.18.60.4 GR1 group
            // inheritance, each asking "is this leaf still at the default DISPLAY usage?" beside its own
            // hand-written set of inheritable usages, which is the WRONG question (GR1 applies a group's clause
            // to EVERY elementary item under it); they became ONE derivation feeding ONE screen
            // (DataBinder.ApplyEffectiveUsage → PictureAnalyzer.ScreenUsageAgainstPicture), drift-tested over
            // the whole Usage enum by UsageInheritanceDriftTests. ⛔ THE FOURTH WENT THE SAME WAY with PB646:
            // ExpandSameAs carried a SECOND copy of the SIGN transform and kept a DISPLAY-only guard after the
            // pair landed, so `01 TN USAGE NATIONAL SIGN IS LEADING SEPARATE. 05 TNE PIC S9(3). 01 SNX SAME AS
            // TNE.` MEASURED LENGTH 3 / BYTE-LENGTH 6 / [04N] against the inheritance route's 4 / 8 / [-045].
            // One method (ApplyEffectiveSign), one guard, and that is the remaining PAIR count below.
            ["Binding/DataBinder.cs"] = (2, 1),

            // PAIR — SPEC-REQUIRED, and it is the spec's own pair. §8.4.3.3.3 SR1 admits reference modification
            // of "a numeric data item of usage display or national that is not subordinate to a strongly-typed
            // group item", so this site enforces the standard. MEASURED: `MOVE NB(1:1) TO X` over a COMP-5 item
            // draws COBOLNET1647 citing that rule. ⛔ WAS 4 DISPLAY-ONLY: the other three asked the IMAGE-FORM
            // question (the RefMod view's wrap gate and the two RENAMES span arms) and now read
            // PicInfo.IsCharacterFormNumeric — before PB646 the view's gate named DISPLAY while the exclusion
            // above it already admitted the pair, so `RM(2:3)` over a national-form numeric passed the
            // exclusion and fell to a Tier-C runtime loud.
            ["Binding/ReferenceResolver.cs"] = (0, 1),

            // (Binding/Passes/StorageFormPass.cs LEFT THE INVENTORY — kb/Work PB1466.) Its last display-only site,
            //      the whole-group / character-channel promotion (IsImagePromotable), now asks
            //      PicInfo.HasImageByteForm minus the national-form numeric (PB646's carriage exclusion) and USAGE
            //      INDEX: a BINARY or PACKED leaf a group MOVE, a READ or a BY REFERENCE crossing reaches holds
            //      content its native carrier cannot, and the numeric class condition (§8.8.4.4.4 GR3 n) 1. c.)
            //      must read that content. UnifyCrossing had left with kb/Work PB187 on the same predicate.

            // DISPLAY-ONLY (2), both CARRIAGE (MarkImageForced):
            //  the figurative pass — a NON-DIGIT figurative fill deposits fill CHARACTERS into the receiver's
            //      cells ("MOVE QUOTE TO PIC 9(3) leaves three quotation marks"); a 2-byte binary carrier has
            //      no character cells to hold them.
            //  MarkRefModStoreImage — ⛔ NOT the §8.4.3.3.3 SR1 screen (that is ReferenceResolver's, above):
            //      this one FORCES the underlying item's carriage to its image so a ref-mod store can write
            //      character cells, which is the promotion question and stays DISPLAY-only for the same reason.
            ["Binding/Procedure/Verbs/MoveBinder.cs"] = (2, 0),

            // PAIR — SPEC-REQUIRED, verbatim. §14.9.48.3 SR4: "Identifier-4 shall be described implicitly or
            // explicitly as usage display and category alphabetic, alphanumeric, or numeric; or as usage
            // national and category national or numeric." A COMP/packed/COMP-5/index/float receiver is
            // rejected BY THE STANDARD.
            ["Binding/Procedure/Verbs/StringUnstringBinder.cs"] = (0, 1),

            // PAIR — SPEC-REQUIRED, verbatim (kb/Work PB1468). §8.8.4.2.5: "The numeric integer operand shall be an
            // integer literal or an integer numeric data item of usage display or national." A COMP/packed/float
            // item against an alphanumeric operand is not §8.8.4.2.1 item 6's pair — refused COBOLNET2532.
            ["Binding/RelationComparability.cs"] = (0, 1),

            // KEEPER — D-U6a's UNIVERSAL box bridge: OoUnivImageBridged asks whether the canonical box is the ZONED
            // display image over a NATIVE local, which is genuinely the DISPLAY-only question. Its twin for every
            // other byte form (native box over an IMAGE local) is OoUnivNativeBoxOverImage, which reads
            // HasImageByteForm — kb/Work PB187.
            ["CodeGen/Verbs/OoEmitter.cs"] = (1, 0),

            // ⛔ TWO FILES LEFT THIS INVENTORY WITH kb/Work PB646, both because the question they ask is the
            // IMAGE-FORM one and it now has a single definition:
            //  CodeGen/Verbs/StringEmitter.cs (was 1) — WriteImage's guard reads PicInfo.IsCharacterFormNumeric.
            //  CodeGen/DataDivision/GroupValueSlicer.cs (was 2) — DistributableSubtree and SliceInit read it
            //      too, and that was a LIVE silent wrong answer rather than a tidy-up: §13.18.63.3 SR14's
            //      usage-DISPLAY requirement is written about items "subordinate to an alphanumeric group
            //      item" and does not reach a GROUP-USAGE NATIONAL group, whose subordinates §13.18.29.3 SR3
            //      requires to be usage national and contemplates as numeric by name. MEASURED:
            //      `01 NG GROUP-USAGE NATIONAL VALUE N"AB123". 05 NGA PIC N(2). 05 NGB PIC 9(3).` deposited
            //      NOTHING — `NGA=[  ] NGB=[000]` — where its alphanumeric twin gives `[AB]` / `[123]`.
            //      (PB184's separate GR5 question — whether a group VALUE should reach BYTE-form leaves at
            //      all — is untouched and still open.)
        };

        var actual = new Dictionary<string, (int DisplayOnly, int Pair)>();
        string root = TestRepo.Src("Cobol.Net.Compiler");
        foreach (string f in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(root, f).Replace('\\', '/');
            var counts = ClassifyOccurrences(File.ReadAllText(f));
            if (counts.DisplayOnly > 0 || counts.Pair > 0) actual[rel] = counts;
        }

        static string[] Render(Dictionary<string, (int DisplayOnly, int Pair)> d) => d.OrderBy(k => k.Key)
            .Select(k => $"{k.Key} display-only={k.Value.DisplayOnly} pair={k.Value.Pair}").ToArray();

        Assert.Equal(Render(expected), Render(actual));
    }

    /// <summary>The drift instance itself, pinned so it cannot come back: the UDF group-RETURNING screen must
    /// ask the DERIVED image predicate. Its elementary arms keep their own spec-required rejections
    /// (§13.18.60.3 SR10 permits an index item in a USING phrase but not as a RETURNING item; the
    /// pointer/object refusals), which is why this pins the GROUP screen specifically.</summary>
    [Fact]
    public void UdfReturningResidue_GroupScreen_UsesTheDerivedImagePredicate()
    {
        string src = File.ReadAllText(TestRepo.At("src", "Cobol.Net.Compiler", "Binding", "Procedure", "Verbs",
                                                  "UdfBinder.cs"));
        Assert.Equal(0, CodeOccurrences(src, "Usage: Usage.Display"));
        Assert.True(CodeOccurrences(src, "ElementImageCapable") >= 1,
            "UdfReturningResidue's group-leaf screen must ask DataItem.ElementImageCapable — a group RETURNING "
            + "item whose leaves all have a byte image crosses the activation boundary through the group codec "
            + "(ISO §14.2.2 SR5 places no category restriction on a RETURNING item). A hand-rolled usage union "
            + "here rejected conforming source once already.");
    }
}
