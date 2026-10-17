      *>--- conformance-case
      *> id: pb668_open_reversed_85
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB668R8.
      *> kb/Work PB668. OPEN INPUT ... REVERSED is the COBOL-85 tape
      *> phrase ISO 2002 deleted (VERSION_CHANGE_REFERENCE row 7.12,
      *> gate open-reversed-removed-2002 / COBOLNET0902), so this is a
      *> --std 85 golden and its negative at 2002+ is that gate's.
      *> Before PB668 the phrase parsed, the edition gate admitted it at
      *> 85, and NO binder read it: the READs below answered AAAAAAAA,
      *> BBBBBBBB, CCCCCCCC - the FORWARD order - with no diagnostic.
      *>
      *> EXPECTED VALUES, COMPUTED FROM THE RULE, NOT MEASURED. The
      *> phrase positions the file at its END and every subsequent READ
      *> makes the PRECEDING record available; that direction is the one
      *> 14.9.30.4 GR21 c) already defines ("the first existing record
      *> in the physical file whose relative key number is ... less than
      *> the file position indicator if PREVIOUS is specified"), and
      *> GR21 e) ("If no record is found that satisfies the above rules,
      *> the at end condition exists") is the at end at the FIRST
      *> record, which 14.9.30.4 GR24 reports as '10'. So a three-record
      *> file read REVERSED yields CCCCCCCC, BBBBBBBB, AAAAAAAA and then
      *> AT END. The OPEN itself is an ordinary successful open: '00'
      *> (9.1.13.2 item 1) - REVERSED is a positioning, not a status
      *> overlay, unlike its sibling WITH NO REWIND whose 14.9.27.4 GR11
      *> '07' pb317_open_no_rewind_85 pins.
      *>
      *> The RE-OPEN at the end is the lifetime witness: the phrase
      *> belongs to the OPEN statement that wrote it, so a plain re-open
      *> of the same connector reads FORWARD again (AAAAAAAA). Without
      *> SequentialConnector.OpenCore clearing the flag this reads
      *> CCCCCCCC and nothing else in the suite would notice.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb668r8.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 WS-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           MOVE "AAAAAAAA" TO F-REC
           WRITE F-REC
           MOVE "BBBBBBBB" TO F-REC
           WRITE F-REC
           MOVE "CCCCCCCC" TO F-REC
           WRITE F-REC
           CLOSE F
           OPEN INPUT F REVERSED
           DISPLAY "OPEN=" WS-ST
           READ F AT END CONTINUE END-READ
           DISPLAY "R1=" WS-ST " " F-REC
           READ F AT END CONTINUE END-READ
           DISPLAY "R2=" WS-ST " " F-REC
           READ F AT END CONTINUE END-READ
           DISPLAY "R3=" WS-ST " " F-REC
           READ F AT END DISPLAY "ATEND" END-READ
           DISPLAY "R4=" WS-ST
           CLOSE F
           OPEN INPUT F
           READ F AT END CONTINUE END-READ
           DISPLAY "PLAIN1=" WS-ST " " F-REC
           CLOSE F
           STOP RUN.
