// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;
using CobolNet.Runtime;

/// <summary>
/// ⛔ THE ONE FUNNEL FOR RESOLVING A WRITTEN REPORT-GROUP REFERENCE (kb/Work PB365). THREE sites name a
/// report group by name — <c>GENERATE data-name-1</c> (ISO §14.9.16.3 SR1), <c>USE BEFORE REPORTING
/// identifier-1</c> (§14.9.49.3 SR9) and, since kb/Work PB482, the SUM clause's <c>UPON data-name-2</c>
/// (§13.18.54.3 SR7: "Data-name-2 shall be the name of a detail. It may be qualified only by a report-name" —
/// the DATA-division site, resolved in <c>DataBinder.ResolveReports</c> once every group is described) — and
/// before this funnel the two statement sites were written as
/// <c>foreach (report) if (report.Groups.FirstOrDefault(name-matches) is {} g) return g;</c>: a loop shape that
/// encodes <i>find one</i> where §8.4.2.2 requires <i>find exactly one</i>. With the same 01-level group name in
/// two report description entries, the reference bound to whichever RD was written first, silently — so a
/// GENERATE of the second report's group ran the OTHER report's declarative and nothing downstream noticed
/// (feedback_two_arm_dispatch: two arms, and neither one was right).
///
/// <para><b>The rule.</b> §8.4.2.2.1: "Identical user-defined names may be specified in a source unit; however,
/// uniqueness shall be established through qualification for each user-defined name explicitly referenced,
/// except as specified in rules 2 through 6" — none of exceptions 2–6 covers a report-group name. §8.4.2.2.3
/// SR1 restates it as the syntax rule: "For each non unique user-defined name that is explicitly referenced,
/// uniqueness shall be established through a sequence of qualifiers that precludes any ambiguity of reference."
/// The available qualifier is the report-name (§8.4.2.2.2 Format 1's file-report-qualifier; §14.9.16.3 SR1 says
/// so in words: "It may be qualified by a report-name"), which is why the qualified spelling exists at all.
/// §8.4.2.2.3 SR3 makes IN and OF equivalent — the grammar's <c>reportGroupReference</c> accepts both.</para>
///
/// <para><b>Why the ambiguity is diagnosed HERE and not at the call sites.</b> One rule, one place. The callers
/// differ only in what a MISS means (GENERATE falls back to the report-name form for summary reporting, SR2;
/// USE and the SUM clause's UPON operand have no fallback), so <see cref="Resolve"/> owns Found/Ambiguous and
/// hands <c>None</c> back for the caller's own SR. On Ambiguous the first candidate is still returned so the bind can continue without a
/// cascade — the diagnostic has already fired.</para>
/// </summary>
internal static class ReportGroupResolution
{
    /// <summary>The outcome of a report-group lookup. <c>Ambiguous</c> is ALREADY DIAGNOSED (COBOLNET1920) and
    /// carries the first candidate for error recovery; <c>None</c> carries nothing and leaves the "this is not a
    /// report group" diagnostic to the caller, whose syntax rule differs per statement.</summary>
    public enum Match { Found, Ambiguous, None }

    /// <summary>Split a <c>reportGroupReference</c> parse context into (head, qualifier). The qualifier is the
    /// <c>IN/OF report-name-1</c> tail (§8.4.2.2.2 Format 1's file-report-qualifier), null when unqualified.
    /// ⚠ Read from the context's CHILDREN, never <c>GetText()</c> of the whole context — that concatenates
    /// <c>DET-A OF R-B</c> into an unmatchable key (the same trap <c>ResolveProcedure</c> documents).</summary>
    public static (string Head, string? Qualifier) Parts(Core.ReportGroupReferenceContext r) =>
        (r.cobolWord().GetText(), r.reportName()?.GetText());

    /// <summary>Resolve a possibly-qualified report-group reference against the unit's report models. Collects
    /// EVERY candidate (a qualified reference restricts the candidate reports to the named one) and reports
    /// COBOLNET1920 when more than one survives — §8.4.2.2.1 / §8.4.2.2.3 SR1.</summary>
    /// <param name="where">The reference site for the diagnostic, e.g. <c>"GENERATE 'DET-A'"</c>.</param>
    /// <param name="scope">The procedure division's binder when <paramref name="reports"/> includes GLOBAL reports
    /// inherited from containers: candidates from more than one source element are narrowed to the NEAREST one
    /// before ambiguity is asked (§8.4.6.2.1 rule 3 — <see cref="DataBinder.NearestInScope"/>). Null for the
    /// data-division site, whose reports are all the unit's own.</param>
    public static Match Resolve(EditionContext edition, IReadOnlyList<ReportModel> reports,
        string head, string? qualifier, string where, out ReportModel? report, out ReportGroupModel? group,
        DataBinder? scope = null)
    {
        report = null;
        group = null;
        var found = new List<(ReportModel Report, ReportGroupModel Group)>();
        foreach (var r in reports)
        {
            if (qualifier is not null && !CobolNames.Same(r.Name, qualifier)) continue;
            foreach (var g in r.Groups)
                if (g.Name is not null && CobolNames.Same(head, g.Name))
                    found.Add((r, g));
        }
        if (scope is not null) found = scope.NearestInScope(found, f => f.Report);
        if (found.Count == 0) return Match.None;
        (report, group) = found[0];
        if (found.Count == 1) return Match.Found;
        var extra = found.Skip(1);

        // §8.4.2.2.3 SR1 — the reference does not preclude ambiguity. Name every report that carries the group
        // so the fix (which qualifier to write) is in the message.
        string owners = string.Join(", ", new[] { report.Name }
            .Concat(extra.Select(e => e.Report.Name)).Select(n => $"'{n}'"));
        edition.Error(DiagnosticCatalog.ReportGroupReferenceAmbiguous, $"{where}: report group '{head}' is "
            + $"described in more than one report description entry ({owners}) — qualify the reference "
            + $"(e.g. '{head} OF {report.Name}'), since uniqueness shall be established through qualification "
            + "(ISO §8.4.2.2.1 / §8.4.2.2.3 SR1)");
        return Match.Ambiguous;
    }
}
