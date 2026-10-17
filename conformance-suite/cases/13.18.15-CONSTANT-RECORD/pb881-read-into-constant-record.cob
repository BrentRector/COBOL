*>--- conformance-case
*> id: pb881-read-into-constant-record
*> rules: [SR-13.18.15.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB881 — ISO 13.18.15.3 SR2 over READ ... INTO: 14.9.30.4 GR4 b) makes the INTO phrase an implicit
*> MOVE whose receiving operand is identifier-1, so identifier-1 is a receiving data item and a CONSTANT RECORD
*> shall not be one. The READ INTO arm (and RETURN INTO beside it) resolved the receiver with the plain
*> reference resolver and overwrote the constant with the record read.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB881NRI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb881nri.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 FR PIC X(5).
       WORKING-STORAGE SECTION.
       01 CA CONSTANT RECORD PIC X(5) VALUE "aaaaa".
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT F
           READ F INTO CA AT END CONTINUE END-READ
           CLOSE F
           STOP RUN.
