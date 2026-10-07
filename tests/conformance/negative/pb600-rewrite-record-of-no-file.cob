      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB600 - ISO 14.9.35.3 SR1: "Record-name-1 is the name of a logical record in the file
      *> section of the data division and may be qualified." (cite.py: OK 14.9.35.3 1)). WS-REC is a
      *> working-storage record, so this REWRITE names no record of any file description entry - a
      *> compile-time violation (ISO 4.2.2), refused at bind by COBOLNET1757 since kb/Work PB347. The
      *> statement sits behind a GO TO so the old posture (a run-time deferral, COBOLNET1756 warning
      *> only) is what this fixture distinguishes; pb347-write-record-of-no-file is the WRITE twin.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W1031GPB600RW.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RWF ASSIGN TO "w1031gpb600.dat".
       DATA DIVISION.
       FILE SECTION.
       FD RWF.
       01 RWF-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 WS-REC PIC X(4).
       PROCEDURE DIVISION.
           GO TO DONE.
           REWRITE WS-REC.
       DONE.
           DISPLAY "DONE".
           STOP RUN.
