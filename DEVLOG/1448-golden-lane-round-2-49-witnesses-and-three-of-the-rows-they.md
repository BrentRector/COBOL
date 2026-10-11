## Entry 1448 — 2026-09-03 12:31 PDT — Golden-lane round 2: 49 witnesses, and three of the rows they were meant to close turned out to be asking a different question than the register said

The golden lane's second round took the CONFORMS-but-untested and DNS-witness-owed bands the lane-3 batch-1
registrar left behind, drafted as six writer slugs — `io-open-start`, `io-read`, `io-use-release-return`,
`misc-p1`, `misc-p2`, `singles`. **GAP 3106 → 3054**, 52 rows closed, 49 new goldens on disk (44 positive across
2023/2002/85, 5 negative), all discovered and green. No compiler change: the only `src/` edit in the whole
landing is a doc comment.

## The round's shape

| slug | rows | landed | held back / not closable |
|---|---|---|---|
| `io-open-start` | 13 | 12 goldens (11 × 2023, 1 × 85), 13 records | — |
| `io-read` | 16 | 9 goldens, 13 records | 3 rows not closable; 1 defect repro kept OUT of the tree |
| `io-use-release-return` | 8 | 6 positive + 3 negative, 7 records | 1 row not closable; 1 already covered by an existing golden |
| `misc-p1` | 9 | 8 positive + 1 negative + 1 unit test, 7 records | 1 defect repro kept OUT; 1 golden deleted by the refuter |
| `misc-p2` | 9 | 8 positive + 1 negative, 8 records | — |
| `singles` | 6 | 1 golden, 3 records | 3 rows not closable |

**Two drafts were held out of the tree** because the validator classified them `suspected-compiler-defect`, and a
suspected defect never becomes a golden edit. Both were **re-measured here against this worktree's own built
`cobol.exe`** rather than inherited from the pinned 5fe593a0 validator, and both reproduce.

## What the drafts collided with, and how it was resolved

`misc-p2` and `singles` independently drafted **different programs at the same path**
(`tests/conformance/2023/l1_random_unseeded_first_reference`) **with the same PROGRAM-ID** (`L1RND01`) — the
`unique_programid_per_test` trap, where .NET happily serves a stale same-named assembly. Both are worth keeping:
`singles`' has the `R4-SEQ=ADVANCES` leg (two successive unseeded draws differ) on sign-carrying COMP-2
receivers, `misc-p2`'s has the `SEED0` leg (§15.75.4 r3's domain floor). `misc-p2`'s landed as
`l1_random_seed_determination` / `L1RNDSD`, and its `DOC-A.1-144` record follows the rename. A PROGRAM-ID sweep
over all 48 drafted ids against the whole corpus found no other collision.

## The finding: three of the writers' four "compiler defects" were already registered

The writers reported four mechanisms as suspected defects. Checking each against `kb/Work/` before allocating an
id — which is the whole point of having one register — found **three already open**:

* READ … PREVIOUS on a RECORD SEQUENTIAL file silently executed as NEXT (`P1=00 R003` where §14.9.30.4 GR21
  sequential rule c requires R001), *and* the same phrase accepted unflagged at `--std 85` — **`PB334`**, which
  already names both arms and the same `SequentialIoBinder.BindRead` omission.
* `SHARING WITH ALL OTHER` with no `LOCK MODE` rejected at the FILE CONTROL ENTRY, on a program with no OPEN at
  all — §14.9.27.3 SR8 is a syntax rule of the **OPEN statement** and has no instance to bind — **`PB319`**.
* `START FIRST` / `START LAST` positioning under the CURRENT key of reference instead of resetting it to the
  prime key — **`PB356`** (whose own writer said so and asked not to be double-registered).

All three were re-measured here anyway, because a note can be stale: `shr_c.cob` still draws COBOLNET1512 at the
SELECT, and the READ PREVIOUS repro still prints `P1=00 R003` at 2023 and compiles silently at 85. So the round
opened **four** new notes, not seven.

## `PB383` — one guard, two rules, and the wrong substituted value for both

`FUNCTION BOOLEAN-OF-INTEGER(5, 8192)` prints `OVER=[0]`. The implementor's own register says it should print
nothing. §15.13.3 r2 is *satisfied* by 8 192 (*"a positive nonzero integer"*), so no ARGUMENT rule is violated;
what §15.4 catches is the returned LENGTH exceeding the documented 8 191-position maximum, and
`docs/CONFORMANCE.md` row `DOC-A.1-93` documents that result as **"the §15.3 checking-off default — a
zero-length value"**, naming `CobolIntrinsics.BooleanOfInteger` as one of its two enforcement sites. The code
guards `if (length < 1 || length > 8191)` and both arms `return "0"`. Two different rules are conflated in one
condition — `length < 1` is row 90's argument case, `length > 8191` is row 93's returned-length case — and both
documented answers are a zero-length value. The sibling proves the row right, not the code: row 93's *other*
named site, `BaseConvert`, does return the zero-length value, and its golden `l1_returned_value_length_limit`
landed in this same change set and passes. Two arms, one fixed.

## `PB384` — a determination filed under the wrong subject, the citation it propagated, and the green check that could not see either

Three A.1 rows were answering questions other than their own, and this is the failure mode CLAUDE.md rule 1
names, arriving from the side `cite.py --check` cannot guard: every sentence in each row was **true**.

**Item 209** is *USAGE DISPLAY (size and representation of characters)*, §13.18.60.4 GR7. The row carried the
**USAGE BIT** determination (GR5 + §8.5.1.6.3) under that number, with `BitLayout.cs` as its code-location. Its
witness `l1_usage_display_character_size` now measures all three of GR7's obligations rather than asserting
them — uniform size (`UNI=1 5 9`), the constraint against the national size (`LEQN=1 2`, and 1 ≤ 2 is what makes
the size a CONSTRAINED choice), character-boundary alignment that generates no filler (`ALIGN=5`), the
position/byte identity (`POS=5 5`), and the numeric-DISPLAY sign grid (`NUM=5 5 6`). The USAGE BIT determination
was **not deleted with the misfiling**: Annex A.1 has no USAGE BIT item at all (208 COMPUTATIONAL runs straight
to 209 DISPLAY, and §8.5.1.6.3 is cross-referenced only by item 195), so it moved verbatim to CONFORMANCE.md
§3, where a §4.2.6 determination with no A.1 number belongs.

**The wrong number had already propagated.** `2002/l1_byte_length_implicit_filler.cob` cited *"Annex A.1 item
209"* for the bits-per-byte determination, which is **item 12**. A landed golden's comment, inherited from the
misfiled row, and mechanically invisible because A.1 item numbers are not §-clauses. Swept: that site, the row,
and one dated forensic artifact deliberately left as the 2026-08-09 snapshot it is.

**Item 22** asked about §15.16.4 r2's SECOND sentence — *"If the order of multiple characters having the same
position is **undefined**, the implementor shall define which of those multiple characters is returned"* — and
the row documented the FIRST, the `ALSO` order, which §12.3.7.4 GR7 k)6 decides for the implementor. The
existing golden `l1_char_national_also_positions` says in its own header that it therefore does not close item
22, and it was right: nothing could. The undefined-order branch is reachable through `ALPHABET … FOR NATIONAL IS
LOCALE`, whose sequence is an ALGORITHM rather than a written order, and COBOL.NET answers **the lowest-coded
member of the rank** — `LocaleCollation.OrderVector.FirstOfRank`, materialized once per collator, which is why
r2's third sentence (every invocation returns the same character) holds structurally.
`l1_char_national_locale_tie` reads position 1 twice and gets `000001` both times.

**Item 188** said UCS-4 names a coded character set only. **Table 6 gives UCS-4 a Y in both columns**, and GR7 f
says outright that its collating sequence is ISO/IEC 10646 appearance order; it is UTF-8 and UTF-16 whose
collating-sequence column is empty. Documentation only — `CollatingModel.cs` had it right, in detail, all along.

**And `audit_annex_a1.py` printed `ok` for item 209 every run since it landed.** The check accepted a row on
*"at least two shared significant words"*, and the misfiled row shared `usage` and `representation` — words that
appear in 14 and 16 A.1 subjects respectively. An **absolute** threshold measures vocabulary, not identity, and
can never separate siblings inside one A.1 family. The fix is **relative and has no tuned constant**: a row must
fit the item it is filed under at least as well as it fits every other A.1 item. The misfiling scores 2 against
209 and **3** against each of 205 / 208 / 210 / 211 / 215. Measured over all 47 §7 rows: **zero** correct rows
flagged. A document-frequency threshold was measured first and rejected — nothing separates the misfiling
(best word df 14) from the correct DISPLAY row (best word df 9) with a margin worth trusting. `--self-test`
gained a case replaying the misfiling verbatim, so the new arm is proven able to fail for its own reason.

The `EveryDocRow_IsFiledUnderTheItemAnnexA1Names` gate then caught the other half on the first run: nineteen
findings where an inventory row cited evidence its own §7 row did not name. Fifteen `Pinned by` cells filled.

## `PB385` — the citation tool is right about the text and wrong about the label, 105 times

`cite.py --check 14.9.49.4 "… exception-name-1 is a level-2 exception-name are examined"` answers
**`OK §14.9.49.4 3) e)`** and then prints **f)** as the excerpt. Sub-items a)–e) of that clause are transcribed
with three spaces of indent and f)–g) at **column 0**, so the resolver's list ends early and the next paragraph
inherits the last letter it saw. Scanning the whole transcription for a column-0 `x)` continuing an indented
run: **105 sites**, including the ten-item §8.8.4.4.4 class-condition ladder and the intermediate-rounding
phrases. This produces exactly the artifact rule 1 exists to prevent — an inherited citation — *from the guard
itself*, and it is why the note asks for a `cite.py` drift check (no clause may resolve two distinct paragraphs
to one label) alongside the re-indent, so a re-transcription cannot reintroduce the class silently.

## `PB386` — the CONFORMS-but-untested band cannot empty, and that is now measurable

`Schema.state_for` makes every resolving verdict owe a SPEC-DERIVED test-ref, with no exemption. After this
landing **nine** rows are CONFORMS-but-untested, and **eight of them cannot earn a witness**: an §A.2 grant of
undefinedness (`GR-14.9.30.4-3`, `GR-14.9.34.4-2`), an antecedent no program can construct (`GR-14.9.30.4-20`,
`-23`, `GR-14.9.11.4-11`), a consequent indistinguishable from a sibling rule's (`RV-15.50.4-9`), §A.2's explicit
undefined list (`GR-14.9.5.4-11`), and a branch unreachable from COBOL source (`DOC-A.1-19`). The ninth is
`PB383`. Manufacturing a golden for any of the eight would pin an implementation choice as though the standard
required it — which is what the spec-derived clause exists to prevent — so they stay GAP and visible, and the
question of whether a DERIVATION can stand where a test cannot goes to the owner. It is the same shape as
`PB280` Q2, and this time the population is the whole band.

`DOC-A.1-19` reached that list the hard way: the writer wrote its golden, the refuter showed the output was
INVARIANT under the determination it claimed to measure (`CANCEL "Cobol.Net.Runtime"` and `CANCEL "L1NOSUCH"`
take the byte-identical path, because `ProgramTable.Cancel` resolves with `probe: false`), and the golden was
deleted. Its record now carries an empty `test-ref` on purpose, and the §7 row says why.

## Refuter and fixer, counted

Only four of the six slugs got a fixer pass, and the round produced no `reports/*.refute.json` at all — the
verdicts live inside `out/<slug>/REPORT.json`, which supersedes the writer's report for those four. Reading the
two versions against each other: **four refuter corrections upheld and applied**, of which exactly **one changed
a disposition** (`DOC-A.1-19`, above). One was non-conforming SOURCE in a golden — `l1_read_lock_mode_governs`
carried `SHARING WITH ALL OTHER` with no LOCK MODE, which §14.9.27.3 SR8 forbids on a file the program opens;
rewritten to `SHARING WITH READ ONLY`, `.out` unchanged. Two were prose that would have shipped wrong (a
§8.3.3.6.4 GR3 citation answering a different question, corrected to GR2; a miscount). And **one refuter claim
was itself refuted**: the assertion that `record_verdicts.py` accepts only one test-ref is false — it splits on
`"; "` and 600 rows already carry multiple — so `GR-14.9.30.4-24` now names both its goldens instead of
orphaning one.

Two other writer statements did not survive contact with the tree, and are recorded because a landing that only
reports its successes teaches nothing. `misc-p1`'s manifest note said `2002/l1_char_national_locale_tie` must be
listed **PENDING, never enabled**, because its `.out` carried the DOCUMENTED value rather than the observed one;
the validator then ran it and it PASSED, so documented and observed are the same value and it is enabled. And
`misc-p1` omitted that same file from `manifest_entries` entirely, which `Manifest_CoversEveryProgram_NoOverlap`
would have caught as an unlisted program.

## Gates

`dotnet build CobolSharp.sln -c Debug` clean. Conformance `Corpus|Negative|Intrinsic|VersionMatrix`
**3655 passed / 0 failed** (5 m 31 s). A second `CorpusRunnerTests` run with a trx logger confirms **all 49 new
goldens by name** as passing rows — registered-but-not-discovered is a red by absence, so it is checked, and the
checker was shown to report a fabricated name as missing. Unit
`SpecTraceabilityInventory|DefectiveRowCoverage|Manifest|AnnexA1Register` **22 passed / 0 failed** (after the
nineteen `Pinned by` findings were fixed). Characterization **33 passed / 0 failed**.
`audit_code_citations.py --check` **0 findings** — it caught a phantom §8.8.4.1.4 in the first draft of PB385
and the note now cites §8.8.4.4.4. `audit_doc_citations.py --check` **0 misfiled**. `semgrep/verify.py` is the
`PB175` pre-existing red and no count rose: `cobolnet-no-biginteger` 36 (= the registered baseline) and
`cobolnet-raw-diagnostic-code-literal` 439 (below the registered 475); the only `src/` edit in the landing is an
XML doc comment on `CobolIntrinsics.Random`, whose "the .NET time-derived default" was a pre-.NET-6 description
of a mechanism the register documents as per-process OS entropy.

`PB370` keeps all **7** of its rows — round 2 drew a different band, and that residue is the harder kind: three
whose existing witness asserts something strictly weaker than the rule. `PB260`, `PB261` and `PB284` are
untouched; no DOCUMENTED-NON-SUPPORT row moved this round, so the ledger DNS figure stays at 346 (344 of them closed). `PB303`'s
`inventory_rows` was corrected from `RV-15.65.4-4` — a MODULE-NAME returned-value rule that merely PERMITS the
AS form, and which closed CONFORMS in this very batch — to the three §11.10 rows the AS phrase actually needs to
parse.

