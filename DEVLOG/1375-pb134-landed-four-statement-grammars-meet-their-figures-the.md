## Entry 1375 — 2026-08-29 06:27 PDT — PB134 LANDED: four statement grammars meet their figures — the GIVING forms' sending operands, DELETE FILE Format 2 whole, END-ACCEPT alive, and the DIVIDE crash shape now a diagnostic

The arithmetic half is ONE discipline over four verbs (Format2SendingOperand / Format1Receivers): a GIVING
form's TO/FROM/BY/INTO operand is ONE sending `{identifier | literal}` — a function-identifier rides per
§8.4.3.1.2, placed as ADDITIVE grammar alternatives so the frozen legacy oracle (which reads
.receivingArithmeticOperand() by name — the first patch broke its build with a wrapper rule and was
reworked) and the §8.4.3.2.3 SR1 receiving-side drift guard both keep holding. The non-GIVING forms print
receivers only, every BY form of DIVIDE prints GIVING, REMAINDER needs GIVING with exactly one quotient
(SR6), and `ADD A B.` / `SUBTRACT A B.` — shapes no format prints — are COBOLNET1689 at bind where the
old binders silently dropped operands and ROUNDED phrases, staged runtime louds, or CRASHED
(`DIVIDE 2 INTO 10.` took the literal alternative with no GIVING and ArithmeticEmitter's targets.Max threw
InvalidOperationException through the CLI). The golden's derivation caught the fix's own regression: the
first attempt passed the raw FunctionCallContext into the operand walk, which descends into the ARGUMENT
— PB45's documented trap, re-sprung — and `ADD 1 TO FUNCTION SQRT(9) GIVING D` computed 10; binding
through the phrase computes 4, and the golden pins the derivation.

DELETE FILE Format 2 lands whole: `[OVERRIDE] {file-name-1}…` parses (OVERRIDE used to be consumed AS the
file-name), the exception phrases take the optional ON (the underline measurement's missed sibling), GR12's
as-if-separate multi-name semantics bind as one BoundKeyedDeleteFile per name in order, GR18/GR19's OVERRIDE
is documented as the whole obligation (this implementation validates no fixed-file attributes), and the
RESERVED COBOLNET1613 (§14.9.10.3 SR4) went LIVE beside its 1612 CLOSE sibling. END-ACCEPT parses at last
— the token existed only in the lexer while its 0816 gate sat registered and dead — and END-DISPLAY's
missing twin gate (accepted at 85 with no gate at all) was added with its own construct row.

THE GATE'S TWO REDS, both the fix's own edges: the greedy fileName+ swallowed `RETRY …` as a second
file-name (delete_file_sharing red — reservation is a per-edition BIND screen, so the edition-shared
cobolWord matches the keyword; the loop continuation is now left-edge-PREDICATED on the lookahead not
being a phrase keyword, the standing predicate rule), and the pending-rows contract test demanded what
the landing meant: end-accept-2002 and end-display-2002 both flip status ACTIVE — the activation
contract firing is precisely the pending state ending.

Verdicts: FMT-14.9.2.2 / FMT-14.9.12.2 / FMT-14.9.10.2 / GR-14.9.10.4-12 / SR-14.9.10.3-4 → CONFORMS,
FMT-14.9.1.2 → PARTIAL (screen ACCEPT stays under the documented A.4.2 Not-claimed owner posture).

