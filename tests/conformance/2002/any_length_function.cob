      *> ISO 1989:2023 §13.18.2 ANY LENGTH — a FUNCTION's LINKAGE formal (SR4: a BY REFERENCE formal
      *> parameter): the user-defined function returns FUNCTION LENGTH of its ANY LENGTH argument, once
      *> for an X(3) and once for an X(8) argument (GR1 — n tracks each activation's argument).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. ALFLENP9AL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X ANY LENGTH.
       01 L-R PIC 99.
       PROCEDURE DIVISION USING L RETURNING L-R.
       COMPUTE-IT.
           MOVE FUNCTION LENGTH(L) TO L-R.
           GOBACK.
       END FUNCTION ALFLENP9AL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ALFNMAINP9AL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALFLENP9AL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A3 PIC XXX VALUE "PQR".
       01 A8 PIC X(8) VALUE "PQRSTUVW".
       01 R PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION ALFLENP9AL(A3).
           DISPLAY "F3=" R.
           COMPUTE R = FUNCTION ALFLENP9AL(A8).
           DISPLAY "F8=" R.
           STOP RUN.
       END PROGRAM ALFNMAINP9AL.
