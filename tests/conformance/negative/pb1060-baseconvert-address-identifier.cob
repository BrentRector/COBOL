      *> reject-at: 2023
      *> Train 1021 review finding C-2 (kb/Work PB1060) - ISO/IEC 1989:2023 15.12.3 r1: "Argument-1 is the
      *> input data item to be converted and shall be a usage display or national data item or literal".
      *> ADDRESS OF X creates a unique data item of class pointer and category data-pointer (8.4.3.11.4 GR1),
      *> neither display nor national, so it is refused as a USAGE POINTER argument-1 is (COBOLNET1642).
      *> It used to compile and abort at run time with NotImplemented.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1060BC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(4) VALUE "ABCD".
       01 R PIC X(40).
       PROCEDURE DIVISION.
           MOVE FUNCTION BASECONVERT(ADDRESS OF X 16 10) TO R
           DISPLAY R
           STOP RUN.
