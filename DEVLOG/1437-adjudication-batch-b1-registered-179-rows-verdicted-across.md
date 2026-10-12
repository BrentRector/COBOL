## Entry 1437 — 2026-09-02 19:12 PDT — Adjudication batch b1 registered: 179 rows verdicted across seven I-O and sort verbs, 85 mechanisms clustered into 57 notes, and one finding that would have reversed a landed row

Lane-3 registrar for batch **b1** — OPEN, READ, RELEASE, RETURN, START, UNLOCK, USE, **184 rules across 15
subject files**. Adjudicated read-only in a pinned worktree, refuted per rule, then merged, clustered, registered
and recorded here. Notes `PB316`–`PB372`; `PB236` extended.

**The shape of the batch.** 179 records applied (180 merged, one dropped — below). Verdicts: **PARTIAL 55 ·
DIVERGES 47 · NOT-IMPLEMENTED 25 · CONFORMS 52**, of which **15 closed the GAP** and **37 are
CONFORMS-but-untested** — verified against the rule text with no spec-derived test on disk, so they are the
golden lane's next input set (listed in `PB370` with what each golden must assert, and dumped to
`<scratch>/lane3/golden-lane-input.txt`). Four rows verdicted NEEDS-OWNER-DECISION are excluded and carried by
`PB371`. GAP **3394 → 3379**.

**The refuters earned their stage.** 70 closing verdicts were re-derived: **51 upheld, 19 overturned** — 13 to
PARTIAL, 5 to DIVERGES, 1 test-ref withdrawn. The five DIVERGES overturns are the ones worth naming, because each
is a wrong answer the adjudication's own evidence had reached and then closed over: OPEN I-O probes writability
only on the sequential arm, so a read-only indexed file opens `00` and the loss surfaces as `30` at CLOSE
(`PB328`); keyed lock governance runs after the physical read, so GR13 a)'s conditional half fails exactly where
GR10 a) says the position must not move (`PB338`); the indexed file position indicator is modelled as
(key, ARRIVAL) where §14.9.41.4 GR17 e) 1. makes it a key VALUE, so a START into a duplicate set loses the rest
of the set in both directions (`PB342`) — the adjudication had NOTICED that modelling error and dismissed it with
*no divergence was demonstrable*, and the construct that demonstrates it is one START and one READ; and
`RELEASE ... FROM x` is not `MOVE x TO record-name-1` plus a plain RELEASE, proved by an A/B pair where the
explicit MOVE runs and the FROM form aborts before the record is released (`PB348`).

**85 finding mechanisms became 57 notes, not 85.** The clustering is across FILES, because one statement's parts
report one root three times: OPEN's per-clause sharing and retry phrases hoisted to statement scope appeared in
p1, p2 and p3 and are `PB316`; the sequential READ direction dropped at bind appeared in p1, p2 and p4 and is
`PB334`; the missing sequential START arm appeared in p1, p2 and p3 and is `PB352`. Five further clusters cross
STATEMENTS: the `BoundUnsupported` carrier doing three incompatible jobs (extended onto `PB236`, seven new sites
across OPEN, CLOSE, READ, DELETE, DELETE FILE, START, UNLOCK, RELEASE and RETURN); the implicit MOVE of every
`INTO`/`FROM` phrase built in the EMITTER, downstream of every binder-side MOVE check and of the storage facts
codegen itself needs (`PB348`, ten sites); optional words written as required tokens in READ, START and USE, with
`audit_grammar_optional_words.py` — the tool built to catch exactly this — reporting *0 candidates* across all
544 parser rules (`PB332`); five exception conditions catalogued as turnable Fatal names with no raise site
(`PB326`, `PB349`, `PB368`); and sixteen inherited clause numbers naming MOVE, COMMIT, MULTIPLY, RESUME, SET,
UNLOCK and START for rules about OPEN, CLOSE, RELEASE, RETURN, WRITE and SORT (`PB320`).

**Every citation was re-derived, and three of my own were wrong.** 106 (clause, text) pairs ran through
`cite.py --check`; three failed and were corrected at source — READ SR10's actual wording, READ SR1's actual
wording, and the general-relation format's clause, which is §8.8.4.2 and not §8.8.4.2.2. Final: 106 OK, 0 FAIL.

**⛔ One finding was FALSE, and recording it would have reversed a landed row.** read-statement-p1 reported
§14.9.30.3 SR6 (*none of the phrases ADVANCING, AT END, NEXT, NOT AT END, or PREVIOUS shall be specified if
ACCESS MODE RANDOM*) as having no implementation anywhere in the greenfield, quoting the comment ABOVE the check
as if it were a decline. `KeyedIoBinder.BindRead` lines 56-66 build the present-phrase list and call
`ScreenForbiddenPhrase`, and both of the finding's own probes are rejected `COBOLNET1720` by this worktree's
fresh build. The row carries CONFORMS/OK on `conformance:negative/read-phrase-random-access`, landed by PB144.
**The record was dropped from the batch**; fourteen other claims across six findings were re-measured on the same
build and every one held. Two further input errors are recorded rather than inherited: start-p2 directs the
START SOURCE-phrase rows onto `kb/Work/PB144` as `status: open` — PB144 is **landed**, and a landed note holds no
rows for the `DefectiveRowCoverage` gate, so they went to `PB358`; and all four NEEDS-OWNER-DECISION
adjudications reason from `inventory-schema.json` carrying exactly ONE derived selector when it carries
**seven**, one of which (`commit-and-rollback-only`) had already NAMED and deliberately excluded three of their
rows, while `format-select-when-only` had already stamped the fourth DOCUMENTED-NON-SUPPORT.

**Shape violations were fixed at their source field, never by weakening a record.** `record_verdicts` refused the
first batch with 165 of them: 98 `editions` fields carried free prose (*1985 / 2002 / 2014 / 2023 — the SHARING
phrase belongs to …*) and 52 `code-location` fields carried line annotations, a C# snippet whose semicolons
shattered the `'; '` separator into fragments named `var` and `_fpiKey`, and `Class.Member` spellings the drift
gate's verbatim word search cannot resolve. Editions were reduced to the vocabulary with the PROSE preserved into
`notes` as `EDITIONS AS ADJUDICATED:`; code-locations were reduced to the shortest dotted suffix that actually
appears in the file, with 0 resolving at no suffix.

**The dossier generator gets its own note.** Fifteen subjects reported the same seven shapes, five of them
independently five times or more, and `PB372` owns them: a citation-keyed selector is structurally blind to code
that fails to implement a rule (which is most of what this review finds); the tests list does not say which tests
can CLOSE a row, so legacy-baked differentials read exactly like spec tests; `determinations` misses the
`CONFORMANCE.md` §5 module rows that decide whether a rule is live at all; `register` matches `spec_refs` only,
so `PB236`'s mechanism was re-found four times; truncation is silent about what it dropped and dropped the
load-bearing rule twice; Annex A.1/A.3 back-references are not harvested; and no dossier records the pin sha it
was built from — one line that would have prevented all four owner questions.

**Gates.** `dotnet build CobolSharp.sln -c Debug` exit 0 on the merged tree;
`dotnet test tests/Cobol.Net.Tests.Unit --no-build --filter
"FullyQualifiedName~SpecTraceabilityInventory|FullyQualifiedName~DefectiveRowCoverage|FullyQualifiedName~DerivedVerdict"`
→ **Failed: 0, Passed: 40**. `python scripts/spec/work.py check` → 425 work items, all well-formed. No `src/`
file is touched by this landing, so the `semgrep/verify.py` baseline (`PB175`) cannot move. Cluster F took
`PB315` while the batch was being written, so `PB315`–`PB370` were renumbered to `PB316`–`PB371` with one regex
over the whole range before the rebase — the collision the workstream skill's central-allocation rule exists to
prevent, arriving anyway because two lanes allocated in parallel.

