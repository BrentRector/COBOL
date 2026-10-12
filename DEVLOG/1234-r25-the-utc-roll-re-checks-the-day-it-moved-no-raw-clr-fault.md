## Entry 1234 — 2026-08-08 16:20 PDT — R25: the UTC roll re-checks the day it moved — no raw CLR fault, no year 1600

FORMATTED-DATETIME("YYYYMMDDThhmmssZ", 3067671, 86399, -1439): every argument individually legal —
the §15.40.3 r4/r5 screens pass — and the §15.40.4 r2 UTC adjustment then rolls the day past
9999-12-31, where Epoch.AddDays threw a raw ArgumentOutOfRangeException. The low-end mirror rolled
day 1 to day 0 and EMITTED year 1600, which §15.3.1.3 flatly bars ("greater than 1600"). The
analyzer side of this exact shape was PB23; this was the emit side, waiting one layer down.

The guard is one block after the roll loops, hasDate-gated — a time-only format never reads the day
and must keep rolling freely — raising EC-ARGUMENT-FUNCTION with the §15.3 default "", exactly as an
out-of-range argument-2 does, and covering FORMATTED-TIME's date-bearing formats through the shared
EmitFormatted. CobolDateUtcRollRangeTests pins five shapes (the two failures, the two one-day-inside
legal rolls emitting 99991231T235859Z / 16010101T000100Z, the unguarded time-only roll); golden
r25_utc_roll_date_range pins the RAISE end-to-end plus the boundary-no-roll control.

Gate: Unit CobolDate 36/36 · Conformance Intrinsic|Corpus 656/656 · characterization 33/33 · the
golden green by name. R26 next, same sitting.

