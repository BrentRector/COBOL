      *> kb/Work PB460 - EC-BOUND-SET, the explicit-SET arm of a dynamic-capacity table's expected
      *> capacity, had NO raise site: FUNCTION EXCEPTION-STATUS stayed blank and a USE declarative never
      *> ran, while docs/CONFORMANCE.md marked Annex A.4.4 (whose item 1 names EC-BOUND-SET) Claimed.
      *> THE RULE: ISO 14.9.39.4 GR30 - "If the new capacity of the table exceeds the implementor's
      *> maximum capacity for this dynamic-capacity table, the EC-BOUND-TABLE-LIMIT exception condition is
      *> set to exist and the capacity of the table is unchanged; otherwise, if an expected maximum
      *> capacity is specified for the table and the new capacity of the table exceeds that expected
      *> maximum capacity, the EC-BOUND-SET exception condition is set to exist."
      *> (python scripts/spec/cite.py --check 14.9.39.4 "if an expected maximum capacity is specified for
      *> the table and the new capacity of the table exceeds that expected maximum capacity, the
      *> EC-BOUND-SET exception condition is set to exist" -> OK 14.9.39.4 30))
      *> EXPECTED, each line from the rules (the table is FROM 2 TO 6, so 6 is the expected capacity):
      *>  . GR30 a)/b)/c) compute the NEW capacity for TO / UP BY / DOWN BY; its minimum clamp makes
      *>    DOWN BY 100 give the minimum 2.
      *>  . The condition keys on the NEW capacity exceeding 6 - with NO first-crossing exemption (that is
      *>    8.5.1.9.6 GR1's, for IMPLICIT changes only) - so TO 9, UP BY 1 (9 -> 10) and DOWN BY 1
      *>    (10 -> 9) all raise, and TO 4, DOWN BY 5 (9 -> 4) and DOWN BY 100 (-> 2) do not.
      *>  . EC-BOUND-SET is nonfatal (14.6.13.1.6 Table 13), and GR30 does not leave the capacity
      *>    unchanged (contrast its first arm), so every capacity change is made: C2..C6.
      *>  . 14.6.13.1.4 3) - the enabled condition runs the applicable USE declarative (BS-DECL) and, it
      *>    completing normally, the statement finishes under its own rules. The declarative shows the
      *>    capacity BEFORE the change - docs/CONFORMANCE.md A.4.4 records that ordering (the raise
      *>    precedes the change, the shape of the implicit twin EC-BOUND-OVERFLOW).
      *>  . The last exception status persists until SET LAST EXCEPTION TO OFF (14.9.39 Format 13).
      >>TURN EC-BOUND-SET CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB460BSET.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-TABLE.
          05 WS-E PIC 9(3) OCCURS DYNAMIC CAPACITY IN WS-CAP FROM 2 TO 6.
       01 WS-A PIC 9(4) VALUE 9.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-BS SECTION.
           USE AFTER EC EC-BOUND-SET.
       D-BS-P.
           DISPLAY "BS-DECL CAP=" WS-CAP.
       END DECLARATIVES.
       MAIN-SECTION SECTION.
       MAIN-P.
           SET WS-CAP TO 4.
           DISPLAY "S1=" FUNCTION EXCEPTION-STATUS.
           DISPLAY "C1=" WS-CAP.
           SET WS-CAP TO WS-A.
           DISPLAY "S2=" FUNCTION EXCEPTION-STATUS.
           DISPLAY "C2=" WS-CAP.
           SET LAST EXCEPTION TO OFF.
           SET WS-CAP UP BY 1.
           DISPLAY "S3=" FUNCTION EXCEPTION-STATUS.
           DISPLAY "C3=" WS-CAP.
           SET LAST EXCEPTION TO OFF.
           SET WS-CAP DOWN BY 1.
           DISPLAY "S4=" FUNCTION EXCEPTION-STATUS.
           DISPLAY "C4=" WS-CAP.
           SET LAST EXCEPTION TO OFF.
           SET WS-CAP DOWN BY 5.
           DISPLAY "S5=" FUNCTION EXCEPTION-STATUS.
           DISPLAY "C5=" WS-CAP.
           SET WS-CAP DOWN BY 100.
           DISPLAY "S6=" FUNCTION EXCEPTION-STATUS.
           DISPLAY "C6=" WS-CAP.
           STOP RUN.
