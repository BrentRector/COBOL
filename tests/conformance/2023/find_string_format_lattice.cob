      *> ISO §15.37.2 general format — the COMPLETE lattice of the format's three optional elements.
      *> The format is: FUNCTION FIND-STRING argument-1 argument-2 [LAST] [[START AFTER] argument-3]
      *> [ANYCASE], so a written call is one of 12 shapes: [LAST] present or absent × argument-3 absent,
      *> written BARE, or introduced by START AFTER (the inner bracket) × [ANYCASE] present or absent.
      *> Every shape is written here once, with the value §15.37.4 requires for it.
      *>
      *> H = "XYxyXYxyXYxy" (12 characters), N = "xy".
      *>   Without ANYCASE the comparison is on the operands as written: H(i:2) equals "xy" only at
      *>   i = 3, 7, 11 — the three lowercase runs. Occurrences: {3, 7, 11}.
      *>   With ANYCASE (§15.37.4 r4) the matching rules are as if every uppercase letter in BOTH
      *>   operands were replaced by its lowercase letter, i.e. as if H were "xyxyxyxyxyxy" and N "xy":
      *>   H(i:2) equals "xy" at i = 1, 3, 5, 7, 9, 11. Occurrences: {1, 3, 5, 7, 9, 11}.
      *> §15.37.4 r1 returns the FIRST occurrence, or the LAST when LAST is written; r2 makes
      *> argument-3 the number of matches to ignore before that determination is made, so argument-3 = 1
      *> drops the leading occurrence in the default direction and the trailing one under LAST.
      *> The [START AFTER] words are an optional introducer of the SAME argument-3, so S03/S05, S04/S06,
      *> S09/S11 and S10/S12 are each one meaning written two ways and must agree.
      *>
      *> NOTHING HERE PINS WHAT AN "OCCURRENCE" IS WHEN MATCHES OVERLAP: both needles above were chosen so
      *> that adjacent matches are exactly len(argument-2) apart, so every value below is the same under an
      *> overlapping-inclusive and a non-overlapping reading. That axis is pinned by its own golden,
      *> conformance:2023/pb314_find_string_overlapping_matches, under the determination recorded in
      *> docs/CONFORMANCE.md (overlapping matches count; §15.37.4 states no consumption rule, unlike
      *> §15.87.4 r3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FSFMT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 H PIC X(12) VALUE "XYxyXYxyXYxy".
       01 N PIC X(2)  VALUE "xy".
       01 P PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
      *> ── no LAST, no argument-3 ──────────────────────────────────────────────────────────────────
           MOVE FUNCTION FIND-STRING(H N) TO P.
           DISPLAY "S01=" P.
           MOVE FUNCTION FIND-STRING(H N ANYCASE) TO P.
           DISPLAY "S02=" P.
      *> ── no LAST, argument-3 written BARE ────────────────────────────────────────────────────────
           MOVE FUNCTION FIND-STRING(H N 1) TO P.
           DISPLAY "S03=" P.
           MOVE FUNCTION FIND-STRING(H N 1 ANYCASE) TO P.
           DISPLAY "S04=" P.
      *> ── no LAST, argument-3 introduced by START AFTER ───────────────────────────────────────────
           MOVE FUNCTION FIND-STRING(H N START AFTER 1) TO P.
           DISPLAY "S05=" P.
           MOVE FUNCTION FIND-STRING(H N START AFTER 1 ANYCASE) TO P.
           DISPLAY "S06=" P.
      *> ── LAST, no argument-3 ─────────────────────────────────────────────────────────────────────
           MOVE FUNCTION FIND-STRING(H N LAST) TO P.
           DISPLAY "S07=" P.
           MOVE FUNCTION FIND-STRING(H N LAST ANYCASE) TO P.
           DISPLAY "S08=" P.
      *> ── LAST, argument-3 written BARE ───────────────────────────────────────────────────────────
           MOVE FUNCTION FIND-STRING(H N LAST 1) TO P.
           DISPLAY "S09=" P.
           MOVE FUNCTION FIND-STRING(H N LAST 1 ANYCASE) TO P.
           DISPLAY "S10=" P.
      *> ── LAST, argument-3 introduced by START AFTER — S12 is the MAXIMAL shape, every optional
      *> ── element of §15.37.2 written at once, which no case exercised before.
           MOVE FUNCTION FIND-STRING(H N LAST START AFTER 1) TO P.
           DISPLAY "S11=" P.
           MOVE FUNCTION FIND-STRING(H N LAST START AFTER 1 ANYCASE)
               TO P.
           DISPLAY "S12=" P.
           STOP RUN.
       END PROGRAM L1FSFMT.
