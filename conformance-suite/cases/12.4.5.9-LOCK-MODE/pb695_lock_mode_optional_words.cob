*>--- conformance-case
*> id: pb695_lock_mode_optional_words
*> rules: [FMT-12.4.5.9.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB695 family 3 - MODE, IS and WITH are OPTIONAL WORDS of the LOCK MODE clause.
      *> ISO 12.4.5.9.2 prints `LOCK MODE IS { MANUAL | AUTOMATIC } [ [WITH] LOCK ON [MULTIPLE]
      *> { RECORD | RECORDS } ]`. Measured on printed page 355 / folio 325: LOCK carries an underline
      *> rectangle (x 87.96-111.07 beneath a box of 86.56-112.03, 90.8% cover), and so do the second
      *> LOCK (90.8%), ON (83.5%), MULTIPLE (95.0%), MANUAL, AUTOMATIC, RECORD and RECORDS - while
      *> MODE's box 115.12-144.04, IS's 146.38-155.14 and WITH's 251.70-278.69 have NO rule anywhere
      *> in their bands. 8.3.2.4.3 therefore makes `LOCK AUTOMATIC LOCK ON RECORD` a conforming
      *> spelling; the grammar demanded MODE until family 3 and rejected it as a syntax error.
      *>
      *> EXPECTED VALUES, DERIVED: 12.4.5.9.4 GR3 - "If a physical file is open in the sharing with no
      *> other mode, the LOCK MODE clause has no effect." This program is the sole opener of the file
      *> and specifies no SHARING clause, so the clause changes nothing observable and the record
      *> written must be read back byte-for-byte: 14.9.51.4 (WRITE releases the record from the record
      *> area to the file) then 14.9.30.4 GR13 (READ makes the record available in the record area).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB695LOCKOW.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb695-lock-mode-ow.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS R-KEY
               LOCK AUTOMATIC LOCK ON RECORD
               FILE STATUS IS WS-FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R.
          05 R-KEY PIC X(2).
          05 R-VAL PIC X(4).
       WORKING-STORAGE SECTION.
       01 WS-FS PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           MOVE "01" TO R-KEY.
           MOVE "ABCD" TO R-VAL.
           WRITE R.
           DISPLAY "WFS=" WS-FS.
           CLOSE F.
           OPEN I-O F.
           MOVE "01" TO R-KEY.
           READ F
               INVALID KEY DISPLAY "MISSING"
               NOT INVALID KEY DISPLAY "VAL=" R-VAL
           END-READ.
           DISPLAY "RFS=" WS-FS.
           CLOSE F.
           DISPLAY "DONE".
           STOP RUN.
