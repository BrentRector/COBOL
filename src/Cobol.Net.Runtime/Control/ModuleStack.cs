// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The run-unit module call-name stack backing FUNCTION MODULE-NAME (ISO §15.65). <see cref="ProgramTable"/>
/// pushes a frame at every activation boundary (RunMain for the main program; CallProgram for every CALL /
/// cross-assembly CALL / user-function reference / nested-program CALL) and pops it on return, so the top of
/// stack is always the currently running runtime element. Each frame carries the element's own name, the
/// outermost program-id of its compilation unit (§15.65.4 r7 CURRENT), and whether it is a nested (contained)
/// program (r8 NESTED). One INSTANCE per run unit on <see cref="RunUnit"/> (was the runtime's ONE
/// <c>[ThreadStatic]</c> — the ambient run unit's AsyncLocal now carries the activation scope uniformly,
/// DESIGN-runtime-library §2.1).
///
/// OO method (INVOKE) activation does not funnel through CallProgram (methods are not registry nodes), so the
/// push is emitted INSIDE the method body — <c>OoEmitter.EmitMethod</c>, in a try/finally — rather than at any
/// call site: a method is reached by a typed direct call, by the universal <c>__CobolInvoke</c> switch, and by
/// an inline invocation, and a per-site push would be three arms of one dispatch (fix-queue PB36).
///
/// ⛔ THIS COMMENT USED TO SAY THE OMISSION WAS "conformant, not residue: §15.65.4 r3/r4 make the returned form
/// for method-id elements implementor-defined". BOTH CITED RULES ARE REAL AND NEITHER GOVERNS. r3 is about a
/// runtime element that is NOT a COBOL runtime element; a COBOL method is one. r4 is about the FORM of the name
/// and explicitly lists "method-id" among the forms an implementor may return — which presumes the element is on
/// the stack. Meanwhile r5 names "an INVOKE statement" as an activation mechanism outright. The cost of the
/// misreading was three wrong answers inside every method: CURRENT returned the CALLER, STACK omitted the method,
/// and ACTIVATING returned the single space r5 reserves for a MAIN PROGRAM.
/// </summary>
public sealed class ModuleStack
{
    private readonly record struct Frame(string Name, string Outermost, bool IsNested, bool IsMain, long Activation);

    private readonly List<Frame> _stack = [];

    /// <summary>The last activation identity handed out — monotonic for the life of the run unit and never
    /// reset, so no two activations, live or finished, ever share one.</summary>
    private long _lastActivation;

    /// <summary>Push the run-unit main program (TOP-LEVEL, §15.65.4 r10; ACTIVATING → single space, r5).</summary>
    public void PushMain(string name) => _stack.Add(new(name, name, IsNested: false, IsMain: true, ++_lastActivation));

    /// <summary>Push a CALLed / referenced element (its name, its compilation unit's outermost program-id, and
    /// whether it is a contained/nested program).</summary>
    public void Push(string name, string outermost, bool isNested)
        => _stack.Add(new(name, outermost, isNested, IsMain: false, ++_lastActivation));

    // ── ACTIVATION IDENTITY (kb/Work PB892 Arm B) ─────────────────────────────────────────────────────────────
    // This stack is the run unit's ONE record of which activation is running: every activation mechanism
    // §15.65.4 r5 names pushes a frame here (ProgramTable.CallProgram / RunMain for a CALL, a function reference
    // and the main program; the method body itself for an INVOKE and an inline invocation). So it is also the
    // one place that can answer §14.9.18.4 GR1 b)'s question "which activation is the ACTIVATING runtime element
    // of the element that is returning with RAISING" — a question with no other runtime chokepoint, because a
    // typed INVOKE is a direct .NET call. Each frame therefore carries a unique activation identity.

    /// <summary>The identity of the RUNNING activation (the top frame), or 0 outside any activation.</summary>
    public long CurrentActivation => _stack.Count > 0 ? _stack[^1].Activation : 0;

    /// <summary>The identity of the activation that ACTIVATED the running one (the frame beneath the top), or
    /// −1 when the running element was activated by the operating environment or a non-COBOL host — an
    /// identity no activation ever has, so nothing staged for it can be taken.</summary>
    public long ActivatingActivation => _stack.Count >= 2 ? _stack[^2].Activation : -1;

    /// <summary>Pop the current element on activation return (paired with every push in a finally).</summary>
    public void Pop() { if (_stack.Count > 0) _stack.RemoveAt(_stack.Count - 1); }


    /// <summary>Resolve MODULE-NAME for the keyword selector (0 CURRENT · 1 ACTIVATING · 2 NESTED · 3 STACK ·
    /// 4 TOP-LEVEL). Returns the §15.65.4-defined value, trimmed (a dynamic-length result — r1, no trailing
    /// spaces).</summary>
    public string Name(int kind)
    {
        var s = _stack;
        if (s.Count == 0) return " ";                        // no running element — a non-COBOL context (r3)
        Frame top = s[^1];
        switch (kind)
        {
            case 0:   // CURRENT — the outermost program of the currently running compilation unit (r7)
                return top.Outermost;
            case 1:   // ACTIVATING (r5/r6)
                if (top.IsMain) return " ";                  // a main program → a single space (r5)
                return s.Count >= 2 ? s[^2].Name : " ";      // the element that activated the current one
            case 2:   // NESTED — the most recently nested currently-running program (r8)
                for (int i = s.Count - 1; i >= 0; i--) if (s[i].IsNested) return s[i].Name;
                return top.Name;                             // arg-rule-1-violating build — implementor-defined (r3/r4)
            case 3:   // STACK (r9) — built EXACTLY as r9 defines it, in terms of the other keywords.
            {
                // r9: "The first entry is the name of the runtime element that would be returned if the CURRENT
                // keyword were specified. The penultimate entry is the runtime element name that would be returned
                // if the TOP-LEVEL keyword were specified. The final entry is a single space indicating the
                // operating environment. The series of module names following the first one are the names of the
                // runtime elements that would have been returned if the ACTIVATING keyword were specified within
                // the previous module in the list."
                //
                // So: entry[0] = CURRENT (r7 — the OUTERMOST of the running unit); every entry after it is the
                // ACTIVATING chain (r5 — the ELEMENT name of the frame below); the last chain entry is s[0].Name,
                // which IS TOP-LEVEL (r10) and therefore lands penultimate as r9 requires.
                //
                // ⛔ THIS REPLACED A COLLAPSE OF CONSECUTIVE SAME-COMPILATION-UNIT FRAMES, AND THE COLLAPSE WAS
                // LOSING REAL DEPTH (owner decision 2026-08-05, fix-queue PB36). It was written to avoid a
                // "MAIN;MAIN" duplicate for a nested program, but it also swallowed every RECURSIVE activation —
                // three nested CALLs of one RECURSIVE program reported `R;MAIN; `, one entry, while ACTIVATING in
                // the same frame correctly answered `R`. STACK and ACTIVATING contradicted each other on plain
                // COBOL with no OO involved, and a stack list that silently drops recursion is worse than a
                // cosmetic repeat: the repeat is visible, the missing frames are not.
                //
                // ⚖ THE COST, DOCUMENTED (docs/CONFORMANCE.md §4.2.16): a CONTAINED program now yields
                // `MAIN;MAIN; ` — CURRENT is outermost-granularity (r7) while ACTIVATING is element-granularity
                // (r5), so for a nested program the first two entries coincide. That is r9's own composition, not
                // a defect; §15.65.1's looser "a list of all the module names" reading was the alternative and it
                // cannot represent recursion at all.
                var sb = new System.Text.StringBuilder();
                sb.Append(top.Outermost);                    // entry 0 = CURRENT (r7)
                for (int i = s.Count - 2; i >= 0; i--)       // the ACTIVATING chain (r5), ending at TOP-LEVEL (r10)
                    sb.Append(';').Append(s[i].Name);
                sb.Append("; ");                             // final entry = a single space (the operating environment)
                return sb.ToString();
            }
            case 4:   // TOP-LEVEL — "the runtime element that was activated by the operating environment to
                      // initiate the run unit" (r10). Frame 0 is that element only when it was pushed as the MAIN
                      // (ProgramTable.RunMain → PushMain); a host that drives a COBOL element WITHOUT RunMain
                      // (RunUnit.Run / ProgramTable.CallProgram from .NET) leaves frame 0 an ordinary CALLed frame —
                      // then the element the environment activated is the non-COBOL host, and r3's documented
                      // value applies (a single space, docs/CONFORMANCE.md A.1 item 134; kb/Work PB63 / RV-15.65.4-10).
                return s[0].IsMain ? s[0].Name : " ";
            default:
                return " ";
        }
    }
}
