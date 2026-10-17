*>--- conformance-case
*> id: pb725_push_pop_display
*> rules: [GR-7.3.12.4-1, GR-7.3.12.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB725P23.
      *> kb/Work PB725 - >>PUSH (7.3.22), >>POP (7.3.20) and >>DISPLAY (7.3.12) AT THEIR
      *> INTRODUCING EDITION. The negative twin is
      *> negative/pb725-push-pop-display-below-2023.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC:
      *>   A=7      W-N is PIC 9 VALUE 7 and nothing has changed it.
      *>   B=7      7.3.22.4 GR2 saves the state of every directive other than EVALUATE,
      *>            IF, PAGE, POP and PUSH; GR3 keeps the pushed directives' effects
      *>            ACTIVE. 7.3.20.4 GR3 restores what was saved. Neither has any effect
      *>            on run-unit data, so W-N still reads 7.
      *> >>DISPLAY contributes NOTHING to stdout: 7.3.12.1 sends it "to the source listing
      *> or an implementor defined compile-time-device", not to the run unit's display
      *> device, and this implementation produces no listing and defines no such device.
      *> A golden that showed the text here would be asserting a NON-conformance.
      *>
      *> 7.3.20.3 SR3 / 7.3.22.3 SR3 confine the ALL form to a compilation unit, between
      *> statements in the procedure division - which is where they are written.
      *> Directives sit at COLUMN 8 (column 7 is the fixed-form indicator area).
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N PIC 9 VALUE 7.
       PROCEDURE DIVISION.
       MAIN.
       >>DISPLAY "PB725 COMPILE-TIME NOTE"
           DISPLAY "A=" W-N
       >>PUSH ALL
       >>POP ALL
           DISPLAY "B=" W-N
           STOP RUN.
