## Entry 1150 — 2026-08-03 11:26 PDT — Closing the session: nine commits, and the four things that will bite whoever resumes

Nine commits: PB13 · the `'i'`/`'n'` adjudication · PB10 · PB11 · Phase-B batch 4 · PB20 · PB19 · PB21 · PB22.
No BLOCKER open. Conformance 4164 → **4180 / 4180**, Unit 972 → **3628 / 3628**, characterization 33/33, the
GnuCOBOL differential at 0 per-case flips. Inventory 253 → **330 adjudicated**; GAP unchanged at 3799, which is
the design working — adjudicating opens items, fixing closes them, and this session did both tracks.

Plan §0 now opens with a SESSION HANDOFF block carrying what follows; this entry is the narrative half.

### The one thing that generalises

**Every substantial defect today was hidden by something built to catch it.**

- PB13's saturation was invisible because the store's rescale destroyed the sentinel *before* the capacity check
  that existed to see it.
- PB20's wrong rule survived because a **fabricated citation** — "§8.4.2.4", a clause that does not exist, in 21
  places across 14 files — made every reader stop at the `§`.
- PB19's screen rejected legal source because the **classifier underneath it** flattened a category. PB1's
  disaster was unaudited ROWS; this was an audited row over a lossy CLASSIFIER, one layer down.
- PB21's ten missing bodies survived because **the guard written to prevent exactly that** exempted the group
  they lived in — on a premise about the rule's INTENT where the code screens on its CLASS — and then
  under-counted the remainder through an or-chain regex.

So: when a guard is green, ask what it LOOKED AT. Three guards were made to fail once before being trusted this
session, and PB21's FACTORIAL exemption was made COUPLED — it applies only while the renderer still contains the
skip it depends on, so removing the skip fails the test instead of silently reopening the hole.

### The second thing: plausible answers

PB5 returned `9223372036.85` for a money value. PB13 returned `0170141183460469231731687303715` into a
`PIC 9(31)`. PB22 returned **143951 — a genuinely valid integer date** — because 2⁶⁴ + 1995046 wraps to 1995046.
None of those looks wrong if you sample outputs. That is why the Phase-B refute stage keeps finding what the
adjudicators looked straight at: it reads the implementation instead of trying values. It is also why the refuter
found a regression I had landed hours earlier (PB13's receiver-less arm bypassing the EC-ARGUMENT-FUNCTION raise
site) — in same-session code, by reading it.

### The third thing: the instruments, again

`guard-fast` printed `=== ALL GREEN ===` at **352 MATCH against a 353 baseline** because a program vanished from
the report and nothing asserts the population. Five runs on one unchanged tree gave five outcomes across five
different programs. The differential scored a case in the 0-tolerance direction with an EMPTY error string, on a
program that compiles clean. Filed as §11 A12c/A12d/A12e with one shared root cause — **a missing observation is
being read as a negative observation** — and until they close, a green `guard-fast` is necessary-but-not-
sufficient. Every affected program was cleared MECHANICALLY (zero INSPECT statements, zero function-identifiers,
clean serial compiles, green in the greenfield leg), never statistically.

### And the process trap worth the most to the next batch

The playbook's own documented merge command, `record_verdicts.py scratchpad/phase-b/out-*.json`, globs every
prior batch's output out of the shared directory. At batch 4 it offered **144 records instead of 77** and would
have re-adjudicated PB11's freshly-closed FORMATTED-* rows BACKWARDS, from files written before PB11 existed. The
`--dry-run` caught it because **the GAP went UP** — which is the entire reason that flag exists. Merge the
batch's own files by name; the glob arms itself the moment a second batch shares the directory.

### Owner decision taken

A numeric-edited item is **not** an arithmetic operand and **not** an integer argument (§8.8.1.1 + §8.5.2.13 +
§8.5.2.1 Table 2; §15.43.3 r1 shows the standard says "or numeric-edited" when it means it). Both external
oracles agree. It had been argued three times; DEVLOG 1142 records the derivation so it is not argued a fourth.

