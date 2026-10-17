*>--- conformance-case
*> id: pb124_always_integral_zoo
*> rules: [AR-15.3-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB124 (AR-15.3-6) — the FAIL-OPEN COMPLEMENT of the 15.3 type-6 screen, pinned compiling
      *> AND running. The screen rejects only PROVABLY not-always-integral expressions; each line here is
      *> legal source that IS always-integral and must never be rejected (each defeated a broader screen
      *> during design): (I/2)*2 = I exactly in exact-rational evaluation; I/I = 1 for every nonzero I;
      *> 362880 = 9! is divisible by every 1-digit divisor; S*10 cancels S's scale 1; S - S nets the scaled
      *> term to zero. Values hand-derived (CHAR returns the character at the given ordinal position; ORD("A")
      *> = 66 in the native collating sequence, so CHAR(66) = "A" and CHAR(65) = "@").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB124ZO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I PIC 9(4) VALUE 6.
       01 S PIC 9V9 VALUE 1.5.
       01 D9 PIC 9 VALUE 7.
       01 R PIC 9(6).
       01 RS PIC X(1).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION CHAR((I / 2) * 2 + 60) TO RS
           IF RS = "A" DISPLAY "DIV2 OK" ELSE DISPLAY "DIV2 BAD " RS
           END-IF
           MOVE FUNCTION CHAR(I / I + 65) TO RS
           IF RS = "A" DISPLAY "SELF OK" ELSE DISPLAY "SELF BAD " RS
           END-IF
           COMPUTE R = 362880 / D9
           MOVE FUNCTION CHAR(362880 / D9) TO RS
           DISPLAY "FACT OK"
           MOVE FUNCTION CHAR(S * 10 + 50) TO RS
           IF RS = "@" DISPLAY "CANC OK" ELSE DISPLAY "CANC BAD " RS
           END-IF
           MOVE FUNCTION CHAR(S - S + 66) TO RS
           IF RS = "A" DISPLAY "NET0 OK" ELSE DISPLAY "NET0 BAD " RS
           END-IF
           STOP RUN.
