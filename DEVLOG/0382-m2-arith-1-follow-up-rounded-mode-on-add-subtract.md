## Entry 382 — M2-ARITH-1 follow-up: `ROUNDED MODE` on ADD/SUBTRACT CORRESPONDING (ISO §14.9.4)

Entry 379 implemented `ROUNDED MODE` for the ordinary arithmetic statements but left ADD/SUBTRACT
CORRESPONDING on the old bool ROUNDED (mode 0/1 only). Closed that gap with the same threading: the two CORR
grammar rules' bare `ROUNDED?` became `roundedPhrase?`; `BindAdd`/`BindSubtract` read the mode via the shared
`BindRounded` and pass it to `BindCorresponding` (new `int roundingMode` param) → `BoundCorrespondingStatement.
RoundingMode`; `LowerCorresponding`'s `corr.IsRounded ? 1 : 0` became `corr.RoundingMode`, which it already
applies uniformly to every corresponding pair.

Verified `ADD CORRESPONDING SRC TO DST ROUNDED MODE IS TRUNCATION` → 1.25→1.2 vs `… NEAREST-AWAY-FROM-ZERO`
→ 1.3, applied to both matched pairs. Conformance `tests/conformance/2002/corresponding_rounded_mode`. M2-ARITH-1
now has only two follow-ups left (OPTIONS DEFAULT ROUNDED MODE — entangled with M2-ARITH-2 OPTIONS-clause
parsing; PROHIBITED → EC-SIZE-TRUNCATION on inexact results).

Guard ALL GREEN: **1047 unit / 474 integration / 364 NIST**, 0 regressions.

