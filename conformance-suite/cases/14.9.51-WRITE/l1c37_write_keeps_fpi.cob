      *>--- conformance-case
      *> id: l1c37_write_keeps_fpi
      *> rules: [GR-14.9.51.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.51.4 GR2 — WRITE does not affect the file position
      *>   indicator (relative file, dynamic access, I-O mode)
      *> Rule: "The file position indicator is not affected by the
      *>   execution of a WRITE statement."
      *>   cite.py --check 14.9.51.4 "The file position indicator is
      *>   not affected by the execution of a WRITE statement"
      *>   -> OK  §14.9.51.4 2)  (General rules)
      *>   cite.py --check 14.9.30.4 "If the file position indicator
      *>   was established by a prior successful READ statement, the
      *>   first existing record in the physical file whose relative
      *>   key number is greater than the file position indicator"
      *>   -> OK  §14.9.30.4 21) (relative-file list, item c); cite.py
      *>   cannot tell the relative and sequential items apart, PB1554)
      *> DERIVATION. The file is loaded with records 1,2,3, reopened
      *> I-O, and a random READ of key 1 establishes the file position
      *> indicator at 1. A WRITE of the HIGHER key 10 succeeds ('00',
      *> GR29 b)). Because GR2 leaves the indicator at 1, the READ
      *> NEXTs select the first record whose number is greater than
      *> it: 2, then 3, then the newly written 10, then AT END. An
      *> implementation that moved the indicator to the written record
      *> would print REL END first.
      *>   REL W 00 / REL N 0002 R02 / REL N 0003 R03 / REL N 0010 R10 /
      *>   REL END
      *> No indexed leg: §14.9.30.4's indexed READ NEXT rules key on
      *> "the previous operation on the file", which the intervening
      *> WRITE is, so the indexed successor is not decided by GR2 alone.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C37B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "L1C37B.REL"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK
               FILE STATUS IS RFS.
       DATA DIVISION.
       FILE SECTION.
       FD RELF.
       01 RREC.
           05 RDATA PIC X(3).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       01 RFS PIC XX.
       01 N PIC 9.
       PROCEDURE DIVISION.
       M1.
           OPEN OUTPUT RELF
           PERFORM VARYING N FROM 1 BY 1 UNTIL N > 3
               MOVE N TO RK
               MOVE "R0" TO RDATA(1:2)
               MOVE N TO RDATA(3:1)
               WRITE RREC
           END-PERFORM
           CLOSE RELF
           OPEN I-O RELF
           MOVE 1 TO RK
           READ RELF
           MOVE 10 TO RK
           MOVE "R10" TO RDATA
           WRITE RREC INVALID KEY DISPLAY "REL W INVALID"
           END-WRITE
           DISPLAY "REL W " RFS
           PERFORM 4 TIMES
               READ RELF NEXT RECORD
                   AT END DISPLAY "REL END"
                   NOT AT END DISPLAY "REL N " RK " " RDATA
               END-READ
           END-PERFORM
           CLOSE RELF
           STOP RUN.
