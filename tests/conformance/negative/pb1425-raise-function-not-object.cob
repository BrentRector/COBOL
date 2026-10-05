      *> reject-at: 2002 2014 2023
      *> kb/Work PB1425 / PB1197 - ISO 14.9.29.3 SR2: "Identifier-1 shall be an object reference; the
      *>   predefined object references NULL and SUPER shall not be specified." A function-identifier is a
      *>   legal RAISE operand only when the item it references (8.4.3.2.1) is an object reference; the
      *>   function PB1425N2F returns PIC 9, so the RAISE is refused by SR2 (COBOLNET0848).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1425N2F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-SEED PIC 9.
       01 L-RES PIC 9.
       PROCEDURE DIVISION USING L-SEED RETURNING L-RES.
           MOVE L-SEED TO L-RES
           GOBACK.
       END FUNCTION PB1425N2F.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1425N2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB1425N2F.
       PROCEDURE DIVISION.
           RAISE FUNCTION PB1425N2F (1)
           STOP RUN.
       END PROGRAM PB1425N2.
