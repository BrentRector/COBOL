## Entry 1136 - 2026-07-30 21:40 PDT - PB10's clean half, and two wrong diagnoses of my own caught by checking the mechanism

PB10 is the widest defect the review has found: §8.4.3.1.2 Format 1 makes a function-identifier an IDENTIFIER and
§8.4.3.2.3 SR1 bars one only from a RECEIVING operand, so every identifier-N SENDING position admits one — and
the grammar reached `functionCall` from exactly four rules, so the rest rejected legal source outright.

**Landed: the four positions the standard itself defines as MOVE sending items.** §14.9.51.4 GR5a makes
`WRITE … FROM identifier-1` equivalent to `MOVE identifier-1 TO record-name-1`; §14.9.35.4 and §14.9.32.4 say the
same for REWRITE and RELEASE; §14.9.20.3 SR4 calls INITIALIZE REPLACING's identifier-2 "the SENDING item" of a
MOVE. So the operand admits exactly what a MOVE sender admits, and that is a derivation rather than a convenience.
All four go through the ONE `WriteSource` helper, so the next FROM phrase inherits the arm — and all four were
verified through their OWN binder path (SequentialIo, KeyedIo, SortBinder, InitializeBinder) rather than inferred
from "the helper is shared", because three of them reach it from different callers.

**THREE THINGS I GOT WRONG, all caught by checking rather than by a test.**

**1. My first attempt broke the build, and it was self-inflicted.** I REPLACED `(dataReference | literal)` with a
reference to `moveSendingOperand` — the tidier "one rule" shape. That deletes the generated `.dataReference()` and
`.literal()` accessors, and this grammar is SHARED with the legacy `CobolSharp.Compiler`, so ~8 call sites across
BOTH compilers stopped compiling. I reverted and reported the work as too large to finish. It was not: making the
change ADDITIVE — `(functionCall | dataReference | literal)` — preserves every accessor, and both compilers build
with zero errors. The blocker was my edit, not the problem. The unification is still the better shape and belongs
at P15, when the legacy side is deleted rather than migrated.

**2. I diagnosed a red as a program-name collision, wrote the fix, and the theory was wrong.** The full battery
came back 4159/4160 with `NumvalC_Formats_PinnedToSpec` failing — a test PB10 cannot semantically reach. It passed
in isolation, so: order dependence. I found that all 73 `Program()` call sites in that class share the PROGRAM-ID
`IFTEST`, matched it against `unique_programid_per_test` ("or .NET serves a stale same-named assembly"), and made
the ids unique. That broke 24 differential goldens, which are keyed by a HASH OF THE SOURCE — renaming the program
changes the source and orphans the golden. Before chasing that, I checked the mechanism I had assumed: `CutRunner`
spawns a SEPARATE PROCESS per test (`ProcessStartInfo("dotnet", dllPath)`), so no `ProgramTable` is shared and a
name collision is impossible in this harness. The memory pattern-matched but describes IN-PROCESS compilation.
Reverted. **A remembered pattern is a hypothesis, not a diagnosis** — and I nearly rewrote 73 call sites and
re-baked 24 goldens on it.

**3. So I did not claim a flake; I re-ran the suite.** Second full run on the identical tree: **4160 / 4160, zero
failures.** Two runs, same commit, different outcomes — that is non-determinism in the harness, not a PB10
regression. The suspected mechanism is the parallel-contention class §0 already documents: each test spawns a
`dotnet` process with a 30-second timeout, and a fully parallel 4160-test suite can exceed it. ⚠ **I did NOT
prove the timeout** — the quiet log does not carry the assertion detail, and the second run gave nothing to
inspect. So it is recorded as "non-reproducible, mechanism suspected but unproven", which is what I actually know.
The dangerous direction is the other one: a harness that can produce a false RED under load can mask a real
failure, so this is worth a named investigation rather than a shrug.

**Split out as PB17:** the SUBSCRIPT and REFERENCE-MODIFIER positions are NOT parse errors, which is what the
findings said. They compile clean and throw `NotImplementedCobolFeatureException` at RUN TIME — the PB7/DA7
wrong-stage family. Both lex in SUBSCRIPT mode, so the operand reaches `ReferenceResolver`'s flat-token segment
renderer, which has no arm for a nested FUNCTION call. Folding that into a grammar fix would have buried a
different root cause, and the durable fix is the renderer anyway, since D10/PHASE-15 removes SUBSCRIPT mode.

**Still open on PB10:** INSPECT, which needs a per-FORMAT bind-time screen — identifier-1 is sending only in
Format 1 (TALLYING), and in Formats 2/3/4 it is modified in place, where SR1 bars a function-identifier.
Admitting it unconditionally would accept ILLEGAL source, so this one cannot be a grammar widening.

