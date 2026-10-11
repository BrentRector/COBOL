## Entry 1025 — 2026-07-26 16:41 PDT — The inventory exists: `invent : 3210 rows · 3210 GAP`. Plus a duplicate-key bug that would have silently corrupted every verdict

Three things land here: the vault gets progressive-disclosure frontmatter, the traceability inventory is seeded so
`session-probe` finally reports a burn-down, and a genuinely dangerous bug in yesterday's-hour-old catalog is caught
by an arithmetic mismatch.

**1. `description:` on 634 generated reference notes.** Research into how others run Obsidian alongside an agent on
a large codebase converges on one mechanic: *instruct the agent to read frontmatter to decide relevance, without
reading the note*. Our generator already extracted each type's `///` `<summary>` — it just put it in the BODY. Now
the first sentence also becomes a frontmatter `description:`. Deciding whether `MoveClassifier` matters to the task
in hand used to cost a full file read; now it costs one line in an index, 634 times over. The
`VaultReferenceGeneratorDriftTests` guard still passes.

**2. The traceability inventory is real.** `scripts/spec/build_inventory.py` seeds one row per rule from the
Phase-A catalog into `tests/version-matrix/traceability-inventory.json` — the exact path `session-probe.ps1` has
been probing for and reporting "not built yet" since P14 opened. It now reports:

    invent : 3210 rows · 3210 GAP (v1.0 = zero GAP)

3,210 GAP is not a failure, it is the honest starting line: nothing has been adjudicated yet. The refresh is
RESUMABLE by design — an existing row's verdict, code-location, test-ref and notes are carried across re-runs,
because Phase B is many sessions of work and the design doc requires a verdict to persist. The GAP-by-clause table
is the work map: §14 statements 1,134 · §13 data clauses 875 · §15 intrinsics 351.

**3. `kb/Conformance/` — the inventory as a Dataview database.** 3,210 per-rule notes plus 10 clause indexes and a
dashboard, gitignored and regenerated exactly like `kb/Reference/`. Per-rule FRONTMATTER is the point: it makes
"every DIVERGES in §14 with no covering test" a one-line Dataview query instead of a script. The dashboard ships
four standing queries — unadjudicated, divergences-without-tests, conforms-but-untested, awaiting-owner-decision.

**⛔ The bug, and it is the reason this entry matters.** The notes generator emitted **3,048 notes for 3,210 rules**.
That arithmetic could not be explained away, and it was not a generator bug — **114 rule ids were DUPLICATED,
covering 173 rules.**

The cause: a rule block can contain more than ONE numbered list. §7.2.3.4 General rules runs 1..n for COPY, then
RESTARTS at 1 for text-word matching, under no intervening heading. Keyed on (kind, section, ordinal), the second
list overwrote the first.

Why this was dangerous rather than merely untidy: **the rule id is the inventory's primary key.** Every refresh
looks up the prior row by id to carry a verdict forward. With colliding ids, a verdict adjudicated for "COPY
library text" would have been silently carried onto "each separator is considered a text-word" — a wrong verdict,
attached to the wrong rule, with full confidence, discovered never. It would have been invisible in the JSON and
invisible in the probe.

Ids are now sub-list-qualified (`GR-7.2.3.4-L2-1`), and uniqueness is a FATAL assertion in the extractor rather
than something noticed by luck. That is the third time today the same lesson arrived: the dead Semgrep rules
(1023), the 7.9% rule undercount (1024), and now a duplicate primary key — each caught only by a check that could
fail, none by anything looking green.

The one-time re-key dropped 243 stale rows, which is safe precisely because no verdicts existed yet. Doing this
after Phase B had run would have destroyed real work.

**Next:** Phase B — fan out by clause, locate the implementing code per rule, assign a spec-verified verdict. That
is the agent-parallel phase, and it is the first work that will move the 3,210 downward.

