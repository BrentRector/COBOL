## Entry 1186 — 2026-08-05 22:52 PDT — the tracking-doc sweep: five notes said OPEN while their frontmatter said landed, and the register had no way to notice

**A status written in two places drifts, and this one had drifted five times.** Every `kb/Work/` note carries its
status twice — in the frontmatter (`status: landed`, which is what `work.py next` reads) and again in the H1
heading (`# PB45 — OPEN — …`, which is what a HUMAN reads first). Flipping the frontmatter is what makes the
worklist correct, so nothing ever contradicted the heading. **PB26, PB36, PB41, PB43 and PB45 all read OPEN while
being landed** — including PB45, which I closed myself earlier today and whose frontmatter, title and body I had
all updated.

Fixed in all five, and then made mechanical: `work.py check` now compares the H1's status word against the
frontmatter and fails with the reason. ⚙ **Proven in the failing direction before being trusted** — reintroducing
the drift in PB43 produced the named error, and restoring it went clean (`feedback_green_gates_arent_evidence`).
That is `feedback_one_rule_one_place` in miniature: the extraction is not always possible (a heading is prose),
but the AGREEMENT can be enforced.

**Stale instructions pointing at retired registers.** Three docs still told a reader to maintain things that no
longer exist: `COBOLNET_DATA_MODEL_DESIGN.md` said "the live remaining-work tracker is the plan's §0" — which is
wrong twice, since §0 is live state and explicitly NOT a worklist (CLAUDE.md rule 8), and the register is
`kb/Work/`; and `DESIGN-ec-oo-superbatch.md` both sourced its "live tally" from the retired
CONFORMANCE-FIX-QUEUE's LANDED header and instructed each commit to UPDATE that header. All three now point at
`kb/Work/`. The kb-sync skill names this case exactly: "If a doc still tells you to tick a tracker box or update
a queue header, that doc is stale — fix it on discovery."

**⛔ AND ONE OF THE STALE CLAIMS WAS MINE, FROM THIS SESSION.** §0's handoff opened "THIRTEEN COMMITS, ELEVEN
FIX-QUEUE ITEMS" — against an actual ten commits, in a paragraph whose very next line says never to quote a number
from it, for a session spanning two days where no single count is well-defined. **The correction is to DELETE the
tally, not to replace it with a number that rots tomorrow**; counts come from `git log` and `work.py stats`. The
same paragraph's enumeration of newly-filed defects was also already out of date (PB49 had been filed since), so
it now points at `work.py next` instead of listing them — §0 carries narrative, never inventory.

**A memory that had outlived its subject.** `project_remaining_work_tracker.md` still existed and still
instructed the reader to mirror `kb/Remaining Work Tracker.md` and update the fix queue's LANDED header — with the
superseding banner at the BOTTOM, twelve lines below the wrong instruction. The memory INDEX claimed the note had
been deleted; it had not. Deleted, and the index line removed.

**⭐ THE SWEEP CLOSED A FINDING NO WORK NOTE KNEW ABOUT, AND OPENED ONE.**
`docs/rearchitecture/evidence/PHASE-B-15.32-15.44-findings.md` had recorded PB25's figurative-argument defect
independently, in more detail than PB25's own note, and it was never filed as a work item — so the register could
not see it. Its scope is the accurate one: the loud arm was reached by EVERY §15 function taking a string argument
through `Str()`/`StrArgList` — FIND-STRING, TRIM, SUBSTITUTE, CONVERT, UPPER-CASE, LOWER-CASE, REVERSE, LENGTH,
NUMVAL/NUMVAL-C, ORD, DISPLAY-OF/NATIONAL-OF — not the two functions PB25 named. ✅ Its own repro was re-run
against the landed fix: `FUNCTION FIND-STRING(H SPACE)` over `"AB CD EFG"` returns **3**, the value §15.37.4 r1
requires and the number that file predicted; `FUNCTION TRIM(H SPACE)` returns `AB CD EFG`. It had also prescribed
the fix SHAPE — route the non-field leaves through the same `OperandText.AsString` entry rather than bolting on
two arms — which is independently what landed.
⚠ **And it named a guard that did NOT land: PB49.** "Add a drift test asserting every `BoundOperand` leaf renders
identically through the intrinsic-argument channel and the DISPLAY channel." Without it the two channels can
re-diverge silently, which IS PB25's root cause rather than a detail of it. Filed with the caveat that the two
channels are legitimately allowed to differ on `BoundComputedOperand`, so the guard must be per-leaf with the
intended answer stated — a blanket "these two are equal" test would be wrong.

**Checked and found CORRECT, recorded so the next sweep skips them:** `constructs.json` needs no row for the
SPECIAL-NAMES LOCALE clause (A.4.9 documented non-support is not a construct — the locale FUNCTIONS have no rows
either, and `ConstructRegistryDriftTests` agrees); `docs/DIAGNOSTICS.md` carries COBOLNET1634; the derived
conformance burn-down matches the inventory (`gen_conformance_notes.py --check`); no corpus or battery number is
duplicated outside §0's BATTERY REFERENCE; and the P13-recorded wrong citation `§13.6.2 GR7` → `§11.9.10.4 GR7`
had already been swept everywhere, with the replacement re-validated by `cite.py --check`.

**Gate.** `work.py check` 77 items well-formed · `gen_conformance_notes.py --check` 14 notes matching · Unit
`~Drift|~TestRepo` **2822/2822**, zero skipped.

