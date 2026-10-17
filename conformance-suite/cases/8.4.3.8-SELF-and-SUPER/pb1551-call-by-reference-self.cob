      *>--- conformance-case
      *> id: pb1551-call-by-reference-self
      *> rules: [SR-8.4.3.8.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1551 review R1-1 - ISO 14.9.4.3 SR5: "If the BY
      *> REFERENCE phrase is specified or implied for an identifier-2
      *> and identifier-2 is not an address-identifier, it is a
      *> receiving operand" (cite.py: OK 14.9.4.3 5)), and ISO 8.4.3.8.3
      *> SR2: "This identifier format shall not be specified as a
      *> receiving operand." (cite.py: OK 8.4.3.8.3 2)). BY CONTENT SELF
      *> is the legal sending form; BY REFERENCE SELF makes SELF a
      *> receiving operand, refused by SR2 (COBOLNET3978) - it was a
      *> parse error re-coded to 8.9 (COBOLNET0901, a reserved word
      *> misused).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1551CRM.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM PB1551CRM.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1551CRC INHERITS FROM BASE.
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
           CALL "PB1551XX" USING BY CONTENT SELF
           CALL "PB1551XX" USING BY REFERENCE SELF.
       END METHOD M1.
       END OBJECT.
       END CLASS PB1551CRC.
