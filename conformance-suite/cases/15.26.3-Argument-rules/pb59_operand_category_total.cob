*>--- conformance-case
*> id: pb59_operand_category_total
*> rules: [AR-15.26.3-1, RV-15.63.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> The OperandCategory TOTALIZATION (PB59 family 7b) - the one 8.5.2.1 classifier answers every
      *> statically categorized shape, and the MAX/MIN TYPE resolution and BODY choice read the same answer:
      *> MR: a ref-mod slice of a NUMERIC display item is class-and-category ALPHANUMERIC (8.4.3.3.4 GR6c),
      *>     so MAX(N9(1:1) "A") is a UNIFORM alphanumeric list (15.59.3 r2) compared by the 8.8.4.2.7
      *>     relation - "A" (ordinal 66) exceeds "0" (49). Before the fix the skipped GR6c rewrite sent the
      *>     call down the numeric row and it ANSWERED 0 - a silent wrong answer on legal source.
      *> MG: an alphanumeric group is class and category alphanumeric (8.5.2.1; per 8.5.2.10 item 3 GROUP-USAGE
      *>     NATIONAL makes a national group, which is staged) - MAX over two groups compares their character
      *>     images and returns the greater image. Before the fix the type resolution saw NO category (the
      *>     null group arm) while the body chose the string comparison, and the halves disagreeing CRASHED
      *>     at run time ("FUNCTION MAX (no numeric render recipe)").
      *> NG: NATIONAL-OF(group) is CONFORMING - 15.66.3 r1 admits class alphabetic or alphanumeric, and an
      *>     alphanumeric group IS class alphanumeric - so the now-live class screen must keep admitting it
      *>     (the PB1 failure mode guarded against: a new screen must not reject legal source).
      *> SP: DISPLAY-OF(SPACE) is conforming (8.3.3.6.3 SR1 admits a figurative where a rule allows a
      *>     literal; SPACE reads national in the national-argument context, GR5) and converts to the one
      *>     alphanumeric space - ORD = 33. Figuratives stay category-NEUTRAL at bind (8.3.3.6.4 GR1/GR4),
      *>     the PB25 render channel carries them.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB59OPCT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          02 F1 PIC X(2) VALUE "AB".
          02 F2 PIC X(2) VALUE "CD".
       01 G2.
          02 F3 PIC X(2) VALUE "WX".
          02 F4 PIC X(2) VALUE "YZ".
       01 WS-N9 PIC 9(3) VALUE 5.
       01 WS-R  PIC X(8).
       01 WS-NR PIC N(4).
       01 WS-O  PIC 9(4).
       PROCEDURE DIVISION.
           MOVE FUNCTION MAX(WS-N9(1:1) "A") TO WS-R.
           DISPLAY "MR=" WS-R.
           MOVE FUNCTION MAX(G1 G2) TO WS-R.
           DISPLAY "MG=" WS-R.
           MOVE FUNCTION NATIONAL-OF(G1) TO WS-NR.
           DISPLAY "NG=" WS-NR.
           MOVE FUNCTION DISPLAY-OF(SPACE) TO WS-R.
           COMPUTE WS-O = FUNCTION ORD(WS-R(1:1)).
           DISPLAY "SP=" WS-O.
           STOP RUN.
       END PROGRAM PB59OPCT.
