      *>--- conformance-case
      *> id: l1c26_rewrite_relative_invalid_key
      *> rules: [GR-14.9.35.4-19, GR-14.9.35.4-20, GR-14.9.35.4-21]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.35.4 GR19 GR20 GR21 — relative REWRITE: key, size
      *> GR19: "Transfer of control following the successful or
      *>   unsuccessful execution of the REWRITE operation depends on
      *>   the presence or absence of the optional INVALID KEY and NOT
      *>   INVALID KEY phrases in the REWRITE statement. (See 9.1.14,
      *>   Invalid key condition.)"
      *> GR20: "The number of bytes in ... the record referenced by
      *>   record-name-1 ... shall not be larger than the largest or
      *>   smaller than the smallest number of bytes allowed by the
      *>   RECORD IS VARYING clause ... If this rule is violated, the
      *>   execution of the REWRITE statement is unsuccessful and the
      *>   I-O status in the rewrite file connector is set to '44'."
      *> GR21: "For a file accessed in either random or dynamic access
      *>   mode, the operating environment logically replaces the
      *>   record identified by the relative key data item ... If the
      *>   file does not contain the record specified by the key, the
      *>   invalid key condition exists. ... the I-O status in the
      *>   rewrite file connector is set to the invalid key condition
      *>   '23'."
      *> cite.py --check:
      *>   OK  §14.9.35.4 19)  (General rules)
      *>   OK  §14.9.35.4 20)  (General rules)
      *>   OK  §14.9.35.4 21)  (General rules)
      *>   OK  §14.9.35.4 14)  (General rules)  unsuccessful: "no
      *>       logical record updating takes place, the content of the
      *>       record area is unaffected"
      *>   OK  §9.1.14 2)  "If the INVALID KEY phrase is specified ...
      *>       any applicable exception processing statements are not
      *>       executed, and control is transferred to the
      *>       imperative-statement specified in the INVALID KEY phrase"
      *>   OK  §9.1.14 3)  "If the INVALID KEY phrase is not specified
      *>       ... any applicable exception processing statements are
      *>       executed"
      *>   OK  §9.1.14 4)  "the INVALID KEY phrase is ignored, if
      *>       specified" -- cite.py labels this "4)" but it is the
      *>       UNNUMBERED paragraph following that list (PB1554)
      *>   OK  §9.1.14 1)  (second list) "If the I-O status indicates
      *>       an unsuccessful completion that is not an invalid key
      *>       condition, control is transferred according to the rules
      *>       of any applicable exception processing statements"
      *>   OK  §9.1.14 2)  (second list) "control is transferred to the
      *>       end of the input-output statement or to the
      *>       imperative-statement specified in the NOT INVALID KEY
      *>       phrase if it is specified"
      *>   OK  §14.9.49.4 6)  format 1 USE procedures "are executed ...
      *>       upon the unsuccessful execution of an input-output
      *>       operation unless an AT END or INVALID KEY phrase takes
      *>       precedence"
      *>   OK  §13.18.43.4 13) a)  REWRITE record size = content of
      *>       data-name-1 (the DEPENDING ON item LN)
      *>   OK  §13.18.43.4 15)  after a successful READ data-name-1
      *>       holds the size of the record just read
      *> Setup: slot 1 = 'AAAAAAAAAA' (10), slot 2 = 'BBBBBBBBBB' (10);
      *> slots 7 and 9 are empty. VARYING 5 TO 20. DECL counts USE runs.
      *> Derivation of every expected line:
      *>  C1 RK=7 (empty), both phrases: GR21 -> invalid key, '23';
      *>     9.1.14 2) -> INVALID arm, declarative NOT run;
      *>     §14.9.35.4 GR14 -> record area unaffected:
      *>     "C1 ST=23 DECL=0 REC=XXXXXXXXXX ARM=INVALID"
      *>  C2 RK=1 (present), LN=12, both phrases: GR21 -> slot 1
      *>     replaced, '00'; 9.1.14 (second list) 2) -> NOT INVALID arm:
      *>     "C2 ST=00 DECL=0 ARM=NOT-INVALID"
      *>  C2R READ slot 1: the new 12-byte record (GR21 + 13.18.43 15):
      *>     "C2R ST=00 LN=0012 REC=CCCCCCCCCCCC"
      *>  C2S READ slot 2: untouched (GR21 replaced slot 1, not 2):
      *>     "C2S ST=00 LN=0010 REC=BBBBBBBBBB"
      *>  C3 RK=9 (empty), only NOT INVALID KEY: invalid key '23';
      *>     9.1.14 3) -> USE procedure runs (DECL 0->1); NOT arm not
      *>     taken (not a successful completion):
      *>     "C3 ST=23 DECL=1 ARM=NONE"
      *>  C4 RK=2 (present), only INVALID KEY: success '00'; the
      *>     INVALID KEY phrase is ignored; no NOT phrase -> end of
      *>     statement; no USE run:
      *>     "C4 ST=00 DECL=1 ARM=NONE"
      *>  C5 RK=1, LN=3 (< 5), both phrases: GR20 -> '44'; not an
      *>     invalid key -> neither arm; 9.1.14 (second list) 1) and
      *>     14.9.49 GR6 -> USE runs (DECL 1->2); GR14 area unaffected:
      *>     "C5 ST=44 DECL=2 REC=ZZZ ARM=NONE"
      *>  C6 RK=1, LN=21 (> 20; RELF-REC is 20 bytes per 13.18.43 SR4;
      *>     LN alone sets the size, GR13 a)), both phrases: as C5,
      *>     DECL 2->3:
      *>     "C6 ST=44 DECL=3 ARM=NONE"
      *>  C6R READ slot 1: C5/C6 did not update it (GR14):
      *>     "C6R ST=00 LN=0012 REC=CCCCCCCCCCCC"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C26A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "L1C26A.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK
               FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD  RELF
           RECORD IS VARYING IN SIZE FROM 5 TO 20 CHARACTERS
               DEPENDING ON LN.
       01  RELF-REC                  PIC X(20).
       WORKING-STORAGE SECTION.
       01  RK                      PIC 9(4).
       01  LN                      PIC 9(4).
       01  ST                      PIC XX.
       01  DECL-CNT                PIC 9 VALUE 0.
       01  ARM                     PIC X(11).
       PROCEDURE DIVISION.
       DECLARATIVES.
       RELF-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RELF.
       RELF-ERR-P.
           ADD 1 TO DECL-CNT.
       END DECLARATIVES.
       MAIN-S SECTION.
       M-1.
           OPEN OUTPUT RELF
           MOVE 1 TO RK
           MOVE 10 TO LN
           MOVE "AAAAAAAAAA" TO RELF-REC
           WRITE RELF-REC
           MOVE 2 TO RK
           MOVE 10 TO LN
           MOVE "BBBBBBBBBB" TO RELF-REC
           WRITE RELF-REC
           CLOSE RELF
           OPEN I-O RELF
      *> C1: absent slot, both phrases
           MOVE "NONE" TO ARM
           MOVE 7 TO RK
           MOVE 10 TO LN
           MOVE "XXXXXXXXXX" TO RELF-REC
           REWRITE RELF-REC
               INVALID KEY MOVE "INVALID" TO ARM
               NOT INVALID KEY MOVE "NOT-INVALID" TO ARM
           END-REWRITE
           DISPLAY "C1 ST=" ST " DECL=" DECL-CNT
               " REC=" RELF-REC(1:10) " ARM=" ARM
      *> C2: present slot, both phrases
           MOVE "NONE" TO ARM
           MOVE 1 TO RK
           MOVE 12 TO LN
           MOVE "CCCCCCCCCCCC" TO RELF-REC
           REWRITE RELF-REC
               INVALID KEY MOVE "INVALID" TO ARM
               NOT INVALID KEY MOVE "NOT-INVALID" TO ARM
           END-REWRITE
           DISPLAY "C2 ST=" ST " DECL=" DECL-CNT " ARM=" ARM
           MOVE SPACES TO RELF-REC
           MOVE 1 TO RK
           READ RELF
           DISPLAY "C2R ST=" ST " LN=" LN " REC=" RELF-REC(1:LN)
           MOVE SPACES TO RELF-REC
           MOVE 2 TO RK
           READ RELF
           DISPLAY "C2S ST=" ST " LN=" LN " REC=" RELF-REC(1:LN)
      *> C3: absent slot, NOT INVALID KEY only
           MOVE "NONE" TO ARM
           MOVE 9 TO RK
           MOVE 10 TO LN
           MOVE "YYYYYYYYYY" TO RELF-REC
           REWRITE RELF-REC
               NOT INVALID KEY MOVE "NOT-INVALID" TO ARM
           END-REWRITE
           DISPLAY "C3 ST=" ST " DECL=" DECL-CNT " ARM=" ARM
      *> C4: present slot, INVALID KEY only
           MOVE "NONE" TO ARM
           MOVE 2 TO RK
           MOVE 10 TO LN
           MOVE "DDDDDDDDDD" TO RELF-REC
           REWRITE RELF-REC
               INVALID KEY MOVE "INVALID" TO ARM
           END-REWRITE
           DISPLAY "C4 ST=" ST " DECL=" DECL-CNT " ARM=" ARM
      *> C5: record size below the VARYING minimum
           MOVE "NONE" TO ARM
           MOVE 1 TO RK
           MOVE 3 TO LN
           MOVE "ZZZ" TO RELF-REC
           REWRITE RELF-REC
               INVALID KEY MOVE "INVALID" TO ARM
               NOT INVALID KEY MOVE "NOT-INVALID" TO ARM
           END-REWRITE
           DISPLAY "C5 ST=" ST " DECL=" DECL-CNT
               " REC=" RELF-REC(1:3) " ARM=" ARM
      *> C6: record size above the VARYING maximum
           MOVE "NONE" TO ARM
           MOVE 1 TO RK
           MOVE 21 TO LN
           MOVE ALL "W" TO RELF-REC
           REWRITE RELF-REC
               INVALID KEY MOVE "INVALID" TO ARM
               NOT INVALID KEY MOVE "NOT-INVALID" TO ARM
           END-REWRITE
           DISPLAY "C6 ST=" ST " DECL=" DECL-CNT " ARM=" ARM
           MOVE SPACES TO RELF-REC
           MOVE 1 TO RK
           READ RELF
           DISPLAY "C6R ST=" ST " LN=" LN " REC=" RELF-REC(1:LN)
           CLOSE RELF
           STOP RUN.
