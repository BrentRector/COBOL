      *>--- conformance-case
      *> id: l1c37_relative_io_insert
      *> rules: [GR-14.9.51.4-32]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.51.4 GR32 — relative file, I-O mode, random/dynamic
      *>   access: WRITE inserts the record at the relative key value
      *> Rule: "When a relative file is opened in the I-O mode and the
      *>   access mode is random or dynamic, records are to be inserted
      *>   in the associated file. Prior to the execution of the WRITE
      *>   statement, the value of the relative key data item shall be
      *>   initialized by the runtime element with the relative record
      *>   number to be associated with the record that is to be
      *>   written. That record is then released to the operating
      *>   environment by the execution of the WRITE statement."
      *>   cite.py --check 14.9.51.4 "When a relative file is opened in
      *>   the I-O mode and the access mode is random or dynamic,
      *>   records are to be inserted in the associated file"
      *>   -> OK  §14.9.51.4 32)  (General rules)
      *>   (The "Prior to the execution ... shall be initialized by the
      *>   runtime element" sentence is also printed in 29) b); cite.py
      *>   reports that first occurrence, so the GR32-only sentence is
      *>   the one checked.)
      *>   cite.py --check 14.9.30.4 "If the file position indicator
      *>   was established by a prior successful OPEN or START
      *>   statement, the first existing record that is selected is
      *>   made available" -> OK §14.9.30.4 21) (relative list, b))
      *> DERIVATION. An empty relative file is created (OPEN OUTPUT,
      *> CLOSE) and opened I-O with DYNAMIC access. The program sets
      *> the relative key to 5 and writes R005, then to 2 and writes
      *> R002, both into EMPTY slots, so each insert succeeds ('00').
      *> Nothing in WRITE's random/dynamic rules moves a number into
      *> the relative key (only 29) a) sequential and 30) extend do),
      *> so RK still shows the value the program set. START KEY NOT
      *> LESS THAN 1 then READ NEXT walks the existing records in
      *> relative-number order: slot 2 (R002), slot 5 (R005), AT END.
      *> Slots 1,3,4 do not exist. An implementation that appended at
      *> the next sequential number (1, 2) would print 0001 first.
      *>   W5 00 0005 / W2 00 0002 / N 0002 R002 / N 0005 R005 / END
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C37C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "L1C37C.REL"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD RELF.
       01 RREC.
           05 RDATA PIC X(4).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       01 FS PIC XX.
       PROCEDURE DIVISION.
       M1.
           OPEN OUTPUT RELF
           CLOSE RELF
           OPEN I-O RELF
           MOVE 5 TO RK
           MOVE "R005" TO RDATA
           WRITE RREC INVALID KEY DISPLAY "W5 INVALID"
           END-WRITE
           DISPLAY "W5 " FS " " RK
           MOVE 2 TO RK
           MOVE "R002" TO RDATA
           WRITE RREC INVALID KEY DISPLAY "W2 INVALID"
           END-WRITE
           DISPLAY "W2 " FS " " RK
           MOVE 1 TO RK
           START RELF KEY IS NOT LESS THAN RK
               INVALID KEY DISPLAY "START INVALID"
           END-START
           PERFORM 3 TIMES
               READ RELF NEXT RECORD
                   AT END DISPLAY "END"
                   NOT AT END DISPLAY "N " RK " " RDATA
               END-READ
           END-PERFORM
           CLOSE RELF
           STOP RUN.
