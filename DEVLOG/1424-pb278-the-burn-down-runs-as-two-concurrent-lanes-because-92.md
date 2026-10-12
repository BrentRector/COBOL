## Entry 1424 — 2026-09-01 22:31 PDT — PB278: the burn-down runs as TWO CONCURRENT LANES, because 92% of the GAP had never been looked at while a thousand agents polished 27 rows — plus battery #41 recorded ALL GREEN

A project-management pass measured the burn-down instead of continuing it, and the numbers were lopsided enough
to be a decision rather than a tuning.

**What was measured**, from the inventory and from the subagent transcripts on disk — not remembered. Of the
**3,636 GAP rows, 3,361 (92%) had never been adjudicated at all.** The defect backlog that the 2026-08-09
standing order put first — 118 defective rows, 54 actionable `kb/Work` notes — is **3% of the GAP**. Another 139
rows were already verified CONFORMS and needed nothing but one spec-derived golden each (every one of the 139
has an EMPTY `test-ref`), and 18 DOCUMENTED-NON-SUPPORT rows owed only their witness. Against that: from
2026-08-29 to 08-31, **thirteen review fleets of 52–117 agents — roughly 1,000 agents and ~4B cache-read tokens
— moved the GAP by 27 rows.** The single adjudication batch in the same window (Phase-B batch 8: 39 agents,
574M cache-read) processed **224**. Five of the last fleet's eleven register notes were defects that wave had
itself introduced. Adjudication costs ≈2.6–3.4M cache-read per row with ~120 turns per agent spent mostly on
DISCOVERY — finding the grammar rule, the binder, the existing tests — which is precomputable. And the CONFORMS
rate when a clause IS adjudicated splits the work cleanly: §15 92%, §13 96%, §12 97%, but **§14 64%** — the
statement clause is where adjudication turns into fix work, and the data/environment clauses are where it turns
into closed rows.

**The owner's question was asked bare — keep backlog-first, or interleave — and answered Interleave.** So the
2026-08-09 "fix the known defect backlog to zero before any new adjudication" order is **replaced**.
Adjudication and fixing are two lanes that run concurrently and never write the same thing: the adjudication
lane READS a `git worktree` pinned at a commit and writes verdict batch files, while the fix lane edits the main
tree. Five lanes in all, ordered by rows closed per token — **golden** (the 139 + 18, no compiler change),
**derived-verdict** (one `derived-verdicts` selector per A.4 module already *Not claimed*, the PB198 mechanism,
no new owner question), **adjudication** (dossier-fed batches, refuters reading only the CONFORMS / DNS
candidates, since those are the only verdicts dangerous when wrong — a wrong PARTIAL is re-probed at fix time by
rule), **fix** (clustered by MECHANISM, the fleet resized to ~40 and run once per cluster, implementers handed an
apply-ready contract rather than a discovery brief), and **A.1 documentation** once the schema defines what
closes a DOC row. The adjudication lane alternates a defect-rich §14 batch with a closure-rich §13/§12/§11/§8/§7
batch and stays one batch ahead, so the fix lane never starves. Projection: ~10–12B cache-read tokens to zero
GAP against ~35B on the per-wave loop, with the lanes overlapping in wall-clock.

⛔ **The method is written ONCE** — `DESIGN-spec-conformance-review.md` §5, now carrying §5.1 (the lane table),
§5.2 (the dossier: agents verify, they do not search) and §5.3 (the fleet sized to the cluster). Plan §0 gets a
pointer bullet and `kb/Work/PB278` the decision and its measurements. **Every lane is a QUERY over `kb/Work/` and
the inventory — no new list, table or tracker anywhere** (rule 8), which is the whole reason the doctrine can
live in one section instead of a spreadsheet. This commit carries no code.

**Battery #41 is recorded ALL GREEN** on tree `2acbd842`, one run, one tree, no split and no rebaseline: FULL
greenfield Conformance 5241/5241 (12 m) · Unit 5143/5143 (2 m 51 s) · characterization 33/33 · NIST 353 MATCH /
0 REGRESSION with a CLEAN audit · GnuCOBOL differential 1323 cases and `=== DIFFERENTIAL: 0 PER-CASE FLIP(S) ===`
· `=== BATTERY: ALL GREEN ===`. The part worth reading is the **refuted prediction**: the accrual bullet had
called the differential "the leg most likely to move", because the arithmetic wave changes an exception-condition
NAME in two directions (`2 ** -600000` raising OVERFLOW where it raised UNDERFLOW, and its mirror). It moved
nothing — and the reason is structural, not lucky: the differential records ACCEPT/REJECT per case, so an EC name
is a runtime value that oracle cannot see by construction. The leg that WOULD see it is greenfield Conformance,
where the wave's own goldens live and where it is green. Per-case attribution stays mandatory regardless —
identical totals are consistent with offsetting flips, which is exactly why the per-case diff exists. Population
+6 Conformance and +37 Unit over #40; those deltas are COMPUTED from the #40 reference bullet, and are noted
because the batch notes had quoted +116 / +79 against some other baseline. No gate is owed.

Lanes launched in parallel this session and still IN FLIGHT: the golden-lane drafts, the A.4 derived-verdict
selectors, and the dossier tooling for `phase_b_batch.py`. The **worktree-aware fleet build guard LANDED in the
preceding commit** (`f7e8382e`, PB279) — which is what makes the two-lane model mechanically possible at all:
until today the guard froze the whole session on any live agent, so an adjudication lane reading a pinned
worktree would have blocked the fix lane's every build.

