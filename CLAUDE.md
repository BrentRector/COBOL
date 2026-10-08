# WiseOwl COBOL — Claude Code Instructions

## ⛔ Non-negotiable process rules
Owner-emphasized, each earned by a correction. These nine are the SSOT; `PROMPT.md` holds the standing doctrine
(mission, architectural commitments, the four required reviews) and does not restate them.

1. **The ISO/IEC 1989:2023 spec (`specs/ISO_COBOL.md`) defines correct behavior for EVERY case.** Read it and cite
   the exact §/GR for any semantics, syntax, or output question. The NIST goldens and the
   GnuCOBOL differential are regression NETS with known holes — never authority. **Where there are several
   implementation options, the precedence is: the ISO spec if it controls → otherwise GnuCOBOL → if GnuCOBOL has
   nothing, IBM Enterprise COBOL or Micro Focus** (owner, 2026-08-08 via `kb/Work/R13`, restated 2026-09-22). This
   settles implementor LATITUDE only — never GnuCOBOL's non-ISO extensions — and the choice is documented in
   `docs/CONFORMANCE.md`. When a general-format DIAGRAM is
   load-bearing, render the PDF page (`scripts/render-spec-page.py <page>`): the OCR'd diagrams were systematically
   lossy toward falsely-restrictive syntax.
   **⛔ VALIDATE EVERY CITATION MECHANICALLY — `python scripts/spec/cite.py --check <clause> "<text>"`.** The
   failure mode is not inventing a citation, it is INHERITING one: a queue entry or design doc carries a §, its
   quoted text is genuinely in the standard, and the clause NUMBER is never re-derived before it propagates into
   code comments, goldens and the DEVLOG. Two CA10 citations were wrong exactly this way. A citation you did not
   run `--check` on is not a citation.
2. **Implement each feature FROM its subsystem deep-dive design doc** (`docs/COBOLNET_DESIGN.md` §0.5 lists them)
   plus the spec. Follow the doc; do not improvise. A design correction updates the doc in the same change set.
3. **Implement the COMPLETE feature to spec + design — never scope to what a test references.** Tests verify, they
   do not scope. Deferral, a GAP, or rejecting legal source is debt, and only an explicit owner decision.
4. **Fix the root architectural cause.** No workarounds, no papering over, no relabeling a bug a "quirk", never
   change valid COBOL to dodge a compiler bug. Every bug is a pattern — sweep for its siblings.
   **⛔ NO WRAPPERS, SHIMS, FORWARDERS, ALIASES OR COMPATIBILITY LAYERS, anywhere: compilers, runtime, tests,
   scripts, tooling, skills.** There is NO backward-compatibility requirement. When something moves, is renamed or
   is replaced, change EVERY caller and delete the old one in the same change (owner, repeated 2026-09-28).
5. **RE-DESIGN AND RE-ARCHITECT WHEN NECESSARY — a stated scope is an estimate, never a ceiling.** This compiler
   is production quality and must stay supportable for MORE THAN A DECADE, so when a queue entry, finding or
   design doc says "one clause, no structural change" and implementation proves otherwise, the answer is the
   restructuring — never the smallest diff that fits the estimate, and never a hand-maintained list where a
   structure belongs. **Prefer the shape that makes the NEXT case automatic over the one that makes this case
   small**, and pair it with a drift test so "automatic" stays true. Correcting the estimate is part of the work:
   update the design doc and say so. (Rule 3 forbids shrinking the FEATURE; this forbids shrinking the DESIGN.)
6. **Keep the docs CURRENT in the same change set.** Every doc except `DEVLOG.md` describes the CURRENT compiler.
   The historical narrative lives ONLY in `DEVLOG.md`, which is DESCENDING — add each new entry at the TOP, under
   the ordering note, headed `## Entry NNN — YYYY-MM-DD HH:MM TZ — Title` (stamp from `date "+%Y-%m-%d %H:%M %Z"`).
7. **Work autonomously.** Commit AND push every checkpoint, with a forensic commit message and a DEVLOG entry.
   Grammar changes are pre-authorized. Prompt only for genuine owner decisions — one at a time, as a bare question.

8. **⛔ THERE IS EXACTLY ONE WORK REGISTER — `kb/Work/` — AND YOU MAY NOT CREATE ANOTHER.** One note per item
   (`kind:` defect · analysis · adjudication · decision), tracked in git, frontmatter carrying `status`,
   `severity`, `area`, the harm flags, `inventory_rows` (the traceability-inventory rows the note CLAIMS while
   it is open) and `closes_rows` (the rows its landing CLOSED, with a `closes_rows_reason:` when it closed none);
   the forensic prose lives in the note body. `kb/Work.base` is the view.
   **Read it with `python scripts/spec/work.py next` and keep it CURRENT in the same change set as the work** —
   a landed fix flips its note's `status` AND writes `closes_rows` in the commit that lands it, and a newly
   found defect becomes a note before it becomes a DEVLOG paragraph.
   ⛔ **DO NOT open a new list, table, tracker, checklist or "remaining work" section anywhere — not in plan §0,
   not in a design doc, not in a new markdown file, not in a JSON sidecar.** Five such registers accumulated by
   2026-08-04, three of which each declared themselves canonical, and the cost was measurable: a WRONG-ANSWER
   defect (`EXCEPTION-STATEMENT` returns `GO` where Table 12 requires `GO TO`) sat inside a prose paragraph where
   no work list could see it, while §0's own duplicate table rotted into listing landed items as open. **If you
   feel the urge to start a list, add notes to `kb/Work/` instead.** The only other registers are the ones
   derived mechanically from the spec (the rule catalog → the traceability inventory → its generated burn-down)
   and `constructs.json`; those are GENERATED or CI-owned, never hand-maintained work lists.

9. **⛔ NO DISPATCH WITHOUT THE `workstream` SKILL, AND AN AGENT FIXES WHAT IT FINDS.** Before ANY implementer, lander
   or fleet dispatch, invoke the `workstream` skill and follow `.claude/skills/workstream/templates/MANDATORY-PRACTICES.md`:
   render specs with `make_dispatch_specs.py`, get `check_practices.py` GREEN, read the quota meter first, dispatch
   through the Workflow rolling wave. `scripts/hooks/dispatch_guard.py` REFUSES the call otherwise (owner 2026-09-30,
   after a session hand-wrote three briefs and skipped the skill: "all future sessions cannot skip learning this").
   Token frugality is met by the skill's levers, never by skipping it. And an agent that has already read a file
   and finds NEW issues in it FIXES them in the same change, with golden and sibling sweep, unless the effort is so
   excessive (a redesign beyond its slice, or its turn budget) that the issue must become a `kb/Work/` note instead
   (MANDATORY-PRACTICES I9, owner 2026-09-30).

## Start here every session
1. **`kb/Work/` — THE WORK REGISTER, and the answer to "what do I do now".** Run
   `python scripts/spec/work.py next`; `kb/Work.base` → **Fix next** is the same list, sortable. It ranks on what
   a defect DOES to a user's program, not on its severity label. ⛔ Never re-derive a worklist from prose.
2. **`docs/COBOLNET_REARCHITECTURE_PLAN.md` §0** — live state, gates, open GAPs, owner decisions and the campaign
   narrative. ⛔ **It no longer carries a worklist and must never regrow one** (rule 8). Trust §0 over any status
   written anywhere else, including memory — but trust `kb/Work/` over §0 for what is OPEN.
3. **`pwsh scripts/session-probe.ps1`** — the mechanical state check (branch · dirty/unpushed · next-free
   diagnostic code · VCR todos · corpus counts · inventory GAP). Never hand-read state a script can compute.
4. Before the session ends: update the `kb/Work/` notes you touched, update §0, add a DEVLOG entry.

## The project
WiseOwl COBOL (repo `BrentRector/COBOL`; code `src/Cobol.Net.*`, exe `cobol`, NuGet `WiseOwl.COBOL`) compiles COBOL into **idiomatic typed-native C# built by Roslyn**: a
COBOL record IS a .NET `record struct`, an elementary item IS a native field. **There is NO byte `ProgramState`
substrate — never reintroduce one.** The legacy CobolSharp engine that had one is deleted from `main` (PHASE 15 Cut 2,
kb/Work PB2110); `docs/rearchitecture/LEGACY-ARCHIVE.md` names the tag that preserves it.

**Mission (owner decision D13):** a commercial-quality, decades-sustainable compiler that is **100% conforming to
ISO/IEC 1989:2023 per §4.2.1, with correct support for 1985/2002/2014** — validated as four per-edition compilers
by the VERSION TEST MATRIX (`docs/VERSION_TEST_MATRIX_DESIGN.md` + `docs/VERSION_CHANGE_REFERENCE.md`; the default
`--std` is COBOL-2023). Done = the PHASE-14 Step-0 traceability inventory at zero GAP.

**The product bar is broader than conformance.** The owner requires four review dimensions — **architecture · full
code review · performance · duplication and efficiency** — as continuous criteria on every change, and as a
comprehensive pass once the design settles (`PROMPT.md` §4).

## Where things live
- **Plan / live state:** `docs/COBOLNET_REARCHITECTURE_PLAN.md` — §0 live state · §3 execution model · §8
  forward-residue ledger · §9 verification commands + corpus mechanics · §11 analysis backlog · §12 risk register.
- **Design SSOT:** `docs/COBOLNET_DESIGN.md` plus the `docs/rearchitecture/DESIGN-*.md` deep-dives.
- **The work register:** **`kb/Work/`** — ONE note per item (defect · analysis · adjudication · decision),
  tracked in git, with the forensic prose in the note body. `kb/Work.base` is the view; **`Fix next`** ranks
  on what a defect DOES to a user's program, not on its severity label. `python scripts/spec/work.py next`
  prints it and session-probe shows it every session. ⛔ It replaced FIVE overlapping registers, three of
  which each claimed to be canonical; `CONFORMANCE-FIX-QUEUE.md` is now a pointer.
- **History:** `DEVLOG.md`, and nowhere else. **Doctrine:** `PROMPT.md`.
- **Doc map:** `docs/DOC_INDEX.md` — consult it to find the right doc and keep it in sync. Exactly one canonical
  doc per subsystem: extend it, never fork a second.
- **Spec:** `specs/ISO_COBOL.md` (tracked in this repo). The licensed PDF is the private submodule `specs-private`
  (`git submodule update --init --recursive`) — needed only to render a page (`scripts/render-spec-page.py`).

## Testing
The owner decided on 2026-09-28 (kb/Work PB1708): ORDER, DON'T SKIP. Every gate runs the whole discovered
population of Conformance, Unit and Characterization, and no gate filters. Per commit, an implementer runs
`pwsh scripts/build-local.ps1 -Mode implementer -Priority BelowNormal` (MANDATORY-PRACTICES I1/I2; kb/Work PB1721):
two legs with the likely failures first, fail-fast, inside a gate slot that caps how many implementer gates run at
once (one shared cap, `gate_slot.py set-cap`, kb/Work PB2514). **Batched-gating trial (owner 2026-10-07, kb/Work
PB2515, until Sat 2026-10-10 10:00 PDT):** while `gate_slot.py`'s implementer scope is `leg1`, an implementer gate
runs leg 1 only and its verdict reads `LEG 1 ONLY (batched-gating trial, PB2515): GREEN|RED`; the lander's
whole-population gate is then each change's population check, and the implementer leaves the Linux gate to the
lander. A lander runs `-Mode lander`: the whole population in one leg, with no slot. A stale or missing impact map only
changes the order, never what runs, and impact maps are recorded on demand, never per commit. Run the comprehensive
battery plus the GnuCOBOL differential once per accumulated batch, pre-merge, in its own worktree. **CI also runs on
Linux, so every gate runs CI's Linux legs under WSL before a push** (`scripts/linux-gate.sh`, kb/Work PB1732). Every
implementer and lander runs all four legs (unit, characterization, conformance, and CI's `guard` job), about 6 minutes. Build `Cobol.Net.sln` (not a single project) before
any `--no-build` run. Commands and the current battery baseline are in plan §0 "Gates" and §9.
⛔ **THE CI INVARIANT (owner 2026-10-04): GitHub CI must never fail when the same code was tested locally by the same
processes.** CI is the verification of the local process, never its first run. A CI red is therefore ALSO a defect in the
local gates: find why they did not see it and fix that in the same change (a missing leg, an unpinned tool, a setting CI has
and the local run lacks), then sweep for the others like it (rule 4). `kb/Work/PB1957` is the survey of the known
differences and carries the drift test that maps every CI job to a local leg or to a recorded reason it cannot run locally.
