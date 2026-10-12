## Entry 1021 — 2026-07-26 15:09 PDT — Memory consolidated 91→55 files, PROMPT/CONSTRAINTS rewritten for Opus 5, and the rename swept through 35 docs + 14 source files

Entry 1020 fixed the *plan*. This entry fixes everything else a session reads before it acts: the durable memory
set and the two doctrine files. The owner's framing was "update all memories, session saved state files to work
best with Opus 5. Clear out over specificity."

**What the audit found.** The memory directory held **91 files / 25,511 words** — roughly a third of it session
narrative rather than durable fact: DEVLOG numbers, commit hashes, dated correction stories, and in two cases a
full progress log embedded in a memory. Three defects were mechanical and provable:

- **44 of 91 files had a `name:` that did not match the filename** (`feedback_use_the_spec` was internally named
  `always-implement-from-the-specification-grammar-and-behavior`). Since `[[links]]` resolve by the `name` slug,
  this produced **15 dangling wiki-links** — the memory graph was substantially disconnected from itself.
- **11 of 13 repo files cited by memories no longer exist** — `resume-prompt.md`, `ROADMAP.md`,
  `docs/MASTER_PLAN.md`, `docs/DATA_MODEL_ARCHITECTURE.md`, `RECORD_STRUCT_STORAGE_DESIGN.md`,
  `docs/dialect-strictness.md`, `PROJECT_PLAN.md`, `MIGRATION_LEDGER.md`, and more. Memories were routing future
  sessions to deleted files.
- **One memory was actively wrong about the current code.** `feedback_managed_pointers` described `ManagedPointer`
  as `byte[] Buffer, int Offset, int Length` with "deref is a bounds-checked `AsSpan`". The real type is now
  `abstract class ManagedPointer` + `sealed class ManagedPointer<T>` — typed-native. A memory asserting the byte
  substrate still exists is precisely the failure mode `feedback_typed_native_only` is supposed to prevent.

**What was done.** 91 files → **55 files / 9,590 words**. The reduction is consolidation, not deletion: ~30 pairs
and triples were one rule stated repeatedly (`production_quality_always` + `production_refactor` +
`commercial_quality_north_star` + `target_latest_dotnet` → one `north_star_commercial_quality`; four separate
guard memories → `gate_on_the_verdict_line`). Every surviving rule was kept; what was dropped is the narrative
around it. Frontmatter is now uniform (`name` == filename, one `type`), and both invariants are mechanically
verified: **zero dangling links, zero name mismatches.** The pre-consolidation set is preserved intact outside the
memory directory at `memory-archive-2026-07-26/` with a `RENAME-MAP.md`.

**The part that was not free.** Consolidation renames memories, and memory names are *cited across the repo* — in
design docs and in C# XML doc comments. Leaving them dangling would have re-created the exact disconnection the
pass was fixing. So the rename map was swept through **35 live docs and 14 source files (17 citations)**.
Deliberately NOT swept: `DEVLOG.md`, `PHASE4_RECONCILIATION.md`, and the `PHASE-*`/`SURVEY-*`/`CRITIQUE-*` records
— those are point-in-time artifacts and their period-accurate citations are correct as written
(`feedback_docs_current_state_only`). Solution builds clean, 0 warnings.

**`PROMPT.md`: 266 lines → 88.** It was the project's original mega-prompt, written for a much weaker model, and it
had aged badly in three distinct ways. It **duplicated** `CLAUDE.md`'s six process rules and `CONSTRAINTS.md`'s
anti-pattern catalog. It carried an obsolete **"Required Output Format"** (Analysis / Proposed Changes / Updated
Code / Regression Report / Next Steps) that nothing has followed in months. And §4 actively **contradicted current
doctrine** — "run all regression tests, integration tests, and NIST tests" per stage is the exact over-gating that
`feedback_tiered_gates` exists to prevent, and that the owner has corrected five-plus times. The rewrite keeps what
only this file owns: the mission, the seven settled architectural commitments, a definition of done for a change,
and working style. The exhaustive "don't use god objects, don't use magic numbers" enumerations are gone — for a
model of this class they are instruction dilution, and `CONSTRAINTS.md` owns that catalog anyway.

**`CONSTRAINTS.md`.** Kept the labelled catalog — a grep proved the labels are live, cited from
`kb/Context/Doctrine & Anti-Patterns.md` and `kb/Spec/Lookup/Constraints.md`, so this file is a real SSOT and that
sync obligation is now stated in its header. Deleted the **"Migration Phases 1–9"** section, which described a
migration bearing no relation to the actual P0–P16 phases. Deleted the **Session Rituals**, which competed with
`CLAUDE.md`'s "Start here every session" for the same job, and whose step 4 was "Confirm scope with user." Fixed
the **Behavioral Constraints**, two of which ("ask targeted clarification questions", "propose concrete options
with tradeoffs rather than making silent assumptions") directly contradicted `feedback_autonomous`. A doctrine
file instructing the agent to stop and ask, while a memory instructs it to proceed, is a live conflict resolved in
whichever direction the model happened to weight — which is a good description of the drift the owner keeps seeing.

**New durable memory: `project_required_reviews`.** Mid-pass the owner stated a standing requirement that had never
been captured: an **architecture review, a full code review, a performance review, and a duplication/efficiency
analysis** — "this compiler is to be a production-quality, commercial product [lasting] a decade or more. This
should already be in memory." It was not; the closest memory (`project_post_conformance_goals`) covered only the
architecture leg and framed it as deferred. All four are now one memory, recorded as continuous criteria rather
than a single end-stage event, plus `PROMPT.md` §4 and a pointer in `CLAUDE.md`.

**Lesson.** Memory rot is not neutral — it is worse than an empty memory directory, because a stale memory is
*asserted with the same confidence as a true one*. The `ManagedPointer` entry would have actively argued for the
deleted byte substrate. The mechanical checks (name == filename, links resolve, cited files exist) cost seconds and
caught all three structural defects; they should be a drift test, not a thing noticed once a quarter.

