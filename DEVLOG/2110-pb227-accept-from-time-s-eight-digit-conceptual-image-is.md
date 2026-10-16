## Entry 2110 — 2026-10-16 14:00 PDT — PB227: ACCEPT FROM TIME's eight-digit conceptual image is pinned whole, with its date siblings

kb/Work PB227 (wave 1097, group I) said that no test observed the whole §14.9.1.4 GR11 image of TIME
(HHMMSScc, "an unsigned elementary integer data item of usage display eight digits in length"): the one TIME witness
stored into PIC 9(4) and so saw only seconds and hundredths.

**Re-probe.** The note's mechanism was written before kb/Work PB887 and named a `sendImage` path that no longer exists:
`AcceptDisplayEmitter.EmitAcceptTemporal` now fills the binder's GR11 conceptual item (an unsigned PIC 9(8)
usage-display compiler temp, `AcceptKinds.ConceptualDigits`) and GR6's transfer is the bound implicit MOVE. Two later
tests already read all eight digits of 14:30:45.67. Still unpinned: the leading-zero hour, the range endpoints 00 and
23 / 00 and 99, and that hundredths truncate. The compiler was right on every one. On this build a PIC 9(8) and a
PIC X(8) receiver printed `09050307` for 09:05:03.07, `00000000` for midnight, `23595999` for 23:59:59.99 and
`14304567` for 14:30:45.679 (679 ms is 67 hundredths; rounding would give 68). The compiler is unchanged.

**Witnesses.** `AcceptDifferentialTests.Time_ExactWidthReceiver_ShowsTheEightDigitImage` (Theory, --std 85, the
introducing edition; TIME has no edition gate, so there is no negative) pins those four clocks through both arms of
GR6's MOVE, numeric and alphanumeric. Sibling sweep over the other conceptual images (GR7-GR10):
`Date_ExactWidthReceivers_ShowEachZeroPaddedImage` (--std 2002, where the YYYY phrases begin) pins the zero padding
of DATE, DATE YYYYMMDD, DAY and DAY YYYYDDD at 2005-01-05 (`050105`, `20050105`, `05005`, `2005005`) and their
upper endpoints at 2004-12-31, a leap year (`041231`, `20041231`, `04366`, `2004366`). DAY-OF-WEEK is one digit and
was already pinned. Every expected value comes from the GR7-GR11 position tables. None was measured.

**Rows.** Batch `w1097i-PB227-accept-time-image`: GR-14.9.1.4-11 goes PARTIAL to CONFORMS (GAP 71 to 70), and
GR-14.9.1.4-7 to -10 each gain the date witness. PB227 is landed with `closes_rows: ["GR-14.9.1.4-11"]`.

