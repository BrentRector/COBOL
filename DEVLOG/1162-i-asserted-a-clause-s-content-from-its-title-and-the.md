## Entry 1162 — 2026-08-03 23:02 PDT — I asserted a clause's content from its TITLE, and the correction is the triage signal the denominator batch needed

**Starting the owner-decided denominator batch — adjudicate all 100 unharvested clauses — began by reading one
of them, and the first thing it produced was a correction to entry 1160 and to my own report to the owner.**

**THE FALSE CLAIM.** Entry 1160, fix-queue PB29 and the decision I put to the owner all said: *"§8.3.2.2
'User-defined words' contributes 15 ordinals and they are the KINDS of user-defined word, not fifteen
requirements."* It was offered as the clean example of why 460 is an upper bound. **I read the TITLE and asserted
the BODY.** §8.3.2.2 does list the kinds of user-defined word — as an EM-DASH list, which the detector never
counted. Its 15 counted items are something else entirely: **five nested `N)` sub-lists inside prose, each
restarting at 1** (1‑2‑3, 1‑2, 1‑6, 1‑2, 1‑2), and several of them are normative statements about when a name may
be the same as another — *"a compilation-variable-name may be the same as any other type of user-defined word"*,
*"a level-number may be the same as a paragraph-name or a section-name"*. The conclusion (460 is an upper bound)
survives; the reason I gave for it was wrong, and it was wrong in the direction that made the work look smaller.

⛔ **THIS IS `validate_the_premise_not_only_the_rule` ONE LEVEL UP.** The citation was real, the clause number was
right, the count was right — and the claim about what the clause CONTAINS was never checked against the clause.
A title is not evidence about a body. The same session had already caught two summaries this way (PB32's three
named instances, PB29's own 5×-short count); this one was mine.

⭐ **AND THE CORRECTION IS WORTH MORE THAN THE ERROR — IT IS THE TRIAGE SIGNAL.** The detector reported a single
number, `ordinals`, and 15 reads exactly like a numbered rule block. Counting the LISTS as well as the items
separates the two shapes mechanically:

| shape | clauses | items | reading |
|---|---|---|---|
| **single ascending list** | **89** | **350** | rule-block shaped — the strong admit candidates |
| **nested sub-lists** | 11 | 110 | prose-enumeration shaped |

A rule block is normally ONE ascending list. So `lists == 1` is weak evidence FOR admitting a clause and
`lists > 1` weak evidence against — **neither is proof**, which is why every clause is still adjudicated one at a
time, but it is what makes 100 clauses triageable instead of 100 independent readings. **All five §8.8.1 clauses
are single lists** (7 · 3 · 4 · 3 · 4 = the 21 PB29 counted), which is independent corroboration that PB29's own
subject really is a rule block; §8.3.2.2 lands in the multi-sublist group, exactly where the corrected reading
puts it.

**LANDED.** `unharvested_rule_blocks` now records `lists`, `longest` and a derived `shape` per clause, and
`spec-unharvested-rule-blocks.json` carries the split totals so the estimate cannot be quoted as one flat number
again. The live docs (fix-queue PB29, plan §0) are corrected in place; entry 1160 keeps its wrong sentence because
`DEVLOG.md` is history, and this entry is its correction.

⚠ **The 460 total is UNCHANGED — no clause entered or left the set**, so the drift guard still matches its
manifest exactly. What changed is only what the number means, which is the part that was misleading me.

