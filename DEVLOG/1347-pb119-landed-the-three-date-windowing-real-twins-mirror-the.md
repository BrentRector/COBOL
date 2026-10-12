## Entry 1347 — 2026-08-28 23:43 PDT — PB119 LANDED: the three date-windowing Real twins mirror the exact cores' overload split — an OMITTED argument-3 windows on the execution year instead of arriving as an explicit 0 the r4 screen rejects

The two-arm dispatch family's sixth occurrence (batch 7's find on YEAR-TO-YYYY), and the sweep found the SAME
shape in `DateToYyyymmddReal` and `DayToYyyydddReal` — all three defaulted `baseYear = 0` and called the
three-argument core unconditionally, so a float-typed call with argument-3 omitted hit §15.100.3 r4's "greater
than 1600" screen and returned the §15.3 default 0 (or aborted under checking) where r3/r5 require windowing
against the execution year. The fix is the ARITY, not a sentinel: a WRITTEN 0 must still reach the r4 screen,
so only the overload split the exact cores already use can express omitted-vs-written. Golden
`2002/pb119_windowing_float_args`: the clock-free written-argument-3 forms hand-derived per §15.100.4 r1 /
§15.24 (YEAR-TO-YYYY(50.0 60 3000) = 3050; DATE-TO-YYYYMMDD(590401.0 20 1985) = 19590401;
DAY-TO-YYYYDDD(59100.0 20 1985) = 1959100) plus the omitted forms range-checked against the execution window
(pre-fix each printed 0). AR-15.100.3-5 → CONFORMS.

