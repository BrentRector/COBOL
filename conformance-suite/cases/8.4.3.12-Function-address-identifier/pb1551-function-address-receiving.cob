      *>--- conformance-case
      *> id: pb1551-function-address-receiving
      *> rules: [SR-8.4.3.12.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1551 review R1-1 - ISO 8.4.3.12.3 SR3: "This
      *> identifier format shall not be specified as a receiving
      *> operand." (cite.py: OK 8.4.3.12.3 3)). The function-address-
      *> identifier is written as the BY REFERENCE argument of CALL,
      *> which ISO 14.9.4.3 SR5 makes a receiving operand because it is
      *> no address-identifier (cite.py: OK 14.9.4.3 5)); 8.4.3.1.2
      *> Format 9 has only the data and program arms. Refused by SR3
      *> (COBOLNET3979) - it was a parse error (COBOL0001).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1551FA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(4).
       PROCEDURE DIVISION.
           CALL "PB1551XX" USING BY REFERENCE X
           CALL "XX" USING BY REFERENCE ADDRESS OF FUNCTION PB1551F
           STOP RUN.
       END PROGRAM PB1551FA.
