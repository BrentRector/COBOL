## Entry 744 — 2026-07-10 16:12 PDT — PHASE 04 Group B: share SUBSCRIPT/DEFAULT literal token bodies via fragment rules (commit boundary B1)

Landed PHASE-04 Group B: the six literal tokenization shapes that the DEFAULT and SUBSCRIPT lexer modes each
re-declared char-for-char are now defined ONCE as `fragment` rules and referenced from both the DEFAULT-mode tokens and
their `SUB_*` twins.

**What changed** (`Grammar/Core/CobolLexer.g4` only): six new fragments — `STR_BODY` (alphanumeric literal, both quote
forms), `NAT_BODY` (= `'N' STR_BODY`), `BOOL_BODY`, `INT_BODY`, `DEC_BODY`, `NAME_BODY` (the user-word shape) — replace
the inline bodies of `STRINGLIT`/`NATLIT`/`BOOLLIT`/`INTEGERLIT`/`DECIMALLIT`/`IDENTIFIER` AND their subscript-mode twins
`SUB_STRINGLIT`/`SUB_NATLIT`/`SUB_BOOLLIT`/`SUB_INTEGERLIT`/`SUB_DECIMALLIT`/`SUB_IDENTIFIER`. Fragments are
mode-independent, so one body serves both modes; a future string-escape / national-literal / data-name fix is applied
ONCE and cannot diverge between the two modes (DESIGN-frontend-grammar §3.3b). The SUBSCRIPT-only
`SIGNED_DECIMALLIT`/`SIGNED_INTEGERLIT` (no DEFAULT twin) and `HEXLIT`/`FLOATLIT` (no SUB twin) were deliberately left
untouched — no DEFAULT/SUBSCRIPT duplication to remove — per the phase doc's scope. No token was reordered (maximal-munch
precedence preserved). Bodies verified character-identical to the retired inline forms before the flip.

**Byte-neutral:** `CobolLexer.tokens` byte-identical (a `fragment` carries no token type — the ATN inlines it, so the 6
DEFAULT + 6 SUB token numbers are unchanged); greenfield conformance 3157 · unit 227 · characterization 32 byte-exact;
FULL legacy guard NIST **353 MATCH / ALL GREEN / 0 regressions** (the frontend is SHARED with the legacy oracle). Probes
confirm the SUBSCRIPT-mode literals still tokenize: `FUNCTION LENGTH("ABCDE")`=05 and `FUNCTION LENGTH(N"XY")`=02
(SUB_STRINGLIT / SUB_NATLIT / SUB_INTEGERLIT), and the single-quote branches `FUNCTION LENGTH('ABC')`=03 /
`FUNCTION LENGTH(N'AB')`=02 validate the composed `NAT_BODY : 'N' STR_BODY` across both quote forms.

**RESUME AT: PHASE-04 Group C** — the typed `Cst/` façade over the ANTLR contexts + migrate the two anchor consumers
(`ReferenceResolver`, `DataBinder.BindEntry`) off raw `GetText()`; commit boundary C3.

