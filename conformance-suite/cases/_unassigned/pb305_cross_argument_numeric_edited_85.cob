      *>--- conformance-case
      *> id: pb305_cross_argument_numeric_edited_85
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB305 at the OLDEST edition. The rule under test is
      *> ISO 8.5.2.1 Table 2's class column - category numeric-edited
      *> (usage display) is class ALPHANUMERIC - and it is
      *> edition-invariant, so the screen that reads it must behave the
      *> same way wherever the function itself is legal.
      *>
      *> MAX / MIN / ORD-MAX / ORD-MIN are the cross-argument family
      *> available at COBOL-85, and their rule is the AllSameClass arm:
      *> 15.59.3 r2, 15.63.3 r2, 15.71.3 r3 and 15.72.3 r3 each read
      *> "All arguments shall be of the same class with the
      *> exception that mixing of arguments of alphabetic and
      *> alphanumeric classes is
      *> allowed", and 15.59.3 r1 / 15.63.3 r1's exclusion list -
      *> "Argument-1 shall not be of class boolean, message-tag, object,
      *> or pointer, nor shall it be a strongly-typed group item" - does
      *> not touch numeric-edited. So the list is legal here too.
      *>
      *> The assertion is the same EQUIVALENCE the 2023 companion uses:
      *> the edited item and the byte-identical PIC X twin built from it
      *> by MOVE are one class, so every class-worded rule answers the
      *> same for both. It carries no dependence on the runtime
      *> collating sequence, which is what a pinned MAX/MIN literal
      *> would smuggle
      *> in. Each line prints the shared value so it cannot pass
      *> vacuously.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB305CROSS85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ED   PIC ZZ9.
       01 EDX  PIC X(3).
       01 P1   PIC 9(4).
       01 P2   PIC 9(4).
       01 S1   PIC X(12).
       01 S2   PIC X(12).
       PROCEDURE DIVISION.
           MOVE 5 TO ED.
           MOVE ED TO EDX.
           DISPLAY "ED=[" EDX "]".
           MOVE FUNCTION MAX(ED "999") TO S1.
           MOVE FUNCTION MAX(EDX "999") TO S2.
           IF S1 = S2
             DISPLAY "MAX85=OK [" S1 "]"
           ELSE
             DISPLAY "MAX85=BAD [" S1 "] [" S2 "]"
           END-IF.
           MOVE FUNCTION MIN(ED "999") TO S1.
           MOVE FUNCTION MIN(EDX "999") TO S2.
           IF S1 = S2
             DISPLAY "MIN85=OK [" S1 "]"
           ELSE
             DISPLAY "MIN85=BAD [" S1 "] [" S2 "]"
           END-IF.
           MOVE FUNCTION ORD-MAX(ED "999") TO P1.
           MOVE FUNCTION ORD-MAX(EDX "999") TO P2.
           IF P1 = P2
             DISPLAY "ORDMAX85=OK " P1
           ELSE
             DISPLAY "ORDMAX85=BAD " P1 " " P2
           END-IF.
           MOVE FUNCTION ORD-MIN(ED "999") TO P1.
           MOVE FUNCTION ORD-MIN(EDX "999") TO P2.
           IF P1 = P2
             DISPLAY "ORDMIN85=OK " P1
           ELSE
             DISPLAY "ORDMIN85=BAD " P1 " " P2
           END-IF.
           STOP RUN.
