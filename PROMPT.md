# WiseOwl COBOL — Engineering Doctrine

> **This file is DOCTRINE — the standing "how we build" that does not change session to session.**
> It deliberately does not restate what other files own:
> **process rules → `CLAUDE.md`** · **the work register → `kb/Work/` (the ONLY one — CLAUDE.md rule 8)** · **live state → plan §0** · **anti-pattern catalog → `CONSTRAINTS.md`** ·
> **design → `docs/COBOLNET_DESIGN.md` + the deep-dives** · **history → `DEVLOG.md`**.
> Where this file and one of those disagree, the owning file wins and this one gets fixed.

## 1. The mission

Build a **commercial-quality COBOL compiler, sustainable for decades** — 100% conforming to ISO/IEC 1989:2023 per
§4.2.1 with correct 1985/2002/2014, emitting idiomatic typed-native C# through Roslyn.

There is **no backward-compatibility requirement and no existing user base**. Any subsystem may be redesigned,
re-architected, or rewritten when that yields the cleaner long-term design. "Minimal blast radius" is never a reason
to ship the worse design; work now is a fraction of the long-term maintenance cost.

Target the **latest stable .NET and C#**. Moving forward a release is pre-authorized — re-prove the battery green on
the new runtime and clean-build with warnings-as-errors before committing.

## 2. What "done" means for a change

A change is finished when all of the following are true. If one is not, the change is not done — it is debt.

- **Complete to the spec and the deep-dive**, not to what a test happens to reference.
- **Root-caused.** No workaround, no shim, no relabeling a defect a "quirk", no editing valid COBOL around a bug.
- **Swept.** Every sibling instance of the same pattern is fixed in the same pass.
- **One mechanism.** No second type/helper/dispatch doing a job something already does.
- **Cited.** Each implemented rule carries its exact ISO §/GR in the code.
- **Proven.** Its spec-derived golden exists, is registered in the same commit, and the wave-local gate is green.
- **Documented.** Docs current in the same change set; a `DEVLOG.md` entry; committed and pushed.

## 3. Architectural commitments

These are settled. Design *within* them; do not relitigate them.

1. **Typed-native data only.** A COBOL record IS a `record struct`; an elementary item IS a native field. A `byte[]`
   appears only at a file boundary or a mixed-USAGE REDEFINES codec. There is no byte `ProgramState`.
2. **Phases are separate and one-directional.** No semantics in the parser, no codegen in the binder, no runtime
   logic in compile-time structures, no write-back from a later phase into an earlier phase's model.
3. **The bound tree is the single semantic model.** There is no separate lowered IR; a backend lowers privately.
4. **Backend-neutral.** No C# text, Roslyn syntax, mangled identifier or format literal enters a bound node.
   Neutrality is proven by a second backend, not asserted.
5. **Leverage the tooling.** ANTLR owns all syntax; a Roslyn source generator owns C#→C# generation and the
   exhaustive bound-tree visitor. A hand-rolled parser, tokenizer or tree-walk beside them is the anti-pattern.
6. **Structural over conventional.** A pass manifest whose dependency DAG is asserted at construction, and a
   generated visitor where a missing arm is a compile error, beat a comment asking the next reader to remember.
7. **Four editions, one executable.** Every construct owes both its per-edition behavior and the correct diagnostic
   when the targeted edition lacks or removed it. Diagnosing is half the product.

## 4. The four required reviews

This is a commercial product with a decade-plus lifetime, so the owner requires four review dimensions — as
continuous criteria on every change, and as a comprehensive pass over the whole source once the design has settled:

1. **Architecture** — layout, naming, single-responsibility, no god classes, clean phase boundaries.
2. **Full code review** — correctness, clarity, comment/doc accuracy, idiomatic modern C#, error handling.
3. **Performance** — hot paths, allocations, data-structure fit, compile throughput.
4. **Duplication and efficiency** — repeated logic, parallel mechanisms, redundant computation.

Findings become tracked work, never prose that evaporates.

## 5. Working style

- **Autonomous.** Fix, test, commit and push each checkpoint. Stop only for a genuine owner decision, asked as a
  bare question, one at a time. Never end a turn asking permission to continue.
- **Spec-first, always in that order.** Derive the expected result from the § and write down the citation *before*
  reading the implementation, building a repro, or looking at the diff.
- **Gate by blast radius.** Wave-local per commit; the comprehensive battery once per accumulated batch, pre-merge.
  Read the verdict line, then commit as a separate step.
- **Honest reporting.** Log missteps, dead ends and friction in the DEVLOG. Never write "verified" or "complete"
  without the evidence in hand. A "flake" verdict requires a named test and a clean re-run of that test.
- **Token-frugal, restart-safe workstreams (owner standing instruction, 2026-09-02).** Every fleet, lander,
  implementer and adjudication workflow runs so that a session-limit kill costs at most one step and a restart never
  repeats work: checkpoint to DISK (WIP commits + `STATUS.md` per worktree; one JSON line per rule per workflow
  stage), replace a killed agent with a FRESH one reading the checkpoint (never resume a long transcript), keep to the
  concurrency budget (1 lander + ≤3 implementers + one 4-wide read-only chunk), land finished work first with one
  lander on main at a time, and allocate ids centrally. The operational SSOT and the brief/workflow templates are the
  `workstream` skill (`.claude/skills/workstream/`) — invoke it before dispatching anything.
