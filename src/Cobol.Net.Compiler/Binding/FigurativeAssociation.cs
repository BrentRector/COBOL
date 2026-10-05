// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

/// <summary>
/// ⭐ ISO §8.3.3.6.3 SR3 — <i>"If the length of literal-1 is greater than one, it is not permitted to be associated
/// with a numeric or numeric-edited item"</i> — in ONE place, for every ASSOCIATION a program can write (kb/Work
/// PB422). §8.3.3.6.4 GR2's NOTE 1 names the associations: "the figurative constant is moved to it, compared with it, or
/// paired with it in a binary operation". The askers are therefore the MOVE edition pass (the written MOVE, every
/// implicit FROM/INTO move, and INITIALIZE REPLACING's hypothetical MOVE — <c>VersionConformancePass.GateSr5</c>)
/// and the ONE relation checkpoint (<c>StatementValidation.CheckRelationalOperands</c>, which IF, EVALUATE
/// pairings and ranges, PERFORM UNTIL and SEARCH WHEN all pass through). The third association, a binary
/// operation, cannot reach a numeric item: the boolean-expression binder refuses every non-boolean operand first
/// (COBOLNET1511).
///
/// <para>⛔ THIS IS NOT §14.9.25.3 SR5. SR5's exception — "an ALL "literal" figurative constant (containing only
/// digits) … to an integer numeric item" — carries no length qualifier, so <c>ALL "57"</c> is inside what SR5
/// permits; what bars it is THIS rule, which is not a MOVE rule at all and reaches a comparison exactly as it
/// reaches a move. Until kb/Work PB422 the multi-character case rode SR5's 2023 removal row, which accepted it at
/// 85/2002/2014 and under a diagnostic naming the wrong rule at 2023, and no comparison was screened at any
/// edition.</para>
///
/// <para>⚠ DETERMINATION — THE EDITION EDGE (VCR Table 7 row 7.13; the repository holds no 1985/2002/2014 text,
/// ratified decision #1, so the edge is DERIVED and stated so it can be overturned in one line): the association
/// is an OBSOLETE element of ANSI X3.23-1985 and was deleted with the rest of that list by ISO/IEC 1989:2002 —
/// the posture of every other Table-7 row (DATA RECORDS, VALUE OF, ALTER …). The 2023 rule carries no edition
/// qualifier and Annex E (the 2014→2023 delta) does not list it, so 2014 and 2023 agree; CCVS-85 carries the
/// '74 tests of the shape (NC105A MOVE-TEST-176..178) only as "DELETED BY FCCTS" — consistent with the
/// designation, not a proof of it. Hence the registry row
/// <c>all-literal-multichar-numeric-removed-2002</c>: accepted at 85 with the §8.3.3.6.4 GR2 repeat-and-truncate
/// value, COBOLNET0902 at 2002 and later (a warning under <c>--permissive</c>, the value preserved). The rejected
/// reading — an error at EVERY edition, from the 2023 text alone — would reject a program the 1985 standard
/// admits.</para>
/// </summary>
public static class FigurativeAssociation
{
    /// <summary>True when <paramref name="operand"/> is the Format-6 <c>ALL literal-1</c> figurative with a
    /// literal-1 longer than one character — the sender shape SR3 names. (Format 7, <c>ALL symbolic-character</c>,
    /// is one character by definition and SR3 is a Format-6 rule.)</summary>
    public static bool IsMultiCharacterAll(BoundOperand operand) =>
        operand is BoundAllLiteral { Literal.Length: > 1 };

    /// <summary>Asks SR3 of one association: a multi-character ALL literal against a data item of category
    /// numeric or numeric-edited. Reports through the construct registry and returns true when SR3 decides the
    /// association (so the caller asks no other rule of the same pair — SR5 in the MOVE pass); false when SR3
    /// does not reach it.</summary>
    public static bool GateSr3(EditionInfo edition, IDiagnosticSink sink, BoundOperand figurative,
        PicCategory? associatedCategory, string where)
    {
        if (!IsMultiCharacterAll(figurative)
            || associatedCategory is not (PicCategory.Numeric or PicCategory.NumericEdited)) return false;
        ConstructRegistry.Check(edition, sink, Constructs.AllLiteralMulticharNumericRemoved2002,
            where);
        return true;
    }

    /// <summary>⛔ THE ONE GATE FOR §8.3.3.6.3 SR1 a)'s ALL HALF (kb/Work PB1415): "If the literal is restricted to a
    /// numeric literal, the only figurative constant permitted is ZERO (ZEROS, ZEROES) WITHOUT the ALL phrase."
    /// The caller has established both facts — the position is numeric-restricted (the VALUE clause of a numeric
    /// subject, §13.18.63.3 SR2; an arithmetic-expression operand, §8.8.1.1) and the operand is ZERO written WITH
    /// the word ALL — and this reports it through the construct registry. ⚠ DETERMINATION (VCR Table 7 row 7.28,
    /// a DERIVED edge like SR3's row 7.13 above): accepted at 85, where ALL before a figurative word is redundant
    /// and CCVS-85 writes <c>VALUE ALL ZEROS</c> on <c>PIC 999</c> (NC201A); COBOLNET0902 at 2002 and later, a
    /// warning under <c>--permissive</c>. Where it is admitted the operand IS the figurative ZERO: the caller
    /// keeps the value zero either way, because a refused compile reaches no output.</summary>
    public static void GateAllZeroAsNumeric(EditionInfo edition, IDiagnosticSink sink, string where) =>
        ConstructRegistry.Check(edition, sink, Constructs.FigurativeAllZeroNumericRemoved2002, where);
}
