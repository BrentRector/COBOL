      *>--- conformance-case
      *> id: l1c04_constant_record_ws_ls
      *> rules: [SR-13.18.15.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.15.3 SR1 — CONSTANT RECORD admitted in BOTH
      *>   local-storage and working-storage.
      *>   "The CONSTANT RECORD clause may be specified only in the
      *>     local-storage or
      *>    working-storage sections."
      *>   cite.py --check 13.18.15.3 "The CONSTANT RECORD clause may be
      *>     specified only in
      *>     the local-storage or working-storage sections" -> OK
      *>       §13.18.15.3 1) (Syntax rules)
      *> This is the ADMITTING half of SR1; the rejecting half (file /
      *>   linkage sections) is
      *> pinned by negative/l1c04-constant-record-linkage and
      *>   l1c04-constant-record-file-section.
      *> Expected output, from the VALUE clauses of the two constant
      *>   records:
      *>   "WS=WSOK" - the WORKING-STORAGE constant record compiles and
      *>     holds its VALUE.
      *>   "LS=LSOK" - the LOCAL-STORAGE constant record compiles and
      *>     holds its VALUE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C04C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-CFG CONSTANT RECORD.
          05 WS-TAG PIC X(4) VALUE "WSOK".
       LOCAL-STORAGE SECTION.
       01 LS-CFG CONSTANT RECORD.
          05 LS-TAG PIC X(4) VALUE "LSOK".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "WS=" WS-TAG.
           DISPLAY "LS=" LS-TAG.
           GOBACK.
