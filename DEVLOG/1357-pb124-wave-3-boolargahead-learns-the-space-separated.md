## Entry 1357 — 2026-08-29 01:08 PDT — PB124 wave 3: boolArgAhead learns the space-separated argument boundary — a B-operator in a LATER argument no longer drags every earlier one into the boolean alternative

AR-15.3-3's open half (batch 7's refuter): the boolean-argument predicate scanned to the argument LIST's end
(a depth-0 comma or ')'), so on a space-separated multi-argument call, a B-operator ANYWHERE later predicated
EVERY argument into booleanExpression — `CONCAT(WS-A B1 B-AND B0)` sent the alphanumeric WS-A down the
boolean channel. The fix is one observation from §8.8.2 r1: a boolean expression connects every term with an
operator, so TWO ADJACENT OPERAND TERMS at depth 0 are an argument boundary — the scan answers false there,
and the operator it would have found belongs to a later argument. LPAREN directly after a term stays a
subscript/ref-mod continuation (BT(1) B-AND BT(2) still recognizes — the parser's own reading of
identifier-paren); FUNCTION joins the term-start set. The row's third charge — the undocumented 96-token
cap — is raised to 512 and documented at the predicate: with the boundary arm only ONE argument written as
a single giant boolean expression can reach it, and it falls to the non-boolean alternative with a bind
diagnostic rather than a silent wrong parse. Golden pb124_boolean_argument_boundary pins the mis-route case
(CONCAT → "ab0"), the subscripted pair, the literal-first boundary, and the single-argument form the fix
must not break. AR-15.3-3 → CONFORMS. PB124 stays open (the class/category model half, the
variable-length-group prohibitions).

