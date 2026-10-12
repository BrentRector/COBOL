## Entry 1193 — 2026-08-06 17:05 PDT — the arm I wrote to close a two-arm defect had a two-arm defect, and only §13.18.2 found it

**`01 AL PIC 1 ANY LENGTH.` received `B"1000"` as `B"1"`.** Entry 1192's `ContentBool` emit arm tested the
formal's CATEGORY before its ANY LENGTH clause:

```
01 AL PIC 1 ANY LENGTH.   INVOKE … BY CONTENT B1 B-AND B2   →  AL=[1]    LEN=1   ⛔ silently truncated
01 AX PIC X ANY LENGTH.   INVOKE … BY CONTENT B1 B-AND B2   →  AX=[1000] LEN=4   ✅ correct
```

§13.18.2.3 SR1 — "The character-string specified in that PICTURE clause shall be one instance of the picture
symbol 'N', 'X', or **'1'**" — so a category-BOOLEAN formal may carry ANY LENGTH, and §13.18.2.4 GR1b then makes
n "the length of the corresponding argument", not the single symbol its PICTURE spells. §14.8.2.3.3 rule 2c is
the conformance half and names the BY CONTENT + ANY LENGTH pairing outright. The boolean branch fitted the value
to `Pic.Length`, which for an ANY LENGTH item is 1.

⛔ **ONE ARM RIGHT, THE ARM BESIDE IT WRONG, NO DIAGNOSTIC — inside a change set written to close exactly that
shape** (`feedback_two_arm_dispatch`, and 1192's own headline was a `default:` arm nobody re-derived). The ANY
LENGTH test now runs FIRST, and the golden asserts BOTH picture symbols together rather than the boolean one
alone, because the asymmetry between them IS the failure mode.

⚠ **NO GATE FOUND THIS, AND THE FULL BATTERY HAD ALREADY PRINTED `ALL GREEN` ON THE COMMIT THAT SHIPPED IT** —
Conformance 4225/4225, Unit 4021/4021, characterization 33/33, guard-fast ALL GREEN with NIST 353 MATCH /
0 REGRESSION and audit CLEAN, GnuCOBOL differential 0 per-case flips. It was found by reading §13.18.2 to check
whether an ANY LENGTH boolean formal was even reachable before writing it off. That is the standing lesson in
its least comfortable form: a green battery is evidence about the cases the corpus contains, and a construct
nothing in the corpus writes is not one of them (`feedback_verdict_evidence_invariant`). The clause was the
oracle; the battery could not have been.

**GATE.** Wave-local on the delta (one emitter arm + the golden's two new assertions):
Conformance `~Corpus|~Negative|~Oo|~Invoke|~Boolean|~VersionMatrix` **2704/2704, zero skipped** · Unit filter 29/29. The
comprehensive battery above covers this tree's parent (`548668fb`); this commit's delta is gated wave-locally
per the tiered-gate rule.

