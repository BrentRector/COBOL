      *>--- conformance-case
      *> id: l1c04-constant-record-file-section
      *> rules: [SR-13.18.15.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.15.3 SR1 — a CONSTANT RECORD entry as a FILE SECTION
      *>   record is rejected.
      *>   "The CONSTANT RECORD clause may be specified only in the
      *>     local-storage or
      *>    working-storage sections."
      *>   cite.py --check 13.18.15.3 "The CONSTANT RECORD clause may be
      *>     specified only in
      *>     the local-storage or working-storage sections" -> OK
      *>       §13.18.15.3 1) (Syntax rules)
      *> The file section is neither of the two sections SR1 admits, so
      *>   the record below
      *> violates SR1 and nothing else (level 01, no REDEFINES/BASED/ANY
      *>   LENGTH/BWZ/SYNC,
      *> the file is never opened or read). COBOLNET1549 =
      *>   constant-record-rule, SR1 arm.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C04B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "L1C04B.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 F1-REC CONSTANT RECORD.
          05 F1-TAG PIC X(4) VALUE "COBL".
       WORKING-STORAGE SECTION.
       01 W-X PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY W-X.
           STOP RUN.
