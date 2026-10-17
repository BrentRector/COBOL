*>--- conformance-case
*> id: char_national
*> rules: [RV-15.16.4-1, RV-15.70.4-2, FMT-15.16.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.16 CHAR-NATIONAL — the character at the 1-based ordinal position of the NATIONAL
      *> program collating sequence (native: UTF-16 code-point order, one char per national
      *> position, D-N1 — position n is code point n-1). The result is class national (15.16.1):
      *> proven by a MOVE to PIC N + an N"" comparison and by FUNCTION LENGTH = 1 character
      *> position. Also pins ORD over a NATIONAL argument (15.70.3 admits category national;
      *> 15.70.4 r2 = the ordinal in the national sequence) — the CHAR-NATIONAL inverse.
      *> The wide leg uses U+4E16 (ordinal 19991) via an N"" literal (UTF-8 source), compared
      *> in-program so the expected output stays ASCII.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CHNATP10EN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N-C    PIC N.
       01 N-W    PIC N VALUE N"世".
       01 LEN-R  PIC 9(2).
       01 ORD-R  PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
      *> Ordinal 66 = code point 65 = "A" (15.16.4 r1).
           DISPLAY "C1=" FUNCTION CHAR-NATIONAL(66).
           MOVE FUNCTION CHAR-NATIONAL(66) TO N-C.
           IF N-C = N"A" DISPLAY "EQ=YES" ELSE DISPLAY "EQ=NO".
      *> One character position (15.16.1; LENGTH 15.50 counts positions, not bytes).
           MOVE FUNCTION LENGTH(FUNCTION CHAR-NATIONAL(90)) TO LEN-R.
           DISPLAY "LEN=" LEN-R.
      *> A wide national character: ordinal 19991 = U+4E16 (compared, not displayed).
           IF FUNCTION CHAR-NATIONAL(19991) = N-W
               DISPLAY "WIDE=YES" ELSE DISPLAY "WIDE=NO".
      *> ORD over national reads the NATIONAL sequence (15.70.4 r2): U+4E16 -> 19991.
           MOVE FUNCTION ORD(N-W) TO ORD-R.
           DISPLAY "ORD=" ORD-R.
           STOP RUN.
