*>--- conformance-case
*> id: pb695_alternate_suppress_no_when
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB695 family 3 - WHEN is an OPTIONAL WORD of the SUPPRESS phrase.
      *> ISO 8.3.2.4.3: "Within each format, uppercase words that are not underlined are called
      *> optional words and may be specified at the user's option WITH NO EFFECT ON THE SEMANTICS OF
      *> THE FORMAT." Printed page 350 / folio 320 carries an underline rectangle under SUPPRESS
      *> (x 254.17-299.45 beneath a box of 252.66-300.40, 94.8% cover) and under DUPLICATES (95.8%),
      *> and NONE anywhere in WHEN's band (box 303.50-334.12; the nearest rule ends at 299.45). WITH
      *> is un-underlined too. So `DUPLICATES SUPPRESS "XX"` is a conforming spelling of the same
      *> clause that altkey_suppress_when.cob writes in full, and it must behave IDENTICALLY.
      *>
      *> EXPECTED VALUES, DERIVED FROM THE SPEC - NOT copied from the sibling golden:
      *> 12.4.5.6.4 GR6 withholds the ALTERNATE access path of a record whose alternate key equals
      *> literal-1, and its NOTE says such a record "is not considered to exist" for READ/START on
      *> that key - so the alternate-key walk yields 01/AA and 03/BB and skips 02/XX. GR6 leaves the
      *> record and its PRIME-key path untouched, so the prime-key walk yields all three keys in
      *> ascending order (12.4.5.20 / 14.9.32.4 GR9 c: sequential access on an indexed file returns
      *> records in ascending record-key order).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB695ALTSUP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb695-alt-suppress-no-when.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS R-KEY
               ALTERNATE RECORD KEY IS R-ALT DUPLICATES
                   SUPPRESS "XX"
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
           DISPLAY "ALT-WALK:".
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
           DISPLAY "PRIME-WALK:".
           PERFORM UNTIL WS-EOF = 1
               READ F NEXT
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END DISPLAY "  KEY=" R-KEY
               END-READ
           END-PERFORM.
           CLOSE F.
           DISPLAY "DONE".
           STOP RUN.
