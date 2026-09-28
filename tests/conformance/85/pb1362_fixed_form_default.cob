      *> options: source-format=fixed
      * ISO/IEC 1989:2023 7.3.24.3 2) "The default reference format of
      * a compilation group is fixed form." (kb/Work PB1362) The
      * sequence area holds any character (6.3.2) and positions 73+
      * lie outside margin R (6.3.1), so every line below is fixed.
ABCDEF IDENTIFICATION DIVISION.                                         PB136201
ABCDEF PROGRAM-ID. PB1362-FIXED-DEFAULT.                                PB136202
       DATA DIVISION.                                                   PB136203
       WORKING-STORAGE SECTION.                                         PB136204
SEQ005 01 W-TEXT PIC X(12) VALUE "FIXED-FORM".                          NOTCODE!
XYZ006* a comment line: col 7 is the comment indicator                  PB136206
       PROCEDURE DIVISION.                                              PB136207
A1B2C3     DISPLAY "SEQ-AREA-OK " W-TEXT                                PB136208
           DISPLAY "MARGIN-R-OK"                                        DISPLAY "NO"
           STOP RUN.                                                    PB136210
