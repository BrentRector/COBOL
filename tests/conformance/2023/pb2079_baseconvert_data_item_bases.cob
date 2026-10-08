      *> kb/Work PB2079 - ISO/IEC 1989:2023 15.12.3 r1: argument-2 and argument-3 "shall be positive nonzero
      *> numeric integer literals or data items with unequal values in the range 2 to 16" (15.6: Int2, Int3), so
      *> a numeric integer DATA ITEM of any usage is a legal base - the class screen that refuses an
      *> alphanumeric, pointer or ADDRESS OF base must keep admitting these. Values per 15.12.4 r1:
      *> "FF" in base 16 is 255; "255" in base 10 is FF in base 16; "A" in base 16 is 1010 in base 2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2079OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B2   PIC 99 VALUE 16.
       01 B3   PIC 99 VALUE 10.
       01 B4   PIC 9 COMP VALUE 2.
       01 R8   PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION BASECONVERT("FF" B2 B3) TO R8
           DISPLAY "D16-10=[" R8 "]"
           MOVE FUNCTION BASECONVERT("255" B3 B2) TO R8
           DISPLAY "D10-16=[" R8 "]"
           MOVE FUNCTION BASECONVERT("A" B2 B4) TO R8
           DISPLAY "D16-2 =[" R8 "]"
           STOP RUN.
