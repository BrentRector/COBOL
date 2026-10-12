## Entry 1138 - 2026-07-30 23:04 PDT - PB12: seven argument-class rows, six deliberate omissions, and a widening a negative fixture refuted

PB1 built the §15.3 argument-class screen and left a DESIGNED residue: a function is screened only when its
argument rule has been read and cited, and the table grows as the review adjudicates each clause. The
§15.32–15.44 batch adjudicated thirteen functions, so their rows are now due.

**Seven landed**, each read from its own §15.x.3 rule and validated with `cite.py --check`: EXP, EXP10,
FRACTION-PART and INTEGER as `'n'`; FACTORIAL as `'i'`; the two zero-argument EXCEPTION-* functions as `' '`.
FACTORIAL is PARTIAL on purpose — §15.36.3 r1 says "an integer GREATER THAN OR EQUAL TO ZERO" and a compile-time
class screen sees only the class half.

**Six got no row, and the omissions carry more information than the rows.** FIND-STRING and the four FORMATTED-*
functions take MIXED argument classes, and this table carries one kind per FUNCTION — a row would screen every
position by the first one's class and reject legal source, so that half needs a per-POSITION schema.
HIGHEST-ALGEBRAIC is sharper: §15.43.3 r1 admits "category numeric OR NUMERIC-EDITED", and Table 2 files
numeric-edited under class ALPHANUMERIC, so an `'n'` row would reject exactly what the rule admits — PB1's own
trap, which cost twelve legal corpus programs when it was sprung the first time.

**And I sprang a version of it anyway, one step short of committing.** `FUNCTION EXP(<numeric-edited>)` is
rejected, and the reasoning for widening `'n'` looked airtight: §15.3 type 10 admits "an ARITHMETIC EXPRESSION or
a numeric data item" — verbatim what type 6 admits for `'i'`, and `'i'` was widened to accept numeric-edited by
PB1 itself for exactly that reason. So I widened `'n'` to match, verified ABS/RANGE/EXP/INTEGER now accepted a
`PIC ZZ9.99` argument and computed correct values, and moved on.

**The negative fixture `pb1-numeric-arg-numeric-edited` failed, and it was right.** Its comment sends you to the
rule I had not read: §8.8.1.1 defines what an arithmetic expression may BE — "an identifier referencing a NUMERIC
DATA ITEM, a numeric literal, the figurative constant ZERO …". A numeric-edited item is category numeric-edited
and class alphanumeric; it is not a numeric data item, so it satisfies NEITHER of type 10's two alternatives. The
exclusion is correct, and my widening would have SILENTLY ACCEPTED ILLEGAL COBOL — the failure direction that
does not announce itself.

Two things I am keeping from that. First: I reasoned from ONE clause (§15.3) to a conclusion the clause it
depends on (§8.8.1.1) forbids — the premise-not-just-the-rule failure, on my own initiative rather than inherited
from a finding. Second: the refutation now lives BESIDE THE SCREEN in `IntrinsicArgumentRules`, not only in the
queue, because the widening argument is genuinely persuasive and someone will construct it again. A `'n'` row
that merely says "class numeric, Table 2 excludes numeric-edited" invites the challenge; one that names §8.8.1.1
and says the challenge was tried and refuted, ends it.

⚠ It also leaves an open question I am NOT asserting: PB1's `'i'` arm admits numeric-edited on the same
"de-edits in an arithmetic expression" reasoning that §8.8.1.1 appears to deny. PB1 landed that with a corpus
golden and a spec-derived unit test, so it is not being disturbed on a hunch — but the two arms now rest on
readings of §8.8.1.1 that cannot both be right, and that is worth an adjudication rather than a silence.

