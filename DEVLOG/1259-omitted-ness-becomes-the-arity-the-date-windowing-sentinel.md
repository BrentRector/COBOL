## Entry 1259 — 2026-08-09 11:40 PDT — Omitted-ness becomes the arity: the date-windowing sentinel dies and errored delegations stop manufacturing values — five more rows

PB65's date-windowing members are out. CobolDate.YearToYyyy carried omitted-ness IN BAND — a
`baseYear == 0` sentinel resolved to the clock year BEFORE the §15.100.3 r4 range test — so a program
that WROTE an argument-3 of 0 (an r4 violation that must set EC-ARGUMENT-FUNCTION) was silently
windowed against the execution year, and the r6 sum was formed from the effective base rather than
the written one. The renderer emits exactly the arguments present, so the arity is compile-time
fact: the two-argument overloads ARE the omitted form (the r5 clock-year default through the ONE
injectable clock), and the three-argument overloads validate argument-3 exactly as written. The
same landing fixed the wrapper composition defect: DAY-TO-YYYYDDD computed
`YearToYyyy(...) × 1000 + nnn` without consulting the delegated core's outcome, manufacturing
0000365 out of an evaluation that had errored — both wrappers now return the documented default 0
WHOLE (the core's 0 is never a legal windowed year, which is what makes the error path detectable).

Probes before goldens: ZERO-A3 flipped from 19851003 (silent re-window) to 0; R6VIOL flipped from a
manufactured 365 to 0; the legal controls match their r2a/r2b hand derivations, and my own first
probe used in-window arguments (8000+1995 = 9995) — re-derived and re-probed with a genuine
violation rather than trusting the first zero-looking output. Golden pb65_date_windowing (4 derived
lines); the CobolDate unit family held 36/36 through the overload split, pinned clocks intact.
FIVE rows re-verdict CONFORMS+tested. GAP 4,203 → 4,198; the defect backlog stands at 196
bad-verdict rows.

