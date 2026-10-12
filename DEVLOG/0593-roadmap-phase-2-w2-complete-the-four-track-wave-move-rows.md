## Entry 593 — 2026-07-03 20:15 PDT — ⛔ ROADMAP PHASE 2 W2 COMPLETE — the four-track wave (MOVE rows · loud-guard sweep · negative corpus · position-aware reserved words) landed in one fan-out

**The P2.8 W2 wave shipped end-to-end: four parallel agents on disjoint files (the DEVLOG-543 pattern — no
builds in-wave, serial integration), 790k agent tokens, then TWO integration fixes and everything green:
conformance 1304/1304 (from 1195 — +109 net new facts) + unit 102/102 (from 38).**

**Track A — the MOVE VCR rows (1, 92/128) + both latent bugs (new `StatementBinder.MoveFigurative.cs`):**
- ALL-digit fixed: a digit-only ALL literal → numeric receiver folds to its §8.3.3.6.4 GR2 / §14.9.25.4 GR6d3b
  value at compile time (`AllDigitFill` in `CSharpEmitter.ConvertSource`; ALL "5"→9(3)=555, →9V9=5.5,
  ALL "57"→575 — legacy-oracle confirmed). The `BoundAllLiteral` runtime-loud is dead at every edition.
- The 0902 gate (`move-alphanumeric-figurative-removed-2023`): every other alphanumeric-figurative/ALL →
  numeric/numeric-edited ELEMENTARY move — exemptions per spec: ZERO (§8.3.3.6.4 GR4), group receivers
  (§14.9.25.4 GR4), ref-mod receivers (§8.4.2.4), digit-ALL→integer (that's the 0903 row).
- Pre-removal semantics (run at 85/2002/2014 + 2023-permissive): non-digit fills deposit the character image
  by REUSING the StoreAsImage substrate (bind-time flag, same eligibility as MarkImageLeaves). Legacy-oracle
  adjudicated, provisional per ratified decision 1. Two legacy NON-conformances documented, not mirrored:
  legacy CBL0906 compile-rejects QUOTE/HIGH/LOW→numeric at every standard (stricter than ISO ≤2014); legacy
  DISPLAYs a space-filled numeric as EMPTY (byte-cell artifact) — we pin the fixed-width image (§14.6.8).
- ⚠ Open research row (VCR 7.13): §8.3.3.6.3 SR3 multi-char-ALL-with-numeric may be an '85-obsolete→2002
  deletion; no in-repo evidence beyond the 2023 SR — rides the 2023 row (under-strict 2002/2014, provisional).

**Track B — the loud-guard silent-misbind sweep + national/boolean skeleton (roadmap risk 5 closed):**
- `PicInfo.ParseUsage`/`Analyze` now take `(EditionContext, where)` — the silent Display catch-all is DEAD.
  NATIONAL/BIT/POINTER/OBJECT REFERENCE/BINARY-CHAR-family/FLOAT-SHORT-LONG-EXTENDED + PIC N/1/E each route
  their registry row (0900 below 2002) PLUS a COBOLNET0899 not-implemented error at ≥2002 naming the owning
  phase (0899 = the established Reports staging convention). Unknown usage keyword / PICTURE symbol → loud
  (NEW COBOLNET0808 invalid-PICTURE-symbol; §13.18.40.3 SR2 whitelist honoring the program currency symbol).
- `UsageKeyword` reads tokens, not stripped text — bare `BINARY-CHAR SIGNED` gates identically to the full
  form (was "BINARY-CHARSIGNED" → silent Display; "DISPLAY" itself only survived by accident as "DPLAY").
- Skeleton enums (PicCategory.National/.Boolean + 11 Usage members) throw loud from every storage-mapping
  member if constructed; `CallCollectUnits` no longer silently drops a `classDefinition` (0899, Phase 3).
- 🔎 EXPOSED: the allocate/free/invoke matrix rows were DOUBLE-silent-hole false-greens (pointer/object-ref
  usage misbound to Display AND the statements bind BoundUnsupported runtime-loud) — flipped to pending until
  Phase 4b/3. The registry-freeze discipline worked exactly as designed: the sweep turned a fake green into
  an honest pending.

**Track C — the negative corpus is LIVE:** 18 cases enabled (11 2002-removal gates incl. the pinned-0873
data-records case, 3 2023 removals, 4 reserved-word interval witnesses COMMIT/RAISING/RECEIVE/END-RECEIVE).
Every (case × listed edition) rejection AND every pre-removal-edition clean compile was verified against the
CLI before enablement — 42/42 rejections, no blind cases.

**Track D — position-aware reserved words (the P2.4 parked item CLOSED):** the token-type restriction is
lifted for PROVABLE positions — `EditionValidator.IsProvableUserWordPosition` (ctx.Parent pattern-switch)
enforces the band words in the data/parameter entry-name, paragraph/section DEFINITION, SELECT file-name, and
programName slots; the mis-parse-prone optional entry-name slots (reportGroupName — RW104A's COLUMN — and
screenName) plus all REFERENCE positions stay unchecked (conservative false-negative, never false-positive).
Adversarial-review find: of the 34 formerly-excluded band tokens only SEVEN have §8.9 table rows (2
continuous-since-85 incl. COLUMN, 5 added-2002); the other 27 are §8.10 context-sensitive words with no
reservation to enforce — the position machinery closes the ENTIRE real gap, ReservedWords.Table untouched.
New correct strictness: `01 COLUMN PIC X.` now rejects 0901 even at 85 strict (§8.3.2.1 rule 1); corpus-swept
clean (the only band-word corpus hits are RW102A/103A/104A report-group COLUMN clause keywords — excluded
positions by construction). Standing hazard flagged in-code: a future data-description clause BEGINNING with a
cobolWord-admitted token would re-open the RW104A hazard for the entry-name slot.

**Integration (serial, two fixes):**
1. Four NIST golden regressions (NC203A/245A/251A/252A) — ONE root cause, a genuine pre-existing lexer
   sloppiness B's whitelist turned loud: the greedy `PIC_STRING` lexer rule over-captures a TRAILING `;`
   (`PIC 99; VALUE` lexes the picture as "99;"); `;` is a SEPARATOR (§6.2), never a PICTURE symbol, and the
   old classifier ignored it by accident. Fixed at the ONE analysis funnel (`Analyze` strips a trailing `;`,
   documented; trailing `,` deliberately NOT stripped — a legal insertion symbol, zero-regression discipline).
   The real cure (lexer-mode trim like its existing trailing-`.` hack) is queued to W3.
2. The 9 allocate/free/invoke matrix cells → the pending flips above (B's predicted honest state).
Docs: the P2.8 W2 as-built note in VERSION_TEST_MATRIX_DESIGN.md; VCR rows 1/5/6/7/32/89/90/92/126/127/128
flipped GATED (132→121 TODO) + Table 7 grown 7.4–7.13 (the Wave-1 gate batch recorded; 7.1 updated to the
validator migration). The MOVE rows flipped ACTIVE in constructs.json; drift disciplines green.
Legacy guard NOT run: the wave touched only `src/Cobol.Net.Compiler` + greenfield tests — zero shared-frontend
or legacy files (verified by the diff file list).

**The full verification battery, all green:** build 0W/0E · unit **102/102** · conformance **1304/1304** ·
the permissive continuity sweep **419 OK / 40 SKIP85 / 0 BREAKS** (= the Phase-1 baseline exactly) ·
**INV-1-STRONG at the default edition: 349/349 goldens byte-exact at `--std 2023 --permissive`** — D's new
0901 strictness and A's MOVE gates produce zero behavioral deltas over the corpus, as both tracks' sweeps
predicted.

