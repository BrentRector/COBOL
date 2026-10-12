## Entry 679 — 2026-07-08 00:38 PDT — Rearchitecture PHASE 02 step 7 — the parse-layer 0900 flows through the ONE Check funnel (forward stamping tried, adversarially disproved, R1-mitigation landed)

Step 7 was scoped as forward `{Gate(edition, GateId)}?` predicate stamping to REPLACE the `EditionGateHints`
reverse-engineering table. I built it in full — `EditionInfo Edition` as the single dialect-year source on
`CobolParserCoreBase`, `Gate(introducedIn, GateId)` recording the furthest rejection, a `Reset()`/`Consume()`
reset pair, 92 stamped grammar predicates — and it passed the greenfield battery + the FULL legacy guard, and my
spot-checks (keyword-led ALLOCATE/FREE/INVOKE/DELETE FILE/BASED, the class/interface unit, JSON/XML) all named the
right construct.

**Then the adversarial review (wf_b73eff97) earned its cost — it found a HIGH regression I had missed, and I
reproduced every case.** Forward stamping is fundamentally unsound for this job: ANTLR evaluates a hoisted
`{Gate}?` predicate SPECULATIVELY — at the stuck token, during a FAILING prediction/recovery — so an ordinary typo
records a gate for a construct the program never used and emits a confidently-wrong edition diagnostic:
`IF W = .` (missing right operand) at 85 → "the boolean operators … requires COBOL-2002"; `01 W PIC X(4) JUSTIFIED
)` → "BASED clause requires COBOL-2002" (and "OCCURS DYNAMIC requires COBOL-2014" at 2002); `SUPPRESS PRINTING.` →
"an interface definition requires COBOL-2002". A forward stamp can never reliably mean "the user WROTE this
construct" (my `Consume()`-reset killed cross-parse pollution and a JSON/XML guard patched one case, but the
general typo-at-the-stuck-token leak is intrinsic to hoisted predicates). This is exactly the doc's anticipated
risk **R1**. LESSON (transparency): my own empirical testing exercised keyword-led constructs + JSON but NOT the
typo-at-the-stuck-token case — the single most common syntax error. For any speculative-predicate-driven
diagnostic, that case is the one to test first.

**Decision (owner-directed toward decades/commercial quality): abandon forward stamping as the identifier; land
the design doc's own R1-mitigation.** The message duplication (P1/P7 — `EditionGateHints` hand-copying
`Display`/`IntroducedIn`/`Citation` that already live in `ConstructDialectStatus`) is the phase's real target, and
it is removed WITHOUT the leaky stamp:
- **Reverted the grammar stamping** (back to `{isXXXX()}?` — a clean `git checkout`, nothing was committed).
- **Kept the genuine win:** `CobolParserCoreBase` now single-sources the dialect year through `EditionInfo Edition`
  (`DialectLevel` is a shim; `is85/2002/2014/2023` delegate to `Edition.Has`), ending the pre-P2 triple-sourcing
  (P5). The `Gate`/`LastRejectedGate`/`Reset`/`Consume` machinery is gone.
- **Thinned `EditionGateHints` to a pure signature→row-id recognizer:** the `(Display, IntroducedIn, Citation,
  RowId)` records are replaced by the generated `Constructs.*` id consts (compile-checked, single-sourced from
  `constructs.json`); `Recognize` returns the construct id (or the fixed JSON/XML vendor disposition). The
  hand-copied metadata is DELETED — it now comes from the registry row. The introduction guard is dropped because
  `ConstructRegistry.Check` no-ops when the construct is available at the targeted edition.
- **`CobolErrorStrategy` renders `COBOLNET0900` through the ONE `ConstructRegistry.Check` funnel** from the
  recognized id (display / introduction edition / ISO citation all the registry row's); JSON/XML → `COBOL0313`
  directly. `CaptureSink` reuses the funnel's exact text.

Why this is correct where the stamp was not: the signature match keys off the construct's OWN tokens actually being
present (a real ALLOCATE keyword, a real B_AND operator, a CLASS-ID), so a typo matches nothing and gets a neutral
parse error. Verified end to end: the review's four false-positive programs now emit neutral `COBOL0001`/`COBOL0307`
at 85 AND 2002; every real gate (ALLOCATE/FREE/INVOKE@85, DELETE FILE@2014, CLASS-ID@85 [names *a class
definition*, not interface], BASED@85, boolean-COMPUTE@85, XOR@2014, OCCURS DYNAMIC@2002) names the right construct
via `Check`; JSON/XML → `COBOL0313`. **`EditionGateDiagnosticTests` stays green unchanged** — it asserts substrings
(`COBOLNET0900`, `requires COBOL-YYYY`, `targeting COBOL-YYYY`) the `Check` template also produces. Battery:
**conformance 2055 · unit 224 · FULL legacy guard NIST 353 MATCH**, 0 regressions (the grammar is byte-identical to
HEAD; the `EditionInfo` single-sourcing is behavior-neutral).

**Honest scope note:** P1/P7 (metadata duplication) are fixed; **P3 (eliminate the reverse signatures via a forward
gate) is NOT achievable** — the review disproved it. The signatures are retained as the reliable recognizer.
**Decades north-star (documented in `PHASE-02-…md` + `DESIGN-edition-framework.md`): move edition gating to BIND
time** — parse the construct unconditionally, gate at the recognition point through the same `Check` funnel (the
exact pattern step 6 used for the five inline gates). That removes both the parse-time rejection predicates AND the
signatures, leaving only a small "reservation-word" residue (context-sensitive keywords — XOR, the boolean
operators, the SHARING/LOCK/RETRY family — that are user words below their edition, so their parse predicate is
load-bearing). That migration is a deliberate follow-on track, not a hasty mid-step rewrite.

