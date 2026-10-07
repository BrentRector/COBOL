      *> ISO 8.4.3 / 11.5 / 15 — user-defined function: FUNCTION-ID unit + FUNCTION user-name(args) invocation
      *> in COMPUTE and MOVE (whole-source form), numeric arg + numeric return.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. DOUBLER.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       COMPUTE-IT.
           COMPUTE L-R = L-X * 2.
           GOBACK.
       END FUNCTION DOUBLER.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UCALLER.
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
           COMPUTE WS-R = FUNCTION DOUBLER(WS-X).
           DISPLAY "C=" WS-R.
           MOVE FUNCTION DOUBLER(WS-X) TO WS-R.
           DISPLAY "M=" WS-R.
           STOP RUN.
       END PROGRAM UCALLER.
