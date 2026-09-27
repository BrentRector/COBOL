# WiseOwl COBOL — Edition Validation (reserved words + edition diagnostics)

> **Status: LIVE. The ONE `VersionConformancePass`
> (`src/Cobol.Net.Compiler/Validation/VersionConformancePass.cs`) is the SOLE edition gate — a TWO-ARM pass
> (parse-tree arm + bound-tree arm) over the bound run unit that runs after bind and before emit, so the binder
> is edition-AGNOSTIC (save the documented exception ledger, DESIGN-version-conformance-pipeline §1.1). It owns the §8.9 reserved-word funnel and routes every version-gated construct through the
> ONE `ConstructRegistry.Check`; the reserved-word tables + both drift disciplines are its inputs. The plan it
> implements is `docs/VERSION_TEST_MATRIX_DESIGN.md` "Phase-2 implementation plan" P2.1–P2.7 + the roadmap
> `docs/COMPLETION_ROADMAP_COUNCIL.md` Phase-1/2 amendments; the canonical edition-gating mechanism doc is
> `docs/rearchitecture/DESIGN-version-conformance-pipeline.md`.** This is the canonical deep-dive for the
> validation subsystem — the pass in `src/Cobol.Net.Compiler/Validation/` plus the edition tables/registry in
> `src/Cobol.Net.Editions/`. Remaining Wave 2–3 work is listed at the end.

## 1. What it is

The syntax-side half of the four-compilers-in-one obligation (`--std 85|2002|2014|2023`, default 2023): every
construct carries (1) its full ISO behavior in every edition that HAS it and (2) the correct DIAGNOSTIC in
every edition that LACKS it — not-yet-introduced (COBOLNET0900), reserved spelling (0901), removed (0902),
obsolete/archaic (0903). Severity is two-axis: **strict** (default; removals reject) vs **`--permissive`**
(the documented migration mode, owner decision 4: removals compile with warnings and the pre-removal
semantics).

## 2. Architecture

- **Channels** (`Binding/EditionContext.cs`): `Diagnostics` is ERRORS-ONLY (any entry fails the
  compile); `Warnings` never fails; **`Removed(code,msg)` is THE severity seam** — error strict / warning
  permissive; one policy, every emit site. Carriers: `CompilerDriver.Options.Permissive`,
  `Result.Warnings` (every outcome), CLI `--permissive`, warnings always printed to stderr.
- **The pass** (`Validation/VersionConformancePass.cs`): THE single post-bind edition-conformance pass — a
  TWO-ARM pass over the bound run unit, run as the manifest's NAMED TERMINAL pass (`BindPipeline.GroupTail`;
  Requires `StorageComputed`, Produces `EditionConformanceChecked`) INSIDE `BinderDriver.Bind`, so the Bind
  result already carries every edition diagnostic and the driver HALTs before emit if errors are present (no
  codegen on an errored tree). The **parse-tree arm** (`ParseArm`, a visitor over the generated
  `CobolParserCoreBaseVisitor<object?>` — no listener is generated) fires every SYNTACTIC introduction/removal/
  phrase gate + the `VisitCobolWord` §8.9 reserved-word funnel on the construct's RECOGNITION; the **bound-tree
  arm** fires only the genuinely-SEMANTIC gates whose identity is a RESOLVED bound fact (MOVE figurative-category,
  plus the gates conditioned on file-organization / access-mode / USAGE / pointer-category). Recognition-based
  syntactic gating is required so an introduction gate names its edition even when the below-edition construct
  ALSO fails to bind: the bound node it would have produced is dropped (`BoundUnsupported`/`BoundNop`), but its
  PARSE node is always present. The binder is edition-AGNOSTIC (save the documented exception ledger, DESIGN-version-conformance-pipeline §1.1) (zero `Check` calls of its own; the one principled
  exception is the UDF-invocation gate, which stays bind-time because an intrinsic FUNCTION and a user-function
  call are syntactically identical). ALL severity routes through `Removed()`/the registry
  (see `docs/rearchitecture/DESIGN-version-conformance-pipeline.md`).
- **Positions** (kb/Work PB82 — `Cobol.Net.Editions/DiagnosticCursor.cs`, `Binding/DiagnosticCursorAt.cs`,
  `Frontend/Common/SourceLineMap.cs`): every diagnostic names the file, line and column the USER edits, in the
  `file(line,col): ` shape the parse layer prints, through TWO structural mechanisms — no report site passes a
  location. (1) **The diagnostic CURSOR**: `IDiagnosticSink.Cursor` (a default no-op member) is positioned by the
  walkers with `using var _ = sink.At(ctx)` — `StatementBinder.BindStatement` per statement (nested statements
  restore), `DataBinder.BindEntries` per entry (+ the FD/SD/SELECT/I-O-CONTROL/SPECIAL-NAMES/OBJECT-COMPUTER/RD/
  USING loops), `ProcedureTableBuilder` per declarative section / procedure unit, the post-build model passes at
  `DataItem.DeclaredAt` (captured from the cursor when the item is bound), and the two parse-tree passes through
  `Validation/CursorFollowingVisitor` (the cursor follows the walk, restored on exit). `EditionContext` implements
  the cursor for real and `Error`/`Warning` stamp the prefix AUTOMATICALLY; no cursor ⇒ the bare
  `error CODE: message` (a diagnostic about the unit as a whole — never a fabricated line). (2) **The ORIGIN LINE
  MAP**: the cursor is in RESULTANT-text space (the ANTLR token line after COPY / REPLACE / continuation joins);
  `EditionContext.OriginOf` maps it to the source file and physical line through `Frontend.LineMap`, the table the
  preprocessing chain builds (`MappedText`/`OriginWriter` through the normalizer, the CC+COPY driver, REPLACE) —
  the same map the parser listener, the directive stages' diagnostics, `EXCEPTION-LOCATION`'s line identifier and
  the bound tree's DEBUG-LINE `SourceLine` fields (`BinderContext.SourceLine`) consume. The `>>TURN` / `>>FLAG` /
  `>>REF-MOD-ZERO-LENGTH` anchors stay in resultant space by design (event lines compared with token lines).
  A new walker positions the cursor once at its loop; a new line-count-changing preprocessing stage is written
  MAPPED (its string overload = the mapped one's `.Text`). Guards: `DiagnosticPositionTests`,
  `SourceLineMapTests`, `2023/pb82_exception_location_source_line`.
- **The band** (`Cobol.Net.Editions/EditionCodes.cs`): 0900 introduction / 0901 reserved word / 0902 removed /
  0903 obsolete-archaic. Pinned pre-band codes kept: 0801/0802 (digit capacity), 0873 (DATA RECORDS),
  0810/0811 (ALTER / bare GO TO), 0882 (CALL ON OVERFLOW) — their sites route through `Removed()` unrenumbered.
- **Reserved words**: `scripts/gen-reserved-words.ps1` derives the
  per-edition table from FOUR sources — the in-repo ISO 2023 §8.9 list (authoritative for 2023), VCR row 32
  (the Annex E.2 item-25 additions), and GnuCOBOL's per-standard 85/2002/2014 word lists (curl disk-to-disk
  into the gitignored `.cache/`; GPL files stay out — only derived FACTS with provenance are committed).
  Outputs: `Cobol.Net.Editions/ReservedWords.Table.cs` + `tests/version-matrix/reserved-words.json`, drift-tested
  both directions. Conservative policy: only `confidence: high` rows reject. ISO Annex E overrides source
  disagreements; continuity interpolation covers single-source gaps; CCVS-conforming usage PROVES a word
  un-reserved (the ORDER override). Consumers go through **`ReservedWordSet`** (the per-unit D9 seam — the
  2023 COBOL-WORDS directive mutates the effective set per compilation group, roadmap Phase 7).
  **The funnel** (`VisitCobolWord`) checks IDENTIFIER occurrences (the whole newly-reserved payload) + the six
  EC-band tokens; the screen/report allowlist band is EXCLUDED pending position-aware checking — the
  permissive grammar can bind those keywords into optional entry-NAME slots (the RW104A COLUMN case).
  ⛔ **Content-filter rule (tripped 4×):** no word list ever transits a conversation stream in any form —
  scripts print counts only; regeneration is disk-to-disk.
- **The registry** (`Cobol.Net.Editions/ConstructDialectStatus.cs`): the in-code rendering of the canonical
  `tests/version-matrix/constructs.json`; `ConstructRegistry.Check(edition, sink, id, where)` is THE gating entry
  point (introduction → error both axes; removal → `Removed()`; obsolete → 0903 warning; dual-obligation
  WINDOW rows use 0900 for the introduction edge and their code for the removal edge). The drift test makes a
  gate unable to land without its matrix row and vice versa. Every version-gated `ConstructRegistry.Check` call
  site lives in the `VersionConformancePass`, and the registry + drift discipline are the pass's data source.
  `status: "pending"` rows are catalogued/frozen but compile-asserted only when their owning roadmap phase lands
  (ONE pending mechanism, shared with the corpus manifests).
- **The DECLINED-OPTIONAL-ELEMENT pass** (`Validation/DeclinedFacilityPass.cs` + the grammar fragment
  `Frontend/Grammar/Core/CobolDeclined.g4`): THE one place an Annex **A.4** optional element whose support
  `docs/CONFORMANCE.md` §5 records as *Not claimed* is **refused by name** — COBOLNET1708 (the VALIDATE
  facility's §13.16.2 *validation-clauses* group and the §13.18.63 format-5 content-validation entry),
  COBOLNET1709 (the I-O-CONTROL `APPLY COMMIT` clause, §12.4.6.3), COBOLNET1710 (a declined module's
  exception-names, emitted from the `EcNameResolution` funnel rather than from the pass). A **third sibling** to
  `VersionConformancePass` and `FlagConformancePass`, run right after them from `BinderDriver`, over the shared
  `CursorFollowingVisitor`.
  - **Why a separate pass, not an arm of the version pass.** Three orthogonal axes, three passes: edition gating
    answers *does this construct exist in the edition you targeted*; flag gating answers *did your directive
    state ask to be told*; this answers *does this implementation claim support for this optional module*. An
    A.4 decline fires at every edition the element exists in and takes its severity from `Removed()`, not from
    `ConstructAvailability`. Folding it in would give `VersionConformancePass` two answers to two questions —
    the shape `DESIGN-version-conformance-pipeline.md` exists to prevent.
  - **Why a parse-tree walk, not a binder hook.** A declined clause has NO bound node — that is what declining
    it means — and the binder's own entry paths drop it exactly where it matters: `DataBinder.BindEntry` returns
    early for levels 66/88 and `BindCondition` returns early for an UNNAMED level-88, which IS the §13.16.2
    format-4 validation entry.
  - **⛔ ERROR, not Warning — the distinction from the ACCEPT-INERT band.** COBOLNET1578/1579/1778 cover
    PROCESSOR-DEPENDENT elements (Annex A.3), where §4.2.6 ¶3 requires only a compile-time warning and the
    construct may be accepted-inert. A.4.1 is stronger: syntax is accepted "only when support … is claimed", so
    accepting a declined optional element's syntax IS the non-conformance. All three route through the ONE
    `Removed()` seam (Error strict / Warning `--permissive`) and share the `declined-optional-element`
    `--suppress` family. It is also what makes the rows WITNESSABLE — the negative corpus asserts a failing
    compile, and the 1560-band warnings have no assertion mechanism at all.
  - **⛔ NO `constructs.json` ROW, deliberately.** The `new-construct` skill's matrix row asserts a construct
    "compiles clean at the introducing edition and produces the gating diagnostic below it" — both halves are
    FALSE for a declined element, which is refused at *every* edition it exists in and has no gating edge to
    assert. `ConstructRegistry` is the edition-gating registry; putting a decline in it would be the same
    two-answers-to-two-questions mistake as folding the pass into `VersionConformancePass`. The declined band's
    per-edition obligation is discharged instead by the negative corpus's `*> reject-at:` header plus the
    below-edition POSITIVE controls (`conformance:85/declined_validate_words_are_user_words` — the clause words
    are legal user-defined words at COBOL-85 — and `DeclinedFacilityTests.CommitAt2014_IsAUserWord…`).
  - **Adding one.** Put the rule in `CobolDeclined.g4` behind an `{isXXXX()}?` LEFT-EDGE predicate (the clause
    words are user-defined words below the edition that introduced them) and, if it is an ENTRY POINT there — a
    rule no other rule in that file references — add a `VisitXxx` override. An alternative added *under*
    `validationClause` needs no code: the message names the clause from its own leading keywords, minus the
    §5.2.3 optional connectives. `DeclinedFacilityDriftTests` derives that obligation FROM the grammar file, so
    a new entry-point rule with no override fails rather than parsing into silence.
  - **The declined-EC-name table** (`EcNameResolution.DeclinedEcFamilies`): prefix → facility / annex /
    documentation, matched on a `-` boundary so a family's level-2 name, its level-3 names and the open
    `EC-IMP` suffixes resolve to one row. It is keyed to the three names Annex A.4.3 item 3 LISTS, never to the
    live `EC-FLOW` level-2 family — `EC-FLOW-RELEASE` and its siblings belong to implemented facilities, and
    `conformance:2023/declined_ec_flow_sibling_still_legal` is that complement's witness. EC-SCREEN-\* and
    EC-MCS-\* have the same zero-setting-site shape and are ONE ROW EACH when their modules' witnesses land.
- **Corpus runners** (`CorpusRunnerTests`, Phase-1 shells): per-edition `tests/conformance/<ed>/manifest.json`
  discovery (enabled compile-asserted strict; pending catalogued; integrity facts forbid silent
  non-discovery) + the `tests/conformance/negative/` must-reject corpus (`.cob` + `.err` + a `*> reject-at:`
  edition header). Seeding: Phase-2 W2.

## 3. Wave-1 coverage (live gates)

85→2002 removals (0902): LABEL RECORDS · VALUE OF · DATA RECORDS (FD+SD, pinned 0873) · MULTIPLE FILE [TAPE]
· MEMORY SIZE · SEGMENT-LIMIT · WITH DEBUGGING MODE (modelled clauses gated at
their nodes — kb/Work PB830) · the five identification comment paragraphs · REMARKS (≥2002 only — CCVS carve-out)
· STOP literal (85 semantics implemented: `BoundStopLiteral` → operator channel/stderr + continue) · OPEN
REVERSED · **the notInGrammar 85-acceptance gates (VCR Table 7 rows 7.15–7.18): RERUN (parsed-and-ignored)
· ENTER (BoundNop; system-name operands outside the funnel) · USE FOR DEBUGGING (the '85 debug module, VCR
Table 7 row 7.17 — the '85 dual posture: comment-treated without WITH DEBUGGING MODE [DB103M], and, WITH the
switch, the ON procedure-name / ALL PROCEDURES leg is MODELED — the DEBUG-ITEM special register + procedure-entry
triggers with the START PROGRAM / SPACES / PERFORM LOOP / FALL THROUGH DEBUG-CONTENTS taxonomy, object-time
switch `RunUnit.DebugMode` default ON; a DEBUG-* register reference under the switch resolves to the register,
never the false 0901; the data-name / file-name / cd-name subject kinds + the SORT/MERGE cause are staged loud
COBOLNET1571) · section-header
segment-numbers (both header rules)**. 2014→2023: CLOSE WITH LOCK · CALL ON OVERFLOW (0882). Windows:
EXIT METHOD/FUNCTION (2002→2023). Archaic 0903 @≥2023: EXIT PROGRAM · NEXT SENTENCE. Reserved-word intervals:
COMMIT@2023, RAISING@2002, RECEIVE + END-RECEIVE (85-reserved → free 2002/2014 → re-reserved 2023). Pass
`Removed()` sites (pinned pre-band codes): ALTER 0810 · bare GO TO 0811 · CALL ON OVERFLOW 0882.

## 4. The measurable G7 exit criteria (roadmap Phase-1 docs item; Phase 8 audits them as counts/exit codes)

1. **INV-1 permissive:** `scripts/version-continuity-sweep.sh` reports 0 BREAKS;
   every ≥2002 STRICT failure of an 85-green traces to a recognized edition-band code —
   0801/0802/0810/0811/0873/0875–0879/0882/0893/09xx.
2. **INV-1-STRONG at the default edition:** `COBOLNET_NIST_STD=2023 COBOLNET_NIST_PERMISSIVE=1` over the
   golden run = **349/349 byte-exact**.
3. **INV-2 both ways:** every constructs.json row's f(case,V) matrix green (strict cells + the removed-
   permissive theory + the obsolete-warning theory); reject cells carry their `expectDiagnostic`.
4. **Census:** ≥357 GREEN on the full NIST census at 85.
5. **Drift:** both drift disciplines green (registry↔constructs.json; word table↔reserved-words.json).
6. **Negative corpus:** every registry gate ≥1 enabled negative case (Phase-2 W2 seeds; Phase-8 completes
   with the registry-coverage unit test).
7. **§4.2.2:** the selectable conformance-checking suboption ships with OO interfaces (roadmap Phase 3) /
   prototypes (Phase 4c) and joins these criteria then.

## 5. Performance

The conformance pass is one visitor pass over the already-built parse tree plus a light bound-tree walk:
full-pipeline CLI compiles of a large NIST program (SQ207M) measure ~1.3–1.4 s wall
(parse+validate+bind+emit+Roslyn, Debug), and the full-corpus guard wall time is ~3.3 min — the pass is
noise (feedback_gate_on_the_verdict_line satisfied).

## 6. Wave 2–3 remainders (P2.8; roadmap Phase 2)

W2 (parallel agents, disjoint files): the loud-guard silent-misbind sweep (PicInfo silent DISPLAY
fallback, PIC N/E/1, the CallCollectUnits class-unit skip, UsageKeyword strip) + the national/boolean skeleton
· negative-corpus seeding (≥1 case per Wave-1 gate + the reserved-word interval witnesses) · position-aware
checking for the screen/report allowlist band (the RW104A exclusion) · VCR status flips · adversarial review.
W1.5: upgrade the ~24 grammar introduction-gate parse errors to edition-naming 0900 diagnostics (serial or
fragment-merge). W3 (single serialized grammar batch + FULL legacy guard): XOR/EXCLUSIVE-OR regating to 2023
per Annex E · preprocessor DialectLevel threading (VCR 2/4/94) · the
cobolWord EC-band comment fix · the 2002-corpus edition audit.
