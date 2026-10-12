## Entry 1312 — 2026-08-18 07:58 PDT — PB75 lands: a size error outside an arithmetic statement is a fatal exception condition — and every fatal EC is dispatched once, not once per nesting level

**PB75.** `IF 10 ** 100000 > 5 …` under STANDARD-DECIMAL died with an unhandled `CobolSizeError` stack trace, exit
127 — as did the same overflow in a function argument, a MOVE sender, or a no-phrase COMPUTE, and the native
receiverless lane's Int128 escape (PB69). §14.7.5: the size error condition "may occur as a result of … the
evaluation of an arithmetic expression", and with no SIZE ERROR phrase EC-SIZE-OVERFLOW "is set to exist, and
processing proceeds as specified in 14.6.13.1.3" — a USE declarative (#5), the enclosing exception-checking
PERFORM's WHEN (#4), abnormal termination (#7), or with checking off the implementor's disposition (#8).

- **`CobolSizeError` IS a `CobolFatalException`** (its EcName the Table 13 level-3 name), so the two existing
  mechanisms cover it: an arithmetic statement's own `catch (CobolSizeError)` (the phrase; `EmitArith`) still takes
  it first, and an ESCAPING one is a fatal EC. The binder's ambient tail queries the EC-SIZE family for every
  NON-arithmetic statement (`EcBinder.EcWrap`) and `EcEmitter.FatalAmbientGates` carries the four names with no flag
  (unconditional raise sites, like EC-OO-NULL) — a checked IF / DISPLAY / MOVE / PERFORM gets the generic guard.
  Arithmetic statements are excluded by a STRUCTURAL marker, `IArithmeticStatement` (every bound statement that
  carries a `SizeErrorPhrase`; the binder's own hand-written arithmetic type list became that marker too), so a
  size error is not dispatched by the statement AND the guard. With checking off the raise reaches
  `ProgramTable.RunMain`'s `CobolFatalException` catch: "abnormal run-unit termination: EC-SIZE-OVERFLOW (fatal):
  …", exit 1 — the #8 disposition, now on CONFORMANCE.md item 70 beside PB77's store rule.
- **Found on the way: every fatal EC was dispatched ONCE PER NESTING LEVEL.** A raising statement's guard ran the
  F3 selection and, unresumed, rethrew for the termination — and every ENCLOSING statement's guard (the PERFORM,
  the IF …) caught the same exception and ran the selection again. Measured before the fix: a fatal
  EC-BOUND-REF-MOD raise inside `PERFORM 2 TIMES` ran the USE declarative twice, then terminated; and with the new
  EC-SIZE gate on the PERFORM, a size error handled by the PERFORM's WHEN fell through to the USE declarative and
  CONTINUED (R5 printed where the standard says terminate). `CobolFatalException.Dispatched` marks a processed
  condition; every guard's `catch … when (!Dispatched && name)` lets it pass; every dispatched-then-terminate throw
  (RAISE unresumed, the arithmetic EC-SIZE default, the I-O fatal default, the CALL-site dispatches) throws it
  pre-marked. The first cut doubled the braces inside a non-interpolated string segment — `{{ Dispatched = true }}`
  reached the generated C# as a collection initializer (CS1922) — and the gate caught it in `PerformFormat3BehaviorTests`.
- **Measured.** Golden `pb75_sdidi_overflow_outside_arithmetic` (USE + RESUME AT NEXT STATEMENT on an IF — its
  branches skipped, §14.9.33.4 GR2 NOTE 1 — a function argument and a MOVE; PERFORM WHEN + RESUME; the ON SIZE
  ERROR phrase untouched); `SizeErrorDispositionTests` (checking off → exit 1 with the named condition on stderr for
  the four shapes and no stack trace; checking on with no handler → abnormal termination; the native receiverless
  lane the same; a fatal raise inside a PERFORM dispatched once, then terminated); `EcSizeGuardDriftTests` (the
  marker ⇔ the phrase, the gate table, the hierarchy). Verdicts GR-14.7.5-7, GR-14.7.5-L3.3, GR-14.6.13.1.3-4/-5/-7 →
  CONFORMS, GR-14.6.13.1.3-8 extended (GAP 4008 → 4003).
- **Registered PB91** (not swept here): the NATIVE carrier's Int128 overflow inside a NON-arithmetic statement
  (`IF A * B > 5` past 10^38 under EC-SIZE-OVERFLOW checking) still wraps silently — `MulChecked` is emitted only
  under an arithmetic statement's `InSizeError` context.

**Gates (wave-local).** Solution build · Characterization 33/33 · Conformance
`Corpus|SizeError|Ec|Exception|Nist|Arith|Compute|Size|Perform|Call|Invoke|Oo|Io|File|Raise|Resume|Use|Declarative|
Standard|Dec` **4437/4437** · Unit `EcSize|Ec|Exception|Registry|Manifest|Guard|SpecTraceabilityInventory|Snapshot|Emit|
Bound` **3148/3148** · CLI probes. Docs: COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md (the EC-SIZE bridge outside
arithmetic statements; one dispatch per raise), COBOLNET_NUMERIC_DESIGN.md, CONFORMANCE.md item 70, kb/Work PB75 →
landed, PB91 registered, plan §0. Battery owed for the PB75 batch with the next landing (battery #10, tree
`e939f354`, was ALL GREEN: Conformance 4690/4690 · Unit 4218/4218 · Characterization 33/33 · NIST 353/0
audit-clean · differential 0 flips).

