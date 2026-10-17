      *>--- conformance-case
      *> id: pb2848_lock_ceiling_after_record_rules
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2848CEIL.
      *> kb/Work PB2848 -- docs/CONFORMANCE.md DOC-A.1-104 (2): when
      *> more than one I-O status applies, the statement's own rules
      *> are tested first, in the order its general rules state them.
      *> The record-lock ceiling (ISO 12.4.5.9.4 GR7, I-O status '54'
      *> for the connector's maximum, 9.1.13.8 item 4) belongs to the
      *> lock the statement requests, and that lock is the statement's
      *> effect on SUCCESS (14.9.51.4 GR11, 14.9.35.4 GR12), so a
      *> record that breaks the statement's own rules is unsuccessful
      *> with that rule's value ('44', 14.9.51.4 GR14 and 14.9.35.4
      *> GR20) and not with '54'. The ceiling is still tested BEFORE
      *> the write, so the refused statement releases no record.
      *>   python scripts/spec/cite.py --check 12.4.5.9.4 "Any I-O
      *>   statement that attempts to obtain a record lock that would
      *>   exceed either limit is unsuccessful"   -> OK §12.4.5.9.4 7)
      *>   python scripts/spec/cite.py --check 9.1.13.8 "this file
      *>   connector holds the maximum number of locks"
      *>                                          -> OK §9.1.13.8 4)
      *>   python scripts/spec/cite.py --check 14.9.51.4 "the record
      *>   lock associated with the record written is set when the
      *>   execution of the WRITE statement is successful"
      *>                                          -> OK §14.9.51.4 11)
      *> The connector's maximum is 15 (DOC-A.1-155; the standard's
      *> floor), reached by fifteen WRITE ... WITH LOCK statements on
      *> records 1..15 of a LOCK ON MULTIPLE RECORDS connector.
      *>  R-W16-OK    - a valid record, a NEW lock, at the maximum: 54
      *>  R-W16-SHORT - record 16, 5 bytes of a 10..20 file: 44
      *>  R-READ16    - neither refused WRITE released record 16: 23
      *>  R-W16-PLAIN - the same record, no lock requested: 00
      *>  R-RW-OK     - REWRITE of 16, valid, WITH LOCK (new): 54
      *>  R-RW-SHORT  - REWRITE of 16, 5 bytes, WITH LOCK: 44
      *>  X-...       - the indexed organization, the same five
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT R ASSIGN TO "pb2848r.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS
               FILE STATUS IS RS.
           SELECT X ASSIGN TO "pb2848x.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS RANDOM
               RECORD KEY IS X-KEY
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS
               FILE STATUS IS XS.
       DATA DIVISION.
       FILE SECTION.
       FD R RECORD IS VARYING IN SIZE FROM 10 TO 20 CHARACTERS
             DEPENDING ON R-LEN.
       01 R-REC PIC X(20).
       FD X RECORD IS VARYING IN SIZE FROM 10 TO 20 CHARACTERS
             DEPENDING ON X-LEN.
       01 X-REC.
          05 X-KEY PIC XX.
          05 X-DATA PIC X(18).
       WORKING-STORAGE SECTION.
       01 RK PIC 99.
       01 R-LEN PIC 99.
       01 X-LEN PIC 99.
       01 RS PIC XX.
       01 XS PIC XX.
       01 I PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT R
           CLOSE R
           OPEN I-O R
           MOVE 10 TO R-LEN
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 15
               MOVE I TO RK
               MOVE ALL "A" TO R-REC
               WRITE R-REC WITH LOCK
           END-PERFORM
           DISPLAY "R-15TH " RS
           MOVE 16 TO RK
           WRITE R-REC WITH LOCK
           DISPLAY "R-W16-OK " RS
           MOVE 5 TO R-LEN
           WRITE R-REC WITH LOCK
           DISPLAY "R-W16-SHORT " RS
           MOVE 10 TO R-LEN
           READ R
           DISPLAY "R-READ16 " RS
           WRITE R-REC
           DISPLAY "R-W16-PLAIN " RS
           REWRITE R-REC WITH LOCK
           DISPLAY "R-RW-OK " RS
           MOVE 5 TO R-LEN
           REWRITE R-REC WITH LOCK
           DISPLAY "R-RW-SHORT " RS
           CLOSE R
           OPEN OUTPUT X
           CLOSE X
           OPEN I-O X
           MOVE 10 TO X-LEN
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 15
               MOVE I TO X-KEY
               MOVE ALL "A" TO X-DATA
               WRITE X-REC WITH LOCK
           END-PERFORM
           DISPLAY "X-15TH " XS
           MOVE 16 TO X-KEY
           WRITE X-REC WITH LOCK
           DISPLAY "X-W16-OK " XS
           MOVE 5 TO X-LEN
           WRITE X-REC WITH LOCK
           DISPLAY "X-W16-SHORT " XS
           MOVE 10 TO X-LEN
           READ X
           DISPLAY "X-READ16 " XS
           WRITE X-REC
           DISPLAY "X-W16-PLAIN " XS
           REWRITE X-REC WITH LOCK
           DISPLAY "X-RW-OK " XS
           MOVE 5 TO X-LEN
           REWRITE X-REC WITH LOCK
           DISPLAY "X-RW-SHORT " XS
           CLOSE X
           STOP RUN.
