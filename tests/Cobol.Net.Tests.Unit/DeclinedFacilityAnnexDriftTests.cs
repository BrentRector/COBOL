// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The drift guard for the DECLINED-FACILITY POSTURE — that every diagnostic which declines a language element
/// cites the clause of the annex the element is actually listed in, and that the annex it claims agrees with the
/// user documentation ISO §4.2.6 and §4.2.7 both oblige (<c>docs/CONFORMANCE.md</c>).
///
/// <para>⛔ WHY THIS EXISTS (kb/Work PB709). §4.2.6 governs Annex A.3, PROCESSOR-DEPENDENT elements, and carries
/// two sentences nothing else in the standard carries — a MANDATORY compile-time warning mechanism, and an express
/// licence not to "diagnose syntax errors within this unsupported syntax". §4.2.7 governs Annex A.4, OPTIONAL
/// elements, and has NEITHER: it makes the element one an implementor "may, but need not, implement" and requires
/// only that the claim be identified in user documentation, after which A.4.1 admits the syntax "only when support
/// for that language element is claimed by the implementor". The two licences reach OPPOSITE postures — accept and
/// warn versus refuse by name — so which clause a decline cites is a normative fact about it, not a reference.
/// §4.2.6 was nevertheless written at four Annex-A.4 sites, and NOTHING could see it: the clause exists, the quoted
/// text is genuinely in the standard, and <c>scripts/spec/audit_code_citations.py --check</c> therefore passes. A
/// real citation answering a different question is invisible to every mechanical check the repo had.</para>
///
/// <para>⛔ AND WHY IT IS A TEST AND NOT A CORRECTED COMMENT. Correcting the four sites fixes those four. The
/// annex is now DATA on the descriptor (<see cref="DiagnosticDescriptor.Annex"/>) and the licensing clause is
/// DERIVED from it (<see cref="DiagnosticDescriptor.PostureClause"/>), so the next declined facility cannot get it
/// wrong silently — these facts are what make "derived" true rather than aspirational (CLAUDE.md rule 5: prefer
/// the shape that makes the NEXT case automatic, and pair it with a drift test so it stays true).</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class DeclinedFacilityAnnexDriftTests
{
    /// <summary>The §2 register — one row per Annex A.3 processor-dependent item.</summary>
    private const string AnnexA3Heading = "## 2. Annex A.3";

    /// <summary>The §5 register — one row per Annex A.4 optional module.</summary>
    private const string AnnexA4Heading = "## 5. Annex A.4";

    /// <summary>A citation of ISO §4.2.6 or §4.2.7 that is the WHOLE clause number: the trailing lookahead keeps
    /// <c>§4.2.6</c> from matching inside a longer path, and keeps <c>§4.2.16</c> (the conformance-claim clause,
    /// which several of these Titles legitimately mention) out of the §4.2.1 family entirely.</summary>
    private static Regex Cites(string clause) =>
        new(Regex.Escape("§" + clause) + @"(?![0-9.])", RegexOptions.CultureInvariant);

    /// <summary>Every declined-facility descriptor — the ones that carry an annex, i.e. the ones whose posture
    /// this class is about.</summary>
    private static IReadOnlyList<DiagnosticDescriptor> Declined() =>
        DiagnosticCatalog.All.Where(d => d.Annex != DeclinedAnnex.None).ToList();

    /// <summary>Every <c>COBOLNET####</c> code named inside one section of <c>docs/CONFORMANCE.md</c>, read from
    /// the heading up to the next <c>## </c>. The registers are markdown, so this is a scan and not a parse —
    /// but it is a scan of the DOCUMENT, which is what §4.2.6 and §4.2.7 make the normative artifact.</summary>
    private static HashSet<string> CodesUnder(string heading)
    {
        var codes = new HashSet<string>(StringComparer.Ordinal);
        bool inSection = false;
        foreach (string line in File.ReadAllLines(TestRepo.Docs("CONFORMANCE.md")))
        {
            if (line.StartsWith(heading, StringComparison.Ordinal)) { inSection = true; continue; }
            if (!inSection) continue;
            if (line.StartsWith("## ", StringComparison.Ordinal)) break;
            foreach (Match m in Regex.Matches(line, @"COBOLNET[0-9]{4}"))
                codes.Add(m.Value);
        }
        return codes;
    }

    /// <summary>The population assertion (feedback_verdict_evidence_invariant). A decline band that parsed to
    /// nothing — a renamed heading, a descriptor set that lost its annex argument — would make every obligation
    /// below vacuously green, which is exactly the failure this class exists to stop. Eleven descriptors carried
    /// the datum when it was introduced; the floor is deliberately below that so ADDING one is never a red, while
    /// LOSING the band still is.</summary>
    [Fact]
    public void TheDeclinedBand_AndBothRegisters_AreNonEmpty()
    {
        Assert.True(Declined().Count >= 11,
            $"only {Declined().Count} descriptor(s) carry a DeclinedAnnex — the declined band has lost its annex "
            + "datum and every check in this class would pass without looking at anything");
        Assert.NotEmpty(CodesUnder(AnnexA3Heading));
        Assert.NotEmpty(CodesUnder(AnnexA4Heading));
    }

    /// <summary>⛔ THE OBLIGATION, ARM 1. A descriptor's own text cites the clause of an annex it is not in.
    /// This is the shape kb/Work PB709 was opened for: §4.2.6 at an A.4-only site reads "we could not implement
    /// this", where §4.2.7 reads "we chose not to, and documented it" — and only the second is the posture
    /// <c>docs/CONFORMANCE.md</c> §5 actually takes. A facility listed in BOTH annexes (commit and rollback is
    /// A.3 items 6-7 AND A.4.3) may cite either.</summary>
    [Fact]
    public void NoDeclinedDescriptor_CitesTheOtherAnnexesClause()
    {
        var wrong = new List<string>();
        foreach (var d in Declined())
        {
            string text = d.Title + " " + d.IsoSection;
            if (!d.Annex.HasFlag(DeclinedAnnex.A3) && Cites("4.2.6").IsMatch(text))
                wrong.Add($"{d.Code} ({d.Id}) is Annex {d.Annex} — an OPTIONAL element — but cites §4.2.6, the "
                    + "PROCESSOR-DEPENDENT clause; §4.2.7 is the one that governs it");
            if (!d.Annex.HasFlag(DeclinedAnnex.A4) && Cites("4.2.7").IsMatch(text))
                wrong.Add($"{d.Code} ({d.Id}) is Annex {d.Annex} — a PROCESSOR-DEPENDENT element — but cites "
                    + "§4.2.7, the OPTIONAL-element clause; §4.2.6 is the one that governs it");
        }
        Assert.True(wrong.Count == 0,
            "declined-facility descriptor(s) citing the wrong annex's clause (kb/Work PB709):\n"
            + string.Join("\n", wrong));
    }

    /// <summary>⛔ THE OBLIGATION, ARM 2 — the datum and the text cannot drift apart in the other direction
    /// either. Every § in the DERIVED <see cref="DiagnosticDescriptor.PostureClause"/> appears in the
    /// descriptor's own <c>IsoSection</c>, so a reader who is handed the code can reach the licence.</summary>
    [Fact]
    public void EveryDeclinedDescriptor_CitesItsDerivedPostureClause()
    {
        var missing = new List<string>();
        foreach (var d in Declined())
        {
            Assert.NotNull(d.PostureClause);
            foreach (string clause in d.PostureClause!.Split(" / "))
                if (!Cites(clause.TrimStart('§')).IsMatch(d.IsoSection))
                    missing.Add($"{d.Code} ({d.Id}) is Annex {d.Annex}, whose licence is {clause}, but its "
                        + $"IsoSection '{d.IsoSection}' does not cite it");
        }
        Assert.True(missing.Count == 0,
            "declined-facility descriptor(s) whose IsoSection omits the clause their annex derives:\n"
            + string.Join("\n", missing));
    }

    /// <summary>⛔ THE OBLIGATION, ARM 3 — the datum agrees with the USER DOCUMENTATION, which is what the
    /// standard actually obliges. §4.2.6 requires the absence of a processor-dependent element to be "specified
    /// in the implementor's user documentation" (<c>docs/CONFORMANCE.md</c> §2) and §4.2.7 requires the optional
    /// elements claimed to be identified there (§5). So a code named in §2 declines an A.3 element and a code
    /// named in §5 declines an A.4 one — and the descriptor has to say the same thing.</summary>
    [Fact]
    public void EveryDeclinedDescriptor_AgreesWithTheConformanceRegisters()
    {
        var a3 = CodesUnder(AnnexA3Heading);
        var a4 = CodesUnder(AnnexA4Heading);
        var disagree = new List<string>();
        foreach (var d in Declined())
        {
            if (a3.Contains(d.Code) && !d.Annex.HasFlag(DeclinedAnnex.A3))
                disagree.Add($"{d.Code} ({d.Id}) is named in docs/CONFORMANCE.md §2, the Annex A.3 register, "
                    + $"but its descriptor claims Annex {d.Annex}");
            if (a4.Contains(d.Code) && !d.Annex.HasFlag(DeclinedAnnex.A4))
                disagree.Add($"{d.Code} ({d.Id}) is named in docs/CONFORMANCE.md §5, the Annex A.4 register, "
                    + $"but its descriptor claims Annex {d.Annex}");
        }
        Assert.True(disagree.Count == 0,
            "declined-facility descriptor(s) whose annex contradicts the conformance registers:\n"
            + string.Join("\n", disagree));
    }

    /// <summary>Prove the guards can FAIL (feedback_green_gates_arent_evidence). Run the two predicates against
    /// FABRICATED descriptors rather than by mutating the catalog, and pin the negative side too — a correctly
    /// cited descriptor must not trip them, or the guard would be unusable and get deleted. The A.4-with-§4.2.6
    /// case is the one PB709 actually found in the tree.</summary>
    [Fact]
    public void TheGuards_CanFail_AndDoNotFireOnCorrectCitations()
    {
        var a4CitingA3 = new DiagnosticDescriptor("COBOLNET9998", "fabricated-a4", EditionSeverity.Error,
            "an OPTIONAL element (§4.2.6) that is not supported", "ISO §4.2.6 / Annex A.4.14",
            Annex: DeclinedAnnex.A4);
        var a3CitingA4 = new DiagnosticDescriptor("COBOLNET9997", "fabricated-a3", EditionSeverity.Warning,
            "a processor-dependent element (§4.2.7) that is not supported", "ISO §4.2.7 / Annex A.3 item 4",
            Annex: DeclinedAnnex.A3);
        Assert.Matches(Cites("4.2.6"), a4CitingA3.Title + " " + a4CitingA3.IsoSection);
        Assert.Matches(Cites("4.2.7"), a3CitingA4.Title + " " + a3CitingA4.IsoSection);
        Assert.Equal("§4.2.7", a4CitingA3.PostureClause);
        Assert.Equal("§4.2.6", a3CitingA4.PostureClause);
        Assert.DoesNotMatch(Cites("4.2.6"), a4CitingA3.PostureClause!);   // arm 2 would flag it too
        Assert.DoesNotMatch(Cites("4.2.7"), a3CitingA4.PostureClause!);

        var correct = new DiagnosticDescriptor("COBOLNET9996", "fabricated-ok", EditionSeverity.Error,
            "an OPTIONAL element (§4.2.7; Annex A.4.14), additionally obsolete (§4.2.13) and outside the "
            + "§4.2.16 claim", "ISO §4.2.7 / Annex A.4.1", Annex: DeclinedAnnex.A4);
        Assert.DoesNotMatch(Cites("4.2.6"), correct.Title + " " + correct.IsoSection);
        Assert.Matches(Cites("4.2.7"), correct.IsoSection);

        var both = new DiagnosticDescriptor("COBOLNET9995", "fabricated-both", EditionSeverity.Warning,
            "listed in both annexes", "ISO §4.2.6 ¶3 / §4.2.7 / Annex A.3 items 6-7 / Annex A.4.3",
            Annex: DeclinedAnnex.A3 | DeclinedAnnex.A4);
        Assert.Equal("§4.2.6 / §4.2.7", both.PostureClause);
        foreach (string clause in both.PostureClause!.Split(" / "))
            Assert.Matches(Cites(clause.TrimStart('§')), both.IsoSection);

        // A descriptor that is not a decline carries no clause at all — the None arm of the derivation.
        Assert.Null(new DiagnosticDescriptor("COBOLNET9994", "fabricated-none", EditionSeverity.Error,
            "not a decline", "ISO §8.1").PostureClause);
    }
}
