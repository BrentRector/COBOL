*>--- conformance-case
*> id: pb452_function_address_runtime
*> rules: [GR-8.4.3.10.4-2, SR-8.4.3.12.3-1, SR-8.4.3.12.3-3,
*>    GR-8.4.3.12.4-1, GR-8.4.3.12.4-2, GR-8.4.3.12.4-4,
*>    GR-14.9.39.4-14, GR-14.9.39.4-15, DOC-A.1-125, DOC-A.1-174,
*>    DOC-A.1-210]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 §8.4.3.12.4 GR1 a) — the ADDRESS OF FUNCTION *identifier-1* form, whose operand's
      *> CONTENT names the function at RUN time — with both run-time screens the compile-time SR20 compare
      *> cannot reach. kb/Work PB452.
      *>
      *> §8.4.3.12.3 SR1 — "Identifier-1 shall be of category alphanumeric or national"; WS-NAME is PIC X.
      *> §8.4.3.12.4 GR4 — "If the runtime system cannot locate the function, the EC-FUNCTION-NOT-FOUND
      *>   exception condition is set to exist and the value of the address-identifier is the predefined
      *>   address NULL."  The VALUE is defined, so the store still happens.
      *> §14.9.39.4 GR14 — "If the address identified by identifier-13 is neither the predefined address NULL
      *>   nor the address of a function defined with the same signature …, the EC-FUNCTION-PTR-INVALID
      *>   exception condition is set to exist, NO DATA ITEMS ARE CHANGED, and the execution of the SET
      *>   statement is terminated."  The outcome is NAMED, so the store is skipped whether or not the
      *>   condition is being checked; only the raise is checking-gated (§14.6.13.1.4).
      *> §14.6.13.1.3 5) — both conditions are FATAL (Table 13): "If execution of the declarative completes
      *>   normally the execution of the run unit is terminated abnormally". >>TURN … CHECKING ON enables
      *>   them so their declaratives run, and each declarative ends with RESUME AT NEXT STATEMENT
      *>   (§14.9.33.4 GR2; NOTE 2 of 5)) so the program continues past the SET (kb/Work PB1549).
      *>
      *> EXPECTED OUTPUT, derived line by line:
      *>   A-SET      WS-NAME holds "PBRTDBL", the function IS locatable, so FPD holds its address (GR1a/GR2).
      *>   NF-RAISED  WS-NAME holds "NOSUCHFN": GR4 sets EC-FUNCTION-NOT-FOUND, whose declarative runs …
      *>   B-NULL     … and the address-identifier's value is the predefined address NULL, which IS stored.
      *>   PI-RAISED  FPD is re-set to PBRTDBL (1 formal), then to PBRTZERO (0 formals). FPD is restricted to
      *>              PBRTDBL, so GR14's signature test fails and EC-FUNCTION-PTR-INVALID's declarative runs …
      *>   C-KEPT     … and NO DATA ITEM IS CHANGED: FPD still holds PBRTDBL's address, so it equals FPREF.
      *>              (Neither NULL — which would be the GR4 outcome — nor PBRTZERO's address.)
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PBRTDBL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-ARG PIC S9(4).
       01 L-RES PIC S9(9).
       PROCEDURE DIVISION USING L-ARG RETURNING L-RES.
           COMPUTE L-RES = L-ARG * 2
           GOBACK.
       END FUNCTION PBRTDBL.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PBRTZERO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-RES PIC S9(9).
       PROCEDURE DIVISION RETURNING L-RES.
           MOVE 0 TO L-RES
           GOBACK.
       END FUNCTION PBRTZERO.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB452FA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PBRTDBL
           FUNCTION PBRTZERO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FPD USAGE FUNCTION-POINTER TO PBRTDBL.
       01 FPREF USAGE FUNCTION-POINTER TO PBRTDBL.
       01 WS-NAME PIC X(12).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-NF SECTION.
           USE AFTER EXCEPTION CONDITION EC-FUNCTION-NOT-FOUND.
       D-NF-P.
           DISPLAY "NF-RAISED"
           RESUME AT NEXT STATEMENT.
       D-PI SECTION.
           USE AFTER EXCEPTION CONDITION EC-FUNCTION-PTR-INVALID.
       D-PI-P.
           DISPLAY "PI-RAISED"
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M-P.
           >>TURN EC-FUNCTION-NOT-FOUND EC-FUNCTION-PTR-INVALID CHECKING ON
           SET FPREF TO ADDRESS OF FUNCTION PBRTDBL
           MOVE "PBRTDBL" TO WS-NAME
           SET FPD TO ADDRESS OF FUNCTION WS-NAME
           IF FPD = FPREF
               DISPLAY "A-SET"
           ELSE
               DISPLAY "A-NOTSET"
           END-IF
           MOVE "NOSUCHFN" TO WS-NAME
           SET FPD TO ADDRESS OF FUNCTION WS-NAME
           IF FPD = NULL
               DISPLAY "B-NULL"
           ELSE
               DISPLAY "B-NOTNULL"
           END-IF
           MOVE "PBRTDBL" TO WS-NAME
           SET FPD TO ADDRESS OF FUNCTION WS-NAME
           MOVE "PBRTZERO" TO WS-NAME
           SET FPD TO ADDRESS OF FUNCTION WS-NAME
           IF FPD = FPREF
               DISPLAY "C-KEPT"
           ELSE
               DISPLAY "C-CHANGED"
           END-IF
           STOP RUN.
       END PROGRAM PB452FA.
