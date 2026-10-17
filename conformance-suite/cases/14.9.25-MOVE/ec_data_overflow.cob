*>--- conformance-case
*> id: ec_data_overflow
*> rules: [GR-14.9.25.4-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> EC-DATA-OVERFLOW (ISO §14.9.25.4 GR6 d)4.a; Table 13 Fatal): "If the algebraic value of the sending
      *> operand is farther from zero than is permitted by the usage specifications of the receiving data item,
      *> the EC-DATA-OVERFLOW exception condition is set to exist, and the content of the receiving data item is
      *> undefined."  MOVE-only (an arithmetic ±Inf result is the valid §14.6.8.3 GR1 value, never this EC), and
      *> an underflow to zero is not this EC.  Observed via a USE declarative + RESUME AT NEXT STATEMENT.
      *>
      *> The rule binds a receiver "described with a standard floating-point usage" — FLOAT-BINARY-32/-64 here
      *> (§13.18.60.4 GR14/GR15).  For COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED §14.6.8.3 rule 1 leaves the
      *> conversion exceptions to the implementor, and COBOL.NET's determination is the SAME condition — one
      *> rule for every float receiver (CONFORMANCE.md §7).
      *>
      *> ⛔ R4-R7 EXIST BECAUSE THE DOUBLE ARM WAS NOT CHECKED AT ALL (kb/Work PB271).  This header used to say
      *> "a double receiver cannot overflow from a finite double" — true, and beside the point: a STANDARD-DECIMAL
      *> sending value reaches ±9.999E+6144 and collapses to ±Infinity in the conversion, so the double arm's bare
      *> cast and the single arm's finite-source guard both saw an already-infinite source and raised nothing.
      *> MOVE 1.0E+400 TO a FLOAT-BINARY-64 stored +Infinity silently, at every checking level.
      >>TURN EC-DATA-OVERFLOW CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EC-DOVF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-S   USAGE COMP-1.
       01 WS-BIG USAGE COMP-2 VALUE 1.0E300.
       01 WS-D2  USAGE COMP-2.
       01 WS-B64 USAGE FLOAT-BINARY-64.
       01 WS-B32 USAGE FLOAT-BINARY-32.
       01 WS-FL  USAGE FLOAT-LONG.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-OVERFLOW.
       H-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           DISPLAY "R1-OVERFLOW".
           MOVE WS-BIG TO WS-S.
           DISPLAY "R2-DOUBLE-NOOVF".
           MOVE WS-BIG TO WS-D2.
           DISPLAY "R3-UNDERFLOW".
           MOVE 1.0E-50 TO WS-S.
      *> A literal past binary64 into each standard floating-point usage, then into the implementor-defined
      *> FLOAT-LONG.  The sending value is a decimal128 intermediate; the receiver's usage cannot hold it.
           DISPLAY "R4-B64-OVERFLOW".
           MOVE 1.0E+400 TO WS-B64.
           DISPLAY "R5-B32-OVERFLOW".
           MOVE 1.0E+400 TO WS-B32.
           DISPLAY "R6-FLOATLONG-OVERFLOW".
           MOVE 1.0E+400 TO WS-FL.
      *> ...and the other direction is NOT this EC: a value below the receiver's smallest magnitude converts
      *> to zero, which no rule makes an overflow.
           DISPLAY "R7-B64-UNDERFLOW".
           MOVE 1.0E-400 TO WS-B64.
           DISPLAY "DONE".
           STOP RUN.
