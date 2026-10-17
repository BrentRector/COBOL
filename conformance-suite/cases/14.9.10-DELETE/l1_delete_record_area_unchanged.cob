      *>--- conformance-case
      *> id: l1_delete_record_area_unchanged
      *> rules: [GR-14.9.10.4-8]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.10.4 GR8 - "The execution of a DELETE RECORD statement
      *> does not affect the content of the record area or the content
      *> of the data item referenced by the data-name specified in the
      *> DEPENDING ON phrase of the RECORD clause associated with
      *> file-name-1."
      *> Both items are loaded with SENTINEL values the file could not
      *> have produced (text that is in no record; a length no record
      *> was written at), and the DELETE must leave them exactly so.
      *> GR8 is not conditioned on success, so the second leg repeats
      *> the reads after an UNSUCCESSFUL delete ('43' - 9.1.13.7 item 3
      *> with 14.9.10.4 GR2: the last I-O statement before a
      *> sequential-access DELETE was not a successful READ).
      *> The "]" terminator pins the FULL 8-character field, so a
      *> blank-fill or a re-splice of the area would show.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1DEL02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "l1del02.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F
           RECORD IS VARYING IN SIZE FROM 3 TO 8 CHARACTERS
               DEPENDING ON WS-LEN.
       01 F-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 WS-ST  PIC XX.
       01 WS-LEN PIC 9(2).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           MOVE 8 TO WS-LEN
           MOVE "AAAAAAAA" TO F-REC
           WRITE F-REC
           MOVE 8 TO WS-LEN
           MOVE "BBBBBBBB" TO F-REC
           WRITE F-REC
           CLOSE F
           OPEN I-O F
      *> WS-LEN is forced OFF the true length before the READ, so the
      *> LEN= below measures 13.18.43.4 GR15 (a successful READ stores
      *> the just-read byte count into the DEPENDING item) and not the
      *> leftover MOVE that sized the WRITE.  99 is safe: 13.18.43.4
      *> GR14's bound check is on a record TO BE WRITTEN only.
           MOVE 99 TO WS-LEN
           READ F AT END CONTINUE END-READ
           DISPLAY "READ=" WS-ST " " F-REC "] LEN=" WS-LEN
      *> Sentinels the DELETE must not disturb.
           MOVE "SENTINEL" TO F-REC
           MOVE 3 TO WS-LEN
           DELETE F RECORD
           DISPLAY "DEL=" WS-ST
           DISPLAY "AREA=" F-REC "]"
           DISPLAY "DEP=" WS-LEN
      *> Unsuccessful leg: no successful READ immediately precedes this
      *> DELETE, so it is '43' - and GR8 still holds.
           MOVE "SECOND" TO F-REC
           MOVE 6 TO WS-LEN
           DELETE F RECORD
           DISPLAY "DEL2=" WS-ST
           DISPLAY "AREA2=" F-REC "]"
           DISPLAY "DEP2=" WS-LEN
           CLOSE F
           STOP RUN.
