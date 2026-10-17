      *>--- conformance-case
      *> id: l1c10_file_control_no_entries
      *> rules: [FMT-12.4.4.2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.4.4.2 format — FILE-CONTROL paragraph with zero entries
      *>
      *> Row pinned: FMT-12.4.4.2 (the "[ file-control-entry ] ..."
      *> optional-repetition arm: zero entries).
      *>  General format: "FILE-CONTROL. [ file-control-entry ] ..."
      *>  The brackets make every file-control-entry optional, so the
      *>  paragraph header and its separator period alone are a
      *>  complete, legal FILE-CONTROL paragraph. §12.4.2 lets it
      *>  stand alone in the INPUT-OUTPUT SECTION (the
      *>  i-o-control-paragraph is also optional).
      *> cite.py --check:
      *>  OK  §12.4.4.2   (General format)  file-control-entry
      *>  OK  §12.4.2   (General format)  file-control-paragraph
      *> Derivation: the program is legal source, so it compiles and
      *> runs; the single DISPLAY gives
      *>  FC-EMPTY=OK
      *> A compiler requiring at least one entry would reject it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C10C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(2) VALUE "OK".
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "FC-EMPTY=" W.
           STOP RUN.
