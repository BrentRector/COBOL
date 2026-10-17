*>--- conformance-case
*> id: intrinsics_test_validators
*> rules: [RV-15.90.4-1, RV-15.91.4-1, RV-15.93.4-1, FMT-15.91.2, FMT-15.93.2, FMT-15.94.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.90 TEST-DATE-YYYYMMDD / §15.91 TEST-DAY-YYYYDDD / §15.93 TEST-NUMVAL / §15.94 and NUMVAL-C
      *> TEST-NUMVAL-C — the validator quartet (P11 Step 6). Values hand-derived in
      *> docs/rearchitecture/PHASE-11-scout-notes.md (spec:validators): the date verdicts are if/else-if
      *> CHAINS (year before month before day — 16000230 is 1, not 2; TEST-DAY has NO code 3, D.31.3.8/9);
      *> the NUMVAL verdicts have THREE legs — 0 (r1a) / the first-error position (r1b, incl. the verbatim
      *> "0 1"→3 embedded-space sub-note and the 32nd-digit native cap) / LENGTH+1 (r1c — zero-length,
      *> all-spaces, incomplete like " +."). No SPECIAL-NAMES CURRENCY, so the injected §15.68.3-r3
      *> compilation-unit currency is the default '$'.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P11TESTVAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R2 PIC 9(2).
       01 W40 PIC X(40) VALUE ALL "1".
       PROCEDURE DIVISION.
       MAIN.
      *> ── TEST-DATE-YYYYMMDD (§15.90.4 r1a-d; D.31.3.8) ──
           COMPUTE R2 = FUNCTION TEST-DATE-YYYYMMDD(20240229)
           DISPLAY "D1=" R2
           COMPUTE R2 = FUNCTION TEST-DATE-YYYYMMDD(16001231)
           DISPLAY "D2=" R2
           COMPUTE R2 = FUNCTION TEST-DATE-YYYYMMDD(20241301)
           DISPLAY "D3=" R2
           COMPUTE R2 = FUNCTION TEST-DATE-YYYYMMDD(20240001)
           DISPLAY "D4=" R2
           COMPUTE R2 = FUNCTION TEST-DATE-YYYYMMDD(20230229)
           DISPLAY "D5=" R2
           COMPUTE R2 = FUNCTION TEST-DATE-YYYYMMDD(20240431)
           DISPLAY "D6=" R2
      *> Chain precedence: the year check fires before month/day (16000230 -> 1, not 2).
           COMPUTE R2 = FUNCTION TEST-DATE-YYYYMMDD(16000230)
           DISPLAY "D7=" R2
      *> ── TEST-DAY-YYYYDDD (§15.91.4 r1a-c; D.31.3.9 — no code 3) ──
           COMPUTE R2 = FUNCTION TEST-DAY-YYYYDDD(2024366)
           DISPLAY "J1=" R2
           COMPUTE R2 = FUNCTION TEST-DAY-YYYYDDD(2023366)
           DISPLAY "J2=" R2
      *> The Gregorian century rule: 1900 is NOT leap; 2000 (÷400) IS.
           COMPUTE R2 = FUNCTION TEST-DAY-YYYYDDD(1900366)
           DISPLAY "J3=" R2
           COMPUTE R2 = FUNCTION TEST-DAY-YYYYDDD(2000366)
           DISPLAY "J4=" R2
           COMPUTE R2 = FUNCTION TEST-DAY-YYYYDDD(1600100)
           DISPLAY "J5=" R2
           COMPUTE R2 = FUNCTION TEST-DAY-YYYYDDD(2024000)
           DISPLAY "J6=" R2
      *> ── TEST-NUMVAL (§15.93.4 r1a/r1b/r1c over the §15.67.3 formats) ──
           COMPUTE R2 = FUNCTION TEST-NUMVAL("123.45")
           DISPLAY "N1=" R2
      *> r2 of §15.67.3: embedded spaces BEFORE the first digit are ignored.
           COMPUTE R2 = FUNCTION TEST-NUMVAL(" + 12 ")
           DISPLAY "N2=" R2
      *> Trailing CR/DB in any case combination (format B).
           COMPUTE R2 = FUNCTION TEST-NUMVAL("123cr")
           DISPLAY "N3=" R2
      *> 'digit [. [digit]]' — a trailing decimal point with no fraction is VALID.
           COMPUTE R2 = FUNCTION TEST-NUMVAL("5.")
           DISPLAY "N4=" R2
      *> The verbatim §15.93.4 r1b sub-note-1 example: "0 1" -> 3 (the first NON-space after the spaces).
           COMPUTE R2 = FUNCTION TEST-NUMVAL("0 1")
           DISPLAY "N5=" R2
      *> A comma is not a NUMVAL character (no DECIMAL-POINT IS COMMA here).
           COMPUTE R2 = FUNCTION TEST-NUMVAL("1,234")
           DISPLAY "N6=" R2
      *> The second decimal point is the first character in error.
           COMPUTE R2 = FUNCTION TEST-NUMVAL("1.2.3")
           DISPLAY "N7=" R2
      *> r1c: all-spaces -> LENGTH+1 (NOT the position of a space).
           COMPUTE R2 = FUNCTION TEST-NUMVAL("    ")
           DISPLAY "N8=" R2
      *> r1c NOTE's verbatim incomplete example: " +." -> LENGTH(3)+1.
           COMPUTE R2 = FUNCTION TEST-NUMVAL(" +.")
           DISPLAY "N9=" R2
      *> r1b sub-note 2: the 32nd digit under native arithmetic (40 all-'1' digits).
           COMPUTE R2 = FUNCTION TEST-NUMVAL(W40)
           DISPLAY "N10=" R2
      *> ── TEST-NUMVAL-C (§15.94.4 over the §15.68.3 formats; injected default currency '$') ──
           COMPUTE R2 = FUNCTION TEST-NUMVAL-C("$1,234.56")
           DISPLAY "C1=" R2
      *> Format A: the sign precedes the currency.
           COMPUTE R2 = FUNCTION TEST-NUMVAL-C("-$123.45")
           DISPLAY "C2=" R2
      *> Format B: currency + digits + trailing CR.
           COMPUTE R2 = FUNCTION TEST-NUMVAL-C("$123.45CR")
           DISPLAY "C3=" R2
      *> Currency-then-sign matches NEITHER format: '-' at position 2 is first in error.
           COMPUTE R2 = FUNCTION TEST-NUMVAL-C("$-123")
           DISPLAY "C4=" R2
      *> The currency precedes the digits in BOTH formats: a trailing '$' is in error at position 5.
           COMPUTE R2 = FUNCTION TEST-NUMVAL-C("1234$")
           DISPLAY "C5=" R2
           COMPUTE R2 = FUNCTION TEST-NUMVAL-C("$12X4")
           DISPLAY "C6=" R2
           COMPUTE R2 = FUNCTION TEST-NUMVAL-C("0 1")
           DISPLAY "C7=" R2
           COMPUTE R2 = FUNCTION TEST-NUMVAL-C("   ")
           DISPLAY "C8=" R2
      *> Grouping groups are ARBITRARY-length digit runs (r4a 'digit [, digit]…' — no 3-digit rule).
           COMPUTE R2 = FUNCTION TEST-NUMVAL-C("1,23,4.5")
           DISPLAY "C9=" R2
      *> §15.68.3 r4a: the argument-2 currency matches character for character ('usd' vs 'USD' fails at 1)…
           COMPUTE R2 = FUNCTION TEST-NUMVAL-C("usd 12", "USD")
           DISPLAY "C10=" R2
      *> …and r4f: ANYCASE folds the currency match.
           COMPUTE R2 = FUNCTION TEST-NUMVAL-C("usd 12", "USD", ANYCASE)
           DISPLAY "C11=" R2
           STOP RUN.
