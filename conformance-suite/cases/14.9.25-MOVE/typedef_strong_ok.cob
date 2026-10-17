*>--- conformance-case
*> id: typedef_strong_ok
*> rules: [SR-14.9.25.3-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> STRONG TYPEDEF (data-model D17; ISO 13.18.58.2 / 8.5.3.3, COBOL-2002). Two records of the SAME strong type
      *> MOVE and COMPARE as whole records - 14.9.25.3 SR2 / 8.8.4.2.3 SR1 permit same-type whole-group operands;
      *> each field is still set individually. STRONG adds only compile-time checks, so the run output is identical
      *> to a hand-written group. A different-type MOVE/compare, a class condition on a strong group, and RENAMES/
      *> REDEFINES over a strong item are all rejected (COBOLNET1532/1533) - see TypedefStrongTests.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TYPEDEF-STRONG-OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 POINT-T TYPEDEF STRONG.
          05 PX PIC 9(3).
          05 PY PIC 9(3).
       01 P1 TYPE POINT-T.
       01 P2 TYPE POINT-T.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 10 TO PX OF P1.
           MOVE 20 TO PY OF P1.
           MOVE P1 TO P2.
           DISPLAY "P2=[" PX OF P2 "][" PY OF P2 "]".
           IF P1 = P2
               DISPLAY "EQUAL"
           ELSE
               DISPLAY "UNEQUAL"
           END-IF.
           MOVE 99 TO PX OF P2.
           IF P1 = P2
               DISPLAY "EQUAL2"
           ELSE
               DISPLAY "UNEQUAL2"
           END-IF.
           STOP RUN.
