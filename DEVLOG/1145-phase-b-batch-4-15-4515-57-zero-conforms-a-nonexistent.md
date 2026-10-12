## Entry 1145 — 2026-08-02 19:00 PDT — Phase-B batch 4 (§15.45–15.57): zero CONFORMS, a nonexistent clause cited in nine files, and the refuters caught a regression I had landed hours earlier

The fix track ran out of blockers, so the review's other track got a turn. `phase_b_batch.py 15.45-15.57` produced
77 rules over 13 subjects; the workflow ran 26 agents — one adjudicator and one INDEPENDENT refuter per subject —
with **zero failures**, and the refuters overturned 16 verdicts. As in all three prior batches, **every overturn
was a downgrade.**

Totals: **DIVERGES 25 · PARTIAL 16 · NOT-IMPLEMENTED 36 · CONFORMS 0.** Adjudicated 253 → 330.

### Zero CONFORMS is not the headline; the clustering is

The playbook says an all-CONFORMS report is a red flag. The mirror caution is that a big number of findings looks
like a big number of defects, and it never is. Of the 77:

- **33 of the 36 NOT-IMPLEMENTED are ONE owner-ratified disposition** — the §A.4.9 locale module, loudly and
  correctly rejected by `COBOLNET1518`. Those are not defects at all.
- **20 more are ONE root cause** — PB1's *designed* residue, the argument-class table growing per clause.
- The genuinely new work is **PB19–PB27**, nine items.

### The finding that matters most: a clause that does not exist

`python scripts/spec/cite.py --check 8.4.2.4 …` → **"there is no clause §8.4.2.4 in the transcription."** I ran
that myself rather than taking the agent's word, because a claim that our own code cites a nonexistent clause is
exactly the sort of thing that should not be inherited a second time. §8.4.2 has only .1/.2/.3; reference
modification is §8.4.3.3.

The citation appears in about nine files — `ExpressionBinder`, `MoveBinder` ×3, `InitializeBinder`, `OoBinder` ×3,
`Place`, `ReferenceResolver`, `MoveClassifier`, `IntrinsicArgumentRules`. This is CLAUDE.md rule 1's stated
failure mode ("the failure is not inventing a citation, it is INHERITING one") realised at scale, inside the
compiler, for months.

**And the substance is wrong, not just the number.** The code types every `RefModPlace` as class ALPHANUMERIC. The
real rules, all `--check`ed: §8.4.3.3.4 **GR1** makes a leftmost-position boolean, alphanumeric or national
*respectively* by identifier-1's class; **GR2** re-reads a usage-DISPLAY item of another category as alphanumeric;
**GR3** does the same for national; **GR6** gives the unique data item "the SAME class, category, and usage as
that defined for identifier-1" except as listed. So a ref-modified BOOLEAN item stays boolean —
and `ConditionRenderer` already says exactly that in its own comment. The codebase contradicts itself, and the
wrong half is the one with the fabricated citation.

That ordering matters for the work: PB20 must land before PB19's INTEGER-OF-BOOLEAN row, or the new screen would
falsely reject **the standard's own Annex D worked example**, `FUNCTION INTEGER-OF-BOOLEAN (bit-item (1:6))`.

### The refuters found a regression I had landed the same day

PB13 made a receiver-less float-family render keep its binary64 value instead of quantizing through
`CobolIntrinsics.FromDouble`. That was right. It also, silently, moved the EC-ARGUMENT-FUNCTION raise site out
from under that arm, because `FromDouble` is where an out-of-domain NaN became the §15.3 default result. The §15.55
refuter noticed. Reproduced:

```
COMPUTE R = FUNCTION ACOS(2)   ->  0      the §15.3 default; FromDouble screened the NaN
IF FUNCTION ACOS(2) = 0        ->  FALSE  a raw NaN; nothing screened it
```

Two things wrong with that. A function's returned value must not depend on the SHAPE of its receiver — §15.4 puts
it in a temporary with the function's own characteristics, and nothing makes it a property of the destination. And
with EC-ARGUMENT-FUNCTION checking ENABLED, §14.6.13.1 requires the condition to be raised; it was not raised at
all, because nothing looked at the value. `CobolIntrinsics.RealResult` restores the screen on the unquantized arms
without re-quantizing, so PB13's own goldens are untouched (`EXP10-DISTINCT=YES` still holds). Pinned by
`pb13_domain_raise_receiver_shape`, which asserts the two shapes AGREE rather than pinning the particular default
— agreement is the part the standard fixes; §15.3 leaves the value implementor-defined while checking is off.

This is the third time today the review found something a green battery could not, and the first time it found it
in code from the same session.

### Two process traps, both caught by a mechanism rather than by me

**The playbook's own documented merge command would have reverted PB11.**
`record_verdicts.py scratchpad/phase-b/out-*.json` globs every out-file in a shared directory. With batch 3's
output still on disk it offered **144 records instead of 77**, and four rows re-adjudicated BACKWARDS —
`CONFORMS → NOT-IMPLEMENTED` on exactly the FORMATTED-* rows PB11 had closed hours earlier, from files dated 07-30
written before PB11 existed. **The tell was the GAP going UP**, 3799 → 3803, in the `--dry-run` — which is the
entire reason that flag exists. Merging the batch's own thirteen files by name gave a clean 77/77. Recorded in
both §0 and the queue, because the trap arms itself the moment a second batch shares the directory.

**The inventory gate caught 15 unresolved `test-ref`s.** Agents wrote the corpus-golden form `conformance:` for
xUnit METHODS. All seven referents existed — only the form was wrong (`conformance-test:<Class>.<Method>` for the
Conformance assembly). Fixed in the BATCH FILES and re-merged, never by hand-editing the inventory: §8's
"shape at record time, reference at battery time" split did exactly what it was designed to do.

### Also worth recording

The playbook's §9 item 8 still said "the fixes that have already landed (currently PB1–PB7)" while PB8–PB11 and
PB13 had all landed. A stale list there is worse than none: it tells agents a fixed defect is still open and they
spend the batch re-reporting it. Corrected, with a note to re-read the queue's LANDED header rather than copy the
sentence.

