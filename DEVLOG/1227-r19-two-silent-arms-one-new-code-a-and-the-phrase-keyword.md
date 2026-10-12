## Entry 1227 â 2026-08-08 13:12 PDT â R19: two silent arms, one new code â and the phrase-keyword functions verified untouched before landing

`FUNCTION EXP10(LEADING)` compiled with zero diagnostics and threw at run time. The defect sat one line
below its own counter-example: BindArgOperand's OMITTED arm reports (COBOLNET1544) while the
fnArgPhraseWord arm returned a silent BoundOperandError â and NonNumericOperand's `??` null fallback was
the same shape one helper over, both named by ledger F18. Both now report the new COBOLNET1638
(intrinsic-argument-not-a-value) at the ONE arm each, generic to every catalogued function.

The check that mattered before landing: the phrase-keyword functions whose formats DO name these words â
TRIM(X LEADING), FIND-STRING(H N ANYCASE), MODULE-NAME(TOP-LEVEL) â all probe green, because their bespoke
binders consume the words before the generic arm can see them. The diagnostic's text names that split (the
five functions whose Â§15.x.2 formats carry phrases; every other format admits only arguments, Â§15.3 /
Â§4.2.2 Â¶3).

Negative fixture intrinsic-phrase-word-argument. Gate: Intrinsic|Corpus 646/646 Â· Diagnostic drift 30/30 Â·
characterization 33/33.

