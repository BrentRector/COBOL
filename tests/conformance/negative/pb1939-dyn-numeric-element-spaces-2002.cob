      *> reject-at: 85 2002
      *> kb/Work PB1939 - the edition floor under conformance:2014/pb1939_dyn_numeric_element_spaces.
      *> A dynamic-capacity table (8.5.1.9; OCCURS Format 4, 13.18.38) is a COBOL-2014 introduction, so at
      *> COBOL-85 and COBOL-2002 the receiving table whose numeric elements the positive case space fills
      *> (14.6.9.2 2), 14.6.9.4) cannot be declared at all; COBOLNET0900 is the version gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1939N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G3.
          05 H3 PIC X(2) VALUE "G3".
       01 GR2.
          05 HR2 PIC X(2).
          05 SR2 PIC 9(4) OCCURS DYNAMIC CAPACITY IN CS3 FROM 3.
       PROCEDURE DIVISION.
           MOVE G3 TO GR2
           DISPLAY SR2 (1)
           STOP RUN.
