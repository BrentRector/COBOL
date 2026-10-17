*>--- conformance-case
*> id: pb347-write-record-of-no-file
*> rules: [SR-14.9.51.3-5]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 §14.9.51.3 syntax rule 5: "Record-name-1 is the name of a logical record in the
*> file section of the data division and may be qualified."
*> WS-REC is a WORKING-STORAGE 01. It is a record description entry but not a logical record OF A FILE,
*> so §14.9.51.3 SR1 - "The write file is the file referenced by file-name-1 or by the file-name
*> associated with record-name-1" - has no file to name. This is the arm that used to draw only the
*> COBOLNET1756 DEFERRAL WARNING: the compiler announced ITS OWN gap for what is the source's error, and
*> the program compiled.
*> §4.2.2 makes the compile-time indication mandatory for "violations of the general formats and the
*> explicit syntax rules of standard COBOL". The rule is written identically in 1985, 2002 and 2014, so
*> there is no edition gate and every edition rejects. COBOLNET1757 (kb/Work PB347).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB347N7.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IOF ASSIGN TO "pb347n7.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  IOF.
       01  IO-REC.
           05  IR-KEY   PIC X(3).
           05  IR-DATA  PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-REC.
           05  WR-KEY   PIC X(3).
           05  WR-DATA  PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IOF.
           MOVE "AAA" TO IR-KEY.
           MOVE "aaaaa" TO IR-DATA.
           WRITE WS-REC.
           CLOSE IOF.
           STOP RUN.
