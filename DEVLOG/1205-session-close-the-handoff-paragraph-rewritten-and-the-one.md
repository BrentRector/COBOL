## Entry 1205 — 2026-08-07 18:25 PDT — Session close: the handoff paragraph rewritten, and the one finding that held on all thirteen items

**Context refresh for the next session — no code in this commit.** Plan §0's SESSION HANDOFF still opened
"⚙ 2026-08-05 SESSION CLOSE" and narrated PB45/PB23/PB25/PB47/PB48, two sessions stale. It now describes the
2026-08-07 close: **PB46 (both halves) · PB27 · PB35 · PB50 · PB54 · PB52 · PB53 · R03 · R01 · R02**, thirteen
defects, the register down to residue. §0 gained no list — `kb/Work/` is the register (rule 8) and the paragraph
says so in its own first line.

**THE FINDING WORTH CARRYING FORWARD, BECAUSE IT HELD ON EVERY SINGLE ITEM: the entry's stated defect was not the
defect.** Wrong layer (PB46 — "thread a channel through four layers", when underneath a `default:` arm made FIVE
PICTURE categories impossible as method parameters). Wrong count (PB35 named MAX and MIN; six clauses carry the
rule). Wrong cause (PB50's "no arm in the token switch" — PB42 had already added that arm, and there were two
other causes). Stale (PB27's "silently accepted" was loud, and printing a FALSE diagnostic beside the true one).
And a blocker that did not exist: PB46's CALL half, filed "blocked on the P13 prototype registry", was selected
by a SYNTACTIC `AS` phrase all along. **Three of that one item's premises failed against its own general format.**
Four sweeps then found something worse than the item being worked — sharpest is **PB54**, a silent wrong answer
where `MIN(ZERO + 5, 2)` returned 0 while `FUNCTION MIN(ZERO + 5, 2)` returned 2, for a spelling §8.4.3.2 SR2
makes the same reference. The practice that found all of it is unchanged: **write the probe from the general
format, never from the reported symptom.**

⛔ **AND THE SESSION'S OWN WORST MOMENT STAYS IN THE HANDOFF, NOT JUST HERE: a green local battery certified a
tree that could not build from scratch** (Entry 1203). A BOM my own tooling wrote into `CobolLexer.g4` failed the
Windows Release job on `main` while Conformance 2686/2686 and Unit 4029/4029 passed locally — the incremental
build never re-ran ANTLR on it. The owner noticed before I did. Compounding it: Entry 1198, written the day
BEFORE, describes the `cancel-in-progress` behaviour that hid the failing verdict. **Writing a hazard down is not
acting on it** — after a push that touches the build, watch the run to completion.

**Two cross-session hazards are now memory files rather than DEVLOG prose**, because both are about tooling and
will recur in any repo: Python's `utf-8-sig` STRIPS a BOM on read and ADDS one on write; and the Bash tool's
working directory persists between calls, which silently redirected a golden registration and a whole conformance
gate — **both reporting exit 0**, because a wrong-directory command usually succeeds at doing nothing.

**GATE:** docs only. Tree clean at `0c198f92` before this commit, CI run 31220997272 green on all four push jobs
(Greenfield · INV-1-strong · Windows build + tests · Guard). Probe: 283 positive goldens · 180 negative fixtures ·
inventory 3981 rows / 3903 GAP · next free diagnostic COBOLNET1636 · `work.py next` → R04 · R05 · R06, 7
actionable.

