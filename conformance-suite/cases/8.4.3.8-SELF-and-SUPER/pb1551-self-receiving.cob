*>--- conformance-case
*> id: pb1551-self-receiving
*> rules: [SR-8.4.3.8.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1551 - ISO 8.4.3.8.3 SR2: "This identifier format shall not
      *> be specified as a receiving operand." (cite.py: OK 8.4.3.8.3 2))
      *> SELF as the RECEIVING operand of SET. The first SET is the legal
      *> direction (SELF the sender); the second writes SELF as the receiver,
      *> which only SR2 forbids. Every receiving position parses SELF so the
      *> receiving chokepoint names SR2 (COBOLNET3978) - it used to be a parse
      *> error re-coded to 8.9 (COBOLNET0901, a reserved word misused).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1551SM.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM PB1551SM.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1551SC INHERITS FROM BASE.
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
           SET O TO SELF
           SET SELF TO O.
       END METHOD M1.
       END OBJECT.
       END CLASS PB1551SC.
