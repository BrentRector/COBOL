*>--- conformance-case
*> id: pb1551-self-arithmetic-sending
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1551 - ADD Format 2's TO operand is identifier-2, a SENDING
      *> operand (ISO 14.9.2.2), and 8.8.1.1 admits only "an identifier
      *> referencing a numeric data item" there; SELF is class object. The
      *> operand rule is a receiving identifier in Format 1, so the GIVING form
      *> reads SELF in its sending role and refuses it by class (COBOLNET0844) -
      *> never as a receiver, and never by binding a view's or ADDRESS OF's
      *> inner data reference in its place.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1551AM.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM PB1551AM.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1551AC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE.
       01 N PIC 9(4).
       PROCEDURE DIVISION.
           ADD 1 TO SELF GIVING N.
       END METHOD M1.
       END OBJECT.
       END CLASS PB1551AC.
