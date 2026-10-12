## Entry 924 — 2026-07-19 19:00 PDT — The grammar-batch research pass: 7 decision-complete specs, and all 14 adversarial verdicts came back NEEDS-CORRECTION

Baseline battery confirmed green at `6d6c655d` and matching §0 exactly (Conformance **3699** · unit **313** ·
characterization **33** · legacy **167**, zero failures) — so the §0 numbers are not stale, and source edits are
cleared.

Then the research pass for the grammar batch: a 22-agent workflow (2.83M tokens, 0 errors, ~27 min) producing a
decision-complete implementation spec per construct, each then verified by **two independent adversarial lenses** —
SPEC FIDELITY (independently re-read the ISO text; hunt omitted formats/rules/tables, paraphrased-not-verbatim
quotes, wrong edition attribution) and CODE REALITY (open every cited `file:line`; hunt already-implemented
duplication, ANTLR DFA hazards, project-invariant violations).

**All 14 verdicts returned NEEDS-CORRECTION.** Not one spec survived clean. That is the headline: a single-pass
research fan-out — which is what I would have shipped without the verify stage — would have put real defects into
every one of the seven constructs. The verification cost roughly half the tokens and paid for itself several times
over. Representative catches:

- **C5 PERFORM Format 3 — a straight spec inversion, in BOTH directions.** The design *builds* the GO TO path that
  §14.9.17.3 SR3 flatly forbids ("A GO TO statement shall not be specified in a WHEN phrase of an exception-checking
  PERFORM statement") — the `return TargetPc` handler path and the whole `>=0` protocol arm exist for it — while
  *excluding* `RESUME AT NEXT STATEMENT`, which §14.9.33.3 SR1 expressly permits, on the strength of an openQuestion
  that admits §14.9.33 was never read. Separately: `TurnState.Empty` is a process-wide **static singleton** returned
  whenever a group has no `>>TURN` (the common case), so pushing scope frames onto it leaks state across compilation
  units and across parallel test compilations.
- **VF4 VALUE Format 2 — the fix would have broken its own headline golden.** It confirms a *live* silent-data-
  corruption bug (`VALUE 1 2 3` binds `123` with zero diagnostics at every edition today), but wrote the initial
  capacity into `OccursSpec.InitialCap` — which is "FROM integer-4, the minimum capacity", used by the runtime as a
  permanent FLOOR. The Annex D.3.7 town-record example would have had its *minimum* set to 3, silently clamping a
  later SET to 1. Needs a distinct `ValueInitialCap` field.
- **C6B SUPPRESS WHEN — a false current-state claim that collapsed the risk argument.** "alternateKeyClause is not
  shared" is flatly false: it is referenced from BOTH `CobolIO.g4:62` and `CobolData.g4:63`. The DEVLOG-736 shared-
  rule over-fire hazard applies after all, and `DataBinder.cs:772`'s FD clause loop has no arm for it — an FD-level
  occurrence would fire 0900 below 2023 and silently no-op at 2023, the worst possible edition asymmetry.
- **Wave H — the SEND gate was dead code.** Predicate `{facilityWord("RECEIVE")}?` guarding a `(RECEIVE | SEND)`
  rule: false for any SEND-led statement, so half the MCS surface never parses — while the spec's own golden asserts
  two warnings. And the greedy `(~DOT)*` swallow eats `END-IF`, breaking every block-structured context (`statement`
  is reachable from `statementBlock`), producing *no* named warning — defeating the entire point of recognize-and-name.
- **VF2 COLLATING — systematically wrong citations headed for user-visible text.** "§12.4.5.2 SR1(g)" is really
  §12.4.5.3 **GR**1(g) (six occurrences, one of them inside a diagnostic message string); "§12.4.5.2 GR6" is really
  §12.4.5.3 GR6 (~8 occurrences, including the `constructs.json` citation field). §12.4.5.2 is the *Syntax-rules*
  subclause and has no general rules at all — and the very file being edited already cites the sibling correctly
  (`BinderDriver.cs:167`). Exactly the [[feedback_spec_fidelity_discipline]] failure: a cited § must MATCH, not merely
  sound plausible.

**Two things are already landed and must NOT be re-implemented** — both research packets were stale on the point:
PERFORM **UNTIL EXIT** (grammar, binder, `PerformForever`, `while(true)` emission, 0900 gate, constructs row, passing
golden) and **C6-A WRITE BEFORE AND AFTER ADVANCING** (verified across all six layers). Only UNTIL EXIT's §14.9.28.3
SR8 lexical-nesting residue remains, and it rides the RW SUPPRESS slot as a micro-item.

This entry records the research + verification; the distilled working doc, the owner escalations (two constructs
recommended for deferral on *unadjudicated spec forks*, plus the VF2 scope posture), and the implementation follow.
Raw structured evidence — every spec and both verdicts — persisted at
`docs/rearchitecture/evidence/PHASE-13-grammar-batch-research.json` (796 KB) so no downstream session re-derives it.

