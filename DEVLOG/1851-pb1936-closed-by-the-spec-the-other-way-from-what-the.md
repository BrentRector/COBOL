## Entry 1851 — 2026-10-03 12:58 PDT — PB1936 closed by the spec (the other way from what the session first said); PB1927 stays open

The owner asked for the open adjudications to be closed from the spec. Re-reading both notes against the clause text and
re-running every citation (`cite.py --check`) changed one answer I had given earlier in the session.

**PB1936 (BY CONTENT arithmetic expression into a non-numeric formal): closed, and the refusal PB1113 landed is wrong.**
§14.8.2.3.3 rule 2 a) applies COMPUTE rules "with the argument as the sending operand" and rule 2 d) uses the identical phrase
for MOVE, so "sending operand" there means the ARGUMENT, expressions included; §14.9.4.3 SR17 makes an arithmetic-expression
argument a sending operand and the CALL syntax admits it BY CONTENT; no clause restricts the formal's category; and a numeric
literal into the same formal is already admitted through the MOVE rules. So the expression's value is a numeric sender judged
by Table 16. The session's earlier statement that refusing is "the literal reading, and correct" read MOVE's own format
(identifier or literal) into a rule that names "the argument", and it reached the wave 1007 group B brief; no code depended on
it. The fix is `kb/Work/PB1946` (a defect, rejects legal source): the `IsExpression` arm of
`OoConformance.ContentValueMismatch` describes the expression as a numeric sender and asks Table 16.

**PB1927 (GR6 c) proviso against GR7 d) 4.): NOT closed.** Both clauses check. A footing at a LOWER level than an OR PAGE
heading has that heading at a HIGHER level, which d) 4.'s "same level as the control footing, or higher" includes, giving the
footing an upper limit below the heading, while GR6 c) says that heading is not reprinted before such a footing. They reconcile
only by reading d) 4. as applying when the heading is on the page, which is a reading and not text; calling them
non-conflicting earlier in the session was too confident. GnuCOBOL 3.2.0 accepts `OR PAGE` syntactically, but a probe at PAGE
LIMIT 4 to 8 never produced a page advance, so the case was not reached. The note records this and its next step: a probe that
forces the advance, then an implementor decision in CONFORMANCE.md.

**Files:** `kb/Work/PB1936.md` (landed, verdict), `kb/Work/PB1946.md` (new defect), `kb/Work/PB1927.md` (analysis appended).
No code changed, no diagnostic code used.

