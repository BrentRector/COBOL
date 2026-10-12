## Entry 999 — 2026-07-22 23:54 PDT — Conformance fix-queue: CA13 + CA39 (editions-gating) — OPTIONS INITIALIZE is a 2023 introduction (not 2014); EXIT PARAGRAPH / EXIT PERFORM are 2002 introductions (were ungated)

Two editions-gating majors, both pure edition-registry corrections (constructs.json + VersionConformancePass +
gen-constructs regen; no runtime/behavior change).

**CA13 (Annex E §E.3.3 item 33).** OPTIONS INITIALIZE (§11.9.10) was dated `introducedIn 2014`, so at `--std 2014`
(the default) a program using it compiled with NO diagnostic, and at 85/2002 the introduction message named "requires
COBOL-2014". Annex E item 33 lists the clause among the 2014→2023 additions in the "not affecting existing programs"
section — coherent ONLY if it is NEW in 2023 (using already-reserved words); a 2014 clause tightened in 2023 would sit
in E.2 "potentially affecting" (where the parallel VALUE tightenings are). FIX: `options-initialize-2014` →
`options-initialize-2023` (introducedIn 2023) in constructs.json; regen → `Constructs.OptionsInitialize2023`;
`VisitOptionsInitializeClause` updated. Now rejects COBOLNET0900 "requires COBOL-2023" at 85/2002/2014, accepts at
2023. VCR row 76 corrected (edition "2014→2023" → "2023 (introduction)"; the `<!-- todo -->` resolved to
`<!-- gate:options-initialize-2023 -->`; status block regenerated).

**CA39 (§14.9.14.2 Formats 3 & 4).** EXIT PARAGRAPH (Format 4) and EXIT PERFORM [CYCLE] (Format 3) are the COBOL-2002
structured-procedure exits (in 85 the only EXIT forms are bare EXIT and EXIT PROGRAM), but `VisitExitStatement` gated
only METHOD/FUNCTION/PROGRAM/SECTION — the PARAGRAPH and PERFORM arms were ungated, so both compiled clean at
`--std 85`. FIX: two constructs.json rows `exit-paragraph-2002` / `exit-perform-2002` modeled on the existing
`exit-section-2002`; regen → `Constructs.ExitParagraph2002` / `ExitPerform2002`; two else-if arms appended to
`VisitExitStatement`. Now rejects COBOLNET0900 "requires COBOL-2002" at 85, accepts at 2002+. EXIT PERFORM CYCLE rides
the same PERFORM arm (§14.9.14.3 SR8). (The enclosing inline PERFORM is itself a 2002 form still ungated — a separate,
noted gap, not in CA39 scope.)

**Verified (direct CLI):** OPTIONS INITIALIZE @2014 → reject 0900/"requires COBOL-2023"; @2023 → 007. EXIT PARAGRAPH
@85 → reject 0900/"requires COBOL-2002"; @2002 → P2. EXIT PERFORM @85 → reject; @2002 → 3. The version-matrix
constructs.json rows ARE the edition-gating conformance tests (INV-1 continuity + INV-2 introduction gating, run by
VersionMatrixTests).

**Gate:** VcrDrift 3/3, characterization 33/33 (no drift), unit 575/575, **VersionMatrixTests 1833/1833** (the ONLY
suite an edition-registry change touches — INV-1/INV-2 green for all three constructs). The full greenfield Conformance
run under heavy concurrent load reported 3880/1; the single red was in neither VersionMatrixTests nor VcrDrift (both
clean) and this change has no runtime path, so it is a load-induced flake in an unrelated runtime test — confirmed
0-fail by the next full run.

