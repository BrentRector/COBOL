*>--- conformance-case
*> id: pb347-rewrite-refmod-record
*> rules: [SR-14.9.35.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 §14.9.35.3 syntax rule 1: "Record-name-1 is the name of a logical record in the
*> file section of the data division and may be qualified."
*> A record-name is a user-defined word (§8.3.2.2.25) and §5.2.4 gives that operand type "User-defined
*> word, including qualification and subscripting if needed" - those two decorations and no other.
*> §8.4.3.3.3 SR5 permits reference modification only "anywhere an identifier referencing a data item
*> of class alphanumeric, boolean, or national is permitted", and its NOTE draws the consequence:
*> "where data-name-n is used in a general format or syntax rule, then reference-modification is not
*> permitted". The printed general format writes record-name-1, not identifier-1.
*> Accepted, REWRITE IO-REC(1:3) replaced the record with a 3-byte slice.
*> §4.2.2 makes the compile-time indication mandatory for "violations of the general formats and the
*> explicit syntax rules of standard COBOL". The rule is written identically in 1985, 2002 and 2014, so
*> there is no edition gate and every edition rejects. COBOLNET1757 (kb/Work PB347).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB347N6.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IOF ASSIGN TO "pb347n6.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  IOF.
       01  IO-REC.
           05  IR-KEY   PIC X(3).
           05  IR-DATA  PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-DUMMY PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN I-O IOF.
           READ IOF AT END CONTINUE END-READ.
           MOVE "AAA" TO IR-KEY.
           MOVE "aaaaa" TO IR-DATA.
           REWRITE IO-REC(1:3).
           CLOSE IOF.
           STOP RUN.
