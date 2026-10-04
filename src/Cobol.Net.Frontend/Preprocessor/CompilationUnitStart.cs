// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// ⛔ THE ONE LINE-LEVEL ANSWER TO "DOES A COMPILATION UNIT START HERE?" for the text-level directive stages that must
/// know where the first unit begins — the §7.3.10.3 SR1 COBOL-WORDS placement rule ("may be specified only before the
/// first IDENTIFICATION DIVISION within a compilation group") and the §7.3.17.3 SR1 LEAP-SECOND placement rule.
/// <para>ISO §11.2.1 prints the division header in BRACKETS — <c>[ IDENTIFICATION DIVISION. ]</c> — so a unit's
/// identification division may begin on its PROGRAM-ID, FUNCTION-ID, CLASS-ID or INTERFACE-ID paragraph with no
/// header line at all (kb/Work PB829). The two stages each carried a private copy of this test, and they had
/// drifted: the COBOL-WORDS copy knew only the header, so a directive AFTER a header-less <c>PROGRAM-ID.</c> line
/// was accepted in silence. FACTORY, OBJECT and METHOD-ID are absent on purpose: they open units NESTED in a class
/// definition, which the CLASS-ID line has already started.</para>
/// </summary>
internal static class CompilationUnitStart
{
    /// <summary>True when <paramref name="trimmed"/> (a line with its leading blanks removed) opens a compilation
    /// unit's identification division — the header (or its <c>ID DIVISION</c> abbreviation), or, the header being
    /// optional, the unit's own first paragraph.</summary>
    public static bool IsAt(string trimmed)
        => CobolNames.StartsWith(trimmed, "IDENTIFICATION DIVISION")
        || CobolNames.StartsWith(trimmed, "ID DIVISION")
        || CobolNames.StartsWith(trimmed, "PROGRAM-ID")
        || CobolNames.StartsWith(trimmed, "CLASS-ID")
        || CobolNames.StartsWith(trimmed, "FUNCTION-ID")
        || CobolNames.StartsWith(trimmed, "INTERFACE-ID");
}
