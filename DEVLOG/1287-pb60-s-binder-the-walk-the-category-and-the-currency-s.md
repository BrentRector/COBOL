## Entry 1287 — 2026-08-09 21:16 PDT — PB60's binder: the walk, the category, and the currency's content

Three screens land on shapes the earlier landings built. BindNumvalCFamily walks
( argument-1 [argument-2] [ANYCASE] ) BY POSITION — the CONVERT-5a template verbatim: slot 0 always
binds as argument-1 (ANYCASE written first now fails name resolution + arity instead of silently
flagging the call), ANYCASE is admitted once and last (FMT-15.68.2 — the doubled trailing ANYCASE and
the leading one were both measured accepted). §15.68.3 r1's screen keys on CATEGORY where the §15.3
class row structurally cannot (class alphanumeric spans the edited categories r1 excludes — a
numeric-edited argument-1 now rejects 1627, a ref-mod view stays admissible per the GR6 classifier).
And r2's content bans land TWICE per the two-stage discipline: a literal argument-2 screens at bind
(no digits, no * + - , . — named outright, mode-independent — no CR/DB pair, at least one non-space),
and a data-item argument-2 has the runtime InvalidCurrency twin in NUMVAL-C (EC + default 0 — a
digit-bearing currency could consume argument-1 digits as "the currency") and TEST-NUMVAL-C (EC + the
r1c LENGTH+1 leg, because 0 would falsely certify conformance). §15.94.3 r1 mirrors everything onto
the TEST- twin. Three negatives; gates 737/737 + 582/582 + traceability 10/10; FMT-15.68.2,
AR-15.68.3-1, AR-15.68.3-2 → CONFORMS. GAP 4145. PB60 stands at 10 of 13 — open: the SDIDI Dec
branch, the currency-SET model, and the DECIMAL-POINT IS COMMA scoping row.

