*>--- conformance-case
*> id: pb347-rewrite-subordinate-item
*> rules: [SR-14.9.35.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 §14.9.35.3 syntax rule 1: "Record-name-1 is the name of a logical record in the
*> file section of the data division and may be qualified."
*> A 05 item subordinate to a record is not itself a logical record, so it is not a record-name-1.
*> Accepted, REWRITE IR-DATA replaced the record with the 5-byte subordinate item.
*> §4.2.2 makes the compile-time indication mandatory for "violations of the general formats and the
*> explicit syntax rules of standard COBOL". The rule is written identically in 1985, 2002 and 2014, so
*> there is no edition gate and every edition rejects. COBOLNET1757 (kb/Work PB347).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB347N5.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IOF ASSIGN TO "pb347n5.dat"
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
           REWRITE IR-DATA.
           CLOSE IOF.
           STOP RUN.
