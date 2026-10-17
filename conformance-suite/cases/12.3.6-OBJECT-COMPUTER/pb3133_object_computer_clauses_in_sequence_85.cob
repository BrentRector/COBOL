      *>--- conformance-case
      *> id: pb3133_object_computer_clauses_in_sequence_85
      *> rules: [FMT-12.3.6.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3133 - the COBOL-85 OBJECT-COMPUTER paragraph prints
      *>   computer-name, then MEMORY SIZE, PROGRAM COLLATING SEQUENCE
      *>   and SEGMENT-LIMIT, each in its own bracket with no ellipsis
      *>   and no choice indicators: each clause at most once, in that
      *>   sequence. DERIVED, NOT QUOTED (the repository holds no ANSI
      *>   X3.23-1985 text; docs/VERSION_CHANGE_REFERENCE.md row 7.7 for
      *>   the clause; IBM Enterprise COBOL prints the same figure). The
      *>   positive control: all three, once each, in the printed
      *>   sequence, compile and run. The negatives below 2002 are
      *>   negative/pb3133-object-computer-clause-repeated-85 and
      *>   negative/pb3133-object-computer-clause-out-of-order-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3133P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XYZ
           MEMORY SIZE 10 WORDS
           PROGRAM COLLATING SEQUENCE IS ASC-SEQ
           SEGMENT-LIMIT IS 10.
       SPECIAL-NAMES.
           ALPHABET ASC-SEQ IS STANDARD-1.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "PB3133 OBJECT-COMPUTER CLAUSES IN SEQUENCE".
           STOP RUN.
