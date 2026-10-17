      *>--- conformance-case
      *> id: l1c10-file-control-no-period
      *> rules: [FMT-12.4.4.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.4.4.2 format — FILE-CONTROL requires its period
      *> General format: "FILE-CONTROL. [ file-control-entry ] ..."
      *> The separator period after FILE-CONTROL is part of the
      *> format (not bracketed), so a paragraph header without it
      *> is not a FILE-CONTROL paragraph. The X3.23-1985 format
      *> also writes the period, hence 85 is listed.
      *> cite.py --check 12.4.4.2 "file-control-entry"
      *>   -> OK  §12.4.4.2   (General format)
      *> Pure general-format rule: the expected diagnostic is the
      *> generic parse code. Adding the period back gives a valid
      *> program (compare 2002/l1c10_file_control_two_entries).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C10N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL
           SELECT L1C10G1 ASSIGN TO "L1C10N2.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD L1C10G1.
       01 R1 PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "X".
           STOP RUN.
