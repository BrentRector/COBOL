## Entry 1306 — 2026-08-18 05:20 PDT — PB65 lands: the last five members — a mixed selection list keeps its content, REPOSITORY rule 5, the boolean-expression argument, LEAP-SECOND ON, and two Annex A.1 rows written from measurement

**PB65 was a bag of single-site chokepoints** (34 inventory rows; seven families landed 2026-08-09) and five members
were still open. Each was fixed at its one site, its siblings swept, and every row it owns is now OK.

- **RV-15.59.4-1 D2 — a MIXED selection list delivers the selected argument's CONTENT.** `FUNCTION MAX(F1 N1)`
  with F1 FLOAT-LONG 1.0 and N1 PIC 9(18) 999999999999999999 returned **13**: `AnyRealArgument` routed the whole
  call through binary64 (`MaxReal(params double[])` → `FromDouble` at scale 9 → the modular store); `MAX(D9 F1)`
  corrupted the last digit of a 9(9)V9(9); `MEDIAN(F1 N1 N2)` returned 0. §15.59.4 r1 / §15.63.4 r1 / §15.61.4 r1
  name the CONTENT of an argument (MEDIAN's odd-count equivalent expression is `(argument-a)` — the middle
  argument itself), so §15.4.1's latitude never reaches the value. A mixed list now evaluates on the SDIDI carrier
  under native too (`RenderNum` → the `RenderDec` bodies; a 38-digit fixed argument exactly, the float through
  §8.8.1.5.1, an exact compare, one landing at the receiver); an all-float list keeps its double; the arithmetic
  statistical family (SUM/MEAN/RANGE/MIDRANGE — genuine equivalent arithmetic expressions) keeps the D16 float
  lane. Golden `pb65_selection_mixed_carriers` (13 rows).
- **FMT-15.43.2 / FMT-15.58.2 — §8.3.2.1 rule 5, written once.** The REPOSITORY sets were filled and consulted
  by NOTHING at declaration time, and `KeywordOmittedFunction` substituted a hand-written "the data item wins"
  precedence for the standard's prohibition — so `REPOSITORY. FUNCTION HIGHEST-ALGEBRAIC INTRINSIC.` +
  `05 HIGHEST-ALGEBRAIC PIC S999 OCCURS 3` compiled clean and `HIGHEST-ALGEBRAIC(A1)` silently read the table
  where §15.43.4 requires +999. `DataBinder.ScreenRepositoryIntrinsicName` is THE screen — a data-name, a
  condition-name, an index-name, a file-name, a paragraph or section name — **COBOLNET1649**; the FUNCTION-less
  reference is the function whenever the REPOSITORY names it, and an un-declared intrinsic name stays the legal
  user word rule 5 permits (`SQRT` as a table, `MOD` as an item — the golden's control). Golden
  `pb65_repository_r5_intrinsic_names`, negatives `pb65-repository-{intrinsic-name-as-data-name,
  all-intrinsic-name-as-user-word}`.
- **FMT-15.45.2 — a boolean EXPRESSION is an intrinsic argument.** `INTEGER-OF-BOOLEAN(BIT-A B-AND BIT-B)` was
  COBOLNET1639 "B-AND is not defined" plus a false arity error: `functionArgument` had no `booleanExpression`
  alternative. It has one now, behind the ARGUMENT-scoped `boolArgAhead()` predicate — the condition-scoped
  `boolExprAhead()` would have read the B-AND that FOLLOWS `COMPUTE BR = FUNCTION BOOLEAN-OF-INTEGER(5, 8) B-AND
  BB` as belonging to the numeric argument 5; the argument scan stops at a depth-0 comma or the list's `)`. The
  operand is a `BoundBoolOperand` of class boolean, imaged as its '0'/'1' string through the ONE `BooleanRenderer`
  (`OperandText.AsString`'s entry, which has the per-unit renderer); LENGTH / BYTE-LENGTH name it correctly when
  they refuse it (they said "a numeric literal"). The lexer's boolean literal admits ZERO length (`[01]*` —
  §8.3.3.4.4 GR4; INTEGER-OF-BOOLEAN(B"") = 0, VALUE B"" fills a PIC 1(4) with 0000). A bit GROUP argument waits
  on GROUP-USAGE (PB79). Golden `pb65_boolean_expression_argument` (8 rows).
- **AR-15.79.3-4 — `>>LEAP-SECOND ON` reaches the date/time family.** The directive was consumed and DISCARDED
  (`ConditionalCompilationProcessor`'s known-ignored set), so `SECONDS-FROM-FORMATTED-TIME("hhmmss", "235960")`
  answered 0 — and killed the run unit under EC-ARGUMENT-FUNCTION checking — where §15.3.3.3 makes 60 a legal
  seconds subfield under ON and §15.79.4 requires 86,400. `LeapSecondDirectiveProcessor` (a line-count-preserving
  stage like TURN; SR1 "not within a compilation unit" and the operand syntax are **COBOLNET1650**; ON is an
  optional word; below 2002 the introduction gate — construct `leap-second-directive-2002`) → `Frontend.LeapSecondOn`
  → the ONE `DirectiveResults` record `Bind` now takes (the fifth positional directive parameter was the
  growing-list shape) → `BindSession` / `DataBinder.LeapSecond` → the renderer's `LeapSecondFlag` — a trailing
  `leapSecond: true` argument to SECONDS-FROM-FORMATTED-TIME, TEST-FORMATTED-DATETIME, INTEGER-OF-FORMATTED-DATE,
  FORMATTED-TIME, FORMATTED-DATETIME, COMBINED-DATETIME, emitted only when ON so every OFF emission is
  byte-identical. Under ON: `Analyze`'s seconds field admits 60, `SecondsOutOfStandardForm` bounds at 86,401 (GR4),
  `EmitFormatted` presents 86,400.x as 23:59:60.x. The REPORTED side stays the implementor's "never" (A.1 item
  112, updated). `ConditionalCompilationProcessor`'s five leave-bools became ONE set (`Frontend.LeftDirectives`).
  Goldens `pb65_leap_second_on` / `pb65_leap_second_off`; negatives `pb65-leap-second-{within-unit,below-2002}`.
- **RV-15.75.4-3 / RV-15.38.4-2 — two Annex A.1 rows, written from what was MEASURED.** Item 145 (RANDOM's seed
  subset): `RandomSeedSubsetTests` finds the first three draws of every seed 0..65,535 pairwise distinct (the
  required 0..32,767 floor with a margin) and pins the wide-seed alias `seed AND 0x7FFFFFFF`; only that is claimed.
  Item 87 (FORMATTED-CURRENT-DATE's time accuracy): the clock's tick — 100 ns, 7 significant fraction digits,
  SECONDS-PAST-MIDNIGHT's precision through the same `RunUnit.Clock` seam; the value is now carried as
  `ticks × 100` (the former `(decimal)TotalSeconds` went through a double), zeros beyond the 7th requested digit.
  Both rows in CONFORMANCE.md §7 (the audit counts them: 174 → 172 obligations remaining).
- **FMT-15.7.2 — the ABS window is settled.** kb/Work R28's adversarially verified WG4 CD 1.2 new-function list
  put ABS at 2014 on 2026-08-08; the sweep's "owner decision" was a day stale. The `--std 2002` rejection is the
  four-compilers rule; the crash leg was already fixed; the §15.7.1 integer→integer type row is modelled. CONFORMS.

**Verdicts:** RV-15.59.4-1, RV-15.61.4-3, SR-8.3.2.1-5, FMT-15.43.2, FMT-15.58.2, AR-15.79.3-4, RV-15.75.4-3,
RV-15.38.4-2, FMT-15.7.2, FMT-15.45.2, AR-15.45.3-1 → CONFORMS (GAP 4040 → 4029). All 34 rows PB65 owns are OK;
PB65 → landed.

**Gates (wave-local).** Solution build · Characterization 33/33 · Conformance
`Corpus|Negative|Nist|Intrinsic|Function|Repository|SpecTraceability|Boolean|Bool|Directive|Date|Time|VersionMatrix|Condition|Compute|Turn|Copy|Flag|Ec`
**4050/4051** — the one red was the new construct's sample program using a 2014 function at --std 2002 (sample
made 2002-legal; its five matrix rows re-run 5/5) · Unit
`SpecTraceabilityInventory|Intrinsic|Registry|Diagnostic|Manifest|Guard|Repository|Symbol|Construct|Directive|Copy|Conditional|Date|Random|Bool|Grammar|Parser|Turn|Flag|Storage|Watermark`
**594/595** — the one red was `BooleanExpressionGateSiteDriftTests`, which does its job: the new
`functionArgument` boolean site had no introduction gate; `VisitFunctionArgument` now gates it like every other
boolean-expression site (re-run 69/69) · Corpus/Negative/Boolean re-run 779/779 · CLI probes. Docs: COBOLNET_INTRINSICS_DESIGN.md (four new sections), CONFORMANCE.md (items 87, 112, 145 and the
§4 directive line), constructs.json + `Constructs.g.cs`, DIAGNOSTICS.md, kb/Work PB65 → landed, plan §0. Battery
owed for the PB71+PB65 batch (grammar and lexer changed).


