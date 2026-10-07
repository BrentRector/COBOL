      *> ISO 8.4.3 / 15 — user-defined function with NON-location arguments: a literal and an
      *> arithmetic-expression argument are encoded into the parameter's format. DOUBLER(5)=10, DOUBLER(4+1)=10.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. DOUBLER.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           COMPUTE L-R = L-X * 2.
           GOBACK.
       END FUNCTION DOUBLER.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UVALARG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION DOUBLER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9(4) VALUE 4.
       01 WS-R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE WS-R = FUNCTION DOUBLER(5).
           DISPLAY "LIT=" WS-R.
           COMPUTE WS-R = FUNCTION DOUBLER(WS-A + 1).
           DISPLAY "ARI=" WS-R.
           STOP RUN.
       END PROGRAM UVALARG.
