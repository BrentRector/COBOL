## Entry 843 — 2026-07-15 22:56 PDT — PHASE-08 CLOSED (Step 10) — SwitchStore joins RunUnit (the §5 gate's find); the doc sweep lands

**Close-out commit for PHASE-08** (`docs/rearchitecture/PHASE-08-runtime-library-reorg-rununit.md` STATUS → `DONE`).

**The §5 hidden-mutable-static gate paid for itself:** `ExternalSwitches`' switch cache (a process-global
`ConcurrentDictionary`) holds RUN-UNIT-scoped state by the spec's own words (ISO §12.3.7 GR4 NOTE 1 — "switch
scope is the run unit") and by its own doc comment — a miss under the phase's gate ("any mutable run-unit state
still static is a miss"). Converted on the established pattern: instance `Control/SwitchStore.cs` on
`RunUnit.Switches` (a plain `Dictionary` now — single-thread run-unit state; the `Concurrent` wrapper was only
ever needed because the store was process-global) + the name-stable `ExternalSwitches` static shim (`Get`/`Set`
are emitted — SET Format 3 §14.9.39 GR5 + switch-status conditions §8.8.4.6). The environment-probe
(`COBOL_<NAME>`) + first-read caching semantics are byte-identical. Post-conversion, the §5 grep's survivors are
all genuinely immutable: `ExceptionCatalog` (table), `Pow10` (tables), `SystemClock.Instance` (stateless).

**Step 10 doc sweep (phase-completion discipline):** `COBOLNET_DESIGN.md` §17's runtime layout → the as-built
role-based tree (with the G8-flip note); `DESIGN-runtime-library.md` → Status EXECUTED, §4 steps 1–5 marked
DONE (step 6 = the G8 namespace flip remains), §6's six open questions resolved inline as executed (AsyncLocal
+ lazy `Current`; hygiene-first concurrency posture; shims kept pre-G8; `Values/` nesting; unified
`SequentialConnector`); `PHASE-08-*.md` STATUS → DONE with the as-landed deviations ledger; the master plan's
STATUS BANNER + phase-index row; `resume-prompt.md` → RESUME AT PHASE-09; `CLAUDE.md` snapshot; `DOC_INDEX.md`'s
plan row compressed to a pointer at the plan banner (that row had drifted twice — duplication removed instead of
re-synced).

**Byte-stability (exit criterion 5), the strong form:** `git diff 534f2253..HEAD -- src/Cobol.Net.Compiler
src/Cobol.Net.Frontend src/Cobol.Net.Cli` is EMPTY across the whole phase — the compiler that renders C# is
untouched, so emitted source is byte-identical by construction; the 33-snapshot characterization gate rode every
commit's battery as the belt-and-suspenders check.

**Battery at this commit:** conformance 3256 · unit 281 · legacy guard NIST 353 MATCH / ALL GREEN.
**PHASE-08 grand total:** 3 commits (DEVLOG 841/842/843), each verdict-gated on the full three-suite battery;
zero regressions anywhere in the phase. NEXT: PHASE-09 (M2 OO rearchitect & complete).

