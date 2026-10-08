      *> reject-at: 2023
      *> kb/Work PB2079 - ISO/IEC 1989:2023 15.12.3 r1 (argument-2 and argument-3 are numeric integer literals
      *> or data items; 15.6 Int2, Int3). A USAGE POINTER item is class pointer (8.5.2.1 Table 2), not numeric,
      *> so it is refused as a base (COBOLNET1627) - it used to die in the generated C# (CS1503). A pointer
      *> holds nothing for the --permissive coercion extension to decode, so that mode refuses it as well.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2079PTR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(40).
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
           MOVE FUNCTION BASECONVERT("1A" P 10) TO R
           DISPLAY "R=" R
           STOP RUN.
