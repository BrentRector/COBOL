*>--- conformance-case
*> id: pb521-property-with-based
*> rules: [SR-13.16.3-21]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB521 / PB956 -- ISO 13.16.3 SR21: "The PROPERTY clause shall not be specified in the same
      *> data description entry as: a) a BASED clause, b) a TYPEDEF clause."  PB is BASED and PROPERTY at once.
      *> Until PB956 this was refused only by the whole-unit "BASED data in a class" stage; now the rule itself
      *> owns it, asked of the entry's own clauses.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB521NB.
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM PB521NB.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB521CB.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  PB PIC X(4) BASED PROPERTY.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB521CB.
