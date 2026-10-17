*>--- conformance-case
*> id: pb401-range-invalid-turn-below-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> The EDITION GATE under kb/Work PB401's positive case (tests/conformance/2002/
      *> pb401_range_invalid_by_class). EC-RANGE-INVALID's OBSERVABLE surface is the >>TURN compiler directive
      *> (ISO 7.3.25) and FUNCTION EXCEPTION-STATUS (ISO 15.22), and the whole 7.3 compiler-directive facility is
      *> a COBOL-2002 introduction - so the same identifier-ended THROUGH range that raises the exception at 2002
      *> and above shall be REJECTED at COBOL-85 for naming a directive that edition has no such thing as.
      *> The EVALUATE range-expression itself is not gated: it is EVALUATE's own since 1985, and at 85 it is
      *> evaluated by 14.7.8 rule 2's empty-range behaviour with no exception condition to set.
       >>TURN EC-RANGE-INVALID CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB401-RNG-85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-C   PIC X VALUE "C".
       01 WS-M   PIC X VALUE "M".
       01 WS-A   PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN-P.
           EVALUATE WS-C
               WHEN WS-M THRU WS-A DISPLAY "IDINV-IN"
               WHEN OTHER          DISPLAY "IDINV-OUT"
           END-EVALUATE.
           STOP RUN.
