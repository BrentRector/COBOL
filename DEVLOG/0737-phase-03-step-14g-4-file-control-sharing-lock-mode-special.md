## Entry 737 — 2026-07-10 00:00 PDT — PHASE-03 Step 14g.4: file-control SHARING/LOCK-MODE + SPECIAL-NAMES FOR + PD RETURNING/RAISING → parse-arm

The fourth 14g commit: seven COBOL-2002 introduction gates on config / file-control / PD-header clauses move from
their bind-time Checks into the `VersionConformancePass` parse-arm, ALL on recognition. The 14g.3 shared-rule lesson
was applied UP FRONT this time (recon-first), and it caught a real hazard before any code landed.

- **SELECT SHARING (§12.4.5.15) + LOCK MODE (§12.4.5.9)** — `VisitSharingClause` / `VisitLockModeClause`. ⚠ The recon
  classified these BOUND-arm ("key on FileModel.Sharing/LockMode"), but that is the SAME inverted rationale the 14g.2
  review corrected for TYPEDEF: a bound-arm gate keyed on the resolved FileModel would DROP the 0900 if the file is
  discarded on a SELECT error, whereas the binder fired on the clause's PRESENCE in the file-control loop. So
  recognition is byte-exact and drop-proof — reclassified bound→parse. (SHARING is a dual-site construct: its OPEN
  SHARING twin already gates parse-arm from `VisitOpenStatement`.)
- **SPECIAL-NAMES … FOR ALPHANUMERIC/NATIONAL (§12.3.7)** — three sites (ALPHABET / CLASS / SYMBOLIC CHARACTERS),
  `VisitAlphabetClause` / `VisitClassDefinitionClause` / `VisitSymbolicCharactersClause`, each firing on the `FOR` token
  (matching the former `alpha.FOR()`/`cd.FOR()`/`sc.FOR()` checks), one constructId + where-string.
- **PROCEDURE DIVISION RETURNING (§14.2) + RAISING (§14.2.2)** — `VisitReturningClause` / `VisitRaisingClause`, guarded
  by `InMethodDefinition`. **The shared-rule hazard, caught in recon:** the `procedureDivision` grammar rule is SHARED
  by program and method PDs, but the former gate (`CallBindLinkage`) ran for PROGRAM units only (a method's RETURNING
  binds through `OoBindMethodData`, ungated) — so a naive parse-arm walk would have over-fired on method RETURNING.

**Scope-union verified** (why parse-arm is byte-exact despite per-scope binders): every program unit (top-level AND
nested — `CallCollectUnits` recurses `nestedProgram`), every class, and every factory gets its OWN `DataBinder` whose
`BindDeclarations` runs `SwitchBindSpecialNames` + `BindFileControl` + `CallBindLinkage`; the parse-arm's single
whole-tree walk equals the UNION of those per-scope binder walks. Confirmed the SPECIAL-NAMES / file-control /
returning clause rules are NOT shared with any other section (the 14g.3 check).

Byte-neutral: characterization 32 byte-exact. The seven gates now reference from EXACTLY the pass + the `GateId`
registry mapping; zero binder references. The `.g4` edits (CobolIO.g4 + CobolParserCore.g4 comments) are comment-only.

New `ConfigPdEditionTests` (Conformance, 8 exact-count witnesses): one 0900 per SHARING/LOCK-MODE/ALPHABET-FOR/
program-RETURNING/program-RAISING below 2002 (zero at 2002); and the load-bearing `MethodReturning_At85_NotGated` —
a method's RETURNING does NOT gate (the InMethodDefinition guard).

Battery: greenfield conformance **3147** (3139 + 8) · unit **223** · characterization **32** · INV-1-strong @ 2023
permissive **349/349** · FULL legacy guard **NIST 353 MATCH / 0 regressions** (legacy unit 1196 · integration 608).
RESUME AT Step 14g.5 (FUNCTION-PROTOTYPE + REPOSITORY + skeleton E/national-edited).

