## Entry 1221 â 2026-08-08 10:52 PDT â R24: the seconds argument finally carries its VALUE â and the last carrier dispatch missing a Dec arm was hiding behind a sibling that had one

The FORMATTED-* seconds channel was blind to two of the four value carriers: a FLOAT argument's fraction
died in a `(long)` cast (FORMATTED-TIME("hhmmss.ss", 45296.5) printed 12345600), and a wide-PICTURE
argument's unscaled form WRAPPED through the same cast â a 9(5)V9(15) item holding the perfectly legal
45296.5 fabricated 02:20:03 with no exception, because the range check received the post-wrap garbage.
Under STANDARD-DECIMAL, an expression argument was a raw CS1503.

Probing before editing shrank the ledger's fix list by a third: CombinedDatetime was already correct on all
four carriers (PB32's Arg landing), and the offset arguments were already safe (PB22's unified AsInt). What
remained was surgical â a carrier-total SecondsArg helper for the two formatted arms (fixed passes raw at
its own scale; float lands at scale 9, the FORMATTED-CURRENT-DATE nanosecond convention; SDIDI lands exactly
at scale 18, the documented Â§15.3.3.2 maximum; unsigned-wide takes the R10 funnel), the CobolDate seconds
parameters widened long â Int128 with an exact scale>18 pre-reduction that also closes F29's divisor-wrap
sibling, and â the one that mattered most â AsInt's missing Dec arm.

That last one is the PB14/PB32 lesson completing its tour: a Dec NumX carries Scale 0 BY CONVENTION, so
AsInt's `Scale == 0` fast path handed the raw CobolDec expression to IntegerArg(Int128) as CS1503. It hid
for a while because FACTORIAL's channel (IntArg â Arg) de-Decs upstream while the formatted family's
channel (ArgInt â ArgNum) does not â two entries, one funnel, and the funnel itself was the arm short.
The dispatch is now carrier-total in the last place it wasn't.

En route, Â§15.3.3.2 settled a suspicion the probes raised: the decimal separator "does not appear in the
data associated with a basic common time format" â so the no-dot output was CONFORMING all along, and the
ledger F46's expected 19950215T05142781+0500 (which the fixed tree now prints exactly) was right to omit it.

Also this entry: the CI artifact actions moved v4 â v5 (the Node 20 deprecation warnings on every shard),
and â owner-flagged after that workflow-only push burned a full two-platform matrix â push-triggered runs
now skip doc-only changes (DEVLOG/docs/kb/PROMPT/CLAUDE/README paths-ignore; workflow edits and every PR
still run everything).

Golden formatted_time_seconds_channel. Gate: wave-local Intrinsic|Date|Formatted|Corpus 648/648 Â· Unit
filter 67/67 Â· characterization 33/33. The R14+R18 batch battery came back ALL GREEN, 0 differential flips,
before these edits began. Next: R12 (interprogram) Â· R15 Â· R16, 11 actionable.

