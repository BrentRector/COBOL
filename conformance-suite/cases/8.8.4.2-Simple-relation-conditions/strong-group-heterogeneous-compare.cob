*>--- conformance-case
*> id: strong-group-heterogeneous-compare
*> rules: [SR-8.8.4.2.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.8.4.2.3 SR1: if either relation operand is a strongly-typed group, both operands shall be of
      *> the SAME type (8.5.3.3). Two records of DIFFERENT strong types in an equality relation are rejected
      *> at every edition that has the TYPEDEF family: COBOLNET1533 (strong-compare-mismatch), fired at the
      *> ONE CheckedRelational chokepoint (so EVALUATE / PERFORM UNTIL / SEARCH WHEN inherit the same gate).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. STRONG-HET-CMP-P10TS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 POINT-T TYPEDEF STRONG.
          05 PX PIC 9(3).
          05 PY PIC 9(3).
       01 PAIR-T TYPEDEF STRONG.
          05 QA PIC 9(3).
          05 QB PIC 9(3).
       01 P1 TYPE POINT-T.
       01 Q1 TYPE PAIR-T.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF P1 = Q1
               DISPLAY "EQ"
           END-IF.
           STOP RUN.
