## Entry 1547 — 2026-09-06 04:37 PDT — Landing train 15: six clusters, GAP 2815 → 2810, and one verdict that had to be un-landed

**PB691 + PB144 — the WRITE arm of a three-time silent drop, and what "tolerated" actually obliges.**
`SequentialIoBinder.BindWrite` never called `w.writeInvalidKey()`. The phrase parsed, bound nowhere, and neither
imperative ran — at every edition and on both severity axes, with no diagnostic. It hid because the
`writeInvalidKey` sub-rule was consumed by the KEYED binder only: the two-arm dispatch with one arm fixed, this
repo's most reproducible defect shape, and the third time this exact family produced it (PB144 for REWRITE,
PB334 for READ). §14.9.51.3 SR2 states it as a format rule — *"If the organization of the write file is
sequential, format 1 shall be specified"* — so the prohibition lives in the DIAGRAM, and PDF pages 785–786 were
RENDERED rather than read off the OCR: Format 1 carries ADVANCING + END-OF-PAGE and no INVALID KEY bracket,
Format 2 the reverse. The screen is `StatementValidation.ScreenForbiddenPhrase` on the existing COBOLNET1720
descriptor, routed through `EditionContext.Removed` (error under strict, warning with an unchanged bind under
`--permissive`), with the REWRITE sibling given the same shape. The deeper finding is that *"the phrase is dead
under `--permissive`"* had been over-read into a licence to DROP it: §9.1.14's final rule has two items, and
item 2 — *"control is transferred to the end of the input-output statement or to the imperative-statement
specified in the NOT INVALID KEY phrase if it is specified"* — makes the NOT INVALID arm **live** on every one of
these statements, because each of them succeeds. Only the INVALID arm is provably dead (§9.1.13.5 names a
relative or indexed file in each of `'21'`–`'24'`). So both sequential arms now bind the pair, and `EmitInvalid`
moved from `KeyedIoEmitter` to `SequentialIoEmitter` — the declared home of the file-I/O common services the
keyed emitter already consumes — with the four keyed call sites repointed; exactly one `EmitInvalid` now exists.
Two negative fixtures, six behavioural tests, and a one-sided D9 sweep over the 459-program NIST corpus
(`COBOLNET1720 hits: 0`, the detector first proved able to fire). At the landing the COBOLNET1720 descriptor,
`BoundStores` and the D9 section were all resolved as the UNION with train 13's PB334 — the code now serves SIX
rules, and the design doc's "Four syntax rules" lead and "One code, five rules" line were corrected with it.

**PB693 — a hand-set flag where a derivation belonged.** `scripts/gen-cobol-words.ps1` emitted every reserved
word carrying `nameSlot: true` as a bare alternative of `cobolWord`, the user-defined-word slot, unless a
hand-set `reservationGated` flag said otherwise — and the flag was set on FIVE words. `UNLOCK` and 51 siblings
were therefore accepted as user words at 2002+, so a period-less `UNLOCK F1` after a `MOVE` was swallowed as two
more MOVE receivers: legal source rejected, §8.9 defeated. The fix is the derivation, not five more flags —
step 4b now computes the gate FROM `reserved-words.json` (5 → 58 gated words), the flag is deleted and rejected,
and `CobolWordsG4_ReservationGate_Is_Derived_From_Section89` keeps it derived. Around it: a strict-only
`userWordHere` predicate with the permissive axis on the parser `Edition`; gated-word re-admission in every
definition slot (program/section/paragraph name, SELECT file-name), because §8.3.2.1 makes those the places a
word is DEFINED; and COBOLNET0901 re-coded in `CobolErrorListener`, fired once per offending token instead of
twice. ⚠ The one gate red was attributed at the ROOT rather than suppressed: `validateStatusStage` had borrowed
`cobolWord` for the KEYWORDS FORMAT/CONTENT/RELATION that §13.18.62.2 underlines, so the new gate rejected legal
VALIDATE-STATUS source; a sweep of ~45 such sites found FORMAT to be the only §8.9 word with a lexer token a
rule expected `cobolWord` to match, and it got its own token arm. Two inherited clause numbers were re-derived
and corrected (§5.2.3 → §5.2.2, §4.2.6 → §4.2.7).

**PB703 + PB364 — the rule's boundary is the CLAUSE, and the register is now one register.**
`DataBinder.ResolveFileCollating` screened §12.4.5.7.3 SR8 with a `HashSet` hoisted OUTSIDE the loop over the
clauses while adding per NAME inside it, so ONE key-level clause naming a key twice was rejected by a diagnostic
the program falsifies — and the two-clause-plus-repeat case reported the rule twice. PDF page 352 rendered puts
the Format-2 ellipsis to the RIGHT of the closing brace, so §5.2.7 makes the brace group repeatable and one
clause is never "more than one"; SR8's own text, re-validated here, is *"Neither data-name-1 nor
record-key-name-1 shall be specified in more than one COLLATING SEQUENCE clause."* PB364 had already met this
rule SHAPE in another subsystem and built `UseOperandRegister` for it, and main had since re-added a private
copy in `ProcedureTableBuilder`; that copy is deleted and the register generalized to
`Binding/ConstructOperandRegister.cs` with its consumer list in the doc comment. The ordinal moved first-seen →
last-seen so one violation is one diagnostic, and that correction was proved load-bearing before it was trusted:
reverting `Register` turns all seven `*_DiagnosedOnce` rows red. An SR8 negative had to be WRITTEN — nothing in
the corpus used `COLLATING SEQUENCE OF` at all, so the over-rejection had never been observed.

**PB686 — an adjudication that changed no behaviour and wrote the reasoning down.** At LINAGE-COUNTER = page
size the two arms of §14.9.51.4 GR26 appear to overlap. Printed page 792 confirms the transcription is faithful,
so this is the standard's own text. The literal reading of a) cannot stand: §13.18.34.4 GR2 would be destroyed
(an N-line body could hold at most N−1 lines), it contradicts GR3's inclusive footing area, and GR26's lead
sentence plus GR7 c) 1 corroborate the other reading. DETERMINATION: keep the strict boundary the connector
implements — `counter > page size` = a), `[footing start, page size]` = b). No owner question, because the
survey did not split: IBM Enterprise COBOL 6.3 overflows on "exceed" with no upper clamp, Micro Focus calls the
footing area inclusive, and the NIST SQ201M golden (`LINAGE IS 50 WITH FOOTING AT 45`) prints footing lines at
45 through 50 inclusive. Recorded as a `docs/CONFORMANCE.md` §4 determination with both `AdvanceLinageCounter`
arms carrying the reasoning and two goldens; `DefectiveRowCoverageDriftTests` forced the honest bookkeeping,
so the still-open PB523 now claims `GR-14.9.51.4-26` rather than leaving a defective row orphaned.

**PB683 — governance the compiler cannot know, and the sibling that "which arm did I fix?" produced.**
`SequentialIoEmitter.LockGoverned` decided at COMPILE time whether a record verb rendered the governed runtime
entry, from the SELECT and the statement's own phrases — but §9.1.15 says *"The SHARING phrase on an OPEN
statement overrides the SHARING clause in the file control entry for establishing the sharing mode."* So a
connector opened `SHARING WITH READ ONLY` or `WITH NO OTHER` ran every unphrased verb ungoverned and read
another connector's locked record with `'00'` where §14.9.30.4 GR9/GR10 b) require `'51'`; adding
`RETRY 0 TIMES`, a no-op by §14.7.9.3 GR4 a), changed the answer because the predicate could see the RETRY
phrase and could not see the OPEN. Only these two spellings were affected because §14.9.27.3 SR8 forces a LOCK
MODE clause whenever ALL is used. The predicate is DELETED and the ten ungoverned renderers removed from
`RuntimeApi` — the wrong route no longer exists — with governance decided one layer down in `FileRegistry`,
where the OPEN's phrase is already recorded. The sibling: `EmitWrite`'s two print-control arms rendered
`WriteAdvancing`/`WriteBeforeAndAfter`, entries with no lock or RETRY parameter, so
`WRITE R BEFORE ADVANCING 1 LINE WITH LOCK` dropped both phrases silently, though §14.9.51.4 GR10/GR11 are
ALL-FILES rules and Format 1 prints ADVANCING, the retry-phrase and the WITH LOCK bracket together. The
ADVANCING phrases now ride into `WriteShared` as a `WriteAdvance` descriptor and `EmitWrite` is one call.
Measured `B=ALPHA|00` → `BST=51`. ⛔ Recorded as **D20**, not the D18 the implementer wrote: train 13 holds D18
(PB345) and train 14's PB692 took D19, and the note's cross-reference was corrected with it.

**PB695 family 1 — fifteen optional words, and six the audit structurally cannot see.** §5.2.2 makes a
non-underlined keyword optional, and fifteen rows across `CobolSpecialNames.g4` and `CobolParserCore.g4` wrote
one as required; every underline was re-measured on the folios. The six siblings — currencySignClause WITH,
classDefinitionClause FOR, symbolicCharactersClause FOR, the switch STATUS pair, implementorSwitchEntry IS,
`CobolIO.g4#collatingForPhrase` FOR — are words required INSIDE an optional group, which reads as optional to a
tool that measures the group, and they were found by reading the folios rather than re-running the tool: the
audit's silence is not evidence. Two restructurings, both because relaxing in place would have been silently
wrong: the three copies of `FOR {ALPHANUMERIC | NATIONAL}` became one `specialNamesForPhrase` (two 2002 gates
keyed on `ctx.FOR()` would otherwise have switched off with nothing failing), and OBJECT-COMPUTER's
clause-or-computer-name ambiguity became one left-edge predicate serving both the optional name slot and the
`computerAttributes` sink. The audit's noise was fixed at the root — 42 candidates → 12 by three structural
changes — and proved against the PRE-fix grammar, where the tool still reports all fifteen real rows.

**The train.** Six clusters, one build, one gate, six commits plus this one. Bases ASCENDED across the rows
(973460ba, 979b670c, then four at 2c8e32cb), so every row predated trains 13 and 14 and each conflict was
resolved by keeping BOTH mechanisms and re-deriving the seam: the COBOLNET1720 union above; `BoundStores`
walking PB334's READ lists AND PB691's WRITE/REWRITE lists; `EmitWrite`/`EmitRewrite` as PB683's unconditional
governed call with PB691's §9.1.14 status snapshot riding after it; and eight conformance-manifest collisions
across rows 1, 2, 3 and 6, all unioned. Union filter over the six rows' terms (23 `FullyQualifiedName~` terms,
every one spelled out — an OR'd bare `~X` matches nothing and exits 0). Gate: Conformance
`Failed: 1, Passed: 4469, Skipped: 0, Total: 4470`, the FULL `Cobol.Net.Tests.Unit` assembly
`Failed: 2, Passed: 22527, Skipped: 0, Total: 22529` (the two known `ExternalCorpusPopulationDriftTests`
reds — no GPL corpus in a worktree — and nothing else), the legacy `CobolSharp.Tests.Integration`
`Failed: 0, Passed: 503, Skipped: 1, Total: 504`, Characterization `0/33 failed`, the NIST leg alone
`Failed: 1, Passed: 351, Total: 352`, `GrammarDiagramGeneratorDriftTests` green in ISOLATION,
`work.py check` ✓ 763 items, `audit_code_citations --check` 0 findings and `audit_doc_citations --check`
0 misfiled of 350. GAP **2815 → 2810**.

⚠ **The gate's ONE Conformance red is NOT this train's, and it is on main right now.**
`NistDifferentialTests_P0.NistProgram_MatchesGolden("NC215A")` — the program that exists to test the ALPHABET
literal phrase and `PROGRAM COLLATING SEQUENCE` — fails `SEQ-TEST-GF-6`, `IF NINE-DU-9V0-1 < SPACE` under
`PROGRAM COLLATING SEQUENCE IS THE-WILD-ONE`, printing `FAIL*` where the golden requires `PASS`. It was NOT
debugged in place and NOT assumed: it was bisected by build, one commit at a time — GREEN at `c34f01ad`
(battery #57's tree), RED at `f798397f` (**train 14's PB678**, "SORT/MERGE resolves a collating sequence PER
KEY CLASS"), RED at pre-train-15 main `f86a5764` with none of train 15 applied, and RED at train 15's own
row-5 boundary and head with the same single assertion at the same position. So it is a wrong-answer
regression that train 14 landed and its gate did not see, and it needs a kb/Work note from central id
allocation. Train 15 lands with it because it is attributed to a commit that is already on main — the brief's
bar is that no red goes in UNATTRIBUTED, and this one is attributed to the commit that introduced it.

⛔ **Row 6 shipped committed conflict markers, and they were repaired rather than landed.** Its merge commit
`c6460d0f` had written four unresolved `<<<<<<< HEAD` hunks into
`scripts/spec/audit_grammar_optional_words.py`, leaving the file a SyntaxError near line 185 — the audit could
not even import on that branch tip. Main's copy is byte-identical to the merge base, so the correct content is
family 1's own pre-merge version, taken from `c321c521`; the file now byte-compiles and runs to its own
PDF-submodule guard. A `grep` for markers over `scripts/ src/ tests/ docs/ kb/` and `git diff --check` are now
part of the per-row routine. Consequently the report's "42 → 12 candidates" figure was measured on the
pre-merge tree.

⛔ **A verdict was un-landed, and the reason is the one this project keeps re-learning: a finding inherited from
a stale tree is not a finding.** PB691's batch carried `GR-14.9.41.4-6` as CONFORMS → **PARTIAL**, with the
note *"SEQUENTIAL HALF — NOT IMPLEMENTED: the statement aborts the run unit"*. That was true on the row's base
`973460ba` and false on today's tree: train 13's PB352 implemented START on a sequential-organization file, and
`979b670c` had already recorded the row CONFORMS with `conformance:2002/pb352_start_sequential_first_last` as
its closing test-ref. Applying the batch as written silently re-opened a row train 13 had earned, and
`DefectiveRowCoverageDriftTests` caught it as the third Unit red — the row was now defective with no open note
claiming it. The record was NOT applied: a corrective batch restores CONFORMS with main's test-ref and notes,
re-pointing only the `code-location` that PB691 genuinely moved (`KeyedIoEmitter.cs#EmitInvalid` →
`SequentialIoEmitter.cs#EmitInvalid`). The lesson for the fleet is that an implementer's *inventory* claims
need the same re-probe on today's tree as its *contract* claims — a batch record is a verdict, and a verdict
derived on a three-train-old base can regress the ledger while every code test stays green.

⚠ **`semgrep` went 423 → 427 and it was the BASELINE that was wrong, not the train.** The whole delta is in
`ProcedureTableBuilder.cs`, and a per-hunk attribution against `git diff -U0` puts ZERO findings on lines the
train wrote. Scanning the pre-train copy of that one file reports `PartialParsing` over lines 7–467: semgrep's
C# front end gave up on two thirds of the file and could see no match in it. PB703 deleted the nested
`private sealed class UseOperandRegister<TKey>(IEqualityComparer<TKey>? comparer = null)` — a nested generic
type with a primary constructor — the file parsed, and four always-existing violations became visible.
Re-baselined to 427 with the mechanism recorded in `kb/Work/PB175`, which also now carries the general point:
a semgrep count is evidence about what the parser could READ, never about what the tree contains, and
`verify.py` should fail or at least print on any `PartialParsing` in the greenfield scan. The current tree still
reports one small partial-parse span in that same file, so 427 is a floor, not a measurement.

