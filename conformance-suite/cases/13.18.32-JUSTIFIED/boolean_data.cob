*>--- conformance-case
*> id: boolean_data
*> rules: [GR-13.18.32.4-2, GR-13.18.40.4-8]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.40.4 PICTURE / §8.3.3.4 / §14.6.8.6 — COBOL-2002 BOOLEAN data.
      *> A boolean item (PIC 1(n), optionally USAGE BIT) holds boolean positions '0'/'1'. Stored one byte
      *> per position (§13.18.40.4 R14 permits an alphanumeric-character representation). Covers B"…"
      *> literals; MOVE boolean←boolean (left-justify, zero-fill right, truncate right); literal and
      *> figurative VALUE; MOVE ZERO and INITIALIZE (zero fill); DISPLAY; and boolean comparison.
      *> (BX"…" hex, the bit operators B-AND/B-OR/B-XOR/B-NOT, and true bit-packing are deferred.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BOOLEANDATA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B-NAME   PIC 1(4).
       01 B-FLAG   USAGE BIT PIC 1(4).
       01 B-WORK   PIC 1(4).
       01 B-VAL    PIC 1(4) VALUE B"1010".
       01 B-JR     PIC 1(4) JUSTIFIED RIGHT.
       PROCEDURE DIVISION.
       MAIN.
      *> Exact fit: a 4-position boolean literal into PIC 1(4).
           MOVE B"0101" TO B-NAME.
           DISPLAY "NAME=" B-NAME.
      *> VALUE clause holding a boolean literal.
           DISPLAY "VAL=" B-VAL.
      *> Right truncation: 6 positions into 4 keeps the left four.
           MOVE B"110011" TO B-WORK.
           DISPLAY "TRUNC=" B-WORK.
      *> Zero fill: a 2-position value then a 1-position value must clear positions 2..4, so the result
      *> is "1000", never "1100".
           MOVE B"11" TO B-WORK.
           DISPLAY "FILL=" B-WORK.
           MOVE B"1" TO B-WORK.
           DISPLAY "REFILL=" B-WORK.
      *> Field-to-field boolean MOVE.
           MOVE B-NAME TO B-FLAG.
           DISPLAY "FLAG=" B-FLAG.
      *> MOVE ZERO and INITIALIZE both yield boolean zero.
           MOVE B"1111" TO B-WORK.
           MOVE ZERO TO B-WORK.
           DISPLAY "ZERO=" B-WORK.
           MOVE B"1111" TO B-WORK.
           INITIALIZE B-WORK.
           DISPLAY "INIT=" B-WORK.
      *> JUSTIFIED RIGHT boolean: a 2-position value right-aligns with '0' left-pad (ISO §13.18.32).
           MOVE B"11" TO B-JR.
           DISPLAY "JR=" B-JR.
      *> Boolean comparison: equality against a literal and a field, and inequality.
           MOVE B"0101" TO B-WORK.
           IF B-WORK = B"0101" THEN DISPLAY "EQ=YES" ELSE DISPLAY "EQ=NO".
           IF B-WORK = B-NAME THEN DISPLAY "EQFLD=YES" ELSE DISPLAY "EQFLD=NO".
           IF B-WORK = B"0100" THEN DISPLAY "NE=YES" ELSE DISPLAY "NE=NO".
           STOP RUN.
