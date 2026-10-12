## Entry 874 — 2026-07-17 11:10 PDT — P11 Step 5 — the Y2K windowing trio on ONE YearToYyyy core + SECONDS-PAST-MIDNIGHT on the RunUnit.Clock seam; Deferred 10→6

The scout notes caught the phase doc's central Step-5 assumption WRONG before a line was written:
argument-2 is NOT a "window size" — the window is ALWAYS exactly 100 years, and argument-2 is a SIGNED
offset added to argument-3 (default: the EXECUTION-time year, §15.100.3 r5) giving maximum-year = the
window's ENDING year (§15.100.4 r1). r2a/r2b then pick the unique in-window year whose low two digits
equal argument-1. DATE-TO-YYYYMMDD / DAY-TO-YYYYDDD are PURE DELEGATION (§15.23.4 r1 / §15.25.4 r1 define
them by reference), so the runtime has ONE windowing core — `CobolDate.YearToYyyy` — and two /10000-and-
recombine wrappers (the singular-mechanism rule). The §15.23.3-r6 "year at the time of execution" wording
divergence is resolved to the uniform argument-2+argument-3 check, documented in the XML doc.

SECONDS-PAST-MIDNIGHT: type NUMERIC (fractional seconds intended — Annex D.31.5.4's 12-fraction-digit
example). The scout's discrepancy note mattered here: CURRENT-DATE calls `DateTimeOffset.Now` DIRECTLY and
bypasses the P8 IClock seam — so this function rides `RunUnit.Current.Clock.Now()` (the AcceptSource
pattern) and is COBOLNET_CLOCK-deterministic. It returns the local time-of-day TICK count = the unscaled
value at SCALE 7, the documented §15.80.3-r3 precision (100 ns). LEAP-SECOND is unsupported (§7.3.17
default OFF is the only mode), so the §15.80.3-r4 ≥86400 question is answered "no"; range [0, 86 400).

Tests: golden `2002/intrinsics_date_window` — 12 probes, every one PINNING argument-3 (a defaults-only
golden would flip every calendar year): the spec's OWN §15.100.4/§15.23.4/§15.25.4 NOTE-1 examples, the
49/50 boundary pair straight AND through the composite, the arg-1=0 legal leg, negative argument-2 forms,
an SPM range probe — ALL 12 first-try byte-exact through the CLI. NEW `CobolDateWindowingTests` (5 unit
facts): the execution-year default legs and the pinned-clock exact-tick value (05:14:27.8124791 →
188 678 124 791 ticks) the corpus runner cannot pin (it passes no env). Negative `date_window_below_2002`
@85 (all four names → COBOLNET1502) + matrix row `date-to-yyyymmdd-2002` (146 rows) + GreenfieldOnly.

Battery on the staged tree: conformance **3481/3481** (+6) · unit **297/297** (+5) · legacy 1196+636 ·
guard-fast **353 MATCH, 0 regressions, ALL GREEN — first pass, no flake this round**. Deferred: **10 → 6**
(BYTE-LENGTH, the four TEST-* validators, CONCATENATE).

