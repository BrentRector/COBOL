      *>--- conformance-case
      *> id: pb1551-invoke-universal-self
      *> rules: [SR-8.4.3.8.3-2, SR-14.9.23.3-20]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1551 review R1-1 - ISO 14.9.23.3 SR6: through a
      *> universal object reference "the BY REFERENCE phrase, if not
      *> specified explicitly, is assumed implicitly" (cite.py: OK
      *> 14.9.23.3 6)), so the keyword-less SELF is identifier-3, a
      *> receiving operand by SR20 (cite.py: OK 14.9.23.3 20)), and ISO
      *> 8.4.3.8.3 SR2 refuses it (cite.py: OK 8.4.3.8.3 2)) -
      *> COBOLNET3978. It was refused as "a literal or arithmetic-
      *> expression argument", which SELF is not.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1551IUM.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM PB1551IUM.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1551IUC INHERITS FROM BASE.
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
           INVOKE O "M" USING SELF.
       END METHOD M1.
       END OBJECT.
       END CLASS PB1551IUC.
