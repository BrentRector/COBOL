      *> ISO/IEC 1989:2023 7.3.17.3 SR1 (cite.py --check 7.3.17.3 "The LEAP-SECOND directive shall not be specified within a compilation
      *> unit." -> OK 1)): the directive below is written BETWEEN two sibling compilation units - after END PROGRAM PB1378A and before
      *> PB1378B's IDENTIFICATION DIVISION - which is outside both, so it is legal. 7.3.4 GR5 (cite.py --check 7.3.4 "A compiler directive
      *> applies to all of the source text and library text that follows" -> OK 5)): it governs the text that FOLLOWS, so unit A runs under
      *> the implied OFF (7.3.17.4 GR1) and unit B under ON. 15.79.4: SECONDS-FROM-FORMATTED-TIME("hhmmss", "235960") is 86400 (8640000 in
      *> PIC 9(6)V99) only where the seconds subfield 60 is admissible (15.3.3.3, "less than 61 ... when the LEAP-SECOND directive with the
      *> ON phrase is in effect"); under OFF "235960" is invalid and the default returned value is 0. kb/Work PB1378.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1378A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R  PIC 9(6)V99.
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION SECONDS-FROM-FORMATTED-TIME
               ("hhmmss", "235960").
           DISPLAY "A SFFT(235960)=" R.
           CALL "PB1378B".
           STOP RUN.
       END PROGRAM PB1378A.
       >>LEAP-SECOND ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1378B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R  PIC 9(6)V99.
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION SECONDS-FROM-FORMATTED-TIME
               ("hhmmss", "235960").
           DISPLAY "B SFFT(235960)=" R.
           GOBACK.
       END PROGRAM PB1378B.
