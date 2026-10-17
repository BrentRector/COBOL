      *>--- conformance-case
      *> id: l1c24-reserve-missing-integer
      *> rules: [FMT-12.4.5.14.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.4.5.14.2 format — RESERVE with integer-1 omitted
      *> General format: RESERVE integer-1 [AREA|AREAS]; integer-1 is
      *> not bracketed, so it is required. "RESERVE AREAS" is not a
      *> RESERVE clause.
      *>   cite.py --check 12.4.5.14.2 "integer-1"
      *>     -> OK §12.4.5.14.2 (General format)
      *> The SELECT entry is otherwise valid; a general-format (pure
      *> syntax) violation is reported as a parse error.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C24G.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "L1C24G.DAT"
               RESERVE AREAS.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(6).
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
