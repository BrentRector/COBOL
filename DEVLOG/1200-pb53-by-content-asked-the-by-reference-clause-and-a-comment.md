## Entry 1200 — 2026-08-07 08:05 PDT — PB53: BY CONTENT asked the BY REFERENCE clause, and a comment called that "conservative"

**Three pairings ISO §14.9.25.3 Table 16 admits were refused**, each with a diagnostic citing the clause it was
not applying:

```
BY CONTENT B1  (PIC 1(4) → PIC N(4))  ⛔ category mismatch (formal National, argument Boolean)     Table 16: Yes
BY CONTENT A1  (PIC X(4) → PIC 1(4))  ⛔ category mismatch (formal Boolean, argument Alphanumeric) Table 16: Yes
BY CONTENT N1  (PIC N(4) → PIC 1(4))  ⛔ category mismatch (formal Boolean, argument National)     Table 16: Yes
```

⛔ **THE CAUSE WAS A COMMENT THAT DESCRIBED THE DEFECT AS A VIRTUE.** `OoContentMismatch`'s default arm called
`OoConformance.DescriptionMismatch` — which is **§14.8.2.3.2, the BY REFERENCE identity rule** — under the
comment *"edited/other: conservative strict gate"*. §14.8.2.3.3 rule 2d sends a BY CONTENT crossing to MOVE
instead. "Conservative" reads as SAFE; what it actually described was REJECTS LEGAL SOURCE, which CLAUDE.md rule
4 forbids outright. **A gate stricter than the standard is not a cautious version of the right answer**, and the
word in the comment is what let it sit.

⭐ **ONE TABLE 16, ONE HOME, TWO CALLERS.** `MoveTable16.Refusal(sender, receiver)` over a `Table16Operand` —
the axes the table actually keys on, which are FINER than `PicCategory`: alphabetic and alphanumeric-edited are
separate rows/columns while sharing one category in this model, and the numeric row splits integer vs
noninteger. `MoveBinder` and `OoContentMismatch` now both ask it.
⚠ **WHAT STAYED WITH `MoveBinder`, because it is not a category question:** §14.9.25.3 SR7 (a figurative whose
characters are not boolean characters, and the ALL-literal form) and SR8 (the fixed-width binary family). Those
key on the bound operand's SHAPE, which no category table can see. Extracting them too would have looked tidier
and been wrong.

⛔ **THE EMITTER HALF WAS SAFE ONLY BECAUSE THE BINDER OVER-REJECTED.** `OoStringReadOf` width-fitted for an
ALPHANUMERIC formal and passed everything else through verbatim — correct only while strict identity guaranteed
both sides agreed in category and width. **Correcting the screen made that line load-bearing in the same change
set**, which is exactly why PB53 was filed rather than folded into PB46 when the screen was first touched. It now
reaches `MoveEmitter.ConvertSource`, where the receiving category's store discipline is already written once.

⚙ **VALIDATED AS AN AGREEMENT, BECAUSE THAT IS WHAT THE RULE SAYS.** Rule 2d prescribes no values — it says the
crossing behaves AS A MOVE DOES. So the golden performs each transfer TWICE, once across the INVOKE and once as
a plain MOVE, and prints both; a pair that disagrees violates rule 2d whatever either value is. All six pairs
agree, including the two WIDTH-DIFFERING crossings strict identity had made unreachable (`PIC 1(6)`→`PIC 1(4)` =
`1100`, `PIC X(6)`→`PIC 1(4)` = `ABCD`, right-truncated per §14.6.8.6) and an identical-description control that
must keep the fast path.
⚠ **What the pairs do NOT assert:** `MOVE A1 TO <boolean item>` stores `WXYZ` into a `PIC 1(4)`. Table 16 says
the move is VALID; the resulting content is an §14.6.13.2 incompatible-data question detected on use. Whether
this compiler should also flag that is a separate question about MOVE, and the pairs claim only the agreement
rule 2d requires.

⚠ **AND A GATE OF MINE REPORTED "COMPLETED, EXIT 0" WITHOUT RUNNING.** A `cd` earlier in the compound command
persisted, so the manifest registration threw `FileNotFoundError` and MSBuild answered
`MSB1009: Project file does not exist` — and the background task still completed green. Reading the OUTPUT rather
than the completion signal is what caught it; trusting the signal would have committed PB53 ungated, with an
UNREGISTERED golden that no suite would have run. The session's own lesson turned on the tooling:
**a green signal is evidence only about what actually ran** (`feedback_gate_on_the_verdict_line`).

