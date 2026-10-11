## Entry 1124 - 2026-07-30 01:40 PDT - PB5: the quantizer saturated at 9.2 billion, and the refute stage found it where the adjudicator had looked straight at it

**THE WORST DEFECT THIS REVIEW HAS SURFACED, and it was silent.** `CobolIntrinsics.FromDouble` returned a `long`
and clamped at `long.MaxValue`. Its caller quantizes at `ws = max(Receiver.Scale, 9)`, so the clamp bit at
**|value| ≈ 9.2 × 10⁹** — and every float-family result at or above that magnitude was replaced by the constant
**9223372036.85**.

```cobol
01 R PIC 9(12)V99.
    COMPUTE R = FUNCTION ANNUITY(10000000000 1)     *> §15.9.4 r1b with argument-2 = 1 reduces to 1 + argument-1
      ON SIZE ERROR ... NOT ON SIZE ERROR ...       *> printed NO SIZE ERROR
    R = 00922337203685                               *> the spec value is 10000000001.00 — wrong by 8%
```

`SQRT(1e20)`, `EXP(23.3)`, `ABS` and `MAX` over a COMP-2 all produced that same constant. **A twelve-digit money
field is routine COBOL**, so this was not an edge: it was wrong arithmetic in ordinary business ranges, with no
diagnostic — §14.7.4 never saw an overflow, because the value had already been clamped to something that fits.

⛔ §15.4.1 licenses an implementor-defined **approximation** of the equivalent arithmetic expression under native
arithmetic. **9223372036.85 is not an approximation of 10000000001.**

**THE FIX IS A TYPE, AND IT IS CORRECTNESS RATHER THAN COMFORT.** The scaled domain of this compiler already IS
`Int128` — every `…Scaled` body takes one — and `FromDouble` was the last member of that pipeline still returning
`long`. At scale 9 the Int128 ceiling is ≈1.7 × 10²⁹, past the 10¹⁸ any PICTURE can describe, so the saturation is
now unreachable from a declarable receiver rather than merely further away. One emit site
(`IntrinsicRenderer.RenderFloat`), so the change is contained; the full suite is the proof it is safe.

After the fix: `ABS`, `MAX` and `SQRT` all give exactly 010000000000.00. ANNUITY lands a cent low
(010000000000.99) because its EAE carries a division and binary64 loses it at the 17th significant digit — THAT
is the §15.4.1 approximation, and the golden deliberately does not pin it. The fixture pins the saturation.

**⚠ THE REFUTE STAGE FOUND IT, AND THE ADJUDICATOR HAD LOOKED STRAIGHT AT IT.** The ANNUITY adjudicator returned
PARTIAL, having checked only small receivers (`V9(4)`, `V9(7)`) where the clamp never bites. The refuter overturned
to DIVERGES and produced the repro — reading `FromDouble` rather than sampling outputs. Second time today the
adversarial pass paid for itself on something the first pass had in front of it. **An all-CONFORMS batch is a
warning sign, not a good result**, and this is why that sentence is now in the fan-out prompt.

**⚠ AND FIVE OF THE TWELVE REFUTERS DIED ON API 529.** `char`, `baseconvert`, `byte-length`, `concat`,
`char-national` errored, so their out files hold UNREFUTED stage-one output — the exact state that cost a wrong
published number in entry 1116, arriving this time by a different route. The workflow reported "completed"
because it had; the per-agent failures are in the notification's own `<failures>` block. Resumed with
`resumeFromRunId`, which replays the cached agents and re-runs only the five. **A workflow completing is not the
same as every agent in it succeeding** — read the failure list before the results.

**A CITATION DEFECT AT THE SAME SITE.** The `FromDouble` comment cited §14.6.13.1.1 "Table 13" for
EC-ARGUMENT-FUNCTION being fatal. `cite.py --check` FAILS on it — that clause is titled "General" — and the real
one is **§14.6.13.1.6**. It had already propagated from this comment into an agent's adjudication. The
quoted-fragment audit could not have caught it: the citation carries no quote, so nothing mechanical connects the
number to the text it claims. That residue is real and stated in the audit's own docstring.

**GATES.** Conformance **4147/4147**, zero skipped, nothing red — the numeric core's return type changed
underneath the whole suite. Unit **963/963**. Characterization **33/33**.

