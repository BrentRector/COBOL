      *> reject-at: 2023
      *> kb/Work PB2079 - ISO/IEC 1989:2023 15.12.3 r1: "Argument-2 and argument-3 shall be positive nonzero
      *> numeric integer literals or data items"; 15.6 types them Int2, Int3 (15.3 r6: an integer data item or
      *> an always-integral arithmetic expression). An alphanumeric data item A (PIC X(2) VALUE "16") is class
      *> alphanumeric, not numeric, so it is refused (COBOLNET1627). It used to compile clean and print R=26.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2079ALN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(40).
       01 A PIC X(2) VALUE "16".
       PROCEDURE DIVISION.
           MOVE FUNCTION BASECONVERT("1A" A 10) TO R
           DISPLAY "R=" R
           STOP RUN.
