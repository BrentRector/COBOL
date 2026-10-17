*>--- conformance-case
*> id: pb923-invoke-content-alnum-function-numeric-formal
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB923 — the refusing half. 14.8.2.3.3 rule 2 a): "If the formal parameter is numeric, the conformance
*> rules are the same as for a COMPUTE statement with the argument as the sending operand and the corresponding
*> formal parameter as the receiving operand." A COMPUTE sender shall be numeric (8.8.1.1), so an ALPHANUMERIC
*> function-identifier into a numeric formal stays refused (COBOLNET0844, the 8.8.1.1 arithmetic-operand screen) while the same function into an
*> alphanumeric formal now takes rule 2 d)'s MOVE lane. The lane is chosen by the FORMAL, as rule 2 chooses it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB923NAN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB923NC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB923NC.
       01 X PIC X(8) VALUE "  12    ".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB923NC "NEW" RETURNING O
           INVOKE O "TAKEN" USING BY CONTENT FUNCTION REVERSE(X)
           STOP RUN.
       END PROGRAM PB923NAN.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB923NC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKEN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-N PIC 9(4).
       PROCEDURE DIVISION USING LK-N.
       MAIN-N.
           DISPLAY "LK-N=[" LK-N "]".
       END METHOD TAKEN.
       END OBJECT.
       END CLASS PB923NC.
