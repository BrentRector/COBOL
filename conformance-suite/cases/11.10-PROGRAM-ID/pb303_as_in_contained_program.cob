*>--- conformance-case
*> id: pb303_as_in_contained_program
*> rules: [SR-11.10.3-2]
*> source-format: free
*> portable: unverified
*>---
       *> kb/Work PB303 - ISO 11.10.3 syntax rule 2 (PROGRAM-ID, FORMAT 1): "Literal-1
       *> shall not be specified in a program that is contained within another program."
       *> ISO 8.3.2.2 2) externalizes "program-names of outermost programs" only, and
       *> 8.4.6.3 scopes a contained program-name to its container, so a contained program
       *> has no externalized name for the phrase to give.  COBOLNET1795.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB303OUT.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB303INN AS "PB303INX".
       PROCEDURE DIVISION.
       INNER-P.
           GOBACK.
       END PROGRAM PB303INN.
       END PROGRAM PB303OUT.
