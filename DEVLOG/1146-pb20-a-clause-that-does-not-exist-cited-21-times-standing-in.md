## Entry 1146 — 2026-08-02 19:41 PDT — PB20: a clause that does not exist, cited 21 times, standing in for a rule that was wrong

`cite.py --check 8.4.2.4` → **"there is no clause §8.4.2.4 in the transcription."** §8.4.2 has only .1/.2/.3;
reference modification is §8.4.3.3. That non-existent number appeared **21 times across 14 files** — the Phase-B
§15.45 refuter found it, and I re-ran the check myself before believing it.

### The citation was not the defect; it was the cover for one

Three places implemented "what is the class of a reference-modified operand", and no two agreed:

- `IntrinsicArgumentRules.ClassOfPlace` — class ALPHANUMERIC, unconditionally.
- `ExpressionBinder.NonNumericOperandKind` — the same claim, in prose.
- `MoveBinder.MoveReceiverCategory` — a partial map: national and boolean preserved, everything else flattened.

The real rule, §8.4.3.3.4 GR6, is that the unique data item has **the same class, category, and usage as
identifier-1**, except three lettered rewrites. The BASE CASE is the whole point, and it is the arm none of the
three had: a ref-modified BOOLEAN item stays boolean, a ref-modified NATIONAL item stays national. `MoveBinder`
came closest and still flattened a national-usage NUMERIC item, which GR6c makes national.

**A fabricated citation is how a wrong rule survives review.** Every reader saw a `§` and stopped. That is the
mechanism CLAUDE.md rule 1 describes — "the failure mode is not inventing a citation, it is INHERITING one" — and
this is what it looks like after it has had time to spread.

### What the model can and cannot express, said out loud

GR6a (alphanumeric-edited → alphanumeric) and GR6b (national-edited → national) have **no arm** in the new
helper, and that is a property of our model rather than of the rule: `PicCategory` has no `AlphanumericEdited`
member (an edited item is already `Alphanumeric` plus an `EditMask`), and national-edited is a
recognized-but-unimplemented skeleton that `PictureAnalyzer` recovers to `Alphanumeric`. So GR6a is satisfied by
the base case and GR6b is not reachable at all. Both are named in the helper's remarks with the instruction that
if either category ever gains a member, both arms must appear — an absent arm that is explained is a decision; an
absent arm that is silent is a bug waiting.

### What this does NOT change, asserted rather than assumed

No ref-mod result is ever category NUMERIC — GR6c rewrites numeric away under both usages. So §8.8.1.1's
rejection of a reference-modified ARITHMETIC operand was always correct on the merits, and PB20 deliberately
touched only its citation. There is a test for that, because "the other screen is still right" is exactly the kind
of claim that rots quietly once the thing it depended on moves.

### Latent on purpose, and the tests say so

Conformance stayed at **4174/4174**: no rule the §15.3 screen consults today demands class boolean, so the
corrected categories change no accepted/rejected verdict yet. PB20's value is that it **unblocks PB19** — over the
old always-alphanumeric answer, an INTEGER-OF-BOOLEAN screen would have rejected the standard's own Annex D
worked example, `FUNCTION INTEGER-OF-BOOLEAN (bit-item (1:6))`. A behavioural golden would therefore pin nothing,
so the rule is tested as what it is: a pure function, exhaustively, over every category × usage pair.

⚠ Two traps in the sweep itself, both worth remembering because they are properties of THIS codebase:
**§8.8.4.2.4 is a real clause** (Comparison of numeric operands, 15 occurrences) sitting one character from the
fabricated one — a naive grep-replace corrupts it, so the guard regex carries a `(?<![.0-9])` boundary. And the
blanket replace rewrote **my own deliberate quotations of the bad citation**, turning two new warnings into
self-contradictions; a warning has to be able to name the thing it warns about. Both caught before the gate.
The guard was made to FAIL once, by reintroducing the clause, before being trusted.

