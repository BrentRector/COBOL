## Entry 1715 — 2026-09-26 12:13 PDT — Golden lane 2 lands: 59 CONFORMS/DNS rows witnessed, GAP 1241 → 1182

**What landed.** Golden lane 2 took the 117 inventory rows verdicted CONFORMS but still GAP (test-needed) plus the 3
DOCUMENTED-NON-SUPPORT rows owing a witness — 120 rows in eleven slugs (dns-witness, misc-p1, gl2-misc-p2,
misc-p3 … misc-p10). Writers drafted spec-derived goldens, independent refuters re-derived each one, a fixer applied
the corrections, and a validator compiled and ran every draft on a compiler pinned at dac7c9d67. This landing brings
in only what the validator classified `pass`: **37 positive goldens** (85: 5 · 2002: 11 · 2023: 21, plus their
copybooks, including the four UTF-16/UTF-32 and one UTF-8-BOM copybooks of `l1_encoding_per_file_bom`), **25
negatives**, and **five xUnit witness classes** (`DocA1Item218WitnessTests`, `TransferOfControlFatalDeclarativeTests`,
`NonCobolActivatorReturnTests`, `FunctionLocateResourceWitnessTests` in Conformance; `Flag02DirectiveScopeTests` in
Unit), each confirmed passing BY NAME in a trx (MANDATORY-PRACTICES I7): all 62 registered goldens appear as passed
`CorpusRunnerTests` cases, and every new xUnit method passed. The `record_verdicts` batch is the writers' records
filtered to rows whose covering drafts all landed (60 records): **GAP 1241 → 1182** (+59), exactly the closing count
— the sixtieth record is GR-14.9.35.4-14, which the misc-p8 refuter overturned CONFORMS → **PARTIAL** on kb/Work
PB1168 (a GR16 fixed-length size mismatch returns 00 and replaces the record, breaching GR14's no-update); the row is
now in PB1168's `inventory_rows`.

**Dispositions, per slug (input = landed + held + not-closable, by rule-id; every slug reconciles).** dns-witness
3 = 2 + 1 + 0 · misc-p1 13 = 7 + 0 + 6 · gl2-misc-p2 13 = 5 + 4 + 4 · misc-p3 13 = 12 + 0 + 1 · misc-p4 3 = 1 + 1 + 1 ·
misc-p5 12 = 9 + 0 + 3 · misc-p6 13 = 4 + 0 + 9 · misc-p7 13 = 5 + 0 + 8 · misc-p8 13 = 8 + 0 + 5 · misc-p9 12 = 4 + 0 +
8 · misc-p10 12 = 3 + 0 + 9. Totals: 60 landed (59 closed + 1 PARTIAL), 6 held, 54 not closable by a test. The 54 are
recorded nowhere — they are rules with no observable consequence a golden can pin (a separate owner-facing
question, the PB386 unobservable-rule route), listed with each writer's reason in the lane reports. The
test-needed band is now 60 rows (59 CONFORMS + 1 DNS) — exactly the 54 + 6.

**Holds, and why.** DOC-A.1-99: its three `l1-dns-a1-99-ec-imp-suffix-*` negatives are suspected-compiler-defect on
kb/Work PB1531 (the catalog accepts any EC-IMP suffix) and stay in the scratchpad to land with its fix; the passing
positive sibling `2002/l1_dns_a1_99_ec_imp_level2_valid` landed, the record did not. GR-13.18.27.4-3:
`2002/l1_global_redefines_elementary_subject` crashes the backend (kb/Work PB1523); its three passing siblings stay
held with it, unregistered, as the fixer left them. SR-7.3.3-9 (`>>DEFINE IMP` accepted) joins kb/Work PB1366's
`inventory_rows`; GR-7.3.12.4-4 and GR-7.3.12.4-6 (`>>DISPLAY` transfers nothing) join PB1538's; SR-8.4.3.11.3-5 stays
on PB1547, whose body now records the writer's repro correction — only `INITIALIZE ADDRESS OF W` isolates SR5, since
`MOVE P TO ADDRESS OF W` is barred by §14.9.25.3 SR1 and `CALL … RETURNING ADDRESS OF x` by §14.9.4.3 SR7 (all four
citations re-run through `cite.py --check`). The held negative `gl2m2-address-of-initialize-receiving` stays in the
scratchpad.

**New note.** kb/Work **PB1592** (analysis): DOC-A.1-29's determination (compile-time arithmetic in `System.Decimal`,
ties to even) is documented and applied "2002 onward", but Annex E.2 6) says the mode was PRESCRIBED before 2023 and
became implementor-defined only in 2023 (`cite.py --check E.2` OK). The 2023 golden and negative landed; the row was
recorded CONFORMS with its editions UNCHANGED (the writer's record had narrowed them to 2023 — the lander restored
them, since the scope is PB1592's to decide).

**Refused (PB1591 population rule).** Every report's rule-ids were checked against its slug's input. misc-p8 carried
a record for GR-14.9.35.4-7, which is not in its input, and the golden `2023/l1m8_rewrite_from_space` that witnesses
only that row; misc-p7 carried `2023/l1m7_module_name_returned_type`, whose only rule is RV-15.65.4-1, also foreign.
All three were refused and stay in the scratchpad. Nothing from the quarantined slug `misc-p2` (kb/Work PB1591: its
writer drafted golden lane 1's rows) landed; its real rows are `gl2-misc-p2`. PB1591 item 2 (a committed subset
assertion) is not done here — the check ran in the lander's plan script only.

**Repairs made here.** The two misc-p3 draft errors named files `PF` and `RF`, which are reserved words (§8.9, and
§8.3.2.1 1) "Reserved words shall not be used as user-defined words or system-names" — both re-run through `cite.py
--check`); they were renamed `L1FDO-P`/`L1FDO-R` and `L1FDR-F` in every program of each draft, and both goldens then
matched their `.out` unchanged on this build. The first gate run's CITATIONS audit flagged three golden COMMENTS
whose wording the phantom-citation linter read as nonexistent rules (`with 7.3.8" and GR9`, `without GR5`, `so SR9
admits`) — each now names its clause (§7.3.13.4 GR9, §14.9.21.4 GR5, §14.9.39.3 SR9); comment-only. The first Unit
run was red on `AnnexA1RegisterDriftTests.EveryDocRow_IsFiledUnderTheItemAnnexA1Names`: the lander's own `Pinned by`
cell for DOC-A.1-29 carried a parenthetical that broke the `;`-separated witness list. The parenthetical was removed
(PB1592 carries the scope) and the class re-ran green.

**Refuters.** 38 overturns across the ten slugs other than gl2-misc-p2, plus 7 in gl2-misc-p2 (45; the brief had
counted 44) — does-not-exercise-rule 29, editions 7, not-spec-derived 5, citation 3, expected-value 1 — every one
applied by the fixers before validation. `docs/CONFORMANCE.md` Annex A.1 `Pinned by` cells now name the witnesses of
DOC-A.1-14, -16, -26, -29, -65, -69, -89, -101, -106, -196 and -218.

**Gate.** `pwsh scripts/build-local.ps1 -Filter "~CobolNet.Tests.Conformance"` (the whole Conformance assembly —
the first attempt with `~Cobol.Net.Tests.Conformance` was refused by the filter-population check as a DEAD term,
since the namespace is `CobolNet.*`): Conformance **Passed 8922/8922**, Characterization **33/33**, Unit 29333/29334
(the A.1 cell above), then the full Unit assembly re-run green after the fix (**29334/29334**); the by-name run (CorpusRunner + the four
Conformance witness classes) **3247/3247**. semgrep verify PASS (no count moved). Review of the train's diff (the
xUnit witnesses read in full, citations spot-checked): no correctness finding.

