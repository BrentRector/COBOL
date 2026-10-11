## Entry 1572 — 2026-09-06 19:36 PDT — Landing train 22: the two decided owner features land together — `docs/rearchitecture/evidence/*.json` becomes FROZEN evidence with a machine-readable `superseded_by` marker and a third static audit, and the implementor code-name set opens with `ASCII` and `EBCDIC`, which drags §13.18.13.4 CODE-SET conversion into existence for the first time; GAP 2752 → 2748, no cluster dropped

**PB785 — the record that may not be repaired, and the marker that replaces the repair (owner question 21).**
The evidence directory is a RECORD — an agent's raw output, recorded as it was said — not a document CLAUDE.md
rule 6 makes current. That was already three quarters true in the tree: `scripts/spec/citation_corpus.py` names
the directory in its `FROZEN` tuple, both citation audits skip it, and `docs/DOC_INDEX.md` described the one
evidence file it indexed exactly that way. What was missing was the other half of the bargain.
`PHASE-13-grammar-batch-research.json:81` says *"C6-A (WRITE BEFORE AND AFTER ADVANCING) IS FULLY LANDED
end-to-end — grammar `writeBeforeAfter : writeAdvancePhrase writeAdvancePhrase?` … C6-A must NOT be
re-implemented"*, and `:106` names `CobolIO.g4:357-366` for it. That judgment is what PB712 refuted off the
rendered PDF page, and — this is the part that matters — **it is also what built PB712's defect in the first
place**: the refuted design was read out of the research artefact and implemented as fact. A record nobody may
repair, asserting something false, with no mark on it, is a trap that re-arms itself. So the answer is not a
repair but a MARK: `superseded_by` — `note` · `devlog` · `date` · `why` — placed as the FIRST key of the object
holding the refuted claim, because JSON preserves key order in the file and a reader going top-down must meet
the refutation before the claim. Both measured lines live inside one object (`specs[0].spec.currentState`), so
one marker covers both, and its `why` names them individually rather than condemning the object: the C6-B
findings around them, the still-unlanded `SUPPRESS WHEN` phrase, are untouched and it says so. Every `*.json`
in the directory also gained a top-level `_frozen` banner stating what the directory is and naming the marker
key, so the convention is discoverable from any file that carries it — `ledger-trend.json` with wording for
what it actually is, an append-only series of measurements whose banner survives `gen_ledger.write_trend()`
because that function replaces only `points`. ⛔ Every edit was TEXTUAL and byte-preserving — **13 inserted
lines across 7 files, zero lines changed**, CRLF intact: a `json.load`/`json.dump` round trip would have
reformatted every line of a frozen record and made the diff the whole file. Not one line of any evidence body
was edited; the refuted sentence still reads exactly as the agent said it, which is the point.

The sweep behind that ONE marker was measured, not assumed. Absent grammar rule names in the whole directory:
`writeAdvancePhrase` and nothing else, twice, both inside that one object — so lines 81 and 106 *were* the
complete set. Dangling `path:line` citations: zero of 225. Bare paths that do not exist: 122 — **and every one
is a PROPOSAL** (`tests/conformance/2023/alt_key_suppress.cob`, a golden the research asked for and nobody
wrote), which is why the gate reads only LINE-ANCHORED citations: a line number can only have been written
about a file that existed, and checking bare paths would have been 122 false reds on day one. Line-COORDINATE
drift, a rule that still exists but has moved: 10 — `alternateKeyClause` cited at `CobolIO.g4:146-149` and
living at `:205`, `writeBeforeAfter` cited at `:357-366`, which is `closeStatement` today. Those are reported
and never gated, a deliberate departure from a literal reading of the dispatch: nothing about them is refuted,
only their coordinates decayed, and gating would force ten markers that say something false or force
re-pointing the record — the one thing the freeze forbids. The banner carries the permanent fact instead
(*resolve the NAME, never the line number*) and `--check` prints the drift COUNT on every run so that sentence
keeps its evidence.

The drift check (rule 5) is `scripts/spec/audit_evidence_supersession.py`, wired into `build-local.sh`,
`build-local.ps1` and `battery.sh` PHASE -1 beside the two citation audits, for the symmetric reason: they ask
whether a citation of the STANDARD is right, this asks whether a frozen file's citation of the TREE is still
true. Four gated checks — the banner (which must NAME the marker key), a `path:line` that still resolves, a
claimed ANTLR rule name still in the grammar, and a marker actually carrying `note`/`date`/`why`, because an
exemption that says nothing is a mute button. The population is asserted — zero files or zero citations is a
refusal, exit 2, never a green — and the guarded directory is read back out of `citation_corpus.FROZEN` rather
than copied, so if the citation audits ever stop skipping it this audit refuses instead of guarding a record
they have started editing. It needs no submodule, which is the one way it differs from its two siblings: CI has
no citation step to add it beside and no job it belongs to, so it inherits their placement rather than opening
a job of its own, and that reasoning is recorded rather than the CI hook the dispatch had assumed existed.
⚠ Two false-accusation shapes were designed out by running the first draft over the corpus. Taking every
camelCase word from any string mentioning a `.g4` file accused four correct sites: `popMode` (an ANTLR lexer
command), `nameSlot`/`subscriptTrigger` (JSON field names quoted in prose) and — the instructive one —
`editingClause` in *"dataDescriptionClause alternatives; no editingClause"*, a NEGATIVE claim that is still
true. Taking every written-out production accused five more, among them `mcsFacilityStatement` and
`commitStatement`, which this corpus writes out under `risks` as rules it is PROPOSING. So a rule name is read
only where the text puts it in a declaration position — adjacent to a `<file>.g4:<line>` citation, or in a
production written with a space before its colon (which a quoted JSON key never has) whose HEAD is a rule that
really exists. A head that exists makes the line a claim about how that rule reads today; a head that does not
is a sketch the grammar has no opinion about. That last arm is what reaches line 81, which cites no file at all
and would otherwise have been invisible to the gate that exists for it. `--self-test` runs every gated check
twice — on a document built to break it and on the same document WITH the marker — plus the drift arm proving a
MOVED rule gates nothing: PASS. The live gate was made to fail once for real: with the marker removed, `⛔ 2
UNMARKED CLAIM(S) THE TREE CONTRADICTS`, exit 1, naming lines 82 and 107. On this train's merged tree, restored:
`7 frozen evidence JSON(s) · 226 line-anchored citation(s) · 48 grammar-rule claim(s) · 1 superseded_by
marker(s)` / `⛔ 0 UNMARKED` / `— 10 line-coordinate drift(s)`, exit 0. `docs/DOC_INDEX.md` gains a row for the
directory ITSELF — the gap PB785 measured, since the index routed a reader to neither artefact — and a GATE row
for the audit.

**PB793 — the implementor code-name set, and the CODE-SET conversion that came with it (owner question 22).**
The owner answered YES: `ASCII` and `EBCDIC` are supported as ALPHABET `code-name-1`, following GnuCOBOL's
latitude and never its GPL tables. §12.3.7.3 SR15 leaves the set to the implementor — *"if any"* — and the
answer is now a TABLE, `ImplementorCodeNames`, one row per name carrying exactly the three things §12.3.7.4 GR7
i obliges the implementor to specify; the binder's refusal became a LOOKUP with a refusal on miss, on BOTH arms,
and the COBOLNET1907 message names the supported set from the table itself, so the next code-name is a row and
no code path. The national arm answers "none" from the table's own contents rather than from a hard-coded
refusal, so a national row would bind with no code change. `ASCII` is ISO/IEC 646 IRV — 128 ordinals, the
identity correspondence, the native order, i.e. GR7 c's own STANDARD-1 answer under the spelling other vendors
use; a 256-character "extended ASCII" reading would name a set no standard defines and a 65,536-character one
would be NATIVE renamed, so no runtime carrier is emitted for it. `EBCDIC` is IBM CCSID 37, derived at bind
time from .NET's in-box code page and never transcribed: GnuCOBOL's default table corresponds to no registered
CCSID and its named alternates are CCSID 500, so its latitude is followed and its tables are not — and there is
no 256-entry literal in this repository to mistype or to drift. Measured rather than assumed:
`Encoding.GetEncoding(37)` is single-byte, maps all 256 code units onto 256 distinct native characters whose
maximum is U+00FF, and round-trips byte-exactly.

Table 6 gives code-name-1 a Y in BOTH columns, and the second one is where the work was. The collating half
cost nothing: §12.3.7.4 GR7 k3's rule for characters the literal phrase does not specify — a position greater
than the highest specified, relative order unchanged from the native sequence — is exactly what the 65,280
native characters CCSID 37 does not spell need, so it is ADOPTED as this implementation's GR7 i specification
and the existing sparse `CollatingTable` serves it, through a new shared `CollatingTable.Build` that is now
also the one place GR8/GR9's HIGH-VALUE/LOW-VALUE extremes are computed. That corrects the note's own cost
estimate, which had budgeted new collating machinery. The coded-set half made `SYMBOLIC … IN` and `CLASS … IN`
resolve ordinals in a 128- or 256-character set with not one line changed in either — the sizes come from the
row through `CodedCharacterSet.OrdinalCount`, so `SYMBOLIC S IS 257 IN A-EBC` and `SYMBOLIC T IS 129 IN A-ASC`
are now refused naming each set's real size. And it made §13.18.13.4 CODE-SET convert for the FIRST TIME: on
main every non-identity set was refused as the Annex A A.3 item 27 non-support, so GR6 had never had anything
to do, and supporting EBCDIC as an alphabet while refusing it in CODE-SET would have been half a feature. GR6
a/b are a 256-entry permutation the COMPILER computes and emits as a `char[]` literal exactly as a collating
sequence's weights are — the runtime holds no code page and knows no alphabet name — applied at the one
physical boundary of each organization: `NextFrame`'s three data returns reading, the new
`EmitRecord`/`EmitRecordLine` plus `OverwriteInPlace` writing, and `RecordFraming`'s payload for the keyed
stores. So a record area, a key value and a lock identity are always native, and the framing this processor
adds around a record stays native — a determination that is now published rather than implicit. ⛔ `NextFrame`
was deliberately not split into a raw twin, which would have got a single conversion point at the price of
moving `FillChars`/`ReadPhysicalLine` outside the body `SharedReadCoherenceDriftTests` inspects BY NAME: that
is perturbing the guard whose subject you are changing. The A.3 item 27 refusal also stopped being a list of
phrase names inside the file-description binder and became `CodedCharacterSet.Medium`, a property of the SET in
three states — which is what would otherwise have refused every code-name set the moment one gained a table.

⚠ One consequence had to be RE-DECIDED rather than re-worded, and it is flagged here for the owner.
`CONFORMANCE.md` DOC-A.1-129 justified leaving the code set out of the validated fixed-file attributes by
saying the value was a CONSTANT — *"on every program it accepts the medium's code set is the native one and the
comparison is a constant with no second value to take"*. EBCDIC makes that false. It STAYS out, but now as a
determination under §14.9.27.4 GR10's explicit latitude, for two reasons: the `.cbattr` attribute catalog is
written only when THIS processor creates a file, while the whole purpose of a CODE-SET clause is to read a file
some OTHER system wrote — so the check would fire exactly where it is not needed and never where it is; and
reading a converted file through a description carrying NO CODE-SET clause is §13.18.13.4 GR7's own default and
the raw-medium idiom, so a '39' there would reject legal source. `FixedFileAttributes.Conflicts`' comment, the
executable twin of that paragraph, carries the same text. The goldens are built so that each one's evidence
cannot be faked: `pb793_code_name_alphabets` prints `a-LT-A=Y` and `A-LT-0=Y`, BOTH the reverse of the native
answer, and its two SORT legs are SIBLING units, so §12.3.7.4 GR1 does not pass the first unit's sequence down
and each leg is evidence about the other; `pb793_code_set_ebcdic` re-reads the written medium through a SECOND
file description with no CODE-SET clause, which is what makes the round trip evidence at all, since a compiler
converting on NEITHER side would also print `AB012`; `pb793_code_name_alphabets_85` runs ASCII beside EBCDIC as
the control, because GR7 c's order IS the native order and a program collating sequence naming it must leave
the native answers alone. The negative that named `ASCII` names `FIELDATA` now. Nine rows re-verdicted with
DOC-A.1-183 LIVE for the first time (it was conditionally required and vacuous while the set was empty) and
GR-13.18.13.4-3 and -6 closed for the first time; COBOLNET1907's catalogue text was rewritten and
`docs/DIAGNOSTICS.md` regenerated; COBOLNET1912–1913 were reserved, never used, and return to the pool.

**The train.** Two clusters, brought in in manifest order onto `d4e09558` and committed one per cluster, no
cluster dropped, nothing carried that its own report did not describe. Both rows were plain patches from
`1ec771a2` and both applied clean against Q23's five intervening commits — PB785's DEVLOG hunk was excluded
because it had claimed Entry 1571, which Q23 now owns, and its paragraph is folded above instead; PB793's
inventory hunk was discarded and its batch re-applied on the merged tree, `record_verdicts.py` reporting 9
records / 9 rows changed with DOC-A.1-184 re-adjudicated DOCUMENTED-NON-SUPPORT → CONFORMS and
**GAP 2752 → 2748** against an unchanged denominator of 4,348 (`build_inventory.py --check`: 4348 rules, 4348
rows, GAP 2748, resolved 1600). `docs/DIAGNOSTICS.md` was regenerated once after the merge and came back BYTE
IDENTICAL to what the branch carried. ⛔ `tests/external/gnucobol-verdict-baseline.tsv` was re-baselined BY
HAND, never with `--write-baseline`: `configuration:885` and `run_misc:5406`, both `DEFAULT_DIALECT`, move
`WE_REJECT_THEY_ACCEPT` → `AGREE_ACCEPT` because PB793 makes both programs compile as GnuCOBOL does — and that
was MEASURED here rather than inherited from the report, by running the two differential legs against the
edited baseline: `configuration` (14 cases) and `run_misc` (179 cases), **0 PER-CASE FLIPS** on both, with each
row reading `AGREE_ACCEPT` in the report. Gate, one build for the train (`dotnet build CobolSharp.sln -c
Debug`, 0 warnings 0 errors) and then: the union Conformance filter
`~Alphabet|~Collating|~SpecialNames|~Sort|~Corpus|~Nist|~VersionMatrix`, every term expanded to
`FullyQualifiedName~` and every term confirmed live by `filter_population.py` (7 terms, 19/20/4/26/1804/356/2244)
— `Passed! - Failed: 0, Passed: 4468, Skipped: 0, Total: 4468`; the FULL `Cobol.Net.Tests.Unit` assembly —
`Failed! - Failed: 2, Passed: 23040, Total: 23042`, the two `ExternalCorpusPopulationDriftTests` reds and
nothing else, both printing `EXTERNAL POPULATION ABSENT … state: absent` because the GPL corpus is not in a
fresh worktree; the legacy `CobolSharp.Tests.Integration` assembly — `Passed! - Failed: 0, Passed: 503,
Skipped: 1, Total: 504`; Characterization 33 of 33; and `NistDifferentialTests` re-run as its own leg, 349 of
349. All three PHASE -1 audits after
the last write (`audit_code_citations`, `audit_doc_citations`, `audit_evidence_supersession`) and
`audit_annex_a1.py` at FINDINGS: none, 56 of 222 items determined, 143 obligations remaining.
`scripts/semgrep/verify.py`: PASS with every count unchanged — 3 / 46 / 2 / 423. `work.py check`: 834 items,
all well-formed. Both notes carry `status: landed`, PB793 keeping `kind: decision` on the PB146 shape with nine
`inventory_rows` matching its batch exactly. The train reached main through `scripts/push-main.sh` — the first
landing train to do so under the required `ci-gate` check that Q23 installed one entry ago.

