*>--- conformance-case
*> id: pb1551-self-undeclared-85
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1551 - at COBOL-85 SELF is no reserved word (8.9 reserves it
      *> from 2002) and there is no predefined object reference: a program that
      *> does not DECLARE it references nothing (8.4.2.1, COBOLNET1639). The
      *> receiving superset parse must not answer with 8.4.3.8.3 SR2, a rule
      *> about an identifier the edition does not have.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1551E8.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "A" TO X
           MOVE "B" TO SELF
           STOP RUN.
       END PROGRAM PB1551E8.
