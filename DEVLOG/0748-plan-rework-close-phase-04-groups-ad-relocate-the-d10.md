## Entry 748 — 2026-07-10 18:18 PDT — Plan rework: CLOSE PHASE 04 (Groups A–D); RELOCATE the D10 SUBSCRIPT-mode removal → PHASE 15 §"CUT 2.5"

Owner direction: remove the D10 deferred-open stamp from PHASE 04 so the phase can close, and move the D10 step to
where it is realistically doable. Done as a plan/doc rework (no code).

**Why D10 can't live in PHASE 04, and where it goes.** D10 (fully remove the SUBSCRIPT lexer mode + the ~250-line
binder subscript re-parse → interpreted grammar rules) is blocked inside PHASE 04's byte-neutral window by the finding
in `DESIGN-frontend-grammar.md §9`: the FROZEN legacy compiler consumes `SUB_*`/`SubscriptEntryContext`
(`ExpressionBinder.BindSubscriptEntry`), so the SUBSCRIPT machinery cannot leave the SHARED grammar until the legacy
tree is deleted. That deletion is **PHASE 15 Cut 2**. So D10 is realistically doable only at/after Cut 2 — it is now
**PHASE 15 §"CUT 2.5"**, sequenced immediately after the legacy delete, staged D10.1–D10.5 (§9.5), gated on the §9.4
ISO §8.3.5 space-separator decision. The owner's "fully remove" ruling (master §6 D10) is unchanged — only the schedule
moved. (This supersedes the DEVLOG-746 "keep Phase 04 open with D10 as a deferred tail" disposition.)

**PHASE 04 → CLOSED.** Groups A+B+C+D are DONE (byte-neutral consolidation: word set single-sourced + drift-guarded;
shared literal fragments; typed `Cst/` façade + 2 anchors migrated; version-conformance leg reconciled). All 5 exit
criteria hold (criterion 5 was reconciled — the surviving predicates are load-bearing cross-edition disambiguations,
not only the two forward-detects; the annotation side-table is superseded by P3's two-arm pass). D10 is no longer a
PHASE-04 criterion, so the phase closes clean.

**Docs swept:** PHASE-04 STATUS→DONE + disposition/override/execution-log notes; PHASE-15 doc gains the §"CUT 2.5"
sub-track (+ GOAL/exit-criterion-0/title/risk); the master roadmap banner + §4 table (P04 ✅, P05 ◐, P15 scope += D10)
+ §6 D10 row + scope-expansion note; `DESIGN-frontend-grammar.md §9` reframed DESIGN→SCHEDULED-in-P15; resume-prompt +
MEMORY + `project_phase04_d10_deferral`. **Active go-forward work: PHASE 05 Step 2** (StorageForm parallel SSOT, D0).

