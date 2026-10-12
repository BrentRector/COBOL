## Entry 1366 — 2026-08-29 03:17 PDT — PB130 LANDED: CALL's grammar meets its figure — BY optional before VALUE everywhere, OMITTED and Format 2's bare literal/expression arguments parse, Format 1 rejects the spellings it never printed, and one program-name-literal reader serves CALL and CANCEL

The grammar half (all four BY VALUE sites gain BY?; callArgument gains OMITTED, the guarded boolean arm,
literal and arithmeticExpression on the callByContent alternation's own precedent; callByReference gains the
OMITTED alternative) converts five parse errors on legal source into parses — `USING VALUE X`,
`USING OMITTED`, `USING BY REFERENCE OMITTED`, `USING 42`, `USING (N + 1)` — and the binder narrows by
formatTwo exactly where §14.9.4.2's two formats differ: Format 1 now rejects BY VALUE, a BY CONTENT literal
and every bare non-identifier argument with the format cited. One DETERMINATION recorded in the grammar
comment: whitespace is lexer-skipped, so `USING N + 1` is genuinely ambiguous between one expression and the
two arguments N and +1 (both legal Format-2 lists) — the list reading wins and parentheses force the
expression (the golden pins `(N + 1)` → 8; the first probe caught the ambiguity when the unparenthesized
form delivered 7).

The literal-target half: ProgramNameLiteral is the ONE reader for CALL and CANCEL — alphanumeric AND
national literals admitted (hex is §8.3.3.2 Format 2 of alphanumeric; concatenations fold; D-N1 makes the
national name the same string), boolean/numeric/zero-length rejected with the cited clause at COMPILE time
where the old STRINGLIT-only arms sent legal source to run-time louds; CANCEL's zero-length screen (BindCall
had it, BindCancel did not), its miscited §14.9.5.2, and its whole-statement loop abort are all fixed — a
bad target now reports and the legal targets beside it survive. OMITTED routes to the OPTIONAL-formal staged
diagnostic until PB133 lands the carrier. FMT-14.9.4.2 → PARTIAL (ADDRESS OF and the AS edition gate
remain), SR-14.9.4.3-2 → CONFORMS, SR-14.9.5.3-2 → CONFORMS, SR-14.9.4.3-24 → PARTIAL.

THE GATE FOUND SIX OF OUR OWN PRE-LAXITY SPELLINGS — the Format-1 narrowing turned them red, and
each was respelled to the conforming Format 2 rather than the screen weakened (the §14.9.4.2 figure was
re-derived first: Format 1 really prints BY REFERENCE and BY CONTENT only): the pb84 pair and udf_by_value
gained AS NESTED on their BY VALUE calls, NationalBooleanLiteralTests likewise, the pb6 negative was
respelled so its SR22 rejection fires under a LEGAL format, and pb46 — its `BY CONTENT N + 1` re-parsed
as the two arguments N and +1 under the documented ambiguity determination — was parenthesized with its
expected output unchanged. The BooleanExpressionGateSiteDrift net demanded (and got) the edition gate for
the new bare boolean-expression arm the same day it was added.

