## Entry 1042 — 2026-07-27 01:34 PDT — Two more optional words — and the systematic audit caught ME about to add a wrong one

Swept the SR1 pattern (DEVLOG 1041): every word the page measurement shows un-underlined, checked against how the
grammar spells it. New tool `scripts/spec/audit_grammar_optional_words.py` does this across all 50 statements
whose general format it can locate.

**Two real bugs, both confirmed against the raw rectangles first:**

| page | word | printed | grammar had |
|---|---|---|---|
| 732 RECEIVE | `FROM` | no underline rule at all | required |
| 756 SEND | `TO` | no rule, in BOTH send formats | required |

On the same lines `RECEIVE` (38.8 pt), `GIVING` (31.3), `SEND` (23.6/23.3), `FROM` (25.7/25.9) and `RETURNING`
(54.9) all carry rules, so the distinction is real on the page, not a detection threshold.

**⛔ And I was one commit away from landing a WRONG one.** I had also made `AFTER` and `SECONDS` optional in
RECEIVE's CONTINUE phrase, because page 732 prints them without underlines. The systematic audit then flagged
page 634 — and page 634 is **exactly inverted**: in 14.9.9.2, the CONTINUE statement's OWN defining general
format, `AFTER` and `SECONDS` ARE underlined and `CONTINUE` is NOT.

I rendered both pages and looked, rather than trusting either measurement. Both are transcribed correctly; **the
standard genuinely contradicts itself.** The defining clause governs, and it is the only reading that means
anything — with both words optional, `CONTINUE 5` would be legal and undefined. Reverted; the grammar now rejects
`CONTINUE 5` again, which I verified rather than assumed.

Two defects in the standard recorded at 14.9.9.2 rather than silently resolved: the contradiction itself, and
`CONTINUE` printed without an underline. **That second one is the single place I have overridden the
transcribe-as-printed rule** — a statement name cannot be an optional word, since omitting it would leave no
statement, so transcribing it faithfully would assert something the standard cannot mean. Flagged in place so the
decision is visible rather than buried.

**The lesson is about the shape of the evidence, not the words.** Un-underlined-on-one-page is NOT sufficient
grounds to relax a grammar rule. What made `ON`, `FROM` and `TO` safe was that no page contradicted them and the
grammar already disagreed with itself. What made `AFTER`/`SECONDS` unsafe was a second page saying the opposite —
and I would not have found it by looking only where the finding pointed.

Golden `2023/optional_words_mcs` pins `FROM` and `TO` omitted, against both a data-name and a literal operand,
then writes every optional word to prove omission is a SPELLING, not a replacement. It deliberately does NOT
exercise `AFTER`/`SECONDS` omission, and says why.

Gates: build clean · golden compiles (6 named MCS warnings) and runs · `CONTINUE 5` correctly rejected ·
catalog 3,790 · anchors 1261.

