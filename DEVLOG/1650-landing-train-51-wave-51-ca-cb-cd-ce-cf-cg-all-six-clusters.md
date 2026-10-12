## Entry 1650 — 2026-09-22 20:32 PDT — Landing train 51: wave 51 (CA, CB, CD, CE, CF, CG), all six clusters, ahead of an unlanded train 50

Train 51 landed six clusters and moved fourteen notes to `landed` in one landing. It was the first PIPELINED lander.
It merged and gated on origin/main at train 49 (`37f19e028`). It did not block on train 50, because train 50's
lander had stopped gracefully before push-main for the 19:40 session reset, and its report says NOT LANDED. The
brief's rule applies: stop waiting and proceed on origin/main as it is. Minutes waited: 0. Train 50 now rebases onto
this train and takes the next DEVLOG number.

**CA — PB971, `*-ARG-OMITTED` fires at the reference.** ISO §14.9.4.4 GR12, §8.4.3.2.4 GR8 and §14.9.23.4 GR10
raise the condition when the activated element REFERENCES an omitted formal. Before this, `CobolArgAdapt.Omitted<T>`
raised EC-PROGRAM-ARG-OMITTED at the crossing, and functions and methods never raised their own names.
`Runtime/Control/OmittedFormal` is now the only raise site, keyed to the element kind.
- Formal roots carry `DataItem.OmittedGuard` only when the group enables that kind's name, so a unit without the
  >>TURN emits byte-identical C#.
- Every path builder roots through `ReferenceResolver.RootOf` / `RootText`.
- A GLOBAL formal reached from a contained program bridges its presence member.
- Drift test: `OmittedFormalGuardDriftTests`.
- Goldens: `2002/pb971_program_arg_omitted`, `2002/pb971_function_arg_omitted` and
  `negative/pb971-arg-omitted-below-2002`.
- The implementer's probes reproduced before the fix and showed the raise after it.
- Rows closed: GR-14.9.4.4-12, GR-14.9.23.4-10 and GR-8.4.3.2.4-8.
- Composition with train 49: `CallAbi`'s PB965 variable-length-group arms are kept, spelled with CA's argument-free
  `Omitted<T>()`.

**CB — PB979 + PB980, UNSTRING receivers through the MOVE chain.** §14.9.48.4 GR11 c)/d) move the examined and
delimiter characters "according to the rules for the MOVE statement".
- Each `BoundUnstringReceiver` now carries `Store` / `DelimiterStore` / `ZeroFill` as bound MOVEs over the conceptual
  character item (`SendingValueTemp.ConceptualCharacterItem`). `StringEmitter`'s private `MoveString` conversion is
  deleted. In the merge, train 49's PB388 edit to that function's comments went with it.
- GR11 b)'s size is `ReceivingStore.ExaminationSize`, measured at execution. Ref-mod and ANY LENGTH receivers now
  work, and the COBOLNET1756 stage is gone.
- PB980 made the all-or-nothing class rule one predicate, `AllOrNothingClass`. STRING SR1, UNSTRING SR3 and INSPECT
  SR4 all ask it, and it reports COBOLNET2306. `pb664-string-national-mix.err` moved from 1626 to 2306.
- Determination D-UN3 (CONFORMANCE.md §3): a numeric identifier-4 answers SR3 by its usage.
- Goldens: `85/pb979_unstring_receiver_size` and `2002/pb979_unstring_any_length_national`, plus two negatives. The
  `char_string_ops` snapshot was re-baselined.
- Rows closed: GR-14.9.48.4-11, GR-14.9.48.4-8, SR-14.9.48.3-3 and SR-14.9.22.3-4.

**CD — PB983 + PB969 + PB974, grammar optional words and AS phrases.**
- PB983: IN is optional in the range alphabet phrase of EVALUATE (§14.9.13.2) and of VALUE formats 3 and 5
  (§13.18.63.2), both rendered from the PDF. VALUE decides by symbol in `DataBinder.RangeAlphabetPhraseOf`. The
  VALUE-constant qualification arm was a sibling, and it is peeled too.
- PB969: an empty argument list is legal for a function-identifier. Empty parentheses on a DATA reference are
  COBOLNET2309. The whitespace-only form used to compile clean and abort at run time.
- PB974: the REPOSITORY CLASS / INTERFACE / PROPERTY / FUNCTION specifiers take the AS phrase, and
  `FUNCTION a b INTRINSIC` takes a list. One screen checks them: `BindSpecifierExternalizedName`. A DOC-A.1-163
  determination is recorded in CONFORMANCE.md §7.
- Goldens: 4 positive and 3 negative.
- Rows closed: 8. The PB983 rows are re-witnessed.

**CE — PB973 + PB975 + PB972, OO binder and emitter.**
- PB973: `NamingConvention` tags every synthesized name that embeds a user word. A method formal named `N` becomes
  `__formal_N`, so it no longer shadows the emitter's `__N` constant. That shadowing caused CS1628 and rc 70.
- PB975: `OoClassTable.Build` scopes each report at its own clause, so OO diagnostics now carry positions.
- PB972: §9.3.8.2.3 rule 9 (RAISING conformance) is written once, in `OoConformance.RaisingMismatches`. All three
  askers reach it: IMPLEMENTS (0841), override (0829) and `InterfaceConformsTo`.
- Goldens: 2 positive and 5 negative.
- Rows closed: GR-9.3.8.2.3-9 and SR-11.7.3-9.

**CF — PB976 + PB977, the SPECIAL-NAMES FOR phrase.**
- PB976: there is now one operand decoder, `DataBinder.LiteralPhraseOperand`, and the CLASS clause's private
  `ClassLiteralChars` copy is deleted. The decoder enforces SR17 b1–b5/c1–c4 and SR11 in both clauses. It also fixes
  four sibling defects:
  - `N"…"` passed SR14 b2.
  - CLASS figuratives were decoded as their spelling.
  - An `ALL` literal was taken as source text.
  - A THRU to U+FFFF looped forever.
- PB977: a trailing FOR phrase is an error production, refused at every edition as COBOLNET2315.
- Composition with train 49:
  - **The CF batch was written against the pre-PB829 tree**, so it restated FMT-12.3.7.2 as PARTIAL ("the
    dynamic-length-structure-clause is NOT MODELLED"). Train 49's PB829 had already closed that residue. Applied as
    written, the batch would have reopened a CONFORMS row (GAP +1). The lander applied a composed copy instead
    (`scratchpad/t51/pb976-merged.json`): verdict CONFORMS, main's notes, plus the PB977 sentence.
  - `ConditionBinder`'s user-class arm keeps PB843's symbol-keyed word, now read through CF's `UserClassDef`.
  - `DataBinder.Switches` keeps CF's deletion of `IsAlphanumericLiteral`. `CobolLiteral.ClassOf` now covers the
    concatenation case that main had added to it.

**CG — PB982 + PB978 + PB981, the data-division cluster with the PB981 finisher.**
- PB982: a bare operand that is none of the conditions (`IF WS-X`, a bare class-name, a switch mnemonic, a literal)
  is COBOLNET2318. `ConditionBinder.Refused` is the one `BoundConditionError` site, with an internal COBOLNET2319 net.
- PB978: `QualifiedCandidates` returns `DataNameCandidates` (no indexer), and `UniqueOrReportAmbiguous` gives the one
  verdict.
- PB981 records determination D-FRA (CONFORMANCE.md §3; files-design D27). A dynamic-length, variable-length-group or
  pointer FD/SD record is an OUT-OF-LINE record:
  - READ and RETURN store the current record through `EmitRecordAreaStore` and `CobolVarGroup.FromContiguous`.
  - WRITE, REWRITE and RELEASE send the contiguous image.
  - The implied RECORD clause is Format 2 when a record varies.
- Goldens: 4 positive and 2 negative.
- Row closed: GR-13.18.33.4-3 (re-witnessed).
- Composition: SortBinder's new PB981 key refusal spells the key with `DataBinder.WrittenText`, following PB983's
  sweep.

**The train.** Merge conflicts were resolved per hunk:
- **Manifests:** both sides taken. Every `enabled` list was checked for element count and uniqueness.
- **`DiagnosticCatalog`, `CONFORMANCE.md` and `BoundTree`:** both sides taken, as whole elements. `DIAGNOSTICS.md`
  was regenerated from the catalog, and the result matched.
- **`CallAbi`, `StringEmitter`, `DataBinder.Switches`, `ConditionBinder` and `SortBinder`:** resolved by reading
  both sides, as described above.

After each checkpoint, the conflict-marker checks on the working tree and on the index both printed nothing.

The gate was the WHOLE Conformance assembly (filter `~CobolNet.Tests`, NIST and the corpus inside) plus Unit and
Characterization, and it was GREEN:
- `Passed! - Failed: 0, Passed: 8084 … Conformance`
- `Passed! - Failed: 0, Passed: 28832 … Unit`
- `Passed! - Failed: 0, Passed: 33 … Characterization`

The GnuCOBOL corpus was fetched and did not fail. The legacy integration assembly also passed:
`Passed! - Failed: 0, Passed: 503, Skipped: 1`.

The other checks:
- `work.py check`: 1018 items, all well-formed.
- `audit_code_citations` and `audit_doc_citations`: 0 findings.
- `audit_witness_loss --check`: GREEN (0 unexcused, 3 retired).
- semgrep: `cobolnet-raw-diagnostic-code-literal` went from 383 to 376, and the baseline is locked.

**GAP 2175 → 2161.** No cluster was dropped. Codes claimed: 2306, 2309, 2315, 2318 and 2319.

The implementers' leads were filed by REGISTRAR #12 (DEVLOG 1649), which landed on main while this train was
in CI: the range IN alphabet phrase at 85 (PB1015), LENGTH OF subscripts (PB1016), REPOSITORY SR1 beyond PROGRAM
(PB1017), the OCCURS KEY capture (PB1018) and method DECLARATIVES (PB1010). The first push-main was refused
non-fast-forward because of that landing, after CI run 35813406382 had gone green on `d333aee2a`. The rebase
conflicted only in DEVLOG.md and plan §0, so this entry was renumbered from 1649 to 1650 and the train was
re-pushed without a local re-gate.

