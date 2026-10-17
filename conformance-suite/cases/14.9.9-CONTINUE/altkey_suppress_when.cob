*>--- conformance-case
*> id: altkey_suppress_when
*> rules: [SR-14.9.9.3-1, GR-14.9.41.4-16, GR-14.9.41.4-17]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §12.4.5.6 SUPPRESS WHEN phrase of the ALTERNATE RECORD KEY clause
      *> (COBOL-2023; Introduction p.27 / Annex E.3.3 item 42). literal-1 is the
      *> key suppression value: when a record's alternate key equals it, that
      *> record's ALTERNATE access path is withheld (§12.4.5.6.4 GR6) and the
      *> record "is not considered to exist" for READ/START on that key (GR6 NOTE)
      *> — but the record itself, and its PRIME-key path, are unaffected.
      *> Record 02 has alternate key "XX" = the suppression value. Reading on the
      *> ALTERNATE key returns only 01 and 03; reading on the PRIME key returns
      *> all three (01, 02, 03). Greenfield-only (the legacy has no SUPPRESS WHEN).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ALTSUPWHEN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "altkey-suppress-when.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS R-KEY
               ALTERNATE RECORD KEY IS R-ALT WITH DUPLICATES
                   SUPPRESS WHEN "XX"
               FILE STATUS IS WS-FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R.
          05 R-KEY PIC X(2).
          05 R-ALT PIC X(2).
       WORKING-STORAGE SECTION.
       01 WS-FS  PIC X(2).
       01 WS-EOF PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           MOVE "01" TO R-KEY. MOVE "AA" TO R-ALT. WRITE R.
           MOVE "02" TO R-KEY. MOVE "XX" TO R-ALT. WRITE R.
           MOVE "03" TO R-KEY. MOVE "BB" TO R-ALT. WRITE R.
           CLOSE F.
           OPEN INPUT F.
           DISPLAY "ALTERNATE-KEY WALK (02/XX suppressed):".
           MOVE LOW-VALUES TO R-ALT.
           START F KEY IS >= R-ALT
               INVALID KEY CONTINUE
           END-START.
           PERFORM UNTIL WS-EOF = 1
               READ F NEXT
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END DISPLAY "  KEY=" R-KEY " ALT=" R-ALT
               END-READ
           END-PERFORM.
           CLOSE F.
           MOVE 0 TO WS-EOF.
           OPEN INPUT F.
           DISPLAY "PRIME-KEY WALK (all three present):".
           PERFORM UNTIL WS-EOF = 1
               READ F NEXT
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END DISPLAY "  KEY=" R-KEY
               END-READ
           END-PERFORM.
           CLOSE F.
           STOP RUN.
