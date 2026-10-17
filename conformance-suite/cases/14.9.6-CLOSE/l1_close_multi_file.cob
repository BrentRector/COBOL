      *>--- conformance-case
      *> id: l1_close_multi_file
      *> rules: [GR-14.9.6.4-10]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.6.4 GR10 - a multi-file CLOSE is N separate CLOSE
      *> statements in written order, and a declarative's RESUME NEXT
      *> STATEMENT resumes at the NEXT implicit CLOSE.
      *> Sentence 1 is pinned twice. (i) CLOSE F1 F2 F3 with F2 never
      *> opened: each file-name gets its OWN I-O status update -
      *> 00 / 42 / 00 - because GR1 makes only the F2 leg unsuccessful
      *> ("If the file connector is not open, the CLOSE statement is
      *> unsuccessful and the I-O status indicator for the file
      *> connector is set to '42'"), and F1 and F3 really were closed,
      *> proven by re-OPENing them ('00', not the already-open '41').
      *> (ii) CLOSE F1 F1 names ONE file twice: written as two separate
      *> statements the first is '00' and the second finds the connector
      *> no longer open, '42' - the order-sensitive form.
      *> Sentence 2: a USE AFTER STANDARD ERROR declarative fires on the
      *> SECOND of the three implicit CLOSEs and executes RESUME AT NEXT
      *> STATEMENT (AT is an optional word, 14.9.33.2); processing must
      *> resume at the THIRD implicit CLOSE, so F3 closes ('00' plus the
      *> successful re-OPEN) instead of control leaving the statement.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1CLS01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "l1cls01a.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST1.
           SELECT F2 ASSIGN TO "l1cls01b.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST2.
           SELECT F3 ASSIGN TO "l1cls01c.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST3.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(5).
       FD F2.
       01 R2 PIC X(5).
       FD F3.
       01 R3 PIC X(5).
       WORKING-STORAGE SECTION.
       01 ST1 PIC XX.
       01 ST2 PIC XX.
       01 ST3 PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-F1-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       ERR-F1-PARA.
           DISPLAY "D1=" ST1
           RESUME AT NEXT STATEMENT.
       ERR-F2-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F2.
       ERR-F2-PARA.
           DISPLAY "D2=" ST2
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN.
           OPEN OUTPUT F1
           MOVE "AAAAA" TO R1
           WRITE R1
           OPEN OUTPUT F3
           MOVE "CCCCC" TO R3
           WRITE R3
      *> F2 is never opened: its implicit CLOSE is the failing one.
           CLOSE F1 F2 F3
           DISPLAY "ST1=" ST1
           DISPLAY "ST2=" ST2
           DISPLAY "ST3=" ST3
      *> Both surviving names really were closed: a re-OPEN reports
      *> '00', never the '41' of a connector still in an open mode.
           OPEN INPUT F1
           DISPLAY "RE1=" ST1
           OPEN INPUT F3
           DISPLAY "RE3=" ST3
           READ F3 AT END CONTINUE END-READ
           DISPLAY "R3=" R3
           CLOSE F3
      *> One file-name written twice = two separate CLOSE statements in
      *> that order: '00' then '42'.
           CLOSE F1 F1
           DISPLAY "TWICE=" ST1
           STOP RUN.
