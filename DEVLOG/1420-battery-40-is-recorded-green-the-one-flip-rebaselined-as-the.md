## Entry 1420 — 2026-08-31 13:13 PDT — Battery #40 is recorded GREEN: the one flip rebaselined as the fix it is, plus two riders — four register notes whose titles were not YAML, and a rule that had stopped naming its own newest field

Entry 1419 stopped battery #40 one row short of green and left exactly one question open: whether to regenerate
the verdict baseline so that it encodes the single attributed flip. The owner's disposition is to regenerate and
record — the convention batteries #38 and #39 both followed. This entry is that recording.

### Why regenerating here does not destroy the evidence

[[PB262]] item 3 raised the objection itself, and it is the right one: *"rewriting the baseline in the same
breath as discovering the list was wrong destroys the only record that it happened."* That is precisely why the
stop was committed **first and separately**, at `645b25bb` — DEVLOG 1419, the `PB262` note, and the refutation
block appended to `kb/Work/PB169.md` are all durable, in git, ahead of any baseline rewrite. The regeneration
now moves one row in a file whose every changed row the commit must attribute; the record that bucket 8 was
refuted lives in three places the rewrite cannot reach.

### The rebaseline, measured

`tests/external/gnucobol-verdict-baseline.tsv` regenerated from a fresh differential run (artifacts
`scratchpad/battery-40-rebaseline`). The delta is exactly one row — `1 file changed, 1 insertion(+), 1 deletion(-)`:

```
@@ -1306 +1306 @@ syn_subscripts:195	DEFAULT_DIALECT	AGREE_REJECT
-syn_subscripts:23	DEFAULT_DIALECT	WE_ACCEPT_THEY_REJECT
+syn_subscripts:23	DEFAULT_DIALECT	AGREE_REJECT
```

**1323 rows before and after**; `external corpus population: 1323 case(s) from 36 .at wrapper(s)` on both runs;
no NEW or REMOVED case, no case without a compiler verdict, no harness failure. Totals moved
**573/473/206/71 → 573/473/207/70** — `AGREE_REJECT` +1, `WE_ACCEPT_THEY_REJECT` −1, consistent with exactly
this one flip and no offsetting pair. The confirming run against the new baseline:

```
=== DIFFERENTIAL: 0 PER-CASE FLIP(S) ===
```

The verdict is spec-correct and was re-derived rather than inherited (`PB262`): §8.4.2.3.2 admits a subscript
only as `ALL` / arithmetic-expression-1 / index-name-1; §8.8.1.1 admits an arithmetic expression only as an
identifier referencing a **numeric** data item, a numeric literal or `ZERO`; §8.5.2.1 Table 2 puts the `PIC X`
operand in class alphanumeric. The flip **converges onto GnuCOBOL's own rejection** of a case they ship as a
negative test titled *"Non-numeric subscript"*, and it closes an under-reject — before the batch the compiler
accepted the program and digit-decoded alphanumeric bytes into an occurrence number. Attributed to
`ReferenceResolver.ScreenPositionOperandClass`, new in `1884ba1e` ([[PB170]]).

### The four legs are NOT re-run, and the licence is mechanical

Conformance **5235 / 5235**, Unit **5106 / 5106**, characterization **33 / 33** (concurrent, no host crash),
NIST **353 MATCH / 0 REGRESSION** with the audit CLEAN and the guard verdict **ALL GREEN** — all earned on tree
`6f8461d9` and carried forward unchanged. The licence to carry them is checkable, not editorial:
`git diff --name-only 6f8461d9 645b25bb -- src/ tests/` is **empty**, and this commit adds only `DEVLOG.md`,
`docs/`, `kb/`, `CLAUDE.md` and the baseline TSV. No compiler behaviour the four legs measured can have moved.
This is the same two-tree split battery #39 recorded, for the same reason — and, as there, the one leg whose
input *did* change (the baseline) is precisely the leg that was re-run.

### Rider (a) — four register notes were not valid YAML, and no gate could see it

`PB117`, `PB121`, `PB137` and `PB154` carry double-quoted `title:` scalars containing **raw inner double
quotes** — a COBOL literal, a diagnostic's quoted text. A double-quoted YAML scalar ends at the first unescaped
quote, so PyYAML — and Obsidian's frontmatter reader, which is what renders `kb/Work.base` — saw the scalar end
early, then a bare scalar, and the whole note's frontmatter failed to parse. The repair escapes the inner quotes
and changes nothing else.

⛔ **Why it sat undetected is the part worth keeping, and it was measured rather than assumed.** Both readers
that gate the register — `scripts/spec/work.py` and `DefectiveRowCoverageDriftTests` — parse the frontmatter
block with a **regex scalar reader**, and neither reads `title`. So the register's gates are structurally
incapable of seeing a note that no YAML parser can read. Proved by reverting the four notes to their broken
state and re-running the gate, which printed **`✓ 318 work items, all well-formed`** — the same green it prints
now. The proof obligation therefore had to sit on the repair, not on the gate: the script decodes each repaired
scalar with PyYAML and refuses to write unless the decoded value is character-for-character the original inner
text. All four verified identical (lengths **393 / 405 / 628 / 836**), **318 / 318** notes now parse as YAML,
and exactly four notes changed. That the gate cannot see this class of breakage at all is the residue, and it is
worth a note of its own if a fifth ever appears.

### Rider (b) — CLAUDE.md rule 8 had stopped enumerating its own frontmatter

The registration wave (`6f8461d9`, DEVLOG 1418) added **`inventory_rows`** as a register frontmatter field and
put `DefectiveRowCoverageDriftTests` on it. Rule 8's own sentence enumerates what the frontmatter carries, and
after that wave the enumeration was incomplete — **50 notes carry the field**, a unit test requires every
non-resolving traceability-inventory row to be claimed through it, and the rule describing the register did not
mention it. One line, restoring currency (rule 6) for the register schema: no new obligation, no changed
obligation. **Named here and in the commit message so the owner can veto it.**

### What stays open

`PB262` remains **open** and MAJOR. This disposition answers its item 3 only; items 1 and 2 — re-deriving the
owed list's bucket 8 *per syntactic position* (the screen takes a two-member `SegmentPosition`, so a list with
one bucket per screen cannot be complete by construction), and the structural half that would GENERATE an owed
list from `corpus_sweep.py --codes` so a bucket cannot be written by hand at all — are untouched, and they are
the half that stops this recurring. Twice in two consecutive batteries a pre-declared-empty blast radius has
been falsified by a corpus case named after the exact shape: [[PB209]]'s *"REDEFINES: with OCCURS"*, then this
one's *"Non-numeric subscript"*.

