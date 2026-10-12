## Entry 1440 — 2026-09-02 22:39 PDT — Fix-lane cluster D: the USAGE clause's float-format phrases, the
Table-3 adjacency screen measured cell by cell, restricted data-pointers — and three BAND codes that had no
catalogue row at all, which took the semgrep raw-literal count DOWN for the first time

Three mechanisms, each REPRODUCED on today's tree before a line was written.

**PB174 — the USAGE clause's per-item endianness and encoding phrases (ISO §13.18.60.2, COBOL-2014).**
Repro: `01 F USAGE FLOAT-BINARY-32 HIGH-ORDER-LEFT.` → `COBOL0307: unexpected 'HIGH-ORDER-LEFT'`. The
printed general format was RENDERED (PDF p.533 = printed folio 503) rather than read off the OCR, and matches
the transcription cell for cell. The phrase tail goes on the CLAUSE — `usageClause : (USAGE IS?)?
usageKeyword binarySign? noSignPhrase? floatFormatPhrase*` — never inside `usageKeyword`, whose `GetText`
would glue the phrase onto the keyword and hand `ParseUsage` a string it cannot parse. Three screens
(**COBOLNET1716** endianness off the standard float usages · **1717** encoding off FLOAT-DECIMAL-16/-34 ·
**1718** a repeated phrase, §5.2.6.4), and the item's own phrase BEATS the OPTIONS-implied one — not by
preference but by §11.9.8.3 SR2/SR3's own wording, which implies the clause only "in which an
endianness-phrase is not specified". Two goldens pin both precedence directions.
`UsageFloatFormatPhraseDriftTests` reads the `usageKeyword` inventory out of the `.g4` and was **proven to
fail once** before being trusted — and it then earned its seat immediately: PB153 turned `POINTER` into its
own rule, and the drift test went red until the classification table decided that rule's tails.

**PB158 — §8.8.1.2 Table 3 / §8.8.2 Table 4 operator adjacency (COBOLNET1719).** Producibility was MEASURED,
one probe per cell, and the measurement **refuted two of the contract's premises**: there is no `--permissive`
arm to build (`EditionSeverityPolicy` softens only `Removed`), and `- -2` is PERMISSIBLE under §8.3.3.3.2
rule 2 while `- - 2` is not — the same token stream in default lexer mode, so the screen reads token
POSITIONS and `unaryExpression`'s self-recursion stays. Only (unary, unary) and (B-NOT, B-NOT) are live
cells. `ArithmeticFormationRules` + a new `ExpressionFormationPass`, called at the top of
`EvaluateArithmeticOperand` BEFORE `SoleNumericLiteral` (the third evaluating arm — the one a two-arm fix
would have missed). Newly-rejected corpus population measured at ZERO.

**PB153 — restricted data-pointers (Annex D.9.2.2), both waves.** Repros: `USAGE POINTER TO T` was a parse
error; `ALLOCATE <strong group> RETURNING <unrestricted ptr>` bound CLEAN; `01 T TYPEDEF STRONG PIC 9(3).`
bound clean. The diagnosis was a grep: `StrongTypeModel` had NINE call sites in the compiler and ZERO in
`PtrBinder.cs` — the file that owns every pointer statement. It is in that list now, with both ALLOCATE forms
and BOTH SET arms screened (§14.9.3.3 SR4/SR5, §14.9.39.3 SR19/SR20, §14.8.2.3.2 on a nested CALL).

**Four defects the implementer found in its OWN diff and fixed**, which is the self-review pass working: the
§14.9.3.3 SR4 arm conflated "a typed data item" with "a strongly-typed group item" and REJECTED A LEGAL WEAK
TYPEDEF (a golden leg `WEAK=0077` now proves it does not); `IntrinsicBinder` carried a second spelling of
§3.166/§3.167 beside the new `UsageFamilies` predicates; a stale "singleton" doc comment on
`PicInfo.PointerItem`, which is a factory now; and the §8.5.3.1 check had to key on `IsElementary` rather
than `!IsGroup`, because `RegisterTypeDecl` runs while the forest is still being built and every group
typedef is `IsGroup == false` at that moment.

⛔ **THE CONSTRUCTS ROW STAYS `pending`, AND THAT IS A DECISION NOT TO GUESS.** `usage-pointer-to-type-2014`
is live in grammar and semantics, but its `introducedIn` was self-labelled "provisional" and the row had
never been exercised. Annex E (2014→2023) carries no pointer entry, which RULES OUT 2023; every dependency
(STRONG TYPEDEF §8.5.3, USAGE POINTER, BASED, ALLOCATE) is COBOL-2002 in this registry — so the answer is
2002 or 2014 and **the 2023 document cannot decide between them**. Activating the row installs an edition
GATE, so a guessed number would stop being a guess and become the compiler's enforced answer. No conformance
hole is held open by the wait: a restricted-pointer program already draws COBOLNET0900 below 2002 through the
existing `usage-pointer-2002` + TYPEDEF gates (measured). The question is recorded as a dated section of
`kb/Work/A1.md`, the standing per-edition AUTHORITY-SUFFICIENCY analysis whose subject this exactly is, with
what would settle it. ⚠ The row's WITNESS was also repaired: the previous source was **doubly nonconforming**
and had never been compiled, so nothing had ever contradicted it — `01 T TYPEDEF STRONG PIC 9(3).` violates
§8.5.3.1 and `01 P USAGE POINTER TO T.` violates §13.18.60.3 SR18. Activating that row would have pinned
illegal COBOL as the construct's definition.

**AND THE LANDING FOUND ONE MORE, IN THE SEMGREP GATE.** `cobolnet-raw-diagnostic-code-literal` went 474 →
**477**: cluster D's three new restricted-pointer screens reused the established `COBOLNET0869` / `0881` /
`1529` bands as BARE STRING LITERALS. Chasing the three found that those codes had **no catalogue descriptor
at all** — 38 bare literals across five binders (`DataBinder` 16 · `SetBinder` 9 · `PtrBinder` 6 ·
`PictureAnalyzer` 5 · `ConditionBinder` 2) and nothing else, so no `docs/DIAGNOSTICS.md` row, no drift test,
and nothing for `session-probe`'s next-free scan to see. They had gone uncatalogued because they are BANDS —
one code over a family of neighbouring rules — which does not fit a naive one-code-one-rule reading of the
descriptor. It fits the catalogue as it actually stands: `COBOLNET1720` already spans three syntax rules and
`1707` four statement formats. So each got a band descriptor whose Title states the band while the site
composes the rule it caught — `pointer-operand-shape`, `usage-clause-compatibility`, `type-declaration-shape`
— and **all 38** sites were converted, not just D's three. No emitted byte changes:
`Error(DiagnosticDescriptor, string)` forwards to `Error(descriptor.Code, string)`. The count is **439**, the
first DECREASE `kb/Work/PB175` has recorded and 36 below its 475 baseline, which the note now carries.

**Rows and gates.** 4 rows → CONFORMS (`GR-8.8.1.2-2`, `GR-8.8.1.2-4` from PARTIAL; `SR-14.9.3.3-4`,
`SR-14.9.3.3-5` from NOT-IMPLEMENTED), GAP **3227 → 3223**. `PB174`, `PB158`, `PB153` land. Build 0 W / 0 E;
Conformance (wave filter, widened for the band conversion) Passed **3751/3751** (5 m 46 s); Unit Passed
**304/304**; Characterization Passed 33/33; `work.py check` 428 items; `audit_code_citations --check` 0
findings; `audit_doc_citations` 6 MISFILED (baseline); `audit_annex_a1 --check` no findings. Corpus 569
positive · 656 negative. Merge cost: five 3-way conflicts, all union-resolved except `docs/CONFORMANCE.md`'s
A.1 item 48, whose table had gained the `DOC-A.1-<n>` key and a `Pinned by` column since cluster D's base —
D's determination cell was RE-SEATED into the four-column row rather than merged, and its two new goldens
fill the evidence cell.

