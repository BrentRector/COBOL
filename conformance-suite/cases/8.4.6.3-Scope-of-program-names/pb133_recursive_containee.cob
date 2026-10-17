*>--- conformance-case
*> id: pb133_recursive_containee
*> rules: [SR-8.4.6.3-3, GR-11.10.4-4, GR-14.9.4.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB133 - ISO 11.10.4 GR4: "The RECURSIVE clause specifies that the program AND ANY
      *> PROGRAMS CONTAINED WITHIN IT are recursive." The R->C->R->C cycle re-enters C while C is active,
      *> which is legal ONLY because C inherits the attribute - the old binder derived it from each unit's
      *> OWN clause and this drew EC-PROGRAM-RECURSIVE-CALL on conforming source. The depth counter lives
      *> in C's OWN working-storage (C is a leaf, so its inherited-recursive WS is the shared STATIC copy,
      *> 13.5.4 GR1 / 14.6.2.3.3 - one last-used copy across activations; R itself declares no WS, the
      *> recursive-container-with-WS composition being a separately staged posture). Derived trace: R
      *> prints R, activates C (#1: DEPTH=1, prints C1), which re-enters R (prints R), which re-enters C
      *> (#2 - the inherited attribute at work: DEPTH=2, prints C2, stops the cycle).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB133R RECURSIVE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "R"
           CALL "PB133C"
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB133C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DEPTH PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P.
           ADD 1 TO DEPTH
           DISPLAY "C" DEPTH
           IF DEPTH < 2
               CALL "PB133R"
           END-IF
           GOBACK.
       END PROGRAM PB133C.
       END PROGRAM PB133R.
