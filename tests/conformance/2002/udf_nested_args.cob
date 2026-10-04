      *> ISO 8.4.3.2.4 GR2 (arguments evaluated left to right; an argument may itself be a
      *> function-identifier), GR5a (an identifier argument that is permitted as a receiving operand passes
      *> BY REFERENCE - the formal operates as if it occupies the argument's storage, 14.2.3 GR8): sibling
      *> activations in one statement, a user function nested in a user function, an intrinsic nested in a
      *> user function, and a BY REFERENCE argument whose storage changes while the function runs (13.7.3
      *> SR5 forbids a function to store into its formal parameter, so UMUT changes the argument through
      *> the EXTERNAL item WS-A both programs describe, and reads the new value through the formal).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UNESTARG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION UDBL3
           FUNCTION UMUT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9(4) EXTERNAL.
       01 WS-R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 4 TO WS-A.
           COMPUTE WS-R = FUNCTION UDBL3(2) + FUNCTION UDBL3(3).
           DISPLAY "T=" WS-R.
           MOVE FUNCTION UDBL3(FUNCTION UDBL3(2)) TO WS-R.
           DISPLAY "N=" WS-R.
           COMPUTE WS-R = FUNCTION UDBL3(FUNCTION MOD(7, 4)).
           DISPLAY "I=" WS-R.
           COMPUTE WS-R = FUNCTION UMUT(WS-A).
           DISPLAY "M=" WS-R.
           DISPLAY "A=" WS-A.
           STOP RUN.
       END PROGRAM UNESTARG.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UDBL3.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           COMPUTE L-R = L-X * 2.
           GOBACK.
       END FUNCTION UDBL3.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UMUT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9(4) EXTERNAL.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           ADD 1 TO WS-A.
           COMPUTE L-R = L-X * 2.
           GOBACK.
       END FUNCTION UMUT.
