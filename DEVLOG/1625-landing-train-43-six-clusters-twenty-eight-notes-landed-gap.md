## Entry 1625 — 2026-09-21 13:01 PDT — Landing train 43: six clusters, twenty-eight notes landed, GAP 2339 → 2310

Train 43 carried SIX clusters into one landing from six implementer worktrees, every one of them based on
`4da7ba02d` — the head `origin/main` still carried when the train was dispatched, so nothing needed rebasing and
the six diffs were brought in one at a time, each with its own commit, in the manifest's order. Three of the six
clusters are wave-42 work that the daily-cap suspension left on a predecessor branch: clusters 1 and 6 each
MERGE their `wf_c18987cb-f4c-*` predecessor, so those two branches carry nothing that is not in this train and
must never be landed separately.

**Cluster 1 — SET-TO-FALSE and SET formats (PB455, PB561, PB920, PB890, PB559, PB503, PB450).** The mechanism
was a VALUE clause whose Format-3 screens asked the wrong question or no question at all. `SET condition-name TO
FALSE` over a GROUP item measured the FALSE image against the elementary item's length rather than the group's,
so a group condition-name received a truncated picture; the SET Format-5 and condition-name arms reach emission
through `CobolParserCore.g4`, `SetBinder`, `SetAlterBinder`, `BoundTree.BoundSetConditions` and `SetEmitter`
together. Two new screens: COBOLNET2176 refuses `VALUE … IN alphabet-name-1` with no THROUGH phrase, on
§13.18.63.3 SR31's "only when" — with no THROUGH phrase there are no "literals specified in the THROUGH phrase"
for the permission to apply to — and COBOLNET2177 refuses the connective spellings no general format prints
(`VALUE ARE`, `VALUES IS`, and `VALUES` at all in Format 1), on §13.18.63.2 read with §5.2.6.3's brace choice.
⛔ The re-probe corrected the queue: PB503's printed operand order was ALREADY correct on today's tree, so
`Core/CobolData.g4` is not touched at all and the note records the measurement instead of a change. The
shared-seam consequence is a signature: `EditedImageOfNumericValue` now takes `(dialectLevel,
decimalPointIsComma)` rather than an `EmitContext`, which moves `ValueInitializer` and `ConditionRenderer`
together. Four positive goldens and eight negatives.

**Cluster 2 — INITIALIZE value occasions (PB415, PB418, PB577, PB576, PB933 arm 1).** §14.9.20 GR7's dynamic-
length determination binds an INITIALIZE to the length its sender actually carries, gated to the edition that
introduces it; ODO-with-VALUE and the float VALUE occasions (including the FILE SECTION case) are pinned at 85
and 2002. The structural half is PB933: `InitializeBinder.InitializeValueOperand` now asks
`FigurativeConstants.Classify` and `InitializeFigurativeKind` is DELETED, and the sibling sweep found a SEVENTH
copy of the same rule one file over — `ValueInitializer.TryParseFloatLiteral` carried its own
`"ZERO"/"ZEROS"/"ZEROES"` test inside the very §13.18.63 recipe that owns the clause. That site asks the WORD
map rather than `Classify` because §8.3.3.6.3 SR1 a) restricts a numeric-literal context to ZERO (ZEROS, ZEROES)
"without the ALL phrase", which is exactly the strip `Classify` performs, so the substitution is
behaviour-identical there. ⛔ Arm 2 stays OPEN as a mechanism of its own, and its reader census was MEASURED on
this tree rather than inherited: five readers plus a writer and a copier, spanning `DataBinder`,
`SearchAllFormat2Rules`, `SetEmitter` and `ConditionRenderer`, correcting the count the note had carried.

**Cluster 3 — exception checking and PERFORM ranges (PB433, PB595, PB441, PB395).** §14.9.28.3 SR11 — "both
shall be procedure-names in the same declarative section" — had no enforcement, and a `PERFORM A THRU B` range
straddling the DECLARATIVES boundary compiled to a program that recursed until the CLR killed it, with no
diagnostic at any `--std`. COBOLNET2186 refuses it as an ERROR, not the §4.2.2 warning, because the range has no
conforming meaning to compile: §14.9.28.4 GR4's specified set of statements runs from procedure-name-1's first
statement to procedure-name-2's last, and across that boundary the intervening procedures belong to a different
USE procedure or to none. COBOLNET2187 is the other shape: §7.3.25.3 SR5, §7.3.22.3 SR4 and §7.3.20.3 SR4 are
three syntax rules of ONE shape — a `>>TURN`, `>>PUSH` or `>>POP` written lexically within an exception-checking
PERFORM — and it is a suppressible WARNING under owner decision D20, flat across the whole statement,
`imperative-statement-1` included, because §4.2.2 requires only a warning mechanism for a syntax-rule violation
and GR14's semantics are implemented for the accepted case. A seventh pin records the boundary: a banned
directive inside an OMITTED conditional-compilation branch draws nothing, because the sites are recorded after
the driver has blanked it. Structurally this adds a Frontend stage, `DirectiveSiteProcessor`, which must run
BEFORE `TurnDirectiveProcessor`; `ConfigureEc` gains a parameter and its three call sites follow.

**Cluster 4 — data-description screens (PB587, PB585, PB570, PB527, PB501, PB921).** Five screens the
data-description entry never ran, drawn together through ONE literal funnel — `ScreenValueLiteral` /
`ValidateValueCategory` — rather than five separate checks: SIGN on a group over a signed picture and below its
edition; PICTURE written on a GROUP item (COBOLNET2191); a Format-3 condition-name entry with no
condition-name; and the numeric-edited VALUE edition screen in BOTH places it can be written, the condition-name
entry and the REPORT SECTION's format-4 literal. The two-arm shape is the point: PB921 would have been half
fixed if only one of those two writing positions had been screened.

**Cluster 5 — MOVE operands (PB886, PB896, PB542, PB922, PB602).** A zero-length GROUP sender gets its own
`ZeroLengthItemRoute` arm in `MoveClassifier`, and `MoveEmitter`'s per-kind switch body is EXTRACTED to
`EmitStore` — the body moves verbatim, one indent out. COBOLNET2196 refuses EXCEPTION-OBJECT as a receiving
operand, and the predefined register is now classified BEFORE the general identifier lookup, matching
`SetFormatSelection.KindOf` on the receiving side; `ctx.Refs.IsExceptionObjectRegister(...)` becomes the single
question and one overload dies. ⛔ The unfiltered gate is what earned this cluster its last fix: three reds on
the implementer's first whole-assembly run, two of them COBOLNET0848 expectations following the deliberate
one-rule-one-code unification, and ONE a real regression — `OoSpineTests.SetTypedFromExceptionObject_WrongClass_
EcOoUniversal`, where the general SET sender arm claimed the register and refused a typed receiver against
§14.9.39.3 SR12 at compile time. A filtered gate would have shipped it.

**Cluster 6 — citation fidelity (PB900, PB838, PB932, PB388).** ⛔ The finding that matters most in this train is
that the gate which exists to catch inherited citations was itself driven by nothing.
`audit_doc_citations.py --self-test` re-implemented the MISFILED ruling in four lines of its own beside `scan`,
so an arm added to `scan` was invisible to it: every case passed and it printed PASS. The ruling is now ONE pure
function, `verdict_of`, called by both, each self-test case names the ARM it expects — and **neither audit's
`--self-test` ran in ANY gate**, only `--check` did, which is how a new arm could be added and never exercised.
`CitationAuditSelfTestDriftTests` (Unit) now shells both and asserts the CASE NAMES, because a self-test reduced
to its happy path still exits 0; it was proven to fire. PB900 arm A is a VETO rather than an `_owning_cite`
rewrite, and the measurement is the argument: spelling a format's three-part NAME is a claim about ONE citation
so re-attributing by POSITION is right for it, but a rule designator is written once and referred BACK to in the
same sentence and is chained (`GR4/GR6/GR9`), so position cannot separate the claim from the back-reference.
Three readings of the same corpus: nearest-left with no veto 209 findings, nearest-left + veto 170, JOIN-ONLY
54. A narrowing can only DROP a finding, so join-only's 54 are a SUBSET of the 170 and it buys its quiet by
discarding 116 accusations no second clause can rescue; the veto drops exactly the 39 sentences where a clause
the line ALREADY NAMES admits the ordinal. Arm B adds the ELIDED quotation check inside `audit_doc_citations.py`
(that audit already reads `src` through `citation_corpus.all_files()`): `elided_window` decides the one
decidable shape inside the `absent` bucket — the clause's own sentence WITH WORDS DROPPED — and its first
harvest was eight sites, six of them real, four unknown to the note, each repaired against the standard's own
words. PB838 lands the decidable half of its note: a `§`-reference in a C# STRING LITERAL that reaches a
diagnostic must RESOLVE, so `DIAG-NO-RULE` GATES (scoped to string literals under `src/`, because the same shape
is 2683 sites tree-wide in prose and 5 in message strings) while `DIAG-UNQUALIFIED` joins the reported backlog.
Seven citations repaired in shipped diagnostics, two outright wrong (§13.18.5 for a rule in §13.16.3; §14.9.18
**SR** for a **GR**), and the seventh was a two-arm dispatch: `CaptureImplements` already threads each
paragraph's own citation into its RESOLUTION arm while the DUPLICATE arm beside it hard-coded §11.8. ⛔ Two
numbers the predecessor's WIP carried — "142 of 209" and "join-only dropped 209 to 67" — did NOT survive
re-measurement and are corrected. A number in a comment is a claim.

**The train itself.** Six clusters, six commits, no cluster dropped. Eighteen conflicts across clusters 2–6,
every one of them resolved as keep-both and then VERIFIED rather than assumed: `git diff --check | grep -i
"conflict marker"` on the working tree and `git grep --cached` on the index were both silent after each cluster.
Seventeen of the eighteen were pure APPEND adjacency in shared registers (`DiagnosticCatalog.cs`,
`docs/DIAGNOSTICS.md`, four conformance `manifest.json`s), and each JSON conflict's element COUNT was measured
against the union rather than trusted — the brief's train-33 lesson, that a JSON list conflict taken on both
sides is silently lossy: enabled 2002 → 312, 2023 → 617, 85 → 121, negative → 1240, zero duplicates, nothing
missing from either side, nothing present that was in neither. The eighteenth was `kb/Work/PB933.md`, where
cluster 1 renames the recipe drift test to `…_HasExactlyThreeReaders` (PB920 makes `DataBinder` the third
deliberate reader) and cluster 2 appends its LANDED section still spelling the old name; both sides were kept
and cluster 2's stale spelling dropped, against the merged tree's actual test name.

⛔ THE MERGES WERE THEN CHECKED BY REGENERATION, not by inspection. Every generated artifact was rebuilt on the
merged tree and compared byte for byte: `docs/DIAGNOSTICS.md` from `DiagnosticCatalog` — IDENTICAL to the
hand-merged file — `ConstructRegistry.g.cs` and `Constructs.g.cs` (239 rows → 239 ids), the cobol-word table at
**total=124** (the exact counter train 33 lost a row on) and the reserved-word tables at total=462. All
identical. The twelve verdict batches were then re-applied in landing order on the merged tree: 29 records,
**rows changed 0** — the textual merge of `traceability-inventory.json` had preserved every cluster's rows
exactly, and the re-application is the authority that says so rather than an assumption that it did.

GAP was measured at BOTH ends rather than inherited: the base `4da7ba02d` inventory regenerates 4348 items /
**2339** GAP, the merged tree 4348 / **2310**. −29, matching the 29 records exactly. Every note flipped to
`landed` carries `closes_rows` (20 notes) or `closes_rows: []` with a `closes_rows_reason` (7), no landed note
claims a row that is still GAP, and `backfill_closes_rows.py` finds nothing to add. All 49 new goldens are
registered in their edition manifest and all 26 negatives carry their `*> reject-at:` line and their `.err`. The
six diagnostic codes claimed — 2176, 2177, 2186, 2187, 2191, 2196 — appear once each in the catalog and once
each in `DIAGNOSTICS.md`, and the clusters' assigned ranges do not collide.

Plan §9's CITATION GATES paragraph was RE-MEASURED on the merged tree rather than carried over from cluster 6's
worktree, which had five fewer clusters of citations in it: 4529 files → 4584, the non-gating backlog 330
(160/102/68) → **328** (155 DIAG-UNQUALIFIED · 105 RULE · 68 SUBITEM), and the doc audit 509 checked / 462
correct → **516 / 469**. `semgrep verify.py` PASSED with no count increased and one DOWN —
`cobolnet-raw-diagnostic-code-literal` 416 → **393** on cluster 6's citation repairs — and the baseline is
locked to the improvement (kb/Work PB175).

**THE GATE — the WHOLE assembly, no filter, on the merged tree.** One `dotnet build CobolSharp.sln -c Debug`
(0 Warning(s), 0 Error(s)) and then four legs, one at a time, each `--no-build`, with the GPL GnuCOBOL corpus
fetched into this worktree first so `ExternalCorpusPopulationDriftTests` MEASURED its population rather than
being red by absence:

```
Passed!  - Failed:     0, Passed:  7709, Skipped:     0, Total:  7709, Duration: 13 m  6 s - Cobol.Net.Tests.Conformance.dll (net10.0)
Passed!  - Failed:     0, Passed: 24365, Skipped:     0, Total: 24365, Duration:  2 m 18 s - Cobol.Net.Tests.Unit.dll (net10.0)
Passed!  - Failed:     0, Passed:    33, Skipped:     0, Total:    33, Duration:       2 s - Cobol.Net.Tests.Characterization.dll (net10.0)
Passed!  - Failed:     0, Passed:   503, Skipped:     1, Total:   504, Duration:      34 s - CobolSharp.Tests.Integration.dll (net10.0)
```

NO FILTER WAS USED, so no term can be inert and CI's `rest` shard — Conformance minus VersionMatrix minus
CorpusRunner, the population no implementer term names and the shard that drew a red first CI run on trains 39,
40 and 41 — ran here like everything else. POPULATION ASSERTED against battery #81: Conformance **7632 →
7709 (+77)**, Unit **24291 → 24365 (+74)**, Characterization **33, unchanged**. **NOT ONE RED on any leg**, so
there was nothing to bisect and no cluster to drop. Statics ahead of it, all on the merged tree:
`audit_code_citations --check` 0 findings over 4584 files, `audit_doc_citations --check` 516 checked / 469
correct / 0 MISFILED / 0 ELIDED, both audits' `--self-test` PASS, `audit_annex_a1` / `audit_derivations` /
`audit_evidence_supersession` / `audit_catalog_coverage` all clean, `filter_population --self-test` ALL GREEN,
`work.py check` 971 items well-formed, `gen_conformance_notes --check` 15 notes matching the inventory exactly.
The train's central code allocation ran COBOLNET2176–2202 of which SIX were used, so the next free code is
**COBOLNET2203** — the returned ones (2178–2185, 2188–2190, 2192–2195, 2197–2202) are never reused, which is
why `session-probe`'s catalog-ceiling answer of 2197 is BELOW the allocator and must not be taken for it.

