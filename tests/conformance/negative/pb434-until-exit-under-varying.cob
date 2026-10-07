      *> reject-at: 2023
      *> kb/Work PB434 - ISO/IEC 1989:2023 14.9.28.3 SR8: "The UNTIL EXIT phrase shall not be specified in a
      *> PERFORM statement with or under a PERFORM statement with the VARYING phrase or either the TEST BEFORE
      *> or TEST AFTER phrase". SUB-P is performed OUT OF LINE from inside a VARYING PERFORM, so by
      *> 14.9.28.4 GR1 ("The range includes all statements that are executed as the result of a transfer of
      *> control in the range of the PERFORM statement") its UNTIL EXIT PERFORM is under it. This used to
      *> compile and print 0002; it now draws COBOLNET2954 at SUB-P's PERFORM. The inline shapes and the
      *> "with" arm are pinned row by row in PerformUntilExitPlacementTests.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB434UV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I PIC 9(4) VALUE 0.
       01 N PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 2
               PERFORM SUB-P
           END-PERFORM
           DISPLAY N
           STOP RUN.
       SUB-P.
           PERFORM UNTIL EXIT
               ADD 1 TO N
               EXIT PERFORM
           END-PERFORM.
