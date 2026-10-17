      *>--- conformance-case
      *> id: pb721_zero_length_record_85
      *> rules: [SR-13.18.43.3-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The COBOL-85 half of the Annex A.3 item 43 claim (the 2023 witness is
      *> tests/conformance/2023/pb721_zero_length_record.cob, which carries the full
      *> derivation). It exists because the capability and 13.18.43.3 SR7's permission
      *> to write a ZERO minimum are NOT edition-dependent, and the way that fact fails
      *> is a gate leaking downward: a >=2023 gate written on RECORD IS VARYING IN SIZE
      *> FROM 0 would reject legal COBOL-85 here and nowhere else.
      *>
      *> The edition question was settled before the code, not after: Annex E is the
      *> COMPLETE 2014->2023 delta and carries no entry for a zero minimum record size
      *> or for zero-length records generally (its only zero-length entry, E.2 item 23,
      *> is reference modification), so SR7 is not a 2023 introduction; and
      *> docs/VERSION_CHANGE_REFERENCE.md records that this repository's spec cannot
      *> speak for the 85<->2002 delta at all, so any gate below 2014 would be INVENTED
      *> rather than derived. Determination: no edition gate anywhere - which is what
      *> this program measures. kb/Work PB721.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB721-ZERO-LEN-85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ ASSIGN TO "pb721z85-sq.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-ST.
           SELECT RL ASSIGN TO "pb721z85-rl.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS WS-RK
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  SQ RECORD IS VARYING IN SIZE FROM 0 TO 20 CHARACTERS
               DEPENDING ON WS-LEN.
       01  SQ-REC PIC X(20).
       FD  RL RECORD IS VARYING IN SIZE FROM 0 TO 20 CHARACTERS
               DEPENDING ON WS-LEN.
       01  RL-REC PIC X(20).
       WORKING-STORAGE SECTION.
       01  WS-ST  PIC XX.
       01  WS-RK  PIC 9(4) VALUE 0.
       01  WS-LEN PIC 9(4) VALUE 0.
       01  WS-Z   PIC X(10) VALUE ALL "-".
       PROCEDURE DIVISION.
       MAIN.
           MOVE ALL "-" TO WS-Z
           MOVE 0 TO WS-LEN
           MOVE SPACES TO SQ-REC
           OPEN OUTPUT SQ
           WRITE SQ-REC
           DISPLAY "SQ W=" WS-ST
           CLOSE SQ
           MOVE 99 TO WS-LEN
           OPEN INPUT SQ
           READ SQ INTO WS-Z
               AT END DISPLAY "SQ ATEND"
           END-READ
           DISPLAY "SQ R=" WS-ST " LEN=" WS-LEN
           DISPLAY "SQ Z=[" WS-Z "]"
           CLOSE SQ

           MOVE ALL "-" TO WS-Z
           MOVE 0 TO WS-LEN
           MOVE SPACES TO RL-REC
           OPEN OUTPUT RL
           WRITE RL-REC
           DISPLAY "RL W=" WS-ST
           CLOSE RL
           MOVE 99 TO WS-LEN
           OPEN INPUT RL
           READ RL INTO WS-Z
               AT END DISPLAY "RL ATEND"
           END-READ
           DISPLAY "RL R=" WS-ST " LEN=" WS-LEN
           DISPLAY "RL Z=[" WS-Z "]"
           CLOSE RL
           STOP RUN.
