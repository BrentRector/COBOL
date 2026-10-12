## Entry 1431 — 2026-09-02 17:44 PDT — The golden lane: 157 verdicted-but-untested rows get their spec-derived witnesses, 151 close, and the six that do not are six registered defects rather than six green tests

The burn-down's lane 1 was the set of inventory rows already carrying a verdict — CONFORMS with an empty
`test-ref`, or DOCUMENTED-NON-SUPPORT owing a witness — and owing nothing but the golden that proves it. 157 rows
across 16 slugs, each drafted by a writer, attacked by a refuter, repaired by a fixer, and then COMPILED by a
validator before anything was allowed near the tree. This entry is the landing.

**The shape.** 157 rows → **117 new-golden · 32 existing-golden · 6 defect-suspected · 2 not-closable**. 142 `.cob`
drafts were produced; 140 carried a validation verdict and **137 passed**. **GAP 3618 → 3467**, 151 rows closed:
148 of the lane's own, plus three Annex A.1 register rows the new goldens turned out to pin. The corpus is now
**553 positive goldens · 577 negative fixtures**; 24 CONFORMS rows remain test-needed across the whole inventory,
one of them contributed here (`RV-15.50.4-9`, recorded CONFORMS on a code read with no observable to pin).

**What landed:** 137 corpus programs — 65 positives (3 at `--std 85`, 5 at 2002, 5 at 2014, 52 at 2023) and 71
negatives — every one registered in its edition dir's `manifest.json`, every negative carrying its `*> reject-at:`
first line and a measured `.err`. Two existing files were changed rather than added:
`negative/intrinsic-index-argument.cob` (its table moved under a group, because §13.18.38.3 SR1 a) forbids OCCURS
on a level-01 entry and the fixture was illegal COBOL in a way the compiler does not yet catch) and
`2023/pb13_domain_raise_receiver_shape.cob`, whose comments cited §15.8.3 **r1** and §15.84.3 **r1** while quoting
r2's text verbatim — r1 is "Argument-1 shall be of class numeric" in both clauses. That is CLAUDE.md rule 1's
named failure mode exactly, `--check` passes on the TEXT and only the NUMBER is wrong, and a sweep of the corpus
for the same shape found those two occurrences and no others (the four other `15.84.3 r1` hits correctly cite the
class rule).

**The refuter earned its seat: 67 of 158 verdicts overturned**, and the distribution says what kind of work it is.
**27 does-not-exercise-rule** — the golden compiles and passes and tests something else; two of them were goldens
that *would not compile at all*, writing `FUNCTION DATE-TO-YYYYMMDD(851003 10 (FUNCTION NUMVAL(…)))` where ISO
forbids a function-identifier in that position. **16 citation** — including a mis-anchor already shipped into a
`.cob` header. **11 editions** — records claiming `85,2002,2014,2023` for screen rows whose witnesses measure
2002+ only, the 1985 division structure having no SCREEN SECTION. **9 not-spec-derived** — expected values the
normative text does not determine, six of them in one FORMATTED-DATETIME golden. **3 convention** and 1 with no
defect at all. Every overturn was applied by the fixer; none was refused.

**Then the validator compiled all 142 drafts, and it is the reason this entry is not a list of green tests.**
Three did not pass:

- `repro/l1_module_name_as_clause.cob` — **a compiler defect much larger than the row it was written for**, now
  `kb/Work` **PB303**. The ISO `AS literal` phrase has **no grammar surface on any identification-division
  paragraph**. `PROGRAM-ID. ASPRB85 AS "ASPRBX".` is refused at `--std` 2002, 2014 and 2023 with `COBOLNET0901:
  'AS' is a reserved word … and cannot be used as a user-defined word` — because `programIdParagraph` has no `AS`
  token, so `AS` falls through `dataReferenceAttribute → cobolWord` and is parsed as a **user-defined word** — and
  it **compiles at `--std 85`**, the one edition whose PROGRAM-ID paragraph has no AS phrase. Exactly inverted.
  The sweep found it missing from PROGRAM-ID, FUNCTION-ID, CLASS-ID, INTERFACE-ID and REPOSITORY, whose grammar
  comment still says the phrase "is deferred (avoids reserving AS as a keyword)" — stale, since `AS` is already a
  lexed token used by CALL Format 2. `RV-15.65.4-4` stays a GAP, and for a **different reason than the writer
  gave**: the writer framed it as `MODULE-NAME` reporting the AS literal against the item-135 determination, but
  no `r4` form is returned at all because nothing compiles. The repro stays in the scratchpad, out of
  `tests/conformance/`, with its `.out` unedited.
- `negative/l1-substitute-index-argument1.cob` — a **draft error**, and the fixture repeated on §13.18.38.3 SR7 the
  exact defect its own header claimed to have fixed on SR1 a). It passed an index-**name** to `FUNCTION
  SUBSTITUTE`, and SR7 admits an index-name only as a subscript, in PERFORM/SEARCH VARYING, in SET, or as a
  relation operand — so the rejection was `COBOLNET1637` (SR7) and not the `COBOLNET1627` the rule under test
  (§15.87.3 r1) produces. §15.87.3 r1 screens **data items**, and §8.5.2.1 Table 2 is headed "Class and category
  relationships for **elementary data items**", so the rule cannot even reach an index-name. Re-derived here from
  the spec rather than inherited (`cite.py --check 13.18.38.3 "Index-name-1 may be specified only in the following
  contexts"` → OK §13.18.38.3 7)), fixed by passing a `77 L1S-IDX USAGE INDEX` item, and **measured**: exit 70,
  `COBOLNET1627: FUNCTION SUBSTITUTE argument-1 is of class index`. Its `.err` is that discriminating sentence and
  not the bare code, because the bare code is also emitted for argument-2 on the same line.
- `AcceptDifferentialTests.cs` — not a `.cob` at all; the validator could not run `dotnet test`. Run here: its
  three new facts are in the 3504 that passed.

**Six rows did not close, and each is a register note rather than a fixture written around the problem.**

- **PB302** — the Annex A.4.2 **item-10 EC-SCREEN naming surface is accepted in silence**, holding
  `GR-14.9.11.4-18`, its two level-2 legs and `GR-14.9.11.4-19` open. Item 10 makes the EC-SCREEN condition NAMES
  optional in six positions — EXIT/GOBACK RAISING, the procedure division header's RAISING, USE, PERFORM's WHEN,
  RAISE, and `>>TURN` — and **none of them needs a SCREEN SECTION**, so none reaches the `COBOLNET1560` site that
  covers the rest of the decline. `grep -rn EC-SCREEN src/` returns exactly six lines, all in
  `ExceptionCatalog.cs`: one level-2 name and five level-3 names, **zero setting sites, zero diagnostics** — and
  the catalogue is a live reader, so `PERFORM … WHEN EC-SCREEN-FIELD-OVERLAP` binds clean. This is precisely the
  measurement PB260 finding 3 asked for ("where the measurement shows a silent reinterpretation instead, that is a
  defect to register, not a fixture to write around"), and it fails.
- **PB303** — the AS phrase above.
- **PB307** — `AR-15.75.3-4`: §15.75.3 r4 scopes the implementor RANDOM seed to "the first reference … **in the
  run unit**", but `private static Random _random` is process-wide and survives both `RunUnit.ResetCurrent` and
  `RunUnit.Run`, so a second run unit in one process continues the first's seeded sequence. `RunUnit.cs`'s own
  summary calls itself "the single owner of all run-unit-lifetime state … replaces the five independent
  process-global static stores" — `_random` is the sixth it missed, and the field's own doc-comment already states
  the correct model ("ONE current pseudo-random sequence per run unit"), which is why no reader caught it. Not
  reachable from the corpus: `CutRunner` starts one process per program, so one process is always one run unit
  there. The witness is a runtime unit test, and the fix is to move the sequence onto `RunUnit` with a drift test
  asserting no mutable static survives a run-unit boundary — not one more reset call.
- **`GR-14.9.11.4-11`** (io-statements) and **`GR-14.9.5.4-11`** (misc-statements) are **not-closable**, and
  deliberately carry no record so they stay visible as GAPs rather than closing on a manufactured test. GR11's
  antecedent — "if vertical positioning is not applicable on the device" — is false for **every** device this
  implementation offers (`DisplayOutputDevices` admits only CONSOLE/SYSOUT/SYSERR and the emitter's sink is a
  two-valued ternary), so a test could only assert an implication with an unsatisfiable premise. ⚠ That vacuity is
  **contingent, not permanent**: it is voided the moment a printer or reel mnemonic is added, and that work must
  re-adjudicate the row rather than inherit its Claimed. §14.9.5.4 GR11's own consequent is "the result … is
  undefined", listed verbatim by Annex A.2 item 4, so every result conforms and any golden would pin an
  implementation choice while looking like a conformance assertion.

**And one negative ships PENDING on purpose.** `negative/pb260-accept-screen-at-phrase` is the only source shape
carrying §14.9.1.3 SR5's identifier-3/-4. It **is** rejected at 2002, 2014 and 2023 — measured here, exit 65,
`COBOL0307: unexpected 'AT'` — but enabling it would pin a **generic parse error** as the expected diagnostic, and
§4's own preamble records the generic parse error as PB260's debt and a named diagnostic as owed. A negative
asserting the generic error is a green test holding that gap open. `SR-14.9.1.3-5` closes instead on
`DocumentedNonSupportWitnessTests.ScreenFormat3AcceptAtPhrase_IsRefusedNotReinterpreted_PinnedToSpec`, which
asserts the refusal without asserting the text. The exact substring to record when the named diagnostic lands is
in PB260.

**The DNS witnesses needed a shape a corpus golden cannot have.** A `.out` can pin the INERTNESS half of a
declined facility; it cannot pin the DECLINE half, because the named non-support diagnostics are **warnings** —
the positive corpus never reads the warning channel and the negative corpus requires the compile to fail. So
`DocumentedNonSupportWitnessTests` reads the same sources the corpus runs and adds the assertion the corpus cannot
make. Two `.out` files say in their own headers that they are **non-discriminating**: `pb261`'s `N=3` (with no
active APPLY COMMIT clause, §14.9.7.4 GR1 makes both verbs a CONTINUE, so a conforming processor prints it too)
and `dns_validate`'s `SUBJECT UNCHANGED` (§14.9.50.4 GR5 / §13.18.17.4 GR1 make an unchanged subject conforming
too). Those rows close on the warning, never on the bytes. **PB260** is now 10 of 163 witnessed and **PB261** 3 of
25 — the 22 that remain are all the `APPLY COMMIT` clause and the EC-FLOW family, which have no grammar surface at
any edition. Both notes stay open.

**Three Annex A.1 determinations were published because a landed golden pins them**, closing three more inventory
rows the lane did not own. `DOC-A.1-195` — SYNCHRONIZED generates **no** implicit filler and performs no
alignment, measured on BOTH halves GR9 names by `l1_byte_length_implicit_filler`'s `SYNC=3` and `SBIT=1` legs (an
aligning implementation answers 4 and 2). `DOC-A.1-204` — location information is **not** made available when the
LOCATION phrase is absent, uniformly across the explicit `>>TURN` and the implicit TURN directive §14.9.28.4 GR14
supplies for a PERFORM WHEN phrase, which `l1_exception_no_location_arms` measures on both legs side by side; that
determination is what three of the exception-module rows depend on and it lived only in a design doc until now.
`DOC-A.1-90` — the substituted result when an argument rule is violated and checking is off is **the zero value of
the returned type**, documented voluntarily (A.1 exempts it) because three different values were in play across
the corpus. §7's counter moves 36 → 38 of 195, obligations remaining 159 → 157; `audit_annex_a1.py --check` is
green and reports zero findings.

⛔ **Two `docs/CONFORMANCE.md` drafts were NOT applied as delivered, and that is the near-miss of this landing.**
Both the string-p1 and dns-witness lanes shipped whole-file copies built from an older `main`, and copying either
in would have silently **reverted** landed corrections — the §4 preamble rewrite, the A.4.2/A.4.3 row measurements,
`kb/Work` PB285's item-45 correction — while looking like an ordinary file update. Only the intended deltas were
re-applied by hand, re-keyed to the `DOC-A.1-<n>` row form the A.1 mechanism landed at `a54f4502`.

**Seven further findings became notes rather than being folded into unrelated work**, clustered by MECHANISM and
not by row. **PB304** — `RemDec` calls `ModZeroDivisor()`, so a §15.77.3 r2 violation on the SDIDI carrier is
reported as "MOD with a zero divisor (ISO §15.64.3 rule 2)"; `ZeroDivisor_RaisesFromBothCarriers` is named for two
carriers while three bodies exist, and its own per-function clause assertion is exactly what would have caught it.
**PB305** — `FUNCTION FIND-STRING(numeric-edited-item "5")` is legal (Table 2 puts numeric-edited display in class
alphanumeric) and is rejected `COBOLNET1627`, because the cross-argument rule's normalization is a hand-written
one-case fold where a Table-2 class projection belongs; the per-position screen has the rule right in a comment
two hundred lines away, and `AR-15.37.3-2` is recorded CONFORMS/state OK today. **PB306** — §15.18.4 r2's "class
**or usage** national" limb is not implemented; latent only because the data shape is declined upstream, and it
becomes the PB15 silent under-rejection again the day the Phase-4a national residue lands. **PB308** — `cite.py
--check` answers OK when the check cannot be valid, twice: an all-punctuation quotation normalizes to the EMPTY
needle and matches every clause that exists (`--check 7.1 "| + | + | - |"` → OK), and an unlettered trailing
paragraph of a lettered rule inherits the previous sub-item's letter (`§14.7.7 2)` reported as `2) b)`). **PB309**
— the transcription twin: `specs/ISO_COBOL.md` §14.9.8.4 GR1 sub-item b) and its NOTE start at column 1 where a)
is indented three spaces, so b)'s text is reported as `1) a)`. Both make a `--check` that PASSES worthless in a
way the caller cannot see, which is worse than one that fails. **PB310** — 145 user-visible diagnostic codes are
emitted as bare literals and are in **neither** `DiagnosticCatalog` nor `docs/DIAGNOSTICS.md`, and
`DiagnosticRegistryDriftTests` structurally cannot see them: every fact enumerates the catalog, so the doc is "in
sync" precisely BECAUSE the catalog does not know the code exists — the gate would pass unchanged if all 145 were
deleted or renumbered on top of each other. **PB311** — `AR-15.23.3-6` is recorded CONFORMS on
`pb65_date_windowing`, whose only r6-shaped line is the **DAY-TO-YYYYDDD** one; §15.23.3 r6 keys its window on the
**execution year** and §15.25.3 r6 on **argument-3**, so an implementation applying the DAY formula to
DATE-TO-YYYYMMDD passes the golden and violates the rule.

**Three more are analysis and adjudication.** **PB312** — two worked examples in the standard's own informative
Annex D contradict its normative rules and would each seed a wrong golden: D.31.5.2 prints the week date of
1995-02-15 one week early (the canonical PDF prints the same wrong values, so the transcription is faithful and
the published standard is wrong), and D.31.5.7 hands FORMATTED-DATETIME a `"YYMMDD"` format whose own stated
result begins with a four-digit year it cannot produce — which resolves the `RV-15.40.4-1` flag that "the
standard's own worked example is rejected by this implementation" in the compiler's favour. One golden had to be
walked back for taking D.31.5.2's `.8124` as an oracle ten lines after its own header declared the table
erroneous. **PB313** — what happens to an ARGUMENT's excess seconds precision in the FORMATTED-* family is an
undocumented implementor determination (item 202 documents the format's maximum fraction digits, item 87 the
clock's zero-fill; neither answers it); the goldens were rewritten to avoid depending on it. **PB314** — whether
FIND-STRING counts OVERLAPPING occurrences is under-determined: §15.87.4 r3 defines non-overlapping resumption for
SUBSTITUTE explicitly where it means it and §15.37.4 says no such thing, which is an argument from the absence of
a sentence and therefore an owner call. A draft DID pin it, with `OVL=0004` taken straight from
`CobolIntrinsics.Text.cs`; those lines were deleted before landing rather than bend a golden toward the
implementation.

**Gate, in the lander's worktree.** `dotnet build CobolSharp.sln -c Debug` → `Build succeeded. 0 Warning(s) 0
Error(s)`. Conformance (`~Corpus|~Negative|~Intrinsic|~VersionMatrix|~DocumentedNonSupportWitness|
~AcceptDifferential`) → `Failed: 0, Passed: 3504, Total: 3504, Duration: 5 m 13 s`. Unit
(`~SpecTraceabilityInventory|~DefectiveRowCoverage|~Manifest|~AnnexA1Register|~DiagnosticRegistryDrift`) →
`Failed: 0, Passed: 27`. Characterization, full → `Failed: 0, Passed: 33`. `python scripts/spec/work.py check` →
360 items, all well-formed. `python scripts/semgrep/verify.py` sits exactly on the PB175 baseline —
`cobolnet-no-biginteger` 36, `cobolnet-raw-diagnostic-code-literal` 475 — no count increased.

⚙ **A green suite is not evidence that the new goldens ran.** "Passed: 3504" is consistent with a golden that was
registered and never discovered, which is a red by absence. So the corpus leg was re-run under a trx logger and
every one of the 139 `.cob` files this change set added or changed was looked for **by name** in the 1143 results:
**138 found, all Passed**. The single absentee is `negative/pb260-accept-screen-at-phrase`, which is exactly the
entry deliberately listed `pending` — correct by construction. Two invented case names were searched for as a
counter-check and are correctly absent, so the matcher is not vacuous.

