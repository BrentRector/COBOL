// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Binding.Model;
using CobolNet.CodeGen;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ "IS THIS PROCEDURE RANGE EMPTY?" IS NOT A QUESTION THE NUMBERS CAN ANSWER, AND FOR YEARS EVERY CONSUMER
/// ANSWERED IT WITH THE NUMBERS (kb/Work PB440).
/// <para>A section with ZERO paragraphs is legal COBOL — ISO §14.4.2, "A section consists of a section header
/// followed by zero, one, or more successive paragraphs" — and it resolves to the pc pair <c>(s, s−1)</c>. So
/// does a legal INVERTED THRU range whose two procedures are ADJACENT, which §14.9.28.4 GR6 explicitly permits
/// ("There is no necessary relationship between procedure-name-1 and procedure-name-2") and NIST NC102A
/// PFM-TEST-F1-10 exercises. One must execute NOTHING; the other must execute from <c>s</c> until control
/// reaches <c>s−1</c>. Every hand-written <c>start &gt; end</c> / <c>Start &lt;= End</c> test was therefore
/// answering the wrong question, and the two answers it got wrong were both wrong-answer defects: the binder
/// deleted a PERFORM's whole control phrase, and the SORT emitter dropped an entire INPUT PROCEDURE.</para>
/// <para>The bit is now CARRIED by <see cref="PcRange.IsEmpty"/> and the dispatch call is built in exactly one
/// place, which refuses an empty range. These tests keep both true.</para>
/// <para>⛔ THE SAME ONE-SITE PROPERTY IS ASSERTED FOR THE USE DECLARATIVE'S <c>__RunUse</c> CALL
/// (kb/Work PB367). Six selection paths — <c>__IoCheck</c>, <c>__IoCheckEc</c>, <c>__EcDispatch</c>,
/// <c>__EcObjDispatch</c>, <c>__RunGlobalUse</c> and the report engine's BEFORE REPORTING hook — each spelled the
/// declarative's pc pair out themselves, so ONE wrong handler end (derived from paragraph SHAPE rather than from
/// ISO §14.9.49.3 SR1's "remainder of the section") truncated the selected declarative on every USE format at
/// once and no single place could be corrected. It is now
/// <see cref="DispatchState.RunUseCall(int, PcRange)"/>, and these facts keep it that way.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ProcedureRangeEmptinessDriftTests
{
    // ── The property the arithmetic cannot have ──────────────────────────────────────────────────────────────

    [Fact]
    public void EmptinessIsNotDerivableFromTheNumbers()
    {
        // The zero-paragraph section at pc 5 and the ADJACENT inverted THRU range `PERFORM P5 THRU P4` are the
        // same pair. Only the carried bit tells them apart (ISO §14.4.2 vs §14.9.28.4 GR6).
        PcRange empty = PcRange.EmptyAt(5);
        PcRange invertedAdjacent = PcRange.Of(5, 4);

        Assert.Equal(invertedAdjacent.Start, empty.Start);
        Assert.Equal(invertedAdjacent.End, empty.End);
        Assert.True(empty.IsEmpty);
        Assert.False(invertedAdjacent.IsEmpty);
        Assert.NotEqual(empty, invertedAdjacent);
    }

    [Fact]
    public void AParagraphAndANonEmptySectionAreNeverEmpty()
    {
        Assert.False(PcRange.At(3).IsEmpty);
        Assert.True(PcRange.At(3).IsParagraph);      // ALTER's procedure-name-1 shape
        Assert.False(PcRange.Of(3, 7).IsEmpty);
        Assert.False(PcRange.Of(3, 7).IsParagraph);  // a SECTION name is not a paragraph
        Assert.False(PcRange.EmptyAt(3).IsParagraph);
    }

    // ── §14.9.28.4 GR4/GR5b composition ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void ThroughComposesTheSpecifiedSet()
    {
        (string Name, PcRange First, PcRange Thru, int Start, int End, bool IsEmpty)[] cases =
        [
            // name                            proc-1              proc-2             Start End IsEmpty
            ("paragraph THRU paragraph",       PcRange.At(2),      PcRange.At(6),       2,   6, false),
            ("same section twice",             PcRange.Of(2, 6),   PcRange.Of(2, 6),    2,   6, false),
            // GR6 — the exit procedure physically precedes the first; the return mechanism is still after ITS end.
            ("inverted, non-adjacent",         PcRange.At(9),      PcRange.At(3),       9,   3, false),
            ("inverted, adjacent",             PcRange.At(5),      PcRange.At(4),       5,   4, false),
            // An EMPTY procedure-name-1: execution begins where it continues past the empty section, and the set
            // is whatever procedure-name-2 ends — non-empty.
            ("empty THRU following section",   PcRange.EmptyAt(4), PcRange.Of(4, 7),    4,   7, false),
            // An EMPTY procedure-name-2 that ends where procedure-name-1's statements end: still non-empty.
            ("section THRU following empty",   PcRange.Of(2, 5),   PcRange.EmptyAt(6),  2,   5, false),
            // BOTH empty with nothing in between (the same section, or two adjacent empty sections) — the ONLY
            // composition the standard makes empty.
            ("empty THRU itself",              PcRange.EmptyAt(5), PcRange.EmptyAt(5),  5,   4, true),
            // Two empty sections with statements between them: the set is those statements.
            ("empty THRU a LATER empty",       PcRange.EmptyAt(3), PcRange.EmptyAt(8),  3,   7, false),
        ];

        foreach (var c in cases)
        {
            PcRange composed = c.First.Through(c.Thru);
            Assert.Equal((c.Name, c.Start, c.End, c.IsEmpty),
                (c.Name, composed.Start, composed.End, composed.IsEmpty));
        }
    }

    // ── kb/Work PB3254: the lexical span of a composed THRU keeps BOTH operands' extents ─────────────────────

    [Fact]
    public void ThroughSpansEveryParagraphOfBothOperandsInWhicheverOrder()
    {
        // §14.9.40.3 SR3 reads the procedure lexically; §14.9.28.4 GR6 lets procedure-name-2 stand BEFORE
        // procedure-name-1. S-A = pcs 2..3 physically before S-B = pcs 4..6, and `S-B THRU S-A` is the pair (4, 3).
        PcRange sectionA = PcRange.Of(2, 3);
        PcRange sectionB = PcRange.Of(4, 6);
        (string Name, PcRange First, PcRange Thru, int[] Spanned)[] cases =
        [
            ("sections, forward",                sectionA,           sectionB,           [2, 3, 4, 5, 6]),
            ("sections, INVERTED (PB3254)",      sectionB,           sectionA,           [2, 3, 4, 5, 6]),
            ("paragraphs, inverted",             PcRange.At(9),      PcRange.At(3),      [3, 4, 5, 6, 7, 8, 9]),
            ("section THRU earlier paragraph",   sectionB,           PcRange.At(1),      [1, 2, 3, 4, 5, 6]),
            ("empty THRU following section",     PcRange.EmptyAt(4), sectionB,           [4, 5, 6]),
            ("earlier section THRU empty",       sectionA,           PcRange.EmptyAt(4), [2, 3]),
            ("later empty THRU earlier section", PcRange.EmptyAt(4), sectionA,           [2, 3]),
            ("paragraph THRU earlier empty",     PcRange.At(6),      PcRange.EmptyAt(4), [4, 5, 6]),
            ("empty THRU a LATER empty",         PcRange.EmptyAt(3), PcRange.EmptyAt(8), [3, 4, 5, 6, 7]),
            ("empty THRU itself",                PcRange.EmptyAt(5), PcRange.EmptyAt(5), []),
        ];

        foreach (var c in cases)
        {
            PcRange composed = c.First.Through(c.Thru);
            Assert.True(c.Spanned.SequenceEqual(composed.SpannedPcs()),
                $"{c.Name}: spanned [{string.Join(", ", composed.SpannedPcs())}]");
            for (int pc = 0; pc <= 10; pc++)
                Assert.Equal((c.Name, pc, c.Spanned.Contains(pc)), (c.Name, pc, composed.Spans(pc)));
            // The span and the carried emptiness are one fact: a range that spans no paragraph is the empty set.
            Assert.Equal((c.Name, c.Spanned.Length == 0), (c.Name, composed.IsEmpty));
        }
    }

    // ── The dispatcher is never asked to run an empty range ──────────────────────────────────────────────────

    [Fact]
    public void DispatchCallRefusesAnEmptyRange()
    {
        var state = new DispatchState();

        // A non-empty range renders the bounded call — inverted included.
        Assert.Equal("__Dispatch(2, 6);", state.DispatchCall(PcRange.Of(2, 6)));
        Assert.Equal("__Dispatch(5, 4);", state.DispatchCall(PcRange.Of(5, 4)));
        Assert.Equal("__Dispatch(2, 6);   // x", state.DispatchCall(PcRange.Of(2, 6), "   // x"));

        // An empty one is an emitter bug: the generated dispatcher's return test (__atExit && __pc == __exitPc+1)
        // cannot fire on it, so the program would run to the end of the pc space. Fail at COMPILE time instead.
        var ex = Assert.Throws<InvalidOperationException>(() => state.DispatchCall(PcRange.EmptyAt(5)));
        Assert.Contains("EMPTY procedure range", ex.Message);
    }

    // ── No second construction site for a bounded dispatch call ──────────────────────────────────────────────

    /// <summary>Emitted dispatch calls that do NOT come from a resolved <see cref="PcRange"/>, each with the
    /// reason it is exempt. Adding a line here is an adjudication: it asserts that the call's bounds are not a
    /// procedure range and therefore cannot be empty.</summary>
    private static readonly Dictionary<string, string> ExemptDispatchCalls = new(StringComparer.Ordinal)
    {
        ["__Dispatch({bound.EntryPc}, {topExit})"] =
            "the WHOLE program's entry dispatch (ISO §14.2.3 GR1 — execution begins with the first "
            + "nondeclarative procedure): its bounds are the unit's own pc space, never a resolved procedure range",
        ["{dispatchState.DispatchName}(__startPc, __endPc)"] =
            "inside the emitted __RunUse / __RunGlobalUse helper — the bounds are that helper's RUNTIME "
            + "parameters, supplied by the declarative's own (never empty) range at the call site",
        ["__Dispatch(__ds, __de)"] =
            "inside the emitted __RunDebug helper — the bounds are its runtime parameters (a debug SUBJECT's "
            + "section range, which by construction contains the subject paragraph)",
        ["{DispatchName}({range.Start}, {range.End})"] =
            "DispatchState.DispatchCall itself — THE one construction site, which refuses an empty range",
    };

    [Fact]
    public void EveryEmittedDispatchCallIsBuiltInOnePlace()
    {
        // Any `__Dispatch(` / `{…DispatchName}(` occurrence inside an EMITTED string, with its argument list.
        var call = new Regex(@"(?:__Dispatch|\{[A-Za-z_.]*DispatchName\})\((?<args>[^()]*(?:\([^()]*\)[^()]*)*)\)");

        // POSITIVE CONTROL — a scan that quietly stopped matching would pass forever. These are the two shapes
        // the fix removed from ControlFlowEmitter and SortEmitter; the regex must still see them.
        Assert.Equal("{dispatch.DispatchName}({p.StartPc}, {p.EndPc})",
            call.Match("            EmitPerform(p.Control, () => w.Line($\"{dispatch.DispatchName}({p.StartPc}, {p.EndPc});\"), inline: false);").Value);
        Assert.Equal("{dispatch.DispatchName}({ip.Start}, {ip.End})",
            call.Match("            w.Line($\"{dispatch.DispatchName}({ip.Start}, {ip.End});   // INPUT PROCEDURE\");").Value);

        var found = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs",
                     SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;
            foreach (string line in File.ReadAllLines(file))
            {
                // Only lines that EMIT: a doc comment or a C# method signature mentioning __Dispatch is prose.
                string t = line.TrimStart();
                if (t.StartsWith("///", StringComparison.Ordinal) || t.StartsWith("//", StringComparison.Ordinal)) continue;
                if (!line.Contains("$\"", StringComparison.Ordinal) && !line.Contains("w.Line(\"", StringComparison.Ordinal)) continue;
                foreach (Match m in call.Matches(line))
                {
                    string sig = m.Value.Replace("\\\"", "\"");
                    if (sig.Contains("int __startPc", StringComparison.Ordinal)) continue;   // the method signature
                    found.Add(sig);
                }
            }
        }

        Assert.NotEmpty(found);   // the scan itself must never silently match nothing
        var unexplained = found.Where(s => !ExemptDispatchCalls.ContainsKey(s)).Distinct().ToList();
        Assert.True(unexplained.Count == 0,
            "A bounded dispatch call is built outside DispatchState.DispatchCall. Route it through DispatchCall "
            + "(which refuses an EMPTY procedure range, kb/Work PB440) or add it to ExemptDispatchCalls with the "
            + "reason its bounds are not a resolved procedure range:\n  " + string.Join("\n  ", unexplained));
    }

    [Fact]
    public void NothingDecodesEmptinessFromTheNumbers()
    {
        // An ordering or equality comparison between a `.Start` and an `.End` is the shape that answered the
        // wrong question. PcRange.IsEmpty / PcRange.IsParagraph are the only sanctioned answers.
        var handDecode = new Regex(@"\.Start\s*(<=|<|>=|>|==|!=)\s*[A-Za-z_][A-Za-z0-9_]*\.End"
            + @"|\.End\s*(<=|<|>=|>|==|!=)\s*[A-Za-z_][A-Za-z0-9_]*\.Start");

        // POSITIVE CONTROL — a zero-hit scan is only evidence if the regex still recognizes the defect. These
        // are the four decode sites the fix removed (SortEmitter ×3 shape, SetAlterBinder ×2 shape).
        Assert.Matches(handDecode, "        else if (so.InputProcedure is { } ip && ip.Start <= ip.End)");
        Assert.Matches(handDecode, "            if (ResolveProcedure(names[0]) is { } t && t.Start == t.End)");
        Assert.Matches(handDecode, "            if (r is not { } target || target.Start != target.End)");
        Assert.DoesNotMatch(handDecode, "        if (lastPc >= Range.Start) Range = PcRange.Of(Range.Start, lastPc);");

        var hits = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs",
                     SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;
            foreach (string line in File.ReadAllLines(file))
            {
                string t = line.TrimStart();
                if (t.StartsWith("//", StringComparison.Ordinal)) continue;
                if (handDecode.IsMatch(line)) hits.Add($"{Path.GetFileName(file)}: {t}");
            }
        }
        Assert.True(hits.Count == 0,
            "A procedure range's emptiness (or paragraph-ness) is being decoded from Start/End arithmetic. "
            + "`End < Start` is ALSO true of a legal inverted THRU range (ISO §14.9.28.4 GR6), so the test is "
            + "ambiguous — use PcRange.IsEmpty / PcRange.IsParagraph (kb/Work PB440):\n  "
            + string.Join("\n  ", hits));
    }

    // ── The USE declarative's bounded call has ONE construction site too (kb/Work PB367) ─────────────────────

    [Fact]
    public void RunUseCallRefusesAnEmptyRange()
    {
        var state = new DispatchState();

        // A declarative's range renders the bounded invoker. The pair is the SECTION's range and nothing
        // narrows it: ISO §14.9.49.3 SR1 makes the use procedure "the remainder of the section", and
        // §14.9.14.4 GR7's NOTE puts the USE return mechanism after that section's LAST paragraph.
        Assert.Equal("__RunUse(0, 2, 6)", state.RunUseCall(0, PcRange.Of(2, 6)));
        Assert.Equal("__RunUse(3, 9, 9)", state.RunUseCall(3, PcRange.At(9)));

        // An empty one cannot arise — the binder gives a zero-paragraph declarative section one no-op pc so the
        // SELECTOR can still stop at it (§14.9.49.4 GR3: no other declarative may then run). Asking for the call
        // anyway is an emitter bug, not something to render.
        var ex = Assert.Throws<InvalidOperationException>(() => state.RunUseCall(0, PcRange.EmptyAt(5)));
        Assert.Contains("EMPTY declarative range", ex.Message);
    }

    /// <summary>Emitted <c>__RunUse</c> calls that do NOT come from a <c>BoundDeclarative</c>'s range, each with
    /// the reason it is exempt. Adding a line here is an adjudication: it asserts the call's bounds are not a
    /// declarative section.</summary>
    private static readonly Dictionary<string, string> ExemptRunUseCalls = new(StringComparer.Ordinal)
    {
        ["__RunUse(int __id, int __startPc, int __endPc)"] =
            "the emitted helper's own signature (class member for a program, local function for an OO method)",
        ["__RunUse(__u, __pc, __pc)"] =
            "the exception-checking PERFORM's imp-2 / imp-3 handler — a SYNTHETIC single-pc range appended above "
            + "the pc space and selected by ISO §14.9.28.4 GR17, not a declarative (§14.9.49.4 GR3)",
        ["__RunUse(__cu, __cpc, __cpc)"] =
            "the same interceptor's WHEN COMMON handler (imp-4, §14.9.28.4 GR19) — likewise synthetic",
        ["__RunUse({id}, {range.Start}, {range.End})"] =
            "DispatchState.RunUseCall itself — THE one construction site",
        ["__RunUse({id}, …)"] =
            "the refusal message inside RunUseCall, naming the call it declined to build",
    };

    [Fact]
    public void EveryEmittedRunUseCallIsBuiltInOnePlace()
    {
        var call = new Regex(@"__RunUse\((?<args>[^()]*)\)");

        // POSITIVE CONTROL — this is the shape the fix removed from all six selection paths. A scan that quietly
        // stopped matching would pass forever.
        Assert.Equal("__RunUse({i}, {decls[i].StartPc}, {decls[i].HandlerEndPc})",
            call.Match("                w.Line($\"case {m}: __RunUse({i}, {decls[i].StartPc}, {decls[i].HandlerEndPc}); return true;\");").Value);

        var found = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs",
                     SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;
            foreach (string line in File.ReadAllLines(file))
            {
                string t = line.TrimStart();
                if (t.StartsWith("///", StringComparison.Ordinal) || t.StartsWith("//", StringComparison.Ordinal)) continue;
                if (!line.Contains("$\"", StringComparison.Ordinal) && !line.Contains("\"", StringComparison.Ordinal)) continue;
                foreach (Match m in call.Matches(line)) found.Add(m.Value.Replace("\\\"", "\""));
            }
        }

        Assert.NotEmpty(found);   // the scan itself must never silently match nothing
        var unexplained = found.Where(s => !ExemptRunUseCalls.ContainsKey(s)).Distinct().ToList();
        Assert.True(unexplained.Count == 0,
            "A USE declarative's bounded call is built outside DispatchState.RunUseCall. Six sites each spelled "
            + "that pc pair themselves, which is why ONE truncated handler end reached every USE format at once "
            + "(kb/Work PB367). Route it through RunUseCall, or add it to ExemptRunUseCalls with the reason its "
            + "bounds are not a declarative section:\n  " + string.Join("\n  ", unexplained));
    }
}
