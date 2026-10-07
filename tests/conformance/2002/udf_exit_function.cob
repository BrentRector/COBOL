      *> Pre-2023 EXIT FUNCTION (introduced 2002 with user-defined functions, REMOVED 2023 - Annex E.2
      *> :49036; the exit-function-window row): inside a function definition it is the function-return
      *> synonym, equivalent to GOBACK - the activation terminates IMMEDIATELY and the RETURNING item's
      *> value becomes the function result (14.9.18.4 GR5 semantics). The IF-guarded early exit keeps the
      *> trailing MOVE emit-live: X=0014 proves the transfer happened (9999 would mean it fell through).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UEARLY.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           COMPUTE L-R = L-X * 2.
           IF L-X > 0
               EXIT FUNCTION
           END-IF.
           MOVE 9999 TO L-R.
           GOBACK.
       END FUNCTION UEARLY.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UEXITFN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION UEARLY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE WS-R = FUNCTION UEARLY(7).
           DISPLAY "X=" WS-R.
           STOP RUN.
       END PROGRAM UEXITFN.
