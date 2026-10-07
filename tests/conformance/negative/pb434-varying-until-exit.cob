      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB434 - ISO/IEC 1989:2023 14.9.28.3 SR8: "The UNTIL EXIT phrase shall not be specified in a
      *> PERFORM statement with or under a PERFORM statement with the VARYING phrase or either the TEST BEFORE
      *> or TEST AFTER phrase". The varying-phrase prints "UNTIL condition-1" only (14.9.28.2), and this
      *> PERFORM writes UNTIL EXIT there. It used to be a bare COBOL0001 naming no rule; the grammar now spells
      *> EXIT in every UNTIL of a PERFORM so the binder names the rule: COBOLNET2954 at every edition, beside
      *> COBOLNET0900 below 2023, where UNTIL EXIT (14.9.28.4 GR11) does not exist at all.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB434VX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM VARYING I FROM 1 BY 1 UNTIL EXIT
               DISPLAY I
               EXIT PERFORM
           END-PERFORM
           STOP RUN.
