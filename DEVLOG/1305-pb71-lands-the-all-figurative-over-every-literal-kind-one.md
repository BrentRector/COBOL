## Entry 1305 — 2026-08-18 04:24 PDT — PB71 lands: the ALL figurative over every literal kind — one grammar arm, one classifier, one constructor — and the PB84–PB88 battery is green

**Battery first (PB84–PB88 batch, tree `d35b84d5`):** Conformance **4645/4645** · Unit **4192/4192** ·
Characterization 33/33 · NIST-legacy 353 MATCH / 0 audit-clean · GnuCOBOL differential 1323 cases, **0 per-case
flips**. ALL GREEN — the NC250A red of battery #5 is closed.

**PB71 — three root sites, as the 2026-08-09 sweep said, and a fourth it could not see.** (a) `ALL N"Q"` had no
grammar arm (COBOL0001 in every position). (b) `ALL B"1"` HAD a grammar arm and `ExpressionBinder.FigurativeOperand`
tested STRINGLIT and HEXLIT only — so `MOVE ALL B"1" TO BR` parsed and DIED AT RUN TIME with
NotImplemented, one arm below the remark that records the identical PB4 defect for `ALL X"41"`. (c)
`DataBinder.ValidateValueCategory` classified a VALUE literal from `raw[0]`/`raw[1]` and refused `NX"…"` / `BX"…"`
(§8.3.3.5.2 / §8.3.3.4.2 Format 2 — the SAME class as N"…" / B"…") and every ALL literal. (d) Found landing it:
§8.3.3.6.3 SR2 says literal-1 "may be a concatenation expression" — `MOVE ALL "AB" & "C" TO AR` was COBOLNET1541
(the parser read a concatenation whose first operand is `ALL "AB"`, illegal by §8.8.3.2 SR1) where the standard
means ALL over "ABC"; and `MOVE ALL "" TO AR` (SR2: "neither a figurative constant nor a zero-length literal")
compiled and stored spaces.

- **One grammar arm.** `figurativeConstant`'s Format-6 arm is `ALL allLiteral` —
  `allLiteralOperand (AMPERSAND allLiteralOperand)*` over STRINGLIT / HEXLIT / NATLIT / BOOLLIT. `nonNumericLiteral`
  lists the figurative BEFORE `concatenationExpression`: `ALL "A" & "B"` is a true ANTLR ambiguity between the legal
  reading (ALL over "AB") and the illegal one (a concatenation with an ALL operand), and the tie goes to the lower
  alternative; `"X" & ALL "A"` still parses as a concatenation and `ConcatFolder` still rejects it. Every consumer
  asks `fig.allLiteral()` — the binder, `ConcatFolder.ClassOf`, the boolean channel (`ConditionBinder`, twice),
  INSPECT's SR3 screen (which tested STRINGLIT only and let `ALL X"…"` / `ALL N"…"` through), and the LEGACY
  oracle's three readers (behavior-preserving: the first operand, the single literal they always read).
- **One classifier, one constructor.** `CobolLiteral.ClassOf(raw)` (N/NX national, B/BX boolean, plain / X
  alphanumeric — front-end side, an enum) and `BoundAllLiteral.Of(raws)` (decoded and concatenated per §8.8.3.3
  GR2, the category from literal-1's class). INITIALIZE REPLACING, the Report Writer VALUE and the UNSTRING
  delimiter producers build through it, so a national or boolean literal-1 keeps its category everywhere.
- **The VALUE path.** `ValidateValueCategory` classifies through `ClassOf` (a plain literal, or `AllLiteralRaw`'s
  literal-1); a concatenated ALL literal-1 folds to ONE re-quoted literal (`ConcatFolder.FoldAll`) in `ExtractValue`
  / `RawValueOperandText` before the raw-text pipeline sees it; and `CobolLiteral.SplitLiteral` now requires ONE
  well-formed literal — it answered "a literal" for `"A"&"B"` (first and last character only), which `Decode` then
  turned into `A"&"B`.
- **The version pass.** `VisitFigurativeConstant` — the tokens are children of `figurativeConstant`, not of
  `nonNumericLiteral`, so `VisitNonNumericLiteral` never saw them: a national / boolean literal-1 is the COBOL-2002
  introduction (COBOLNET0900 below 2002 — `ALL B"1"` compiled at --std 85), the `&` is the concat-operator
  introduction, the §8.3.3 hexadecimal grouping applies, SR2's zero-length is **COBOLNET1648** (not reported over a
  malformed hexadecimal literal-1, which is already reported), and a mixed-class concatenation is COBOLNET1540.

**Measured** (golden `pb71_all_national_boolean_figurative`, 18 rows): `ALL N"Q"` / `ALL NX"0051"` → N"QQQ";
`ALL B"10"` → 101010, `ALL BX"5"` → 010101; `IF BR = ALL B"1"` and `IF NR = ALL N"Q"` (the relation operand
`StatementValidation` was waiting for); VALUE `ALL N"Z"`, `NX"00410042"`, `ALL B"1"`, `BX"A"`, `ALL BX"5"`; a
length-unspecified DISPLAY takes the literal once (GR3c); the concatenated forms `ALL "AB" & "C"` → ABCABCA,
`ALL N"Q" & N"R"` → QRQRQ, `ALL B"1" & B"0"` → 10101, in a VALUE (ABABA / QRQR) and a relation. Negatives: the two
below-2002 gates, `VALUE ALL "AB"` on a national item (0898 — an alphanumeric literal-1), the zero-length literal-1
(1648), the mixed-class concatenation (1540). Verdicts SR-8.3.3.6.3-2 + FMT-8.3.3.6.2 → CONFORMS (GAP 4042 → 4040).
The note's severity is corrected (crashes, MAJOR).

**A gate mistake, recorded.** The first wave-local run reported the new golden RED for a diagnostic the compiler no
longer emits: the two follow-up fixes after the solution build had been built through the CLI PROJECT only, so the
Conformance test bin held the previous compiler — exactly `feedback_fresh_build_before_no_build_test`. Solution
build, re-run, green.

**Gates (wave-local).** Solution build · Characterization 33/33 · Conformance
`Corpus|Negative|Nist|Figurative|Concat|Literal|Move|Value|Inspect|String|Boolean|National|SpecTraceability|VersionMatrix|Condition|Evaluate|Initialize|Report`
**3524/3525** on the stale bin (the one red above), then `Corpus|Negative|Value|Figurative|SpecTraceability`
**758/758** on the fresh one · Unit
`SpecTraceabilityInventory|Concat|Literal|Figurative|Grammar|Parser|Registry|Diagnostic|Manifest|Guard|Value|Bool|National`
**1334/1334** · CLI probes. Docs: DESIGN-frontend-grammar.md §3.3d (the arm, the alternative order, the one
classifier), DIAGNOSTICS.md regenerated (COBOLNET1648), kb/Work PB71 → landed, plan §0. Next: PB65's open members.


