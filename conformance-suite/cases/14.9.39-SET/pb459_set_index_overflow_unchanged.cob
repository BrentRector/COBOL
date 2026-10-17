      *>--- conformance-case
      *> id: pb459_set_index_overflow_unchanged
      *> rules: [GR-14.9.39.4-4, DOC-A.1-128]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB459 - the COBOL-85 leg of the index range rule, where there IS no exception-condition
      *> mechanism: no >>TURN directive, no EC-RANGE-INDEX, no declarative to reach. What remains is the OTHER
      *> half of ISO 13.18.38.4 GR2 / 14.9.39.4 GR4 a), which is not conditional on checking at all - "the
      *> execution of the SET statement is unsuccessful, and the content of the receiving operand is
      *> unchanged". Before PB459 the augment was formed in the 64-bit carrier, so crossing the boundary was a
      *> silent WRAP to a negative occurrence number at every edition, 85 included.
      *>
      *> An 85 numeric literal is limited to 18 digits, so the boundary is approached by repeated addition
      *> rather than by one 19-digit literal, and the witness is arithmetic rather than a printed 19-digit
      *> value (an 85 PICTURE is limited to 18 digits too).
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN:
      *>
      *> IX starts at 1. Each of the ten UP BY 999999999999999999 statements is governed by GR4:
      *>   - passes 1..9 give 1 + k * 999999999999999999; after the 9th, IX = 8999999999999999992, which is
      *>     inside the implementor index range (docs/CONFORMANCE.md 7, DOC-A.1-128: the signed 64-bit
      *>     interval, maximum 9223372036854775807), so GR4 b) applies and the index moves;
      *>   - the 10th would make it 9999999999999999991, OUTSIDE that range, so GR4 a) applies: the SET is
      *>     unsuccessful and IX is unchanged at 8999999999999999992.
      *> The nine DOWN BY statements then subtract 8999999999999999991, leaving IX at exactly 1, and
      *> SET N TO IX reads that occurrence number => 0001.
      *>
      *> If the tenth UP BY had wrapped instead of being refused - the pre-PB459 behaviour - IX would have
      *> become -8446744073709551625 and the nine subtractions would have driven it further away, so this one
      *> line distinguishes the two implementations exactly.
      *>
      *>   BACK-TO=0001
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB459SETIX85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC 9(2) OCCURS 5 TIMES INDEXED BY IX.
       01 N PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           SET IX TO 1
           PERFORM 10 TIMES
               SET IX UP BY 999999999999999999
           END-PERFORM
           PERFORM 9 TIMES
               SET IX DOWN BY 999999999999999999
           END-PERFORM
           SET N TO IX
           DISPLAY "BACK-TO=" N
           STOP RUN.
