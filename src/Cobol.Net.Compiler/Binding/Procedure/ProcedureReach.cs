// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Procedure;

/// <summary>
/// ⛔ THE STATIC RANGE OF A PROCEDURE TRANSFER — which paragraphs execute UNDER a set of procedure ranges, written
/// once over the bound procedures (kb/Work PB434).
///
/// <para><b>The rule it models.</b> ISO §14.9.28.4 GR1: "The range includes all statements that are executed as
/// the result of a transfer of control in the range of the PERFORM statement". A range is therefore not the text
/// between two procedure-names: a PERFORM of another procedure from inside it, a SORT or MERGE whose INPUT or
/// OUTPUT PROCEDURE runs from inside it, and a handler of an exception-checking PERFORM inside it all execute
/// their own procedures and RETURN, so those procedures are in the range too, transitively.</para>
///
/// <para><b>What it follows, and what it does not.</b> It follows every RETURNING transfer
/// (<see cref="ReturningTransfersIn"/>) and starts from each seed's specified set read LEXICALLY
/// (<see cref="PcRange.Spans"/>, both physical orders of a GR6 inverted range). It does NOT follow a GO TO or a
/// declarative: where a GO TO leads after it leaves a range, and whether an I/O failure invokes a USE procedure,
/// are run-time facts, and a syntax rule that followed them statically would refuse statements no execution
/// ever reaches under the PERFORM.</para>
///
/// <para>A SYNTAX rule scoped to "under" or "within the range of" asks this model, never its own walk:
/// §14.9.28.3 SR8 (<see cref="UntilExitPlacement"/>) does, and kb/Work PB812 records the SORT/MERGE-procedure
/// bans (<c>VersionConformancePass.GateSortMergeProcedures</c>) as the next reader.</para>
/// </summary>
internal static class ProcedureReach
{
    /// <summary>Every procedure range a statement list transfers to AND RETURNS FROM, at any nesting depth: an
    /// out-of-line PERFORM's range (§14.9.28.4 GR4/GR5), a SORT's INPUT and OUTPUT PROCEDURE and a MERGE's OUTPUT
    /// PROCEDURE (§14.9.40.4 GR10/GR11, §14.9.24.4), and each handler body of an exception-checking PERFORM, which
    /// the binder appends as its own one-paragraph pc range (§14.9.28.4 GR17).</summary>
    public static IEnumerable<PcRange> ReturningTransfersIn(IEnumerable<BoundStatement> statements)
    {
        foreach (var s in statements)
        {
            if (s is BoundOutOfLinePerform perform) yield return perform.Range;
            if (s is BoundSort { InputProcedure: { } input }) yield return input;
            if (s is BoundSort { OutputProcedure: { } sortOutput }) yield return sortOutput;
            if (s is BoundMerge { OutputProcedure: { } mergeOutput }) yield return mergeOutput;
            if (s is BoundExceptionPerform f3)
            {
                foreach (var when in f3.Whens) yield return PcRange.At(when.Imp2Pc);
                if (f3.OtherPc is { } other) yield return PcRange.At(other);
                if (f3.CommonPc is { } common) yield return PcRange.At(common);
            }
            foreach (var nested in ReturningTransfersIn(s.StatementChildren())) yield return nested;
        }
    }

    /// <summary>The paragraph pcs executed under <paramref name="seeds"/>, each mapped to the origin of the FIRST
    /// seed found to reach it: the seeds' own specified sets, then — breadth-first, so a pc is reached by its
    /// shortest chain — every range a reached paragraph transfers to and returns from.</summary>
    public static Dictionary<int, TOrigin> Closure<TOrigin>(
        IReadOnlyList<BoundParagraph> paragraphs, IEnumerable<(PcRange Range, TOrigin Origin)> seeds)
    {
        var reached = new Dictionary<int, TOrigin>();
        var work = new Queue<int>();
        void Enter(PcRange range, TOrigin origin)
        {
            foreach (int pc in range.SpannedPcs())
                if (reached.TryAdd(pc, origin)) work.Enqueue(pc);
        }
        foreach (var (range, origin) in seeds) Enter(range, origin);
        while (work.TryDequeue(out int pc))
            foreach (var range in ReturningTransfersIn(paragraphs[pc].Statements))
                Enter(range, reached[pc]);
        return reached;
    }
}
