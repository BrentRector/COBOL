## Entry 1245 — 2026-08-08 18:58 PDT — R28 decided from the primary documents: two windows were wrong in EACH direction

The owner pointed the run at the web for the 2002-vs-2014 gap, and the research workflow brought back
the primary documents themselves: the WG4/J4 CD 1.2 (2009) — the committee draft of ISO 1989:2014 —
whose Annex D.2 item 4 lists the new intrinsic functions verbatim (archive.org copy, downloaded and
grepped by the verifying agent, corroborated against the published-2014 inventory), and the COBOL
Consortium's COBOL2002 introduction naming the 2002 additions.

The verdicts: the nine provisional 2014 windows CONFIRMED (COMBINED-DATETIME, the FORMATTED-* five,
INTEGER-OF-FORMATTED-DATE, SECONDS-FROM-FORMATTED-TIME, TEST-FORMATTED-DATETIME, TRIM). NUMVAL-F and
TEST-NUMVAL-F are 2002 — the provisional window was REJECTING LEGAL 2002 COBOL, the exact harm R28
feared, now fixed. And the same D.2 list exposed the mirror error nobody had asked about:
LOCALE-TIME-FROM-SECONDS and SECONDS-PAST-MIDNIGHT are 2014-new while our catalog said 2002 —
over-accepting — also fixed. The edition-gate sweep caught the 2002 intrinsics_date_window golden's
SECONDS-PAST-MIDNIGHT leg, which split into the new 2014 seconds_past_midnight golden; the windowing
trio keeps its 2002 coverage. The catalog's "provisional pending the matrix wave" caveat is retired.

Probes: NUMVAL-F accepts at --std 2002 (was 1502); SECONDS-PAST-MIDNIGHT draws 1502 at 2002, accepts
at 2014. Gate: full Unit 4099/4099 · Intrinsic|Corpus 667/667 · the split goldens by name ·
characterization 33/33. R37's and R13's surveys landed in their notes the same hour — R37 adjudicated
(strict stands, the vendor sizes recorded for any future dialect decision), R13 now an informed owner
call (IBM prints the container value, GnuCOBOL truncates to the PICTURE — the vendors are split).

