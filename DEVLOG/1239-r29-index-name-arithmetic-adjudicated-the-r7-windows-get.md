## Entry 1239 — 2026-08-08 17:25 PDT — R29: index-name arithmetic adjudicated — the r7 windows get their own operand context, COMPUTE gets the rejection

COMPUTE N = IX + 1 computed the occurrence number silently. §13.18.38.3 r7's closed list admits an
index-name only in a subscript, PERFORM/SEARCH VARYING, SET, or a relation condition, and §8.8.1.1
names no index-names among arithmetic operands — but the SAME RefExpr index arm serves both the illegal
and the r7-legal consumers, so the note demanded a context audit before any rejection. The audit
enumerated every BindExpr caller and split them: the r7 windows — the D18 subscript fragment, PERFORM
VARYING FROM/BY, every SET amount (index, pointer, CAPACITY, SIZE), RW VARYING, EVALUATE operands, the
relation comparison arm — now bind under OperandContext.ArithmeticIndexWindow (identical §8.8.1.1
screening, index-names pass), while COMPUTE, the arithmetic verbs, RETRY, CONTINUE AFTER, STOP STATUS
and compound function arguments keep the strict contexts, where IndexNameExpr applies the adjudicated
PB1/DA6 disposition: strict = COBOLNET1637 citing r7; --permissive = warning + the documented
occurrence-number coercion (GnuCOBOL's extension, admitted only on the leniency axis).

Probes: strict errors, permissive warns and prints 0003; T(IX + 1) = 7, SET IX UP BY 1, IF IX = 3 and
IF IX + 1 = 4, PERFORM VARYING V FROM IX — all green end-to-end. Goldens: negative
index-name-arithmetic (all four editions) + positive index_name_r7_windows (85, the over-rejection
guard). Gate: Corpus|Intrinsic 662/662 · both by name · FULL Unit 4099/4099 · characterization 33/33.
Process note, honestly: the first probe ran against a stale build via a persisted cd
(compound_cd_persists_in_bash) — caught because the verdict line was COUNTED, not eyeballed.

