// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Editions;

/// <summary>WHERE a compiler directive may be written — the placement rules ISO §7.3 states per directive
/// (kb/Work PB1377, PB1378, PB1065). One value per RULE SHAPE; a directive row names the shape and quotes its clause,
/// so the stage that judges the shape is written once and a new directive with the same restriction is one
/// <c>constructs.json</c> field plus a regen, never another hand-written check.</summary>
public enum DirectivePlacementRule
{
    /// <summary>"shall not be specified within a compilation unit" — §7.3.17.3 SR1 (LEAP-SECOND), §7.3.21.3 SR1
    /// (PROPAGATE). A directive BETWEEN two sibling units, or before the first, is outside every unit and legal;
    /// judged on the parse tree's unit spans by <c>DirectivePlacementPass</c>.</summary>
    OutsideCompilationUnits,

    /// <summary>"may be specified only before the first IDENTIFICATION DIVISION within a compilation group" —
    /// §7.3.10.3 SR1 (COBOL-WORDS). Judged on the text, where the directive's own state boundary is, by
    /// <c>DirectiveSiteProcessor</c> (<c>CompilationUnitStart</c> is the one line-level unit-start test).</summary>
    BeforeFirstCompilationUnit,

    /// <summary>"shall be specified only between clauses … and between statements" — §7.3.14.3 SR1 (FLAG-02),
    /// §7.3.15.3 SR1 (FLAG-14), and the ALL form of PUSH/POP (§7.3.22.3 SR3, §7.3.20.3 SR3). Judged on the parse
    /// tree's clause and statement spans by <c>DirectivePlacementPass</c>.</summary>
    BetweenClauses,
}

/// <summary>
/// One directive row's placement rule, rendered from the row's <c>directivePlacement</c> object in
/// <c>tests/version-matrix/constructs.json</c>.
/// </summary>
/// <param name="Rule">Which rule shape.</param>
/// <param name="Citation">The ISO clause and syntax rule it comes from, e.g. <c>§7.3.17.3 SR1</c>.</param>
/// <param name="Text">The rule's own words, quoted in the diagnostic — a PUSH or POP that NAMES the directive
/// (§7.3.20.3 SR2, §7.3.22.3 SR2) quotes the named directive's rule here, not a paraphrase.</param>
public sealed record DirectivePlacement(DirectivePlacementRule Rule, string Citation, string Text);
