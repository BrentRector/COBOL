      *>--- conformance-case
      *> id: l1_value_thru_through_equivalent_85
      *> rules: [SR-13.18.63.3-28]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.63.3 SR28 — "The words THROUGH and THRU are
      *> equivalent." — THE EDITION-FLOOR COPY, compiled at --std 85.
      *>
      *> The condition-name range is a FORMAT 3 construct present in
      *> every edition this row claims (85/2002/2014/2023) and the
      *> production carries no version predicate, so THROUGH and THRU
      *> must be interchangeable at the FLOOR as well as at the
      *> default edition. This copy is what goes red if either word is
      *> ever edition-gated to 2002+;
      *> 2023/l1_value_thru_through_equivalent is what goes red if the
      *> equivalence itself drifts.
      *>
      *> DERIVED EXPECTATIONS — from the rule text, not from a run.
      *> §13.18.63.4 GR18 sends the range to §14.7.8, whose rule 1
      *> makes a numeric range include "literal-1, literal-2, and all
      *> algebraic values between literal-1 and literal-2"; §8.8.4.5.3
      *> GR1 tests the conditional variable "within the specified range
      *> or ranges, including the end values" and GR3 makes the test
      *> true when one of the values equals the variable. §13.18.63.3
      *> SR26 a) requires literal-2 < literal-3, which 1 < 5, 1 < 2 and
      *> 8 < 9 satisfy. Position p of each image is the verdict for
      *> CV = p - 1, so with CV walking 0 … 9:
      *>   RANGE  1 THRU 5 is true at 1,2,3,4,5 (ENDS INCLUDED) and
      *>          false at 0 and at 6 … 9  =>  FTTTTTFFFF
      *>   LIST   two ranges in one clause, 1 THRU 2 and 8 THRU 9, is
      *>          true at 1,2,8,9 and false elsewhere  =>  FTTFFFFFTT
      *> ⛔ Each line PRINTS the shared image, so the equality cannot
      *> pass VACUOUSLY — two always-false condition-names would agree
      *> on "FFFFFFFFFF", which is not the derived image.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1VTH85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  CV PIC 9 VALUE 0.
           88  CV-THRU     VALUE 1 THRU 5.
           88  CV-THROUGH  VALUE 1 THROUGH 5.
       01  DV PIC 9 VALUE 0.
           88  DV-THRU     VALUE 1 THRU 2 8 THRU 9.
           88  DV-THROUGH  VALUE 1 THROUGH 2 8 THROUGH 9.
       01  S-THRU     PIC X(10) VALUE SPACES.
       01  S-THROUGH  PIC X(10) VALUE SPACES.
       01  L-THRU     PIC X(10) VALUE SPACES.
       01  L-THROUGH  PIC X(10) VALUE SPACES.
       01  I  PIC 99 VALUE 0.
       01  P  PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM VARYING I FROM 0 BY 1 UNTIL I > 9
               COMPUTE P = I + 1
               MOVE I TO CV
               MOVE I TO DV
               MOVE "F" TO S-THRU(P:1)
               MOVE "F" TO S-THROUGH(P:1)
               MOVE "F" TO L-THRU(P:1)
               MOVE "F" TO L-THROUGH(P:1)
               IF CV-THRU
                   MOVE "T" TO S-THRU(P:1)
               END-IF
               IF CV-THROUGH
                   MOVE "T" TO S-THROUGH(P:1)
               END-IF
               IF DV-THRU
                   MOVE "T" TO L-THRU(P:1)
               END-IF
               IF DV-THROUGH
                   MOVE "T" TO L-THROUGH(P:1)
               END-IF
           END-PERFORM
           IF S-THRU = S-THROUGH
               DISPLAY "SR28-RANGE=OK " S-THRU
           ELSE
               DISPLAY "SR28-RANGE=BAD " S-THRU " " S-THROUGH
           END-IF
           IF L-THRU = L-THROUGH
               DISPLAY "SR28-LIST=OK " L-THRU
           ELSE
               DISPLAY "SR28-LIST=BAD " L-THRU " " L-THROUGH
           END-IF
           STOP RUN.
