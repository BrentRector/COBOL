## Entry 1180 — 2026-08-05 10:30 PDT — the STACK collapse was losing recursion depth, and the mechanism it lived in got challenged, measured, and kept

**PB36 closed.** Entry 1179 landed the INVOKE activation frame and left the r9 STACK granularity open. Two owner
interventions turned that open half into something better than a coin-flip between readings.

## ⛔ THE DECISIVE CASE HAD NO OO IN IT AT ALL

I had framed the question as method-vs-module granularity. Probing further found the collapse losing **recursion
depth** on a plain `RECURSIVE` CALL chain:

```
PB36REC → PB36R2 → PB36R2 → PB36R2      three activations
REC-STACK = [PB36R2;PB36REC; ]          one entry
REC-ACT   = [PB36R2]                    correct
```

STACK and ACTIVATING contradicting each other, in the CALL path the original adjudicator called "already right",
with no methods involved. **The method case was the second symptom, not the defect.** Both the adjudicator and the
refuter had measured a blast radius smaller than the real one — the refuter was right that STACK was wrong and
still under-called how wrong.

**Owner ruling: STACK lists RUNTIME ELEMENTS** — r9 read literally (entry 1 = CURRENT, then the ACTIVATING chain,
penultimate = TOP-LEVEL, last = a space). Recursion is now faithful; method-to-method agrees with ACTIVATING; a
CALL to a separate program is unchanged. The documented cost is that a CONTAINED program yields `MAIN;MAIN; `,
because r9's first entry is outermost-granularity and its chain is element-granularity, so for a nested program
they coincide. That is r9's own composition. The alternative reading (§15.65.1's "a list of all the module names")
reads better for nesting and **cannot represent recursion at all** — recorded in `docs/CONFORMANCE.md` row 213.

## ⚙ "WE ALREADY HAVE A STACK TRACE" — THE RIGHT QUESTION, ASKED AT THE RIGHT TIME

The owner asked why a custom module stack exists when .NET has one. **I had extended that mechanism without ever
asking whether it should exist**, and the design doc records only WHERE the state lives, never WHY it is ours.

Three reasons `StackTrace` is not a drop-in: JIT **inlining elides frames** (tolerable in a diagnostic dump, a
correctness hole for a function a conforming program can branch on — suppressing it means `NoInlining` everywhere,
costing far more than the alternative); CLR frames are **not** COBOL runtime elements (0..n CLR frames per COBOL
element depending on typed / universal / inline invocation, so attributing them is a mapping that tracks the
emitter); and the frame carries COBOL facts the CLR has no notion of — the unit's outermost id (r7), containedness
(r8), main-ness (r5).

**Then the owner said measure, and let that decide.** 5 M activations, emitted code at
`OptimizationLevel.Release`, median of 5 runs:

| build | median | per activation |
|---|---|---|
| no push | 334 ms | — |
| push/pop as landed | 393 ms | **+11.8 ns** |
| push/pop, stack cached | 372 ms | **+7.6 ns** |

⭐ **The cost was the ambient lookup, not the bookkeeping.** `RunUnit.Current` is an `AsyncLocal` and Push/Pop each
resolved it — every activation paid it TWICE. `CobolModule.Stack` resolves once and the emitted method holds it
across the pair: 36% of the cost gone, golden byte-identical. A perf fix that fell out of asking where the time
actually went rather than assuming the list was to blame.

⚠ **And it retired a proposal of mine.** I had suggested eliding the push when a compilation unit contains no
`MODULE-NAME` reference. It is **unsound under separate compilation**: a method that elides its push is still on
the stack when a separately-compiled program it CALLs asks ACTIVATING, and that program gets the wrong activator —
the same reason the CALL-side push lives in the runtime rather than in emitted code. Better to retract it here
than leave it in a note for someone to implement.

## 🔬 THE WAVE-LOCAL GATE WAS TOO NARROW, AND THE BATTERY CAUGHT WHAT IT MISSED

I ran the wave-local gate as `--filter "FullyQualifiedName~Corpus"` and got 440/440 green. The comprehensive
battery then came back **NOT GREEN** on one conformance test the filter never selected:
`IntrinsicFunctionDifferentialTests.ModuleName_StackFromNestedProgram_2023`.

It was **a green test pinning the behaviour the owner decision removed**, and its comment stated the rejected
reading as fact: *"STACK collapses same-unit frames to ONE runtime element (OUTERM), not the OUTERM;OUTERM
duplicate a per-CALL-frame walk gives."* Exactly the shape of the D-B1 unit test replaced two entries ago —
`feedback_green_test_can_hold_a_gap_open`. Updated to the new value with the reasoning and the §4.2.16 pointer, so
the next reader sees WHY the duplicate is right rather than inheriting an assertion.

⚠ **The filter was the mistake, not the test.** The `gate` skill defines wave-local as "the fix's own tests +
immediate neighbours + the relevant unit filter"; I took only the corpus half. A change to MODULE-NAME semantics
had a name to filter on — `~ModuleName` — and running both gives 447/447 in the same ~20 s. The tiering worked
(the comprehensive gate is exactly the net for this), but it cost a full battery run to learn something a wider
two-term filter would have shown in seconds. **Pick the filter from what the CHANGE touches, not from where the
new goldens live.**

⚖ **What the measurement did NOT do is rescue `StackTrace`.** 7.6 ns is small enough that the three blockers still
decide it; the number just prices the alternative honestly instead of leaving it an argument.

