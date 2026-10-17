      *>--- conformance-case
      *> id: l1c04-constant-record-linkage
      *> rules: [SR-13.18.15.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.15.3 SR1 — a CONSTANT RECORD entry in the LINKAGE
      *>   SECTION is rejected.
      *>   "The CONSTANT RECORD clause may be specified only in the
      *>     local-storage or
      *>    working-storage sections."
      *>   cite.py --check 13.18.15.3 "The CONSTANT RECORD clause may be
      *>     specified only in
      *>     the local-storage or working-storage sections" -> OK
      *>       §13.18.15.3 1) (Syntax rules)
      *> The linkage section is neither of the two sections SR1 admits,
      *>   so the entry below
      *> violates SR1 and nothing else (level 01, no REDEFINES/BASED/ANY
      *>   LENGTH/BWZ/SYNC,
      *> never stored into). COBOLNET1549 = constant-record-rule, "WS/LS
      *>   sections only (SR1)".
      *> Not rejected at 85 by THIS rule: CONSTANT RECORD is itself a
      *>   2002 construct there.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C04A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC X VALUE "A".
       LINKAGE SECTION.
       01 LK-CFG CONSTANT RECORD.
          05 LK-TAG PIC X(4) VALUE "COBL".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY W-X.
           GOBACK.
