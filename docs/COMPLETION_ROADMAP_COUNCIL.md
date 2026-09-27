> **Status: RATIFIED (2026-07-03, owner). This is the ratified G7→G8 completion plan.** The edition-conformance
> and M2/M3/M4 feature work it scopes is now carried under `docs/COBOLNET_REARCHITECTURE_PLAN.md` (its phases
> 09–14 subsume this G7→G8 drive), and this report's ISO adjudications, feature catalog, and owner decisions
> remain the reference for that work. ⚠ FROZEN HISTORICAL RECORD (2026-07-19): superseded by the consolidated plan `docs/COBOLNET_REARCHITECTURE_PLAN.md` (its §0 banner
> points to the rearchitecture plan, not here. The owner resolved the §5
> decision packet: **#1 = NO further standards acquisition** — the ISO docs in hand are the authority
> (`specs/ISO+IEC+1989-2023_ for X_952804 COBOL.pdf` + the extracted `specs/ISO_COBOL.md`); prior-edition
> edges derive from the 2023 spec's Annex E + the legacy inventory as interim authority, with
> provisional-confidence markers retained wherever the 2023 text cannot adjudicate (the "blocked on standards
> acquisition" track in Phase 4 is thereby re-scoped to that permanent policy). **#2–#11 = the council's
> recommended defaults ADOPTED as written** (incl. #6 — the OO/M2 grammar grant, same log + full-guard +
> committed-regen discipline; memory `never-change-grammar-without-user-approval` updated).
>
> Provenance: produced by an 8-agent council workflow (5 advisors — edition-gating,
> feature-catalog, architecture-readiness, verification, sequencing — → 2 adversarial critics [26 challenges,
> 14 gaps] → chair synthesis with in-repo re-verification of contested claims).
>
> **ISO-VALIDATED.** Systematically validated against `specs/ISO_COBOL.md` by a
> 12-agent workflow (4 claim-group verifiers + a double-verified edition-adjudication group
> + residual-claim extraction + 3 reverse-direction completeness sweeps + chair): **39 claims — 30 CONFIRMED,
> 9 PARTIAL, 0 REFUTED, 0 UNVERIFIABLE; 0 fatal / 10 serious coverage gaps.** All corrections (D1–D16 +
> minors) are APPLIED INLINE below, marked "(ISO-validation Dn)"; the full audit report is the Appendix. The
> ratified spine, sequencing, and every owner decision survived validation unchanged.

# WiseOwl COBOL Completion Roadmap — Council Report

## 1. Where the project stands

The greenfield compiler (src/Cobol.Net.*, ~26k handwritten LOC) is a complete, byte-exact COBOL-85 compiler: Phase 1 (G0–G6) closed with all 318 golden-bearing NIST programs locked, 357/403 census GREEN with zero diffs, 1074 conformance + 29 unit tests green, and the full EC exception model (ISO §14.6.13) landed. The legacy guard stands at 353 MATCH + 11 LEGACY_DIVERGENT (ISO re-baselines cited in scripts/guard.sh), 0 regressions. G7 Phase-2 EditionValidator is design-complete (P2.1–P2.8, docs/VERSION_TEST_MATRIX_DESIGN.md after §8) but **zero-implemented**: no Validation/ directory, no permissive axis, no negative corpus, and 131 of 134 VERSION_CHANGE_REFERENCE.md rows still TODO. Every off-repo scout artifact (/e/tmp/g7, /e/tmp/phase2-briefs, /e/tmp/nc-sweep) is verified gone — the in-repo docs are the sole surviving plan. Essentially the entire M2/M3/M4 positive catalog remains to implement greenfield (the ISO2023_CONFORMANCE_PLAN §3 checkmarks record the retiring legacy engine, per that doc's own banner). Chair-verified honest budget to G8: **~22–32 sessions** (the sequencing lens's 16–24 omitted ~5–10 sessions of council-added work and under-rated OO; corrections below).

## 2. The recommended path to done

The council unanimously upholds the canonical spine — validator first, then M2 (OO largest) → M3 → M4 → closure → G8 — with the reorders below. Chair adjudications of advisor conflicts and critic challenges are inline, each grounded in repo evidence I re-verified today.

### Phase 0 — Session-0 re-entry + owner decision packet — **S** (serial)
**Lands:** Fresh full clean build (never incremental after the gap — the MSBuild-masking lesson), guard-fast + full conformance (1074) + unit (29) suites, spec-submodule check, end-to-end smoke; DEVLOG entry recording the verified /e/tmp artifact loss. Post the complete owner decision packet (Section 5) in one message — the critics are right that scope decisions currently parked "before M3" change **M2 and M4 sizing** and the standards-acquisition request is the plan's only external-lead-time dependency, so both move to day one. Proceed autonomously on the council's recommended defaults where no reply arrives (feedback_autonomous).
**Why now:** cheapest insurance against 22 days of environment drift; unblocks everything downstream.
**Exit:** the recorded baseline state reproduces green; decision packet posted.

### Phase 1 — EditionValidator Wave 1 (P2.1–P2.7) + metadata adjudications + runner shells — **M**
**Lands (main-session serial):**
- P2.1 EditionContext warning channel + Permissive axis + Removed() seam (verified errors-only today, EditionContext.cs:13-40); P2.2 visitor validator with the fail-fast-before-Emit reorder at CompilerDriver.cs:80-85; P2.3 the COBOLNET0900-0903 band; P2.4 scripted reserved-word tables generated **in-session** from spec §8.9 (~line 10306) — never agent-emitted (content filter tripped twice) — and **designed as a per-compilation-unit overridable layer, not baked constants**: the COBOL-WORDS directive (Annex E.3.3 item 12) mutates the reserved/context-sensitive/function-name lists per unit (ISO-validation D9).
- **Pre-P2.5 metadata scrub (critic fix, adopted):** before the ConstructDialectStatus registry + drift tests freeze constructs.json behind a guard, correct it. Chair-verified defects: (a) rows `json-generate-2014`/`xml-generate-2014` cite "ISO 2014 §14.9.x" while **json/xml have 0 hits in the entire 2023 spec** — remove or re-tag as vendor-extension rows pending the Phase-0 decision; (b) the TYPE IS row is seeded 2023, but TYPEDEF has ~33 spec hits yet **zero Annex E (2014→2023 change) rows** — proof it predates 2023, so the `{is2023()}?` gate at CobolData.g4:246 is provably wrong; set a **provisional 2002** edge (feature-catalog's lineage claim; final edge blocked on the older standards) with a provisional-confidence marker. This overrules the architecture lens's "resolve via VCR lookup, scope S" — the VCR is a 2014→2023 ledger and cannot answer (critic 2 confirmed); (c) align the XOR rows to Annex E (see Phase 2); (d) **split the float_usage family edges** — FLOAT-SHORT/LONG/EXTENDED provisional 2002 vs FLOAT-BINARY-32/64/128 + FLOAT-DECIMAL-16/34 provisional 2014 (spec 22668–22671; ISO-validation D16); (e) **seed constructs.json rows for the ISO-validation additions** before the P2.5 freeze — CONSTANT entry §13.10 / CONSTANT RECORD clause §13.18.15 (mandatory core, zero Annex E hits ⇒ predates 2023; D5) and the `&` concatenation operator §8.8.3 (mandatory 2002-era core; D6), both with provisional edges per the decision-1 policy.
- P2.5 registry + both drift tests; P2.6 gates (~14) + 3 binder migrations, **plus** the 85-side runtime-loud fix for the STOP-literal mis-bind (StatementBinder.cs:168) in the same commit as its ≥2002 gate — the critic is right that splitting the two halves across waves leaves a silent 85 semantics bug live.
- P2.7 as a **one-time global** permissive flip (not per-gate — critic's decomposition fix adopted): P2.1 axis → flip + first LABEL RECORDS gate in one commit (243/459 NIST programs affected, verified) → remaining gates ordinary. **Attached to the flip commit: re-run the 318 goldens at --std 2023 permissive and triage every diff against VCR behavior rows.** This answers the critics' one **fatal** challenge — the shipping default edition would otherwise never be behaviorally executed before G8. Golden re-match at the default edition becomes a G7 exit criterion (INV-1-strong at 2023; other editions best-effort triage).
- Wire scripts/version-continuity-sweep.sh into .github/workflows/build-and-test.yml (verified: CI runs legacy guard.sh + both greenfield suites but **not** the sweep — an unowned gap the council adopts).
- Docs in the same change set: Validation/ deep-dive banner + DOC_INDEX row (CLAUDE.md doc-map rule); the measurable G7/G8 exit criteria (verification rec 5) with the critic's wording fixes — traceability = "any recognized edition-band code" (0801/0802/0810/0811/0873/0875-0879/0882/0893/0900-band), census criterion "**≥**357 GREEN"; a validator compile-time perf measurement (feedback_gate_on_the_verdict_line). The exit criteria also cite the **§4.2.2 selectable conformance-checking suboption** (delivered in Phases 3/4(c); ISO-validation D8).

**Parallel agent waves:** read-only OO re-scout (2-3 agents: legacy algorithm mining, regenerating the lost oo-plan brief **into** docs/COBOLNET_OO_DESIGN.md, folding the four spec-verified corrections from memory project_oo_port_legacy_algorithms); the positive-corpus discovery runner and the negative-corpus (.cob + .err) runner as **shells with an explicit pending manifest** — the critic's serious challenge to verification rec 2 is upheld: tests/conformance/2002 is full of unimplemented-feature programs (national_data, boolean_data, based_pointer, float_usage, logical_xor — verified by listing), so unconditional discovery today is mass-red; entries are enabled per feature as waves land. Create tests/conformance/2014/ (empty + manifest) now so the directory gap stops being invisible.
**Exit:** P2.1-P2.7 green; continuity sweep green permissive at all four editions with every strict failure tracing to a recognized edition-band code; 2023-permissive golden triage complete; drift tests green over **scrubbed** metadata; runners discovering with manifests.

### Phase 2 — Validator W2 (parallel) + W1.5, then W3 (serial grammar batch) — **M**
**Lands:**
- **W2 (parallel agents, disjoint files, per-agent worklist files):** MOVE VCR rows (1, 92/128); the MOVE ALL-digit latent bug (citation corrected: §14.9.25 **Syntax rule 5**, not GR5 — the permission is ALL-digits to an **integer** numeric item only; non-integer numeric/numeric-edited receivers stay prohibited; F.2 item 2; ISO-validation D13); the **loud-guard silent-misbind sweep** (architecture rec 2, scheduled here per its own P2.8 fit): PicInfo.ParseUsage's silent DISPLAY fallback for 2002+ usages, PIC symbol whitelist (N/E/1 loud), classDefinition silent drop in CallCollectUnits, the UsageKeyword string-strip fallback — plus the national/boolean **skeleton** (PicCategory/Usage enum entries, edition gates, loud not-implemented) so the holes close permanently; negative-corpus seeding **≥1 .err case per Wave-1 gate** (verification's per-gate timing wins over edition-gating's W2-pull — the wrong-reason-rejection argument applies to the first gate) plus the reserved-word interval witnesses; VCR status flips; adversarial review.
- **W1.5:** upgrade the grammar introduction-gate rejections to edition-naming 0900 diagnostics. Chair-verified count: **~24 actual gate sites** (44 grep hits minus 20 comment-only doctrine lines in CobolDialect.g4) — edition-gating's "39, scope M, highly parallel" is overruled: the doc figure is stale, and its fan-out has every agent writing the same visitor class + constructs.json (the exact shared-file race verification's own risk names). Run it **serially or as fragment-merge**, scope S-M, noting the critic's mechanism caveat: predicate failures surface as parse errors pre-bind, so some constructs need parse-error mapping or binder-side gates.
- **W3 (single serialized end-of-session grammar batch — sequencing's serialization wins over edition-gating's W2∥W3 concurrency; the waves contend on VCR rows and constructs.json):** the XOR/EXCLUSIVE-OR regating. **Chair adjudication of the in-repo contradiction:** VCR rows 32/41 cite the spec's own change annex (E.2 item 25 / E.3.2 item 4: these were user-defined words before 2023); ISO2023_CONFORMANCE_PLAN M4-2a's "(2002-era)" is an unsourced parenthetical — presence in the 2023 spec body (§8.7.6/§8.8.4.9, verified) proves nothing about 2002. **Annex E wins: the operators are 2023.** W3 therefore also corrects the M4-2a note, re-editions tests/conformance/2002/logical_xor to 2023, and runs the **2002-corpus edition audit** (33 programs' introduction claims vs the VCR — the critics' unowned gap, now owned here). Plus the notInGrammar 85-acceptance set, preprocessor DialectLevel threading (VCR 2/4/94), full legacy guard + committed regenerated parser (the committed-regen rule), and the queued path-portable regen fix.
**Exit:** all W2/W3 VCR rows flipped; every gate has a negative witness pinning its code; corpus audit clean; full legacy guard green.

### Phase 3 — M2 OO port — **XL** (upgraded from sequencing's L; feature-catalog wins)
Prior OO work covered only slices 1-3b, and FACTORY is not even in the grammar (CobolOO.g4:17); PROPERTY, INTERFACE-ID, universal object reference, and EC-OO are net-new mandatory surface (A.4.10 makes only multiple inheritance + parametric polymorphism optional).
**Lands:** (1) **Serial spine first** (sequencing's ordering wins over feature-catalog's immediate-parallel M2 data waves): the GOBACK-vs-STOP-RUN bind-time split + the CSharpEmitter emit-into-a-type parameterization. Chair note on the advisor conflict: the deep-dive (COBOLNET_OO_DESIGN.md:18, D8, :155-157 — verified) still requires the **binder-side** BoundMethodReturn/BoundStopRun split; the runtime ProgramReturn carrier architecture cites does not resolve it — the deep-dive stands, and it must be reconciled with the as-built state in the first docs commit (process rule 4). (2) Slice waves in the proven legacy order (port algorithms, never the byte substrate — owner directive), then FACTORY → PROPERTY (§13.18.42) → INTERFACE-ID (§11.6) → universal reference → EC-OO + exception objects (RAISE identifier) + GLOBAL-walkable F3 declaratives (the deep-dive's recorded deferral homes). ISO-validation additions to the spine: the BoundMethodReturn/BoundStopRun carrier must accommodate the **2023 GOBACK status phrase** (Annex E item @50308 — implement here or explicitly defer to Phase 7); the **ANY LENGTH clause (§13.18.2)** — load-bearing in the method-conformance rules — lands with the OO spine (or with Phase 4(c) prototypes; D14); and the **§4.2.2 selectable conformance-checking suboption** (a CLI switch toggling the §14.8.2/§14.8.3/§9.3.8.2.3 conformance diagnostics — a spec-mandated mechanism) ships its interface leg here, its prototype leg in Phase 4(c) (D8). Each slice ships its conformance programs (oo_* pairs enabled in the manifest), its reject-at-85 matrix rows, and its negative cases in the same commit. **OO grammar increments require the Phase-0 owner grant** — the critic is right that feedback_grammar_preauthorized covers NIST work only; sequencing's "pre-authorized" claim is overruled pending that grant.
**Parallel:** slice implementation + corpus authoring fan out after the spine; FACTORY/PROPERTY are disjoint once type emission exists.
**Exit:** legacy slice-order parity + the mandatory net-new surface; 14 OoTests re-landed as greenfield facts; multi-base INHERITS rejected loudly per SSOT §18 item 18.

### Phase 4 — M2 residual catalog — **L** (after the OO spine; can overlap late OO slices with verified file partitions)
**Lands:** half-session greenfield-vs-catalog reconciliation audit first (the §3 checkmarks are legacy-era; several items already landed greenfield — intrinsic catalog, ROUNDED modes, standard-decimal, EC), adding a greenfield-status column so waves are sized against truth. Then parallel disjoint tracks: (a) national/boolean data end-to-end (skeleton from Phase 2; char-vs-byte boundary design at AsImage/IRecordCodec first) + **boolean OPERATIONS** (§8.8.2 boolean expressions, §8.8.4.3 simple boolean conditions, §8.8.4.2.8 boolean-operand comparisons, the COMPUTE boolean format — mandatory 2002 base, ordered before Phase 7's 2023 shift additions; catalog row added before the reconciliation audit; ISO-validation D4) + the ALPHABET national/UCS-4/UTF-8/UTF-16 phrases (§12.3.7 — mandatory core, NOT part of the A.4.9 locale module) + the EC -N twins + EXCEPTION-FILE-N; (b) pointers/ALLOCATE/FREE/BASED/SET ADDRESS on the settled ManagedPointer carrier (COBOLNET_INTERPROGRAM_DESIGN D1/D5) incl. **USAGE PROGRAM-POINTER** (2002-era GRs; D14); (c) UDF units/prototypes/REPOSITORY (§8.13/§12.3.8) + the §4.2.2 suboption prototype leg; (d) SHARING/LOCK (A.4.7-gated) + **RETRY** (core §14.7.9 syntax — NOT A.4.7-gated, behavior reachable only with A.4.7 locking; D15) + UNLOCK (named for auditability) + line-sequential (provisional 2002 edge); (e) **ARITHMETIC IS STANDARD positive behavior at 2002/2014** + the M2-ARITH-2 recognize-and-ignore residuals — the critics' orphaned behavior obligation, now owned; (f) **Report Writer 2002 additions** — PRESENT WHEN format 1 + VARYING format 1 (report-writer formats NOT disposed by the VALIDATE non-support ruling: the §13.18.41/§13.18.64 obsolescence NOTEs scope to VALIDATE only; flagged to this phase's reconciliation audit so the M2 catalog's mis-filing under VALIDATE is not inherited; D3); (g) **concatenation expressions — the `&` operator (§8.8.3)** — mandatory 2002-era core, usable anywhere a literal of the class may be used; the 2023 CONCAT function is defined by reference to it, so it cannot be dispositioned away (D6). Alongside: the **interim** 85→2002 delta work — grow VCR Table 7 from the legacy FlagsFeaturesRemovedAfter85/DialectStrictnessChecks inventory (S-M, scheduled), with the full delta-research track carried as **blocked on standards acquisition** (edition-gating's "L track, depends on W1 registry" is overruled — Table 7 rows are markdown; the only real blocker is sourcing the documents).
**Exit:** every track's positive corpus discovered by the greenfield runner + matrix rows + negative cases; catalog marks flipped to greenfield truth.

### Phase 5 — Deferred-intrinsics backlog — **M** (runs parallel with Phase 4/6; leaf functions, disjoint from emitter core)
**Lands:** the 43 IntrinsicBind.Deferred rows by family (2002 set, 2014 FORMATTED-* dates, seven 2023 rows), each with **window-enforcement negative rows** (a later-edition function under an earlier --std emits the per-edition diagnostic — M4-3's mandate, the critics' dangling gap), firming the provisional windows as each lands. -N twins wait on Phase 4(a).
**Exit:** zero Deferred rows — each row **implemented OR dispositioned**: the 5 A.4.9 locale-module functions (IntrinsicCatalog.cs:144-149 — LOCALE-COMPARE, LOCALE-DATE, LOCALE-TIME, LOCALE-TIME-FROM-SECONDS, STANDARD-COMPARE) and the LOCALE keyword variants of LOWER-CASE/UPPER-CASE/TEST-NUMVAL-C resolve to the A.4.9 not-supported diagnostic path per ratified decision 3, counted into the G8 Cut-3 §4.2 conformance document (ISO-validation D1); remaining rows implemented with windows non-provisional or explicitly blocked on standards (FORMATTED-\* marked "present in 2014", not "introduced in 2014"; the 2002 set provisional except the TEST-DATE/TEST-DAY pair, which carries direct in-spec 2002 attribution @D.31.3.1).

### Phase 6 — M3 2014 — **L**
**Lands:** the OCCURS DYNAMIC deep-dive **before** implementation (fixed-physical-capacity is load-bearing across DataItem.Occurs/OdoModel/image facility — architecture rec 6; a scout inventories assumption sites in parallel), then dynamic-capacity tables as the serial spine; **DYNAMIC-LENGTH elementary items** (A.4.5 = the §13.18.19 DYNAMIC LENGTH clause + §8.5.1.10 + the SPECIAL-NAMES DYNAMIC LENGTH STRUCTURE clause — ratified decision-3 IMPLEMENT, previously owned by no phase; base feature provisional pre-2023 edge, only the SET-length enhancement is the 2023 delta [E.3.3 item 17], which layers in Phase 7; ISO-validation D2); TYPEDEF/SAME AS/TYPE TO (provisional 2002/2014 edges per Phase 1) parallel; **>>PROPAGATE (§7.3.21)** — re-editioned to **≤2014** (zero Annex E hits and absent from E.2 item 5's added-directive list — moved here from Phase 7; D11); **CONSTANT entries §13.10 + the CONSTANT RECORD clause §13.18.15** (mandatory core, zero owner before validation; provisional edge; D5); the **M3-4 catchall** — IEEE-754 float usages (currently silent-misbinding, made loud in Phase 2; family edges split 2002/2014 per the Phase-1 scrub), **FUNCTION-POINTER** (prototype-dependent; "METHOD-POINTER" does not exist in the spec — 0 hits; USAGE PROGRAM-POINTER is Phase 4(b)), conditional-expression enhancements — now explicitly owned here (critics' gap; ~~"increased limits"~~ DROPPED: no anchor in the 2023 text — §4.2.15 delegates all limits to the implementor; D14). **JSON/XML is removed from M3** — sequencing rec 6 is overruled on feature-catalog's verified evidence (0 spec hits; the M3-3 "§14.9" citation is wrong): it is vendor-dialect work, default-deferred post-G8 per the Phase-0 decision. The 2014 positive corpus grows with every feature (directory seeded in Phase 1).
**Exit:** 2014 corpus non-empty and discovered; dynamic-table matrix rows green at all editions.

### Phase 7 — M4 2023 deltas + EC remnants + behavior-row burn-down — **L** (upgraded from sequencing's 1-2 sessions per the critic; contingent on Phase-0 scope decisions)
**Lands:** PERFORM...WHEN exception-checking (annex-attested 2023: E.3 item 36 / E.2 item 19; >>PROPAGATE moved to Phase 6 per D11), the EXCEPTION-FILE 2023 connector argument, SMALLEST-ALGEBRAIC + EXCEPTION-FILE-N, bit/boolean 2023 shift additions (on the Phase-4 boolean-operations base; D4), group SYNCHRONIZED / NO SIGN packed rows, the DYNAMIC-LENGTH SET delta (from Phase 6; D2); ISO-validation additions so engine-level surface is sized, not discovered mid-wave: **DELETE FILE** (E.3.3 item 15 + its five new I-O statuses — file-I/O runtime work, not just a validator row), the **CONTINUE timed pause** + the EC-CONTINUE family, **INSPECT BACKWARD** (scanner direction), the **PICTURE EDITING phrase** (PicInfo insertion machinery), the **COBOL-WORDS / PUSH / POP / DISPLAY directive quartet** (COBOL-WORDS mutates the per-unit word lists — the Phase-1 tables are designed overridable for exactly this; D9), and the **EXTERNAL run-unit conformance cluster** (E.2 items 9/10/12/24 + the EC-EXTERNAL family) with a design touchpoint on the Phase-1 cross-assembly EXTERNAL machinery (D10); the flag-obsolete warning rows (VALIDATE family 117-125/129 etc.) on the Phase-1 warnings channel — with the corrected framing (D12): **FLAG-02/FLAG-14 are user-invoked incompatible-behavior directives** (2002→2014 / 2014→2023 respectively, all options default OFF; the conformance obligation is mechanism-PROVISION per A.1 items 79/80; FLAG-02 is itself obsolete per F.2 item 1), obsolete-element flagging is the distinct §4.2.13 mechanism over the F.2 list, archaic is §4.2.12 over F.1 — the four archaic rows (VCR 89/90/126/127: EXIT PROGRAM, NEXT SENTENCE) get their own sub-code in the 0903 band; a one-pass **A.3 disposition sweep** (46 items; §4.2.6 requires a compile-time warning on unsupported syntactically-detectable elements); and — the critics' serious orphan fix — an explicit **disposition/sizing pass then implementation wave for the ~44 Table 1+5 behavior rows** on already-complete subsystems (I-O status, VALUE semantics), which no feature wave would otherwise touch. MCS and commit/rollback ride the ratified decision (documented non-support — MCS via **Annex A.3**, not A.4.3, per the critic's verified correction; A.4.3 is commit/rollback only).
**Exit:** VCR Tables 2/3 rows dispositioned green or documented; Table 1/5 rows implemented or per-row dispositioned.

### Phase 8 — Matrix closure + greenfield guard + equivalence proof — **M**
**Lands:** drive all VCR rows to zero TODO (green or written disposition); full INV-1/INV-2/INV-3 sweeps, strict + permissive legs, **including golden re-match at --std 2023** (INV-1-strong at the default edition — the fatal-challenge criterion); negative-corpus completion with the registry-coverage unit test (every registry entry ≥1 case); build the **in-repo greenfield guard** (rebuilding the lost /e/tmp/nc-sweep census tooling; full 403-census basis, run-only/chain-intermediate handling ported from guard.sh), run the one-time verdict-diff **equivalence proof against the legacy guard while it still runs** — the single irreversible ordering constraint in the plan — and migrate the 11 LEGACY_DIVERGENT ISO citations into the new guard/a LEDGER doc. ⚠ Annex-enumeration tooling must not rely on the numbered-item regex — E.3.3 items 5/6 are mangled into `##` headers inside Unicode tables in the extracted spec (a 43-of-45 silent-undercount hazard; ISO-validation note).
**Exit:** the Phase-1 G7 exit criteria all satisfied, as counts/exit codes.

### Phase 9 — G8 in three serial cuts — **M** (strictly serial; adopted over the SSOT's implied single step)
**Cut 1:** drop legacy from the test graph (convert the ~47 differential files per the Q13 decision — pinned goldens recommended, amortized opportunistically from Phase 3 onward), replace the CI guard.sh step with the greenfield guard. **Cut 2:** delete the byte engine (legacy preserved at a git tag). **Cut 3:** rename CobolSharp→WiseOwl COBOL/cobol.exe as one atomic commit with regenerated committed Generated/* (the regen-path portability fix landed in Phase 2), the final doc/DOC_INDEX pass, and the **ISO §4.2 conformance documentation — the full §4.2.16 documentation set, not just the A.4 leg (ISO-validation D7):** A.4 optional-element claims, the ~100-item A.1 implementor-defined list (most items "shall be documented" — this pass sized separately, likely M), A.3 claims AND absences (§4.2.6), nonstandard extensions + any added reserved words (§4.2.10 — with the extension-use warning emitted on the 09xx channel per decision 4), archaic (§4.2.12) and obsolete (§4.2.13) identification, and the §4.2.3/§4.2.4 interaction statements. This is the artifact that makes documented non-support conformance-legal (§4.2.7). Disposition rows for A.4.10 (the three optional OO items — documented non-support per decision 7), A.4.11 (Report Writer — already implemented), and A.4.12 (per the validation sweep: already implemented) enter this claimed-support checklist.
**Exit:** grep-clean of legacy references; one greenfield guard exits 0 covering goldens + census (≥357 GREEN) + per-edition discovery + negative corpus + sweeps + dotnet test.

## 3. Changes vs the current canonical plan

The council **confirms** the SSOT §16 / kickoff-banner backbone (now the plan §0) (EditionValidator → M2 with OO largest → M3 → M4 → G8) and the P2.1-P2.8 plan as written. Deltas:

1. **Owner decisions and standards acquisition move to Session-0** (canonically parked mid-plan/"before M3"). They gate M2/M4 sizing and TYPEDEF/RW-130d adjudication.
2. **A metadata-adjudication step is inserted before P2.5**: constructs.json scrub (JSON/XML fiction, TYPE IS edition, XOR alignment) so the drift tests never fossilize wrong data. Not in the canonical plan.
3. **JSON/XML is removed from M3** (SSOT §16 lists it): not ISO (verified 0 spec hits); vendor-dialect disposition to the owner, default post-G8.
4. **A behavioral leg at the default edition attaches to the P2.7 flip** (318 goldens at 2023 permissive) and INV-1-strong-at-2023 joins the G7 exit criteria — the canonical plan never executes behavior at 3 of 4 editions before G8.
5. **New named workstreams** absent from §16: W1.5 (~24 intro-gate diagnostic upgrades), the silent-misbind loud-guard sweep, the positive/negative discovery runners with pending manifests, the 43-row intrinsics backlog, the Table 1/5 behavior-row wave, the greenfield guard + equivalence proof, CI wiring, and the §4.2 conformance document.
6. **G8 splits into three serial cuts** (test-graph → deletion → rename) vs the implied single step, with the equivalence proof hard-ordered before Cut 1.
7. **Budget re-baselined to ~22-32 sessions**; OO re-rated XL.
8. **ISO-validation amendments (Appendix):** newly-owned mandatory surface — boolean OPERATIONS (Phase 4a), the `&` concatenation operator (4g), CONSTANT entries (6), DYNAMIC-LENGTH elementary items (6), RW PRESENT WHEN/VARYING format 1 (4f), the EXTERNAL conformance cluster + DELETE FILE/CONTINUE-pause/INSPECT BACKWARD/PIC EDITING/COBOL-WORDS quartet (7), the §4.2.2 checking suboption (3/4c), the A.3 sweep (7); >>PROPAGATE re-editioned ≤2014 (7→6); the §4.2 conformance document expanded to the full §4.2.16 seven-leg set; Phase-5 exit reconciled with decision-3 locale non-support; FLAG-02/14 framing, MOVE ALL-digit SR5, RETRY-not-A.4.7, float-family edge split, "increased limits" dropped.

## 4. Top risks

1. **Content-filter kill during reserved-word work** (occurred twice). *Baked in:* Phase 1 generates all tables in-session via scripts/gen-reserved-words.ps1 from spec §8.9; every G7 agent brief forbids emitting word lists — agents return counts and file paths only.
2. **A removal gate lands before the permissive axis, turning the guard corpus-red** (243/459 NIST programs carry the affected clause). *Baked in:* Phase 1 hard-sequences P2.1-P2.5 before any P2.6 gate; the one-time flip + first gate ship in a single commit with the sweep green as the commit gate.
3. **Drift tests fossilize wrong edition metadata** (non-ISO JSON/XML rows, a provably wrong 2023 TYPE gate, the XOR mislabel), forcing multi-wave rework of registry + grammar + matrix + regen. *Baked in:* the pre-P2.5 scrub and the three chair adjudications land inside Phase 1/2, before the registry freezes.
4. **The G8 equivalence window closes unproven** — census tooling is already lost off-repo; deleting the oracle first makes the greenfield guard unverifiable forever and destroys the 11 re-baseline citations. *Baked in:* Phase 8 builds the in-repo guard, runs the verdict-diff while legacy still runs, and migrates the citations; Phase 9 Cut 1 cannot start until Phase 8 exits.
5. **Silent mis-binds produce false greens through the M2/M3 waves** (2002+ usages → DISPLAY, PIC N → numeric, dropped class definitions — pattern-mates of the STOP-literal bug). *Baked in:* the Phase 2 loud-guard sweep closes the family before any feature wave exercises those paths, each closure with an adversarial negative test in the same commit.

## 5. Decisions needed from the owner

> ✅ **RATIFIED 2026-07-03:** the owner adopted the council recommendation on every row. For #1
> the ruling is **no acquisition** — the in-repo 2023 spec (PDF + extracted MD in `specs/`) is the sole ISO
> authority; Annex E + the legacy inventory adjudicate prior-edition edges, provisional markers stay where
> they cannot. #6's grammar grant is standing (logged in memory). The table below is preserved as decided.

| # | Decision | Council recommendation |
|---|---|---|
| 1 | Acquire ISO 1989:2002 and :2014 (or their change annexes) for specs/? | **Yes — request at Session-0** (only external-lead-time dependency; gates TYPEDEF edge, VCR Table-7 growth, RW row 130d, 2014 reserved-word confidence). Until then: legacy inventory = interim authority, provisional markers on affected edges. |
| 2 | JSON/XML GENERATE/PARSE — not ISO (0 spec hits): keep/defer/drop? | **Re-tag as vendor-dialect extension, defer post-G8**; scrub from the conformance matrix now; if built later, pick a dialect model (IBM Enterprise) and add a dialect axis. |
| 3 | Annex A.4 optional-module line (screen A.4.2, commit/rollback A.4.3, dynamic tables A.4.4, dynamic length A.4.5, extended letters A.4.6, sharing/locking A.4.7, FORMAT/SELECT WHEN A.4.8, locale A.4.9, REWRITE/WRITE FILE A.4.13, VALIDATE A.4.14; MCS via A.3)? | **Implement:** dynamic tables, dynamic length, sharing/locking. **Documented non-support** (with the §4.2 conformance doc + uniform not-supported diagnostics): screen, MCS, commit/rollback, locale, extended letters, A.4.8, A.4.13, and **VALIDATE** (obsolete-in-2023, VCR row 95: "never implemented; no interest" — the feature-catalog's implement recommendation is overruled). |
| 4 | --permissive: supported CLI surface or internal affordance? | **Supported, documented migration mode** (a modest fifth persona); warning-text stability guaranteed only for the 09xx codes, not prose. |
| 5 | Strict-by-default UX at --std 2023 once removal gates land? | **Keep strict** (§10 decision 2 stands); diagnostic text names the removing edition and both escape hatches (--std 85, --permissive). No W1 block — logged, not gating. |
| 6 | Grammar-change approval for OO/M2 increments (FACTORY, INTERFACE-ID, PROPERTY…)? | **Explicit grant requested** — the NIST pre-authorization does not cover them; same logging + full-guard discipline as feedback_grammar_preauthorized. |
| 7 | OO v1: multiple inheritance + parametric polymorphism? | **Documented non-support per A.4.10**, loud rejection of 2+ bases; SSOT §18 item 18 stays deferred until a real program triggers it. |
| 8 | Q13 — fate of the ~47 differential test files at G8? | **Convert to pinned goldens** (amortized from Phase 3 onward); archive legacy at a git tag with a WSL run recipe rather than irrecoverable deletion. |
| 9 | COMMUNICATION module? | **Documented non-support** + clean per-edition diagnostics (a validator row, not an implementation). |
| 10 | INV-1 strong form scope for G7 exit? | **Required at the default edition (2023)** — golden re-match, seeded by the Phase-1 flip-commit run; other editions triaged best-effort, promoted at Phase 8. |
| 11 | G8 = finish line, or does the full architectural review live inside it? | **Separate post-G8 phase**, along with packaging/distribution (dotnet tool vs installer, runtime redistribution, licensing) — currently owned by no phase; schedule after cut-over. |

## 6. Coverage ledger

| Obligation | Phase |
|---|---|
| Session-0 rebuild/baseline, decision packet, standards request | 0 |
| Validator P2.1-P2.5 (permissive axis, visitor, 0900 band, word-table generation P2.4, registry + drift tests) | 1 |
| constructs.json metadata scrub (JSON/XML, TYPE IS, XOR rows; float-family 2002/2014 edge split; CONSTANT + `&`-concat row seeds) — pre-P2.5 | 1 |
| P2.6 removal gates + binder migrations (+ STOP-literal 85-side fix) | 1 |
| P2.7 one-time permissive flip + continuity restatement | 1 |
| Behavioral leg at default edition (318 goldens @ 2023 permissive; INV-1-strong seed) | 1 (promoted at 8) |
| Positive-corpus discovery runner (pending manifest) + negative (.err) runner + 2014 dir seed | 1 |
| CI wiring of the continuity sweep | 1 |
| Validation/ deep-dive + DOC_INDEX row; measurable G7/G8 exit criteria; validator perf budget | 1 |
| OO re-scout + deep-dive corrections folded in | 1 (parallel) |
| W2: MOVE rows, MOVE ALL-digit bug (§14.9.25 SR5 — integer receiver only), VCR flips, adversarial review | 2 |
| Silent-misbind loud-guard sweep + national/boolean skeleton gates | 2 |
| Negative-corpus seeds (per Wave-1 gate + reserved-word interval witnesses) | 2 (grows every phase) |
| W1.5: ~24 intro-gate diagnostic upgrades | 2 |
| W3: XOR regating per Annex E + M4-2a correction + logical_xor re-edition + notInGrammar set + preprocessor threading + regen-path fix | 2 |
| 2002-corpus edition audit | 2 |
| M2 OO: spine (GOBACK split incl. 2023-status-phrase accommodation, type-parameterized emitter), legacy slices 1-6, FACTORY, PROPERTY, INTERFACE-ID, universal reference; ANY LENGTH §13.18.2; §4.2.2 checking suboption (interface leg) | 3 |
| EC remnants: exception objects, GLOBAL-walkable F3 | 3 |
| Greenfield-vs-catalog reconciliation audit | 4 |
| M2: national/boolean data AND boolean operations (§8.8.2 expressions, §8.8.4.3/§8.8.4.2.8 conditions/comparisons, COMPUTE boolean — before Phase 7's shifts) + ALPHABET UTF phrases (+ EC -N twins, EXCEPTION-FILE-N) | 4 |
| M2: pointers/ALLOCATE/FREE/BASED/SET ADDRESS + USAGE PROGRAM-POINTER | 4 |
| M2: UDF prototypes/REPOSITORY (+ §4.2.2 prototype leg); SHARING/LOCK (A.4.7) + RETRY (core §14.7.9) + UNLOCK + line-sequential | 4 |
| RW 2002 additions: PRESENT WHEN format 1 + VARYING format 1 (not disposed by the VALIDATE ruling) | 4(f) |
| Concatenation `&` expressions §8.8.3 (2023 CONCAT defined by reference to it) | 4(g) |
| ARITHMETIC IS STANDARD behavior @ 2002/2014 + M2-ARITH residuals | 4 |
| Interim 85→2002 Table-7 growth (legacy inventory); full delta research + RW row 130d | 4 (interim); blocked track per decision 1 |
| 43 deferred intrinsics: implement or disposition (5 A.4.9 locale functions + LOCALE variants → the decision-3 non-support diagnostic) + window-enforcement negative rows | 5 |
| M3: OCCURS DYNAMIC deep-dive then dynamic tables; DYNAMIC-LENGTH elementary items (A.4.5 — decision-3 implement; SET delta → 7); TYPEDEF/SAME AS/TYPE TO; >>PROPAGATE §7.3.21 (≤2014); CONSTANT §13.10 + CONSTANT RECORD §13.18.15; M3-4 catchall (IEEE-754 floats [split edges], FUNCTION-POINTER, conditional-expression enhancements — "limits" dropped per §4.2.15) | 6 |
| JSON/XML | Owner decision 2 (default post-G8) |
| M4: PERFORM...WHEN, EXCEPTION-FILE 2023 arg, SMALLEST-ALGEBRAIC/EXCEPTION-FILE-N, bit/boolean shift additions, DYNAMIC-LENGTH SET delta, DELETE FILE (+5 statuses), CONTINUE timed pause (+EC-CONTINUE), INSPECT BACKWARD, PICTURE EDITING phrase, COBOL-WORDS/PUSH/POP/DISPLAY directives, EXTERNAL conformance cluster, GOBACK status phrase (if deferred from 3) | 7 |
| VCR Table 1/5 behavior rows (~44) disposition + implementation | 7 |
| Flag-obsolete warning rows (VALIDATE family etc.) + archaic §4.2.12 sub-code rows (VCR 89/90/126/127); A.3 disposition sweep (46 items, §4.2.6 warnings); MCS/commit-rollback disposition diagnostics | 7 |
| Matrix closure: all VCR rows dispositioned; INV-1/2/3 sweeps strict+permissive | 8 |
| Greenfield guard + census rebuild + equivalence proof + LEGACY_DIVERGENT citation migration | 8 |
| G8 Cut 1: test-graph cut + CI guard replacement + Q13 execution | 9 |
| G8 Cut 2: byte-engine deletion (legacy tagged) | 9 |
| G8 Cut 3: rename + committed regen + final doc pass + the full §4.2.16 conformance documentation set (A.4 claims incl. A.4.10/11/12 rows, A.1 items, A.3 claims/absences, extensions + added reserved words, archaic/obsolete IDs, interaction statements) + DOC_INDEX reconcile | 9 |
| Architectural review; packaging/distribution/licensing | Post-G8 (decision 11) |

---

> **Audit record.** Produced by the 12-agent validation workflow: 5 claim-group verifiers (the
> edition-adjudication group double-verified), residual-claim extraction +
> 2 follow-up verifiers, 3 reverse-direction completeness sweeps (Annex E in full — 81 items; Annex A/F +
> conformance clause 4 in full; the §13.18/§14.9/§15 catalogs by header enumeration), and a chair that
> re-verified every contested point in the spec text. All *Change* instructions in §2 below are APPLIED INLINE
> in the roadmap above; this appendix is the permanent audit trail and citation source. Spec
> references are line numbers in `specs/ISO_COBOL.md` at submodule commit c1435f3.
# Appendix — ISO Validation of the Completion Roadmap (2026-07-03)

## 1. Verdict

The roadmap is **ISO-sound in substance and safe to execute as ratified, subject to the corrections below**. Of 39 unique spec claims validated (the 4 edition-adjudication claims E1–E4 were double-verified by two independent verifiers, who agreed unanimously; their one evidence-line discrepancy was chair-resolved — both quoted sentences sit in the same paragraph at spec line 8982): **30 CONFIRMED, 9 PARTIAL, 0 REFUTED, 0 UNVERIFIABLE**. No claim the roadmap makes about the 2023 spec is contradicted by the spec; every PARTIAL is a detail error (a wrong rule number, a wrong edition attribution for one directive, an over-broad gating claim) with the substance intact. The completeness sweep found **0 fatal and 10 serious gaps** — all of them coverage/ownership holes (mandatory 2002-era surface with no owning phase, or conformance-clause obligations with no ledger row), none of them contradictions of the ratified spine. Three serious findings touch ratified owner decision #3 and need reconciliation text, not re-decision.

## 2. Defects requiring correction

Ordered by severity. Spec references are line numbers in `specs/ISO_COBOL.md`; roadmap references are line numbers in `docs/COMPLETION_ROADMAP_COUNCIL.md`.

### Serious — internal contradiction with a ratified owner decision

**D1. Phase 5's exit criterion contradicts ratified decision #3 (locale non-support).** Five of the 43 `IntrinsicBind.Deferred` rows are A.4.9 locale-module members (spec 40379–40405; `src/Cobol.Net.Compiler/Binding/IntrinsicCatalog.cs:144-149` — LOCALE-COMPARE, LOCALE-DATE, LOCALE-TIME, LOCALE-TIME-FROM-SECONDS, STANDARD-COMPARE), plus LOCALE keyword variants of three other functions (A.4.9 items 6/12/13). Decision #3 (roadmap line 111, RATIFIED) dispositions locale to documented non-support; Phase 5 (lines 61–62) reads implement-all.
*Change* (line 62): "**Exit:** zero Deferred rows; windows non-provisional or explicitly blocked on standards." → "**Exit:** zero Deferred rows — each row **implemented OR dispositioned**: the 5 A.4.9 locale-module functions (IntrinsicCatalog.cs:144-149) and the LOCALE keyword variants of LOWER-CASE/UPPER-CASE/TEST-NUMVAL-C resolve to the A.4.9 not-supported diagnostic path per decision 3, counted into the G8 Cut-3 §4.2 conformance document; remaining rows implemented with windows non-provisional or explicitly blocked on standards."

**D2. DYNAMIC-LENGTH elementary items: ratified-implement (decision #3) but owned by no phase.** The base feature predates 2023 (only the SET-length enhancement is in Annex E — E.3.3 item 17 at ~50271); its surface is §8.5.1.10 (8265–8300), the DYNAMIC LENGTH clause §13.18.19 (18541, confirmed as A.4.5's sole element at 40339–40341), the SPECIAL-NAMES DYNAMIC LENGTH STRUCTURE clause (context words 10886–10904), and variable-length-group rules (8360–8395). "dynamic length" appears in the roadmap only inside decision #3 (line 111); Phase 6 (line 65) and ledger row 150 name only dynamic-capacity **tables**. Phase 8's "all VCR rows dispositioned" exit is satisfiable while this ratified-implement module is never built (VCR row 60 covers only the 2023 SET delta).
*Change* (line 150): "| M3: OCCURS DYNAMIC deep-dive then dynamic tables; TYPEDEF/SAME AS/TYPE TO; M3-4 catchall (IEEE-754 floats, function pointers, limits) | 6 |" → append "**+ DYNAMIC-LENGTH elementary items (A.4.5, §8.5.1.10/§13.18.19 — decision-3 implement; base feature, provisional pre-2023 edge; VCR row 60 SET delta layers in Phase 7)**". Add the matching sentence to Phase 6 line 65 after "dynamic-capacity tables as the serial spine".

**D3. VALIDATE non-support orphans two supported Report Writer clause formats.** A.4.14 (40497–40519) covers the pure-VALIDATE surface, but PRESENT WHEN **format 1** and the VARYING clause's report-writer use are RW surface whose obsolescence NOTEs (21044, 23508) are scoped to the VALIDATE feature only; RW is a supported module (A.4.11) yet Phase 4 tracks (a)–(e) exclude RW and `ISO2023_CONFORMANCE_PLAN.md` line 519 files PRESENT WHEN entirely under the non-supported VALIDATE. Touches ratified decision #3 — the decision stands; the disposition boundary needs redrawing.
*Change* (line 57): after "(e) **ARITHMETIC IS STANDARD positive behavior at 2002/2014** …" add "; (f) **Report Writer 2002 additions** — PRESENT WHEN format 1 and VARYING format 1 (report-writer formats, NOT disposed by the VALIDATE non-support ruling; obsolescence NOTEs at §13.18.41/§13.18.64 scope to VALIDATE only), flagged to the Phase-4 reconciliation audit so the M2 catalog's mis-filing is not inherited."

### Serious — unowned mandatory surface or conformance obligation

**D4. 2002-era boolean OPERATIONS have no owner — only the data class does.** §8.8.2 boolean expressions (9323–9356), §8.8.4.3 simple boolean condition (9795–9818), §8.8.4.2.8 boolean-operand comparison (9683), and the COMPUTE boolean format are mandatory 2002 surface. Phase 4(a) (line 57) says only "national/boolean **data** end-to-end"; Phase 7's 2023 shift operators (E.3.3 item 3, 49447) presuppose this base; the Phase-4 audit substrate itself records the hole ("Bit operators deferred", plan lines 277/405/429-430) with no catalog row.
*Change* (ledger line 144): "| M2: national/boolean data (+ EC -N twins, EXCEPTION-FILE-N) | 4 |" → "| M2: national/boolean data **AND boolean operations (§8.8.2 expressions, §8.8.4.3/§8.8.4.2.8 conditions/comparisons, COMPUTE boolean format — ordered before Phase 7's 2023 shift additions; catalog row added before the reconciliation audit)** (+ EC -N twins, EXCEPTION-FILE-N) | 4 |".

**D5. CONSTANT entry (§13.10, 16780) and CONSTANT RECORD clause (§13.18.15, 18316) — mandatory core, zero owner.** Not in the A.4 optional list (40229–40519), zero Annex E hits (predates 2023), zero mentions in the roadmap, the conformance plan, or greenfield src.
*Change*: add a ledger row "| Constant entries §13.10 + CONSTANT RECORD §13.18.15 (provisional 2002/2014 edge per decision-1 policy) | 6 |" and seed a constructs.json row before the P2.5 drift-test freeze (Phase 1, line 34–35 territory).

**D6. Concatenation expressions — the '&' operator (§8.8.3, 9429–9467) — mandatory core, zero owner.** 2002-era, usable anywhere a literal of the class may be used; E.3.3 item 23 defines the new CONCAT function **by reference to this operator** (~50283), so it cannot be dispositioned away. Zero hits in roadmap, plan, and greenfield frontend.
*Change*: add to Phase 4 (natural fit: adjacent to Phase 4(a) literals and the Phase-5 CONCAT row) with a ledger row and a constructs.json row + negative witness before the registry freeze.

**D7. The G8 §4.2 conformance document is under-scoped to one of seven required legs.** Roadmap line 77 defines it as "the **ISO §4.2 conformance documentation** (which optional elements are supported …)" — the 4.2.7/A.4 leg only. 4.2.16 (2539–2543) makes documentation a conformance requirement across 4.2.3, 4.2.4, 4.2.5 (the ~100+-item A.1 list at 39232–39830, most items "shall be documented"), 4.2.6 (claims AND absences, 2435), 4.2.10 (extensions + added reserved words, 2476–2478), 4.2.12 (archaic), 4.2.13 (obsolete).
*Change* (line 77): replace the parenthetical with "(the full §4.2.16 documentation set: A.4 optional-element claims, the A.1 implementor-defined items, A.3 claims and absences, nonstandard extensions and any added reserved words, archaic and obsolete identification, and the 4.2.3/4.2.4 interaction statements — the A.1 pass sized separately, likely M)".

**D8. The 4.2.2 selectable conformance-checking suboption has no home.** Spec 2393–2397 (chair-verified verbatim): the compile-time warning mechanism "shall provide a suboption for selection or suppression of checking" for the 14.8.2/14.8.3 parameter/returning rules and 9.3.8.2.3 interface conformance. No phase, decision, or ledger row mentions it.
*Change*: add a ledger row "| §4.2.2 conformance-checking suboption (CLI switch toggling 14.8.2/14.8.3/9.3.8.2.3 diagnostics) | 3 (interfaces) / 4(c) (prototypes) |" and cite 4.2.2 in the G7/G8 exit criteria (line 38).

**D9. COBOL-WORDS directive collides with Phase 1's frozen word tables.** E.3.3 item 12 (50239) lets source modify the reserved/context-sensitive/function-name word lists per compilation unit; Phase 1 P2.4 (line 33) generates static in-session tables and P2.5 freezes constructs metadata behind drift tests. Related: PUSH/POP (50320), DISPLAY directive (50269).
*Change* (line 33): after "P2.4 scripted reserved-word tables generated **in-session** from spec §8.9 (~line 10306)" add "— **designed as a per-compilation-unit overridable layer, not baked constants** (the COBOL-WORDS directive, Annex E.3.3 item 12, mutates these lists per unit)"; name the COBOL-WORDS/PUSH/POP/DISPLAY directive trio in Phase 7's M4 list (line 69).

**D10. The EXTERNAL run-unit conformance cluster has no named workstream.** E.2 items 9/10/12/24 + E.3.3 item 20 + the EC-EXTERNAL family (spec 49138, 49148, 49164, 49312, 50277, 49428) — cross-unit runtime conformance semantics riding exactly the Phase-1 cross-assembly EXTERNAL machinery; VCR rows 15/16/18/31/63 exist but no phase names them.
*Change* (line 69): add to the Phase 7 named list "EXTERNAL run-unit conformance (E.2 items 9/10/12/24 + the EC-EXTERNAL family), with a design touchpoint on the Phase-1 cross-assembly EXTERNAL machinery".

### PARTIAL claim corrections (detail fixes; substance holds)

**D11. >>PROPAGATE is mis-editioned as a 2023 addition.** §7.3.21 exists (4803, chair-verified) but PROPAGATE has 28 hits spec-wide and **zero inside Annex E** (49011–50350, chair-verified), and it is absent from E.2 item 5's 9-entry added-directive-words list (49084–49100) — it was already in the 2014 edition.
*Change* (line 69): "PERFORM...WHEN exception-checking + >>PROPAGATE (§7.3.21)," → "PERFORM...WHEN exception-checking (annex-attested 2023: E.3 item 36 @50316, E.2 item 19 @49248); **>>PROPAGATE (§7.3.21) moves to the ≤2014 surface (zero Annex E hits — implement in Phase 6, not as an M4 delta)**". Amend ledger row 152 to match.

**D12. FLAG-02/FLAG-14 are mischaracterized in the Phase-7 behavior-row framing.** They exist (§7.3.14 @4359, §7.3.15 @4439, chair-verified) but flag **edition-incompatible behavior** (2002→2014 and 2014→2023 respectively, 4364/4444), all options default OFF (4430, 4544); the implementation must merely *provide* the mechanism (A.1 items 79/80, 39453/39455). Obsolete-element flagging is the distinct §4.2.13 mechanism (2505–2511) over the F.2 list; archaic is §4.2.12 (2496–2502) over F.1. FLAG-02 is itself obsolete (4366, F.2 item 1 @50395).
*Change* (line 69): where the behavior-row wave lists "(I-O status, VALUE semantics, FLAG-02/FLAG-14 directives)", add "— FLAG-02/FLAG-14 = user-invoked incompatible-behavior directives, options default off, mechanism-provision required (A.1 79/80); obsolete flagging = the separate §4.2.13 mechanism, archaic = §4.2.12 — enumerate the four archaic rows (VCR 89/90/126/127: EXIT PROGRAM, NEXT SENTENCE) with their own sub-code in the 0903 band".

**D13. MOVE ALL-digit rule citation: Syntax rule 5, not GR5; integer receiver only.** The permission is §14.9.25 **Syntax** rule 5 (28810: "…to an **integer** numeric item"; obsolete NOTE follows; F.2 item 2 @50397; survived the 2014→2023 removals per E.2 item 1 @49026). MOVE GR5 is the de-editing rule (28903).
*Change*: wherever Phase 2's "the MOVE ALL-digit latent bug" row (line 45, ledger 135) carries the citation, cite "14.9.25 SR5" and state the receiver constraint "integer numeric item" (ALL-digits to non-integer numeric or numeric-edited is prohibited).

**D14. The M3-4 catchall (Phase 6, line 65; ledger 150) has three wording defects.** (a) "function/method pointers" — METHOD-POINTER has **0 hits** in the spec (chair-verified); the usages are FUNCTION-POINTER / PROGRAM-POINTER (22685–22686, GRs 22945/22960). (b) "increased limits" has no 2023 anchor — §4.2.15 (2523–2525) delegates all limits to the implementor; no limits table exists to increase. (c) PROGRAM-POINTER (2002-era GR) is named in neither Phase 4(b) nor Phase 6.
*Change* (line 65): "function/method pointers, increased limits," → "function/program pointers (FUNCTION-POINTER here, prototype-dependent; **USAGE PROGRAM-POINTER named in Phase 4(b)** with a 2002 edge), ~~increased limits~~ (no anchor in the 2023 text — §4.2.15 delegates limits; re-source or drop),". Also add the ANY LENGTH clause (§13.18.2 @17576, load-bearing in method-conformance rules 12177/12247/12335) to the Phase-3 OO spine or the Phase-4(c) prototypes row.

**D15. RETRY is not gated by A.4.7.** Chair-verified: the A.4.7 element list (40349–40363, items 1–7) contains sharing mode, record locking, LOCK MODE/SHARING clauses, OPEN SHARING, and EC-I-O-FILE-SHARING — **no RETRY**; RETRY is core §14.7.9 syntax (25199), listed in Annex A only as required implementor-defined items 165/166 (39679/39681).
*Change* (line 57): "(d) SHARING/LOCK/RETRY + line-sequential (contingent on the A.4.7 decision)" → "(d) SHARING/LOCK (A.4.7-gated) + RETRY (core §14.7.9 syntax, behavior reachable only with A.4.7 locking — not itself A.4.7-gated) + line-sequential (provisional 2002 edge) + UNLOCK named for auditability".

**D16. Blanket 2002 seed for IEEE float usages is likely a mis-edge.** Split the float_usage family: FLOAT-SHORT/LONG/EXTENDED (provisional 2002) vs FLOAT-BINARY-32/64/128 and FLOAT-DECIMAL-16/34 (provisional 2014, spec 22668–22671). Apply in the Phase-1 metadata scrub (line 34) alongside the TYPE IS fix.

### Minor completeness additions (one-line amendments)

- **DELETE FILE** (E.3.3 item 15 @50267; five new I-O statuses, item 35 @50314) → name in Phase 7 (needs file-I/O runtime work, not just a validator row).
- **CONTINUE timed pause** (item 14 @50265) + the EC-CONTINUE family (49426) → Phase 7 named list.
- **INSPECT BACKWARD** (item 34 @50312) and the **PICTURE EDITING phrase** (item 19 @50275) → name in Phase 7 so their engine-level surface (INSPECT scanner direction; PicInfo insertion machinery) is sized, not discovered mid-wave.
- **GOBACK 2023 status phrase** (item 32 @50308) → note in Phase 3's spine (line 52) that the BoundStopRun/BoundMethodReturn carrier must accommodate it (implement or explicitly defer to Phase 7).
- **A.4.10/A.4.11/A.4.12 disposition rows** → add to the decision-3 table (implement / already-implemented / already-implemented) and to the Phase-9 Cut-3 claimed-support checklist.
- **A.3 disposition sweep** (46 items @40064–40228; 4.2.6 requires a compile-time warning on unsupported syntactically-detectable elements @2437) → one-pass table attached to Phase 7.
- **4.2.10 extension-use warning** (2480) → note in decision 4's --permissive design that leniency/permissive acceptances emit it on the 09xx channel.
- **ALPHABET national/UCS-4/UTF-8/UTF-16 phrases** (§12.3.7, ~14109–14250 — mandatory core, NOT in the A.4.9 locale module per 40399) → fold into the Phase-4(a) row text.
- **Extraction hazard**: E.3.3 items 5 and 6 are mangled into `##` headers at 49479/50048 inside Unicode tables — annex-enumeration tooling must not rely on the numbered-item regex (43-of-45 silent undercount).

## 3. Confirmed load-bearing rulings (permanent citations)

- **XOR/EXCLUSIVE-OR are 2023** (Phase 2 W3 adjudication): body §8.7.6 @8980–8982 and §8.8.4.9 @10125; Annex E.2 item 25 @49320 (added reserved words, incl. @49330/@49344, justification @49348) + E.3.2 item 4 @49434. Both annex identifiers the roadmap cites are exact. Double-verified, unanimous.
- **TYPEDEF predates 2023** (the `is2023()` gate is provably wrong): exactly 33 spec hits; clause §13.18.58 @22557; **zero** hits inside Annex E (49011–50350). Double-verified, unanimous.
- **JSON/XML: 0 hits in the entire spec** (case-insensitive, chair re-verified today) — decision #2 (vendor-dialect, post-G8) and the M3 removal stand; the M3-3 "§14.9" citation is confirmed fictional.
- **Annex E scope = previous-edition (2014→2023) delta only**: E.1 @49019 — the foundation of every prior-edition adjudication and of the ratified decision-1 policy. All 81 annex items (E.2: 30, E.3.2: 6, E.3.3: 45) are individually cited by VCR rows — no homeless annex item.
- **Decision-3 module mapping is exact**: A.4.2–A.4.14 headings at 40247/40311/40330/40339/40344/40349/40372/40379/40408/40417/40485/40490/40497; **MCS via A.3** (asynchronous messaging @40080), A.4.3 = commit/rollback only.
- **A.4.10 lists exactly three optional OO items** (40408–40414: two multiple-inheritance entries + parametric polymorphism) — FACTORY/PROPERTY/INTERFACE-ID/universal references/EC-OO are mandatory; the Phase-3 XL re-rating and decision #7 stand.
- **Removal gates confirmed**: LABEL/VALUE OF/DATA RECORDS absent from FD formats (16212–16282); CALL ON OVERFLOW absent (26036/26071) and its removal annex-attested (E.2 item 1 @49030); EXIT METHOD/EXIT FUNCTION/CLOSE WITH LOCK are listed 2014→2023 deletions (49034/49036/49038, + file-status-38); STOP-literal absent (32224–32226); ALTER and no-operand GO TO absent (GO TO formats @27619–27625).
- **VALIDATE obsolete** (F.2 item 5 @50405) and the F.2 support-exception rule (@50380) + A.4.14 optionality make decision-3's documented non-support conformance-legal.
- **§4.2.7 documentation route** (2440–2447) confirms documented non-support of optional modules is conforming — decision-3's legal basis.
- **Fatal-challenge fix intact**: PERFORM...WHEN is genuinely annex-attested 2023 (E.3 item 36 @50316); the EXCEPTION-FILE/-N 2023 argument (50287/50296), the seven new 2023 intrinsics (49178–49186), boolean shift additions (49060), group SYNC/NO SIGN (49436/49438), and the RAISE identifier form (29739–29761) all confirmed.

## 4. Unverifiable-by-design (2002/2014 TEXT claims)

The in-repo authority is the 2023 edition only; Annex E covers only the 2014→2023 delta (49019). The following cannot be decided in-repo; the ratified decision-#1 provisional-marker policy **already covers each** unless noted:

- **Exact post-85 deletion editions** for LABEL/VALUE-OF/DATA-RECORDS, STOP-literal, ALTER, no-operand GO TO (proven ≤2014 removals by Annex E absence; the "2002" timing is uncontradicted but unprovable) — covered.
- **TYPEDEF / SAME AS / TYPE TO 2002 edges** — covered (roadmap already marks them provisional).
- **FORMATTED-\* dates**: "introduced in 2014" should read "present in 2014" (a 2002 introduction is not excluded) — covered, but Phase 5's family label should carry the marker.
- **The "2002 intrinsic set" membership**: only the two TEST-DATE/TEST-DAY complements have direct in-spec 2002 attribution (D.31.3.1 @48716) — mark the rest provisional; the pair may be marked firm.
- **Line-sequential 2002 edge** (proven ≤2014) — needs the marker actually applied in Phase 4(d) (see D15 wording).
- **IEEE float 2002-vs-2014 split** — needs the D16 family split, then markers.
- **RECEIVE/SEND "(re-)reserved" 1985 history** — the 2023-facing claim is fully confirmed (§8.9 entries @10645/10683; E.2 item 25 additions @49334/49336); the 1985 parenthetical is harmless history.

## 5. Completeness sweep results

**Swept**: Annex E in full (81 items enumerated across E.2/E.3.2/E.3.3, incl. the two extraction-mangled items; 81/81 cited by VCR rows — the Phase-8 blanket disposition is structurally complete); Annex A structure (A.1/A.2/A.3 with 46 items/A.4 with 13 modules); Annex F in full (2 archaic + 5 obsolete, every one with a flag row); conformance clause 4 in full (4.2.1–4.2.17); the spec's own catalogs by header enumeration — 51 statements (§14.9, 25553–33274), 94 intrinsics (§15, 34350–39063), 64 data-division clauses (§13.18, 17538–23499), ENV-division clauses (13840–15938) — each post-85 item matched against roadmap phases, the ledger, the VCR, the M2/M3 catalogs, and greenfield src where absence mattered. Statement-level result: **all 51 statements resolve to an owner or a ratified disposition**; every real hole is clause/expression-level, a consequence of the VCR being 2014→2023-only while the 2002/2014 catalogs carry recorded-but-rowless deferrals.

**Gaps worth a roadmap amendment, mapped to absorbing phases**:

| Gap | Phase | Defect |
|---|---|---|
| Phase-5 exit vs decision-3 locale non-support | 5 | D1 |
| DYNAMIC-LENGTH base feature unowned | 6 (+7 for the SET delta) | D2 |
| RW dual-use PRESENT WHEN/VARYING orphaned by VALIDATE ruling | 4(f) | D3 |
| Boolean operations (2002 base) | 4(a) | D4 |
| CONSTANT entry / CONSTANT RECORD | 6 (+ Phase-1 constructs row) | D5 |
| Concatenation '&' expressions | 4 | D6 |
| §4.2.16 seven-leg conformance document | 9 Cut 3 | D7 |
| §4.2.2 checking suboption | 3 / 4(c) | D8 |
| COBOL-WORDS mutability vs frozen word tables | 1 (design) + 7 | D9 |
| EXTERNAL conformance cluster | 7 | D10 |
| >>PROPAGATE re-editioned to ≤2014 | 6 (moved from 7) | D11 |
| Archaic-vs-obsolete flag split; FLAG directive framing | 7 + 9 Cut 3 | D12 |
| DELETE FILE, CONTINUE pause, INSPECT BACKWARD, PIC EDITING, A.3 sweep, extension-use warning | 7 | minors |
| GOBACK status carrier; ANY LENGTH; PROGRAM-POINTER; ALPHABET UTF phrases | 3 / 4 | D14 + minors |
| A.4.10/11/12 disposition rows | decision-3 table + 9 Cut 3 | minor |

**Bottom line**: the ratified spine, sequencing, and every owner decision survive validation; apply D1–D16 and the minor additions as a single roadmap-amendment commit before the Phase-1 registry freeze (D5, D9, D16 land inside Phase 1 itself), and the roadmap is fully ISO-grounded.