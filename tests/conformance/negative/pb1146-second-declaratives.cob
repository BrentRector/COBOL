      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1146. ISO 14.2.1 Format 1 prints ONE bracketed DECLARATIVES ... END DECLARATIVES portion
      *> right after the procedure division header (rendered from PDF page 557: no ellipsis follows it), so a
      *> second portion is refused COBOLNET2797 at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1146ND.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB1146ND.TMP".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-S SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F.
       ERR-P.
           DISPLAY "IN-DECLARATIVES".
       END DECLARATIVES.
       DECLARATIVES.
       ERR-T SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       ERR-Q.
           DISPLAY "IN-DECLARATIVES-2".
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           DISPLAY "B".
           STOP RUN.
