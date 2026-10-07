      *> ISO 8.4.3 / 15 — user-defined function invoked INSIDE a larger expression (general inline form):
      *> COMPUTE WS-R = FUNCTION DOUBLER(WS-X) + 1  ->  21*2 + 1 = 43.
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
       PROGRAM-ID. UINLINE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION DOUBLER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC 9(4) VALUE 21.
       01 WS-R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE WS-R = FUNCTION DOUBLER(WS-X) + 1.
           DISPLAY "EXPR=" WS-R.
           STOP RUN.
       END PROGRAM UINLINE.
