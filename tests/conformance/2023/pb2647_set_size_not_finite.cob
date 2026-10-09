      *> kb/Work PB2647 - SET SIZE (COBOL-2023) with an amount that is not a finite number.
      *> ISO 14.9.39.4 GR37: "If arithmetic-expression-5 does not evaluate to a nonnegative number,
      *> the length of data-name-3 is set to 0 and an EC-STORAGE-NOT-AVAIL exception condition is set
      *> to exist." GR38: "if arithmetic-expression-5 evaluates to a number that is greater than the
      *> maximum size of data-name-3, the length of data-name-3 is set to the maximum size allowed and
      *> an EC-STORAGE-NOT-AVAIL exception condition is set to exist."
      *> F is a FLOAT-LONG item (not a standard floating-point usage, so 14.6.13.2 item 3's
      *> EC-DATA-NOT-FINITE does not apply) loaded through a redefinition with a quiet NaN, then
      *> negative and positive infinity. A NaN is not a number, so it does not evaluate to a
      *> nonnegative number: GR37. It used to set the length to 0 and set NO condition.
      *> DETERMINATION: an infinity is a number (ISO/IEC 60559's floating-point numbers include the
      *> infinities; the NaNs are the only data that are not), so -infinity is GR37's negative case
      *> and +infinity is GR38's "greater than the maximum size" (the LIMIT, 10).
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES AND NOT FROM A RUN:
      *> each raise runs the USE declarative (14.6.13.1.4: a nonfatal condition with checking on
      *> selects it), which displays the 31-character EXCEPTION-STATUS; the statement then finishes.
      *> NAN LEN=0000         GR37.
      *> NEGINF LEN=0000      GR37.
      *> POSINF LEN=0010      GR38's clamp to the maximum size, the added positions spaces (GR39).
       >>TURN EC-STORAGE-NOT-AVAIL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2647SZ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 F USAGE FLOAT-LONG.
       01 G2 REDEFINES G PIC X(8).
       01 D  PIC X DYNAMIC LENGTH LIMIT 10.
       01 W  PIC 9(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-STORAGE-NOT-AVAIL.
       H-P.
           DISPLAY "HANDLED=[" FUNCTION EXCEPTION-STATUS "]".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE X"7FF8000000000000" TO G2.
           MOVE "ABC" TO D.
           SET SIZE OF D TO F.
           MOVE FUNCTION LENGTH(D) TO W.
           DISPLAY "NAN LEN=" W.
           MOVE X"FFF0000000000000" TO G2.
           MOVE "ABC" TO D.
           SET SIZE OF D TO F.
           MOVE FUNCTION LENGTH(D) TO W.
           DISPLAY "NEGINF LEN=" W.
           MOVE X"7FF0000000000000" TO G2.
           MOVE "ABC" TO D.
           SET SIZE OF D TO F.
           MOVE FUNCTION LENGTH(D) TO W.
           DISPLAY "POSINF LEN=" W.
           STOP RUN.
