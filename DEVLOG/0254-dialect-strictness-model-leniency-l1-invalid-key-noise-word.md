## Entry 254 — Dialect/strictness model + leniency L1 (INVALID KEY noise word) → RL105A/RL108A

**Design first** (`docs/dialect-strictness.md`). The remaining RL/IX progress is gated not by missing
features but by a handful of *non-conformant* constructs in the CCVS suite — chiefly the INVALID KEY
phrase written without the required `KEY` keyword (`REWRITE rec INVALID GO TO …`). Verified in the
upstream master `newcob.val`: `INVALID KEY` appears 1,490× vs `INVALID`-without-`KEY` 10× — a ~0.7%
errata rate the 1980s/90s validating compilers tolerated (they treated `KEY` as optional noise), which
is why the suite still "passed." So this is a *dialect/strictness* question, orthogonal to the
"support latest spec" (feature/version) goal.

The model: two axes. **Version** (`--standard cobol85…cobol2023` → `DialectMode`, already scaffolded and
threaded through Binder/Lowering) selects which *features* are legal — additive. **Strictness** selects
how tolerant of non-conformant syntax. CCVS leniencies live on the strictness axis and must never leak
into a named-strict mode. Pattern (mirrors GnuCOBOL `-std`): the grammar parses the *permissive
superset*; a centralized post-parse check diagnoses the lenient form under named-strict modes and
accepts it under `Default`. Discipline rule: **every leniency is dialect-gated from the moment it is
added** — never an unconditional grammar relaxation.

**Implementation (L1).**
- Grammar (`CobolIO.g4`): `INVALID KEY?` in all five INVALID KEY phrases (read/write/rewrite/delete/start).
  `INVALID` is a reserved word, so making `KEY` optional after it is unambiguous (low masking risk).
- New `DialectStrictnessChecks.CheckInvalidKeyNoiseWord` (single home for the strictness axis): counts
  direct `INVALID` vs `KEY` tokens in the phrase; if a `KEY` was dropped, reports `CBL3611` (error) under
  `Dialect >= StrictCobol85`, or `CBL3612` (warning) under `Default` when `WarnNonStandard`. Called from
  the five `FileIoBinder` phrase sites. Mirrors the existing CBL3601/3602 ALTER dialect-gate pattern.
- CLI: `--nist` implies `--standard default` (permissive) unless an explicit `--standard` is given, so
  the CCVS suite's documented leniencies are accepted; `default` added as an explicit `--standard` value.

Verified: RL109A/RL206A/RL207A now compile under `--nist`; under `--standard cobol2023` RL109A is
rejected with the targeted `CBL3611` citing COBOL-2023 — exactly the two-answers-one-source behavior a
multi-standard compiler needs. Re-surveying RL, COMPILE_FAIL dropped 12→5; three became CLEAN. Of those,
**RL105A** (creates + verifies three relative files in one run) and **RL108A** (TF061 create/process/
verify bundle) are self-contained and deterministic (0 FAIL* across repeated standalone runs) →
baselined, listed ahead of the TF021 chains (which re-create the file). RL207A was **not** baselined:
it consumes RL206A's output and RL206A still has 22 genuine FAIL* (a dependent pass on a failing
producer is not an honest baseline).

**NIST baselines 201 → 203** (94 NC + 42 IF + 12 SM + 4 IC + 39 SQ + **11 RL** + 1 IX). Full guard ALL
GREEN: 1000 unit / 348 integration / 203 NIST, 0 regressions. Deferred leniencies L2/L3 (no-KEY
`RELATIVE`/`RECORD` — data-name-anchored, higher masking risk, and need indexed/relative runtime behind
them) and L4 (`USE … ERROR` without `STANDARD`) are catalogued in the doc's registry for when they're
tackled.

