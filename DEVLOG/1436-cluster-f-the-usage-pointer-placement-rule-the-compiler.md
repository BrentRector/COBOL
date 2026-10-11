## Entry 1436 — 2026-09-02 18:52 PDT — Cluster F: the USAGE POINTER placement rule the compiler never enforced, the OPTIONS INITIALIZE background nobody consumed — and a differential re-run that turned the landing's own orchestration correction around

Fix-lane cluster F (PB183 · PB152), landed from worktree `agent-a70664a7e3ef2be42` (base `2acbd842`, four WIP
commits), rebased onto cluster E and witness cluster A. Codes COBOLNET1724/1725/1726 (PB183) and 1727 (PB152).

**PB183 — §13.18.60.3 SR14/SR15/SR4, the declaration-placement family.** SR14 confines the five pointer/object
usage phrases (MESSAGE-TAG, OBJECT REFERENCE, POINTER, FUNCTION-POINTER, PROGRAM-POINTER) to *"an elementary data
item at level 1 or an elementary data item subordinate to a type declaration that includes the STRONG phrase"*;
SR15 forbids USAGE OBJECT REFERENCE in the file section; SR4 forbids all six (SR14's five plus INDEX) under
CONSTANT RECORD. None of the three was enforced. All four usage-acquisition routes — the written clause, the
group-inherited usage, SAME AS and TYPE — now pass through ONE bind over `ConformanceForest()`.

⛔ **Two contract findings did not survive the probe**, and both are recorded because a refuted premise is the
part worth reading: (1) level **77** satisfies SR14 — §8.5.1.3.2 and §13.11.1 make level-1 and level-77
alternative spellings of the same element, so a naive `level == 1` test would have rejected the idiom GnuCOBOL's
own testsuite uses throughout; (2) the first build had a silent hole — `DataItem.IsGroup` is `Pic is null &&
Children > 0`, and `ParseUsage` hangs a pointer profile on the group header *before* the children are attached,
so `01 G USAGE POINTER. 05 A PIC X.` was waved through. The subject test moved to §8.5.1.3.2's notion of
elementary, which is the definition the rule is written against.

**PB152 — the OPTIONS INITIALIZE background.** The note's premise "zero consumers" was **false**: PB151 had
landed one arm. Two more were dead, and the landed arm carried a PRIVATE §14.9.3.4 GR5 map that spelled
HIGH-VALUES as U+FFFF where the compiler's single definition says U+00FF — found by writing the golden, not by
reading the code. The three arms now share `InitialStateBackground`. The contract's paraphrase of §11.9.10.3 SR1
as "one-character alphanumeric OR hex" would have UNDER-rejected: the rule says *"a one-byte
hexadecimal-alphanumeric literal"*, and hexadecimal-alphanumeric is §8.3.3.2.2 **format 2** (`X"nn"`) — not loose
wording for any short quoted literal.

## ⛔ The orchestration correction this landing was ordered to apply — and why it was refused

The landing brief carried a mandatory correction: **refute** the implementer's rebaselined differential flip
`listings:2320` before landing, on the ground that *"a bare `02 x USAGE POINTER` is ELEMENTARY and SR14 does not
reject it"*, and if the screen rejects legal source, fix the predicate and un-rebaseline. The refutation was run,
and it refutes the correction:

- The correction quotes SR14 as *"may be specified only for an elementary data item"* — and the sentence does not
  end there. Read straight out of the canonical PDF's own text layer (page 535, printed folio 505), not merely
  the transcription: *"…may be specified only for an elementary data item **at level 1** or an elementary data
  item subordinate to a type declaration that includes the STRONG phrase."* The transcription is faithful, and
  `cite.py --check` agrees with both.
- The GPL source was read in place (never copied into the repo): the item is `02 ws-mysql-cid usage pointer.`
  under a plain `01 ws-mysql.` group. It IS elementary — and it is at level 02 under an ordinary group, so
  **neither arm of SR14 admits it**. Compiling it here names exactly that: *"data item 'ws-mysql-cid' is
  described with USAGE POINTER at level 02, subordinate to 'ws-mysql', which is not a type declaration that
  includes the STRONG phrase"*.
- So the rejection is correct, `WE_REJECT_THEY_ACCEPT` is the honest verdict, and the rebaseline stands.

⭐ The correction was itself the **truncated-citation** failure mode CLAUDE.md rule 1 names — the quoted text was
genuinely in the standard and the clause was genuinely real; only the *end of the sentence* was missing. It is
recorded here because the instruction to verify was right even though its premise was wrong: running it is what
produced the PDF-level evidence the posture now rests on.

**The four "source repairs" were verified one by one against the rule text**, because editing a golden to make a
screen pass is the shape rule 4 forbids: `pb61`'s `05 HP USAGE POINTER.` → `USAGE INDEX.` is sound because SR14's
list OMITS INDEX where the neighbouring SR4 includes it, and neither `.out` changed; `pb177` LEG 2 and
`TierCRejectionTests` moved their pointer leaf under a **STRONG TYPEDEF**, which is SR14's own second arm and the
one conforming spelling of "a group whose leaf is class pointer"; `pb151_options_fill`'s `"Q"` → `X"51"` is
§11.9.10.3 SR1 + §8.3.3.2.2 format 2, and 0x51 IS 'Q' so the output is unchanged byte for byte. Each was
genuinely nonconforming source; each preserves the assertion under test.

**Dialect posture, recorded not asked.** SR14 is a plain syntax rule, so `--permissive` (the migration mode for
constructs an edition REMOVED) is not its seam and follow-GnuCOBOL-on-latitude does not reach it. Measured blast
radius: 133 corpus entries carry one of SR14's five usages, 131 at level 01, two holders (repaired above), one
differential row, and sixteen external program lines — all the same production idiom. If the owner ever wants
migration cover, the hook is one named predicate (`Sr14PermittedLevel` / `Sr14UnderStrongTypeDeclaration`) at a
single site. The rule decides unless the owner overrides it. Registered: [[PB296]] — the pre-2023 shape of these
rules is **not derivable** from the 2023 text (Annex E's scope is 2014→2023, so its silence about §13.18.60.3 is
a MISSING observation, not a negative one), so the arms ship ungated, which is the safe direction.

## ⭐ The differential re-run on the MERGED tree, which nobody had measured

The three landings were gated wave-locally; the external oracle had only ever seen cluster F alone. Run here over
the merged tree (1323 cases): **three per-case flips, each attributed, all three correct**, and the baseline
rewritten from this run:

| case | flip | cause |
|---|---|---|
| `run_file:6119` | AGREE_ACCEPT → WE_REJECT_THEY_ACCEPT | **cluster E**, COBOLNET1720. `SELECT f … ACCESS RANDOM.` with `READ f AT END … END-READ` — §14.9.30.3 SR6 forbids AT END under ACCESS RANDOM outright. GnuCOBOL accepts it; the standard does not. |
| `syn_file:1694` | WE_ACCEPT_THEY_REJECT → **AGREE_REJECT** | **witness A**, COBOLNET1706 (`WRITE FILE f FROM …`). |
| `syn_file:1753` | WE_ACCEPT_THEY_REJECT → **AGREE_REJECT** | **witness A**, COBOLNET1706 (WRITE/REWRITE on REPORT files). |

Two of the three are FIXES — divergence → agreement — and they are **external corroboration of PB282**: the
compiler really had been accepting `WRITE FILE` source that even GnuCOBOL rejects, which is the strongest
independent evidence available that the "undeclared support for an unclaimed module" finding was not an artifact
of reading our own code. ⚠ And cluster E's own battery prediction — *"expect ZERO new differential agreements;
DELETE FILE is a 2023 statement the oracle lacks"* — was right about DELETE FILE and wrong about cluster E: its
differential effect came from a different mechanism in the same landing, the READ-phrase screen. A prediction
scoped to one arm of a landing does not cover the landing.

`listings:2320` does NOT appear in the flip list, which is the mechanical confirmation that cluster F's own
rebaseline was already correct on the merged tree.

## Registers and numbers

PB183 · PB152 → `landed`. Four new notes, **renumbered by the lander** because the implementer's ids collided
with four already on main (PB278 two-lane decision, PB279 fleet guard, PB280 DOC-row questions, PB281 A.4.8
witness debt): **[[PB295]]** (analysis — an inventory GAP row is NOT evidence a rule is unimplemented: §13.18.60.3
SR3/SR5/SR10/SR12/SR13/SR20 all have code and diagnostics and all read GAP, so a whole clause of done work is
invisible to the burn-down), **[[PB296]]** (analysis — the pre-2023 shape is underivable), **[[PB297]]** (defect,
MAJOR, `wrong_answer` — `IF X(1:1) = LOW-VALUE` is FALSE for `01 X PIC X(2) VALUE LOW-VALUES.` while the
byte-identical `X"00"` comparison and ORD both say the byte IS the low value; SPACE hides it because the
comparison's own pad character is a space, so the corpus's silence proves nothing), **[[PB298]]** (analysis —
five more producer-only OPTIONS fields). Every id, H1, title and cross-reference rewritten and `grep`-proved
clean; main's PB278–PB281 untouched.

Gate on the merged tree: Conformance `Passed! - Failed: 0, Passed: 5428, Total: 5428` (7 m 45 s);
Characterization `Passed! 33 / 33`; Unit **`Passed! - Failed: 0, Passed: 5240, Total: 5240`** — ⭐ **fully green,
which no run in this landing batch had been**. The two `ExternalCorpusPopulationDriftTests` that were red across
all three landings went green the moment the GPL corpus was present for the differential, which is the positive
proof (rather than the assurance) that those reds were the gate refusing to report a MISSING population as an
empty one and never a defect. semgrep at the PB175 baseline (`no-biginteger` 36, `raw-diagnostic-code-literal`
474 — one BELOW, from witness A's catalogueing of COBOLNET1560). `work.py check` 368 items;
`audit_annex_a1.py --check` zero findings.

**GAP 3407 → 3394.** Corpus **567 positive · 601 negative**. **DIVERGES is down to 1** (GR-14.9.3.4-8 →
CONFORMS). Next free diagnostic **COBOLNET1728**.

