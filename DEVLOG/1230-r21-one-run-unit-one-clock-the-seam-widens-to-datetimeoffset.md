## Entry 1230 — 2026-08-08 13:48 PDT — R21: one run unit, one clock — the seam widens to DateTimeOffset and the last two direct reads die

CURRENT-DATE and FORMATTED-CURRENT-DATE predated the RunUnit.Clock seam and kept their direct
DateTimeOffset.Now reads through every green battery — no runtime test can tell two clocks apart while
both tick together. With COBOLNET_CLOCK pinned they split: SECONDS-PAST-MIDNIGHT returned the pinned
instant while the two now-functions returned real now, two clocks inside one run unit, and no
deterministic golden for §15.21.3 r1 / §15.38.4 r1 could exist.

The fix is the ledger's own: IClock.Now() returns DateTimeOffset — §15.21.3 r1's positions 17–21 render
the local time differential factor, so the seam must CARRY the offset — and both functions now read
RunUnit.Current.Clock.Now() (§15.21.1 / §15.38.4 r1: "provided by the system on which the function is
evaluated", and the system clock is ONE; citations cite.py-checked). SystemClock parses the pin with
AssumeLocal: an offset-less pin behaves exactly as before, and a pin may now carry an explicit offset
(+02:30) — which is what makes the new tests machine-independent: CURRENT-DATE's full 21 characters
under a +02:30 pin prove the offset came through the seam, and the three-now-functions-one-instant
fact IS the defect's repro. AcceptSource takes .DateTime (the reading as pinned, not re-zoned).

ClockSeamDriftTests is the shape that can actually see this class of defect: a source-form guard that
no DateTime[Offset].{Now,UtcNow,Today} survives in src/Cobol.Net.* outside SystemClock's fallback and
the WHEN-COMPILED compile-time capture (§15.99.3 r2) — proven RED against an injected offender before
being trusted, then green. DESIGN-runtime-library §2.7 records the widened contract in the same change
set; R21's note flips to landed. Gate: Unit clock/date/drift filter 3204/3204 · Conformance
Intrinsic|Corpus|Accept 693/693 · characterization 33/33.

