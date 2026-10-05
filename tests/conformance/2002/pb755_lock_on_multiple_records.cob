      *> kb/Work PB755 - the LOCK MODE clause's lock phrase written OUT IN FULL, the spelling the tightening of
      *> `ON?` to `ON` must keep. ISO 12.4.5.9.2 prints `LOCK MODE IS { MANUAL | AUTOMATIC } [ [WITH] LOCK ON
      *> [MULTIPLE] { RECORD | RECORDS } ]`; ON is underlined (printed page 355 / folio 325: box 311.09-325.35,
      *> rule 312.52-324.43, 83.5% cover), so it is required (5.2.2), while WITH is plain and MULTIPLE is
      *> bracketed. This program writes every word; the WITH-omitted spelling is pinned by
      *> 2002/pb695_lock_mode_optional_words, and the ON-omitted one is negative/pb755-lock-on-omitted.
      *> 12.4.5.9.3 SR2 - "The MULTIPLE phrase shall not be specified for a file described with sequential
      *> organization or sequential access mode" - so the file is INDEXED with DYNAMIC access.
      *>
      *> EXPECTED VALUES, DERIVED: 12.4.5.9.4 GR3 - "If a physical file is open in the sharing with no
      *> other mode, the LOCK MODE clause has no effect." This program is the sole opener of the file and
      *> specifies no SHARING clause, so the clause changes nothing observable: 14.9.51.4 (WRITE releases
      *> the record to the file, status 00) then 14.9.30.4 (READ by key makes it available in the record
      *> area) - the record written is read back byte for byte.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB755LOCKON.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb755-lock-on.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS R-KEY
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS
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
           MOVE "07" TO R-KEY.
           MOVE "WXYZ" TO R-VAL.
           WRITE R.
           DISPLAY "WFS=" WS-FS.
           CLOSE F.
           OPEN I-O F.
           MOVE "07" TO R-KEY.
           READ F
               INVALID KEY DISPLAY "MISSING"
               NOT INVALID KEY DISPLAY "VAL=" R-VAL
           END-READ.
           DISPLAY "RFS=" WS-FS.
           CLOSE F.
           DISPLAY "DONE".
           STOP RUN.
