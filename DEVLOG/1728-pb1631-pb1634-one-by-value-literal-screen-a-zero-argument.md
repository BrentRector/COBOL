## Entry 1728 — 2026-09-27 04:22 PDT — PB1631 + PB1634: one BY VALUE literal screen; a ZERO argument into a numeric formal is zero

**PB1631.** INVOKE §14.9.23.3 SR16 and CALL §14.9.4.3 SR23 are the same sentence, but each had its own copy. INVOKE's
admitted only NULL, so BY VALUE ZERO got the SR16 message; CALL's asked `zeroWord()`, which `ALL ZERO` also carries,
so `CALL … USING BY VALUE ALL ZERO` compiled. Both now ask `CallBinder.ByValueLiteralAdmitted`, built on the ONE
§8.3.3.6.3 SR1 a) predicate `ExpressionBinder.IsNumericRestrictedZero` (ZERO without ALL), which the arithmetic
operand already used inline. Both messages printed `ALLZERO`. `ConcatFolder.Spelling(LiteralContext)` now spells
them, and also the CURRENCY SIGN and program-name literal messages. Negative: `pb1631-call-by-value-all-zero`
(COBOLNET1762 at 2002/2014/2023).
**PB1634 (new, wrong answer, found probing PB1631).** A figurative ZERO passed to an elementary numeric CALL formal
crossed as the fill "0", and the numeric landing read the character code: `BY VALUE ZERO` into `PIC 9(4) BINARY`
displayed 0048, as did BY CONTENT and the keyword-less spelling. §14.8.2.3.3 2) a) makes it a COMPUTE, which reads
ZERO as zero. INVOKE already did this inline (PB1617); the rule now lives in `ParameterConformance.ArgumentForFormal`,
used by CALL's formal-correspondence loop and by INVOKE. A user-defined function was already right. Golden:
`2002/pb1634_zero_argument_numeric_formal`. The note records the residue: BY CONTENT ALL ZERO into a numeric formal
still reads as zero, which still needs to be derived against the spec.
**Gate.** Characterization 33/33; Conformance (Call, Invoke, Oo, CorpusRunner, Negative, Drift, Figurative,
Currency, Function, Nist, Parameter) 4899/4899; Unit 29491/29491.

