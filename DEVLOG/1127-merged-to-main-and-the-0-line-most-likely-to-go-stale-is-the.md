## Entry 1127 - 2026-07-30 05:05 PDT - Merged to main, and the §0 line most likely to go stale is the one about the merge

**`phase-14` → `main` at `1d0c24c9`, 33 commits**, under the comprehensive gate with every leg measured at the
merge rather than carried:

```
greenfield Conformance   4148 / 4148   zero skipped, nothing red
greenfield Unit           963 / 963
characterization           33 / 33
guard-fast.sh            ALL GREEN · NIST 353 MATCH / 0 REGRESSION
                         legacy Unit 1203/1203 · Integration 503/504 (1 skipped)
GnuCOBOL differential    10 per-case flips, ALL ATTRIBUTED, 0 unexplained
```

Conformance and characterization were re-run **on the merge commit itself**, because a merge produces a tree that
neither branch was ever tested as. It came back 4148/4148 there too.

**⛔ THE §0 MERGE LINE IS THE ONE THAT ROTS.** The line this replaces said `phase-14` and `main` had identical
trees — true when written, false from the moment the V59 wave started, and it stayed said through eighteen
commits. Its replacement carries the same hazard: "the branches are at the same commit" is false the next time
anyone commits. So the new text states the merge as a DATED FACT (`1d0c24c9`, 2026-07-30) and then says outright
that the relationship must be COMPUTED — `git rev-list --count main..phase-14`, or the probe, which does it. A
branch relationship is live state, and live state written into prose is a snapshot that keeps claiming to be
current.

**WHAT THE GATE BOUGHT, since the case for a five-leg gate is easy to doubt until one of the legs pays.** Four
legs were green. The fifth — the GnuCOBOL differential's per-case diff — showed two AGREE→divergence flips that
became **PB6**: `CALL … USING BY VALUE` was being refused by DA6's §8.8.1.1 ARITHMETIC screen, quoting a rule
about arithmetic expressions at a programmer who had broken §14.9.4.3 SR22. No other leg could see it: the
verdict was RIGHT, so nothing failed anywhere; only the reason was wrong.

⚠ **And a correction to how §0 describes that net.** It records the differential as "structurally blind to a
change in RUNTIME OUTPUT" — true, and incomplete. **Six of its eight FIXES this run were PB2**, a runtime-shaped
defect (a floating-point argument to an exact intrinsic) that manifested as a *failed compile*: `CS1503` from the
generated C#. A runtime defect that stops the program COMPILING is fully visible to a compile-time accept/reject
net. The rule is not "runtime changes are invisible" but "changes that leave compilability unaltered are
invisible", which is a narrower and more useful claim.

**WHAT IS NOW ON `main`.** V59; DA1–DA7; the P14 Step-0 mechanism (schema-as-data, the atomic writer, the battery
gate that recomputes every row's state and resolves every reference); 122 rules adjudicated across two Phase-B
batches; and PB1–PB6. The denominator is **3,846** — corrected from 3,790, which was short by 56 and had been
flattering every percentage quoted against it.

⚖ **AND ONE THING `main` NOW CARRIES THAT IS A KNOWN, DELIBERATE EXCLUSION**: the catalog does not model
§13.18.40.5 "Editing rules", §13.18.40.6 "Precedence rules" or §5.3 "Rules". They are rule KINDS, not spelling
variants, and admitting them changes what the denominator MEANS — so the extractor reports them loudly on every
run and the decision is the owner's. Recorded here so it cannot be discovered later as a surprise.

