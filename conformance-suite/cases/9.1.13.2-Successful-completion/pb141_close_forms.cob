      *>--- conformance-case
      *> id: pb141_close_forms
      *> rules: [GR-9.1.13.2-6, SR-14.9.6.3-2, GR-14.9.6.4-2, GR-14.9.6.4-3, GR-14.9.6.4-4, FMT-14.9.6.2, DOC-A.1-24]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB141CF.
      *> kb/Work PB141 - ISO 14.9.6.4 Table 14, Non-unit column, derived
      *> per cell: CLOSE REEL / UNIT [FOR REMOVAL] = symbol e (successful,
      *> file REMAINS OPEN, FPI unchanged, status '07', no other action);
      *> CLOSE WITH NO REWIND = c,g (the file IS closed and the status is
      *> '07' - symbol g: executed as if no optional phrase were present,
      *> status 07). 9.1.13.2 item 6 names all three phrases. The old
      *> BindClose folded NO REWIND into a plain CLOSE ('00'), and REEL
      *> and UNIT FOR REMOVAL prove byte-identical behavior (SR2).
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S ASSIGN TO "pb141cf.dat"
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD S.
       01 S-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 WS-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT S
           MOVE "DATA0001" TO S-REC
           WRITE S-REC
           CLOSE S REEL
           DISPLAY "REEL=" WS-ST
           CLOSE S UNIT FOR REMOVAL
           DISPLAY "UNITR=" WS-ST
           CLOSE S WITH NO REWIND
           DISPLAY "NOREW=" WS-ST
           CLOSE S
           DISPLAY "CLOSED=" WS-ST
           OPEN INPUT S
           DISPLAY "REOPEN=" WS-ST
           READ S AT END CONTINUE END-READ
           DISPLAY "READ=" WS-ST " REC=" S-REC
           CLOSE S
           STOP RUN.
