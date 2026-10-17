*>--- conformance-case
*> id: pb271_float_receiver_overflow
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB271 — the STORE BEHAVIOUR of a MOVE past a float receiver's range, with EC-DATA-OVERFLOW
      *> checking OFF (the default: §14.6.13.1.1 — "By default, checking is not enabled for any exception
      *> condition … if checking for an exception condition is not enabled, the exception condition will not be
      *> raised").  ec_data_overflow.cob pins the same MOVEs with checking ON; this pins what a default build
      *> does, because that is the half a program actually sees.
      *>
      *> §14.9.25.4 GR6 d)4.a makes the receiving item's content UNDEFINED in this case, so the value below is a
      *> DETERMINATION and not a derivation: COBOL.NET performs the ISO/IEC 60559 conversion and keeps its
      *> result, which is ±Infinity.  For FLOAT-SHORT/-LONG/-EXTENDED the same value is the §14.6.8.3 rule 1
      *> implementor choice ("the implementor specifies any exception conditions that might be set to exist
      *> during data conversion") — here, none with checking off.
      *>
      *> The under-range direction is not symmetric and is not an exception at all: a magnitude below the
      *> receiver's smallest converts to zero.
      *>
      *> ⛔ WHY THIS FIXTURE EXISTS. Owner decision D-B made a float literal past binary64 legal source in the
      *> DEFAULT arithmetic mode (it was legal only under STANDARD-DECIMAL before), which put a decimal128
      *> sending value in front of a binary64 receiver for the first time in ordinary programs.  Nothing pinned
      *> what happened next, and what happened was silence in both directions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB271FRO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B64 USAGE FLOAT-BINARY-64.
       01 B32 USAGE FLOAT-BINARY-32.
       01 FL  USAGE FLOAT-LONG.
       01 FS  USAGE FLOAT-SHORT.
       PROCEDURE DIVISION.
           MOVE 1.0E+400 TO B64.
           DISPLAY "B64OVF=[" B64 "]".
           MOVE -1.0E+400 TO B64.
           DISPLAY "B64NEG=[" B64 "]".
           MOVE 1.0E+400 TO B32.
           DISPLAY "B32OVF=[" B32 "]".
           MOVE 1.0E+100 TO B32.
           DISPLAY "B32E100=[" B32 "]".
           MOVE 1.0E+400 TO FL.
           DISPLAY "FLOVF =[" FL "]".
           MOVE 1.0E+100 TO FS.
           DISPLAY "FSOVF =[" FS "]".
      *> ...and the under-range direction: zero, no condition, both usages.
           MOVE 1.0E-400 TO B64.
           DISPLAY "B64UND=[" B64 "]".
           MOVE 1.0E-400 TO FL.
           DISPLAY "FLUND =[" FL "]".
      *> An in-range literal past binary64's PRECISION is not an overflow — it rounds, as 60559 requires.
           MOVE 1.0E+300 TO B64.
           DISPLAY "B64OK =[" B64 "]".
           STOP RUN.
