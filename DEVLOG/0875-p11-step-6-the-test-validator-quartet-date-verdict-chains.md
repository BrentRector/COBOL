## Entry 875 — 2026-07-17 11:53 PDT — P11 Step 6 — the TEST validator quartet: date verdict chains + NUMVAL positional scanners + the ONE BindNumvalCFamily bind with ANYCASE; Deferred 6→2

The scout notes caught TWO phase-doc omissions before a line was written: the §15.93.4/§15.94.4 r1c THIRD
verdict leg (FUNCTION LENGTH(argument-1)+1 for zero-length / all-spaces / valid-but-incomplete like " +.",
NOT the position of any space), and TEST-NUMVAL-C's optional ANYCASE keyword (§15.94.2 — orthogonal to the
currency argument, and NOT covered by the A.4.9 LOCALE non-support: only the LOCALE phrase is decision-3).

- CobolDate: TestDateYyyymmdd / TestDayYyyyddd — if/else-if verdict CHAINS, year before month before day
  (16000230 → 1, not 2; TEST-DAY has NO code 3; codes confirmed by the informative D.31.3.8/9). Gregorian
  leap via DateTime.DaysInMonth / IsLeapYear (1900 not leap, 2000 leap — golden-pinned).
- CobolIntrinsics.Exact.cs (beside TestNumvalF — the scout corrected the phase doc's Text.cs placement):
  TestNumval / TestNumvalC as dedicated position-reporting SCANNERS (positions are ordinal in the ORIGINAL
  string, so the remove-and-delegate value path cannot report them). Three-leg verdicts: 0 (r1a) /
  first-error position (r1b — the verbatim "0 1"→3 embedded-space sub-note; digit caps 31 native / 34 SDIDI
  via the renderer's `num.StandardDecimal` arithmetic-mode flag, standard-binary's 35 → P12/P13) / LENGTH+1
  (r1c — a digit-free scan that BROKE on a real char is r1b at that char, one that ran off the end is r1c).
  TestNumvalC scans the §15.68.3 formats: sign-before-currency (A) XOR trailing sign/CR/DB (B), at-most-once
  currency char-for-char (ANYCASE folds it, r4f), ARBITRARY-length grouping groups ('1,23,4.5' conforms),
  grouping illegal after the decimal, comma-mode SWAPS both separator roles (r4d).
- Binder: the ONE BindNumvalCFamily bespoke bind now owns NUMVAL-C AND TEST-NUMVAL-C (§15.94.3 r1 imports
  every §15.68.3 rule) — the §15.68.3-r3 compilation-unit currency injection MOVED here (the old inline
  injection deleted; singular pattern) + ANYCASE extraction. BoundIntrinsicCall.FindAnycase RENAMED to the
  ONE `Anycase` flag shared by FIND-STRING (§15.37.4 r4) and the currency fold. NumvalC's value parser
  gained the matching anycase leg (§15.94.1 — TEST verifies what NUMVAL-C accepts).
- Tests: golden 2002/intrinsics_test_validators — 34 probes spanning every verdict code, the chain-precedence
  probe, the Gregorian century pair, the verbatim spec examples ("0 1"→3, " +."→4), the 32nd-digit cap, the
  ANYCASE pair (ALL 34 first-try byte-exact via CLI); negative test_validators_below_2002 @85 (all four
  → COBOLNET1502); matrix row test-numval-2002 (147 rows); NEW TestNumvalScannerTests (4 facts: the
  comma-mode role swap, the 34-digit SDIDI cap, b-vs-c-leg discrimination, the ANYCASE fold + NumvalC
  value-parser mirror); GreenfieldOnly exclusion.

Battery on the staged tree: conformance **3487/3487** (+6) · unit **301/301** (+4) · legacy 1196+636 ·
guard-fast first pass 351+2 (IX109A/IX110A, the indexed-suite flake) → **solo rerun 353 MATCH, 0
regressions, ALL GREEN** (the DEVLOG-870/872/873 environmental flake class — now 5th occurrence, always a
file-I/O suite under JOBS=32). Deferred: **6 → 2** (BYTE-LENGTH, CONCATENATE — both Step 7).

