      *>--- conformance-case
      *> id: pb485_level_number_legal_forms
      *> rules: [GR-8.5.1.3.2-1, SR-13.18.33.3-3, SR-13.18.33.3-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 13.18.33.3 SR3/SR5 and 13.18.33.4 GR2 -- the OVER-REJECTION
      *> GUARD for the COBOLNET1746 screen. Every level-number the
      *> standard permits in the working-storage section is written here
      *> and must still compile at COBOL-85, the earliest edition the
      *> matrix covers: the 01 through 09 spellings SR3 sanctions ("A
      *> level-number in the range of 1 through 9 may be specified as 01
      *> through 09"), the bare 1 spelling of the same value, the top of
      *> the 13.18.33.1 hierarchy range at 49, and the three special
      *> levels 13.18.33.4 GR2 assigns -- 66 RENAMES, 77 noncontiguous,
      *> 88 condition-name. A screen that tested a flat range, or that
      *> compared the level TEXT instead of its VALUE, would reject one
      *> of these. kb/Work PB485.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB485P1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           02  L2.
               03  L3.
                   04  L4.
                       05  L5.
                           06  L6.
                               07  L7.
                                   08  L8.
                                       09  L9 PIC X(2) VALUE "AB".
       01  H.
           10  H10.
               49  H49 PIC X(3) VALUE "XYZ".
           10  H2 PIC X(2) VALUE "PQ".
       66  R-ALL RENAMES H10 THRU H2.
       77  IND PIC 9 VALUE 1.
       88  IND-ON VALUE 1.
       1   ONEV PIC X VALUE "Z".
       PROCEDURE DIVISION.
           DISPLAY L9
           DISPLAY H49
           DISPLAY R-ALL
           IF IND-ON DISPLAY "ON" END-IF
           DISPLAY ONEV
           STOP RUN.
