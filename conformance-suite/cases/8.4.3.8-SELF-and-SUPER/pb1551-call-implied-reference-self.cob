      *>--- conformance-case
      *> id: pb1551-call-implied-reference-self
      *> rules: [SR-8.4.3.8.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1551 review R1-1 - ISO 14.9.4.3 SR5 makes
      *> identifier-2 a receiving operand when BY REFERENCE is
      *> "specified or implied" (cite.py: OK 14.9.4.3 5)), and 14.9.4.4
      *> GR5 makes the BY REFERENCE phrase transitive (cite.py: OK
      *> 14.9.4.4 5)): with no phrase written, Format 1 passes the
      *> keyword-less SELF BY REFERENCE, so ISO 8.4.3.8.3 SR2 (cite.py:
      *> OK 8.4.3.8.3 2)) refuses it (COBOLNET3978). It was refused as a
      *> function-identifier temporary, which SELF is not.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1551CIM.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM PB1551CIM.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1551CIC INHERITS FROM BASE.
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
       01 X PIC X(4).
       PROCEDURE DIVISION.
           CALL "PB1551XX" USING SELF.
       END METHOD M1.
       END OBJECT.
       END CLASS PB1551CIC.
