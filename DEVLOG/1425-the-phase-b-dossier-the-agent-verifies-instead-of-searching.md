## Entry 1425 — 2026-09-02 14:37 PDT — The Phase-B dossier: the agent VERIFIES instead of searching — plus PB286, a slug that wrote 3,674 bytes into an NTFS stream no glob could see, and a grammar-comment citation cluster where every wrong § names a real statement

`scripts/spec/phase_b_batch.py` now builds a **dossier** into every agent input file, and
`DESIGN-spec-conformance-review.md` §9 states what it is and — more importantly — what it is not.

### Why it exists

Phase-B batch 8 is the measurement. 39 agents, 224 rows, **574M cache-read tokens**, roughly **120 turns per
agent** — and most of those turns were not adjudication. They were discovery: *where is the grammar rule for this
statement, where are the binder and emitter that implement it, where are the tests.* Three greps with **exactly
one answer per subject**, bought once per agent, at agent prices. Two agents adjudicating different parts of the
OPEN statement each paid for the same search, separately, and neither could tell the other what it found.

The dossier computes that search **once per subject** and pastes the answer into each of that subject's part
files. PB278 already made this doctrine (§5.2, "agents verify, they do not search"); this is the mechanism.

### What it carries

Per subject: the ANTLR rules — matched three ways, on the rule name, on the clause the rule's comment block
cites, and on the construct keyword; every `src/Cobol.Net.*` file citing the subject's clauses, with the citing
lines; the same for `tests/`, including the `.cob` goldens; the `docs/CONFORMANCE.md` determinations; the
citing `docs/DIAGNOSTICS.md` rows; and the `kb/Work/` notes whose `spec_refs` or `inventory_rows` fall inside
the subject **with their `status`** — which is how §8 item 8's "do not re-report a landed fix" finally gets
answered without anyone hand-writing the list that CLAUDE.md rule 8 forbids.

Nothing is hand-mapped, and that is deliberate. The clause set comes from the catalog rows plus their construct
root; the keyword comes from the catalog's own `subject` field. A hand-kept construct→file table would have been
a sixth work register whose staleness was invisible — **a dossier missing an entry looks exactly like a construct
that has none.**

### ⛔ Its stated limit, repeated to the agent in every file

The dossier is a **selector**, so it is evidence about what it RETURNED and none whatsoever about what it
dropped (`feedback_measure_the_selectors_complement`). An empty list means "this derivation found none", never
"the compiler has none": 8 of the catalog's 599 subjects are prose with no keyword to search on at all, and any
code implementing a rule without citing its clause is invisible to it. **A NOT-IMPLEMENTED verdict still requires
the agent's own search**, and each input file says so in a `how-to-read` field rather than trusting that to the
prompt.

The generator prints **three self-check tables** before the fan-out: which subjects exceeded `--max-rules` and
were split; each part's slug → clauses → rule count; and the per-subject dossier counts across grammar, source,
src-named, tests, test-named, determinations, register and diagnostics. That third table exists so a **row of
zeros is visible as a signal** — it names, in advance, which agent is starting cold. Proved on main:

```
dossier per subject (0 = this derivation found none, NOT 'the compiler has none'):
    subject                    grammar  source  src-named  tests  test-named  determ  register  diags   keywords
    OPEN statement                   8      16          0      4           2       2         1      0   OPEN
    VALUE clause                     8      29          4     22          40       1        11      8   VALUE
```

### PB286 — the slug that exited 0 and lost an agent

The generator's `base_slug` replaced only `' '` and `'/'`, passing every other character through. Fine for eight
batches, because almost every catalog subject is bare words. The **Annex A.1** subjects are full sentences, with
parentheses, commas and **colons** — and `Alignment of data for increased efficiency (special or automatic
alignment: interpretation, rules)` produced a file name containing a colon.

On Windows that is not an error. `path:stream` is NTFS **alternate-data-stream** syntax. So: the process exited
**0**; `is_file()` was True; `st_size` was **3,674** — the agent's input really was written; the printed slug
list still promised the subject to the workflow; and `glob("in-*.json")`, which is how the workflow actually
enumerates a batch, **returned nothing for it**. The batch ran one agent short of what it announced — **674
promised, 673 on disk** — with no log line, exit code or exception marking the difference. A missing item read
as no item.

`base_slug` now sanitises to `[a-z0-9-]`, truncates at 80 so `<out>/in-<slug>.json` clears the 260-character
path limit, and de-duplicates deterministically.

**The lesson is the invariant, not the slug.** The first check written for this — *does the file exist and is it
non-empty afterwards* — **PASSED on the bug**, because the stream genuinely existed and genuinely held 3,674
bytes. A check downstream of a defect can be satisfied BY the defect. It was replaced by two placed where the
defect is: a **slug-shape check before the write** (the name, where the answer is the same on every OS) and a
**promised-vs-glob population comparison after** (the batch is defined as what the glob can see). Both were
fired against the real defect before being trusted (`feedback_green_gates_arent_evidence`) — invariant 1 rejects
the historical slug and accepts `open-statement-p1`; invariant 2, given a real NTFS write of that slug, watched
the write exit cleanly, watched the glob return an empty set, and reported the loss.

### PB159 — a grammar-comment citation cluster, with a consumer

Because the dossier's grammar arm matches on *the clause a rule's comment cites*, those comments stopped being
decoration. A heuristic sweep over the `.g4` statement headers returned **92 candidates**; **8 are hand-verified**,
each re-derived with `cite.py --check` in both directions:

`CobolParserCore.g4:813` ROUNDED §14.9.4→**§14.7.4** · `:847` ADD §14.9.1→**§14.9.2** · `:894` SUBTRACT
§14.9.42→**§14.9.44** · `:927` MULTIPLY §14.9.23→**§14.9.26** · `:1009` MOVE §14.9.24→**§14.9.25** · `:1338`
GOBACK §14.9.16→**§14.9.18** · `Core/CobolIO.g4:250` OPEN/CLOSE §14.9.25,§14.9.7→**§14.9.27,§14.9.6** · `:279`
OPEN tape phrase §14.9.25→**§14.9.27**.

⛔ **Every wrong number is a valid clause naming a DIFFERENT REAL statement** — §14.9.4 is CALL, §14.9.1 is
ACCEPT, §14.9.42 is STOP, §14.9.23 is INVOKE, §14.9.24 is MERGE, §14.9.16 is GENERATE, §14.9.7 is COMMIT. Not
one of them dangles, so nothing that merely asks "does this clause exist" can see any of them; the check has to
compare the clause against the construct the comment is about. That is
`feedback_a_real_clause_can_answer_a_different_question`, at grammar scale.

The harm is measured, not supposed: running `phase_b_batch.py 14.9.25` — MOVE — returns a grammar list
containing **`openFileSpec` (`Core/CobolIO.g4:281`)** among MOVE's rules, purely because that rule's comment
cites §14.9.25. An adjudicator reading MOVE's dossier is handed an OPEN-statement rule as evidence about MOVE.
A separate agent is repairing the eight; the ~84 unverified candidates are the rest of the sweep. **PB159 stays
open.**

`work.py check` → 336 items, all well-formed.

