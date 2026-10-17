*>--- conformance-case
*> id: pb1417-function-address-alphabetic-operand
*> rules: [SR-8.4.3.12.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1417 — the negative twin of 2014/pb1417_function_address_identifier_operands. §8.4.3.12.3 SR1
      *> "Identifier-1 shall be of category alphanumeric or national": a PIC A item is category ALPHABETIC (§8.5.2.1
      *> Table 2 lists alphabetic as its own category), outside SR1's list, though the storage model folds it into
      *> alphanumeric. The raw category test accepted it and located the function named by its content; the shared
      *> predicate ItemCategory.IsAlphanumericOrNational refuses it.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PBNEGFUN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-RES PIC S9(9).
       PROCEDURE DIVISION RETURNING L-RES.
           MOVE 7 TO L-RES
           GOBACK.
       END FUNCTION PBNEGFUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1417N1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PBNEGFUN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FP USAGE FUNCTION-POINTER TO PBNEGFUN.
       01 AB PIC A(8) VALUE "PBNEGFUN".
       PROCEDURE DIVISION.
       MAIN.
           SET FP TO ADDRESS OF FUNCTION AB
           STOP RUN.
       END PROGRAM PB1417N1.
