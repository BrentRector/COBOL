      *> reject-at: 85 2002
      *> kb/Work PB1267 - the edition floor under conformance:2014/pb1267_dyn_initialized_seed (and the
      *> other wave-67 group O goldens: pb1144_dyn_move_recreate, pb1269_dyn_overflow_resume_next,
      *> pb1411_dyn_vargroup_element_receiver). A dynamic-capacity table (8.5.1.9; OCCURS Format 4, 13.18.38)
      *> and its INITIALIZED phrase (8.5.1.9.5) are COBOL-2014 introductions, so at COBOL-85 and COBOL-2002
      *> the table cannot be declared at all; COBOLNET0900 is the version gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1267N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 R OCCURS DYNAMIC CAPACITY IN RCAP FROM 1 INITIALIZED.
             10 RE1 PIC ZZ9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 5 TO RE1 (2).
           DISPLAY RE1 (2).
           STOP RUN.
