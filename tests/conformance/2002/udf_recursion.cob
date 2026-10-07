      *> ISO 9.4 (:12529 a user-defined function "always possesses the recursive attribute and may call
      *> itself") + 8.4.6.6 (within a function definition its OWN user-function-name is referable with NO
      *> REPOSITORY declaration) - self-recursive factorial through five nested activations: 5! = 120.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UFACT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-N PIC 9(4).
       01 L-R PIC 9(8).
       PROCEDURE DIVISION USING L-N RETURNING L-R.
       P.
           IF L-N < 2
               MOVE 1 TO L-R
           ELSE
               COMPUTE L-R = L-N * FUNCTION UFACT(L-N - 1)
           END-IF.
           GOBACK.
       END FUNCTION UFACT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. URECURSE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION UFACT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-R PIC 9(8).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE WS-R = FUNCTION UFACT(5).
           DISPLAY "F5=" WS-R.
           STOP RUN.
       END PROGRAM URECURSE.
