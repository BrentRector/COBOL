// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Binding;
using CobolNet.CodeGen;
using CobolNet.Frontend.Diagnostics;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB405 + PB414 — the invariant that keeps every transfer of control OUT of a paragraph correct as the
/// emitter grows new containers: <b>a statement leaves the paragraph body only through the planted dispatcher
/// label (<c>__pc = t; goto __xfer;</c>), never through a bare C# <c>break;</c>.</b>
///
/// <para><b>Why the invariant exists.</b> C# binds <c>break</c> to the innermost enclosing breakable statement,
/// and the emitter lowers COBOL containers to real C# breakables — an inline PERFORM is a <c>for</c>/<c>while</c>/
/// <c>do</c> (<c>ControlFlowEmitter.EmitPerformLoop</c>), GO TO … DEPENDING is a <c>switch</c>. A <c>break</c>
/// written for the dispatcher's <c>switch (__pc)</c> was therefore CAPTURED by whatever container the statement
/// happened to sit in, and the paragraph's fall-through epilogue then overwrote the <c>__pc</c> the statement had
/// set: EXIT PARAGRAPH silently degraded to EXIT PERFORM (ISO §14.9.14.4 GR6), EXIT SECTION's target was
/// discarded so the rest of the section ran (GR7), NEXT SENTENCE in a last sentence resumed inside the sentence
/// it was told to leave (§14.9.19.4 GR4/GR6), and GO TO did not transfer at all (§14.9.17.4 GR1/GR2). None of
/// those is diagnosable by reading one emit site — the defect is the CONJUNCTION of a statement and a container,
/// so it is measured here mechanically, over the generated C#, for every container the emitter lowers.</para>
///
/// <para><b>What is measured.</b> The fixture puts each transfer statement inside a lowered container, and the
/// scanner walks the generated source with a real brace-depth counter (string literals and comments removed):
/// a <c>__pc = …;</c> assignment whose next jump is <c>break;</c> is a dispatcher transfer, and it is legal ONLY
/// at case-body depth (the fall-through epilogue and the <c>default:</c> arm). Anywhere deeper it is a captured
/// transfer — the PB405 defect — and this test names it. A new container the emitter learns to lower is covered
/// automatically: nothing in the scanner knows what a PERFORM is.</para>
/// </summary>
public sealed class DispatcherTransferIdiomDriftTests
{
    // Every transfer statement there is, each written INSIDE a lowered C# container. S1-A alone carries the
    // inline PERFORM (a while), an EVALUATE inside it, and a SEARCH inside it; S1-B carries GO TO … DEPENDING
    // (whose selector switch is itself breakable) and a NEXT SENTENCE in the paragraph's LAST sentence.
    private const string Src = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. XFERDRIFT.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 I PIC 9 VALUE 0.
        01 K PIC 9 VALUE 0.
        01 T.
           05 E OCCURS 4 TIMES INDEXED BY X PIC 9.
        PROCEDURE DIVISION.
        S1 SECTION.
        S1-A.
            PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
                EVALUATE I
                    WHEN 1 EXIT PARAGRAPH
                    WHEN 2 EXIT SECTION
                    WHEN 3 GO TO S2-B
                END-EVALUATE
                SEARCH E
                    AT END GO TO S2-A
                    WHEN E (X) = 9 EXIT SECTION
                END-SEARCH
            END-PERFORM
            DISPLAY "TAIL-A".
        S1-B.
            PERFORM UNTIL K > 3
                ADD 1 TO K
                GO TO S2-A S2-B DEPENDING ON K
                IF K = 2 NEXT SENTENCE END-IF
            END-PERFORM
            DISPLAY "TAIL-B".
        S2 SECTION.
        S2-A.
            DISPLAY "A".
        S2-B.
            DISPLAY "B".
            STOP RUN.
        """;

    // The control: the same shape with NO transfer statement anywhere. Its dispatcher must plant no label (an
    // unreferenced C# label is a warning, and the zero-scaffolding rule is that a program pays for nothing it
    // does not use).
    private const string NoTransferSrc = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. XFERNONE.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 I PIC 9 VALUE 0.
        PROCEDURE DIVISION.
        MAIN-P.
            PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
                DISPLAY "IT " I
            END-PERFORM
            DISPLAY "TAIL".
        NEXT-P.
            DISPLAY "NEXT".
            STOP RUN.
        """;

    private static string Emit(string src)
    {
        string path = Path.Combine(Path.GetTempPath(), $"xferdrift_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2002 }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var emitter = new CSharpEmitter();
            return emitter.EmitBound(emitter.Bind(tree!, new EditionContext(2002)), "DRIFTPROBE");
        }
        finally { try { File.Delete(path); } catch (IOException) { /* best-effort */ } }
    }

    /// <summary>The generated source as a flat statement stream — (text, brace depth) — with string literals
    /// blanked and comments removed, so a brace inside a DISPLAY literal cannot skew the depth.</summary>
    private static List<(string Text, int Depth)> Statements(string cs)
    {
        var stmts = new List<(string, int)>();
        var buf = new StringBuilder();
        int depth = 0;
        for (int i = 0; i < cs.Length; i++)
        {
            char c = cs[i];
            if (c == '"' || c == '\'')          // a literal: skip it whole (it may contain braces or semicolons)
            {
                char q = c;
                i++;
                while (i < cs.Length && cs[i] != q)
                {
                    if (cs[i] == '\\') i++;
                    i++;
                }
                continue;
            }
            if (c == '/' && i + 1 < cs.Length && cs[i + 1] == '/')   // a line comment
            {
                while (i < cs.Length && cs[i] != '\n') i++;
                continue;
            }
            switch (c)
            {
                case ';':
                    stmts.Add((buf.ToString().Trim(), depth));
                    buf.Clear();
                    break;
                case '{':
                    stmts.Add((buf.ToString().Trim() + " {", depth));
                    buf.Clear();
                    depth++;
                    break;
                case '}':
                    stmts.Add((buf.ToString().Trim(), depth));
                    buf.Clear();
                    depth--;
                    stmts.Add(("}", depth));
                    break;
                default:
                    buf.Append(c == '\n' || c == '\r' ? ' ' : c);
                    break;
            }
        }
        return stmts;
    }

    /// <summary>Every dispatcher transfer (a <c>__pc = …;</c> assignment whose next jump is <c>break;</c>) that
    /// sits DEEPER than a paragraph case body — i.e. every transfer a lowered C# container captures. A transfer
    /// at case-body depth is the legitimate fall-through epilogue or the <c>default:</c> arm, whose <c>break</c>
    /// really does leave <c>switch (__pc)</c>.
    /// <para>The GO TO … DEPENDING selector's own arms (<c>case 1: __pc = 5; break;</c>) are a deliberate TWO-STEP
    /// idiom — the <c>break</c> leaves the SELECTOR switch and the transfer is completed by the
    /// <c>if (in range) goto __xfer;</c> that follows it — and they are recognized by carrying their <c>case</c>
    /// label in the same statement. If that emission is ever split across lines this scanner reports it; a red
    /// drift test asking a maintainer to re-read the idiom is the safe direction of the two.</para></summary>
    private static List<string> CapturedTransfers(string cs)
    {
        var stmts = Statements(cs);
        var captured = new List<string>();
        var switchDepths = new Stack<int>();   // the depth of each open `switch (__pc)` (one per dispatch method)
        for (int i = 0; i < stmts.Count; i++)
        {
            var (text, depth) = stmts[i];
            while (switchDepths.Count > 0 && depth < switchDepths.Peek()) switchDepths.Pop();
            if (text.Contains("switch (__pc) {")) { switchDepths.Push(depth); continue; }
            if (switchDepths.Count == 0) continue;
            int caseBodyDepth = switchDepths.Peek() + 2;   // switch block, then the case block
            if (!text.StartsWith("__pc =", StringComparison.Ordinal)) continue;
            // The next statement is the jump this assignment was made for (comments are already gone).
            string next = i + 1 < stmts.Count ? stmts[i + 1].Text : "";
            if (next == "break" && depth > caseBodyDepth)
                captured.Add($"depth {depth} (case body is {caseBodyDepth}): `{text}; break;`");
        }
        return captured;
    }

    /// <summary>The scanner FAILS on the defect it exists to catch — the generated <c>case 0</c> PB405 measured,
    /// verbatim from the note — and passes the same paragraph once the transfer is a <c>goto</c>. Without this a
    /// green run above would prove only that the scanner found nothing, not that it can find anything
    /// (kb/Work feedback: a passing check proves nothing if it never looked at what changed).</summary>
    [Fact]
    public void TheScannerFlagsThePreFixEmission()
    {
        const string before = """
            private int __Dispatch(int __startPc, int __exitPc)
            {
                int __pc = __startPc;
                while ((uint)__pc < (uint)__N)
                {
                    bool __atExit = __pc == __exitPc;
                    switch (__pc)
                    {
                        case 0:
                        {
                            while (!(I > 3L))
                            {
                                if (I == 2L) { __pc = 1; break; }
                                __pcont0: ;
                                I = I + 1L;
                            }
                            __pexit0: ;
                            System.Console.WriteLine("TAIL-MAIN");
                            __pc = 1;
                            break;
                        }
                    }
                    if (__atExit && __pc == __exitPc + 1) return __pc;
                }
                return __pc;
            }
            """;
        var captured = CapturedTransfers(before);
        Assert.Single(captured);
        Assert.Contains("__pc = 1", captured[0], StringComparison.Ordinal);

        // The same paragraph with the transfer repaired — and the epilogue's own `break`, at case-body depth,
        // still not flagged.
        Assert.Empty(CapturedTransfers(before.Replace("{ __pc = 1; break; }", "{ __pc = 1; goto __xfer; }")));
    }

    /// <summary>⛔ THE INVARIANT: no transfer of control out of a paragraph is spelled as a bare
    /// <c>break;</c> inside a lowered container. Measured over the generated C#, not over the emitter's source —
    /// so a NEW container (or a new transfer statement) is covered without being enumerated here.</summary>
    [Fact]
    public void NoDispatcherTransferIsCapturedByALoweredContainer()
    {
        string cs = Emit(Src);
        var captured = CapturedTransfers(cs);
        Assert.True(captured.Count == 0,
            "a `__pc = …; break;` inside a lowered C# container — the break binds to THAT container, not to the "
            + "dispatcher (ISO §14.9.14.4 GR6/GR7, §14.9.17.4 GR1/GR2, §14.9.19.4 GR4/GR6; kb/Work PB405):\n  "
            + string.Join("\n  ", captured));
    }

    /// <summary>The fixture really does exercise the failing conjunction: its transfers are emitted DEEPER than
    /// the case body (inside the inline PERFORM's loop, the EVALUATE arm, the SEARCH scan, the DEPENDING
    /// selector), so the invariant above is not vacuously true. If a future lowering flattens those containers —
    /// or the fixture stops parsing the way it reads — this fact goes red and says the measurement stopped.</summary>
    [Fact]
    public void TheFixturePlacesEveryTransferInsideALoweredContainer()
    {
        var stmts = Statements(Emit(Src));
        var switchDepths = new Stack<int>();
        int deepJumps = 0;
        foreach (var (text, depth) in stmts)
        {
            while (switchDepths.Count > 0 && depth < switchDepths.Peek()) switchDepths.Pop();
            if (text.Contains("switch (__pc) {")) { switchDepths.Push(depth); continue; }
            if (switchDepths.Count == 0) continue;
            // The jump half of the transfer idiom, deeper than a case body: EXIT PARAGRAPH, EXIT SECTION ×2,
            // GO TO ×2 (an EVALUATE arm and a SEARCH AT END), GO TO … DEPENDING, NEXT SENTENCE in a last sentence.
            if (text.EndsWith("goto __xfer", StringComparison.Ordinal) && depth > switchDepths.Peek() + 2)
                deepJumps++;
        }
        Assert.True(deepJumps >= 7,
            $"the fixture emitted only {deepJumps} transfers inside a lowered container — it no longer "
            + "measures the PB405 conjunction");
    }

    /// <summary>The label is planted exactly where it is used: a dispatch method that renders a transfer plants
    /// its landing, and one that renders none plants nothing (no unreferenced C# label, and a transfer-free
    /// program's generated source is unchanged by this machinery).</summary>
    [Fact]
    public void TheTransferLabelIsPlantedIfAndOnlyIfATransferIsRendered()
    {
        string withTransfers = Emit(Src);
        Assert.Contains("goto __xfer;", withTransfers, StringComparison.Ordinal);
        Assert.Contains("__xfer: ;", withTransfers, StringComparison.Ordinal);

        string none = Emit(NoTransferSrc);
        Assert.DoesNotContain("goto __xfer;", none, StringComparison.Ordinal);
        Assert.DoesNotContain("__xfer: ;", none, StringComparison.Ordinal);
    }

    /// <summary>THE SECOND ARM. A COBOL method's dispatcher is a LOCAL FUNCTION (<c>__MDispatch</c>, OO deep-dive
    /// D3/D6), and a C# <c>goto</c> may not leave the member it is written in — so the method dispatcher plants
    /// its OWN landing and its transfers jump there. Asserted because "one dispatch shape, two emitters, only one
    /// of them fixed" is this repo's most reproducible defect: the program arm above would stay green forever
    /// while an OO method silently kept a captured transfer (or failed to compile).</summary>
    [Fact]
    public void TheOoMethodDispatcherPlantsItsOwnLabel()
    {
        string cs = Emit(OoSrc);
        Assert.Empty(CapturedTransfers(cs));
        Assert.Contains("goto __mxfer;", cs, StringComparison.Ordinal);
        Assert.Contains("__mxfer: ;", cs, StringComparison.Ordinal);
    }

    // A METHOD whose paragraph holds a GO TO inside an inline PERFORM — the OO twin of S1-A above.
    private const string OoSrc = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. XFERDRIFTOO.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS XFERDC.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 O USAGE OBJECT REFERENCE XFERDC.
        PROCEDURE DIVISION.
        MAIN-P.
            INVOKE XFERDC "NEW" RETURNING O
            INVOKE O "RUNIT"
            STOP RUN.
        END PROGRAM XFERDRIFTOO.

        IDENTIFICATION DIVISION.
        CLASS-ID. XFERDC INHERITS FROM BASE.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS BASE.
        IDENTIFICATION DIVISION.
        OBJECT.
        PROCEDURE DIVISION.
        METHOD-ID. RUNIT.
        DATA DIVISION.
        LOCAL-STORAGE SECTION.
        01 I PIC 9 VALUE 0.
        PROCEDURE DIVISION.
        M-A.
            PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
                IF I = 2
                    GO TO M-C
                END-IF
            END-PERFORM
            DISPLAY "M-TAIL".
        M-B.
            DISPLAY "M-B".
        M-C.
            DISPLAY "M-C".
        END METHOD RUNIT.
        END OBJECT.
        END CLASS XFERDC.
        """;
}
