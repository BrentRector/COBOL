*>--- conformance-case
*> id: pb86-perform-times-non-integer-function
*> rules: [SR-14.9.28.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.28.3 SR2: "Identifier-1 shall be an integer." A function-identifier is an identifier
      *> (8.4.3.2.4 GR1) whose temporary item has the function's type (15.2): SQRT is type numeric, not
      *> integer (15.83), so it is not an integer count. kb/Work PB86: the FUNCTION spelling was a parse
      *> error and the keyword-omitted spelling ran the body once; now both bind, and both are screened here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB86NEGFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 99 VALUE 0.
       PROCEDURE DIVISION.
           PERFORM COUNT-IT FUNCTION SQRT(2) TIMES.
           STOP RUN.
       COUNT-IT.
           ADD 1 TO CNT.
