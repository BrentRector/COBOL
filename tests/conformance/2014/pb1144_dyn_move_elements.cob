      *> kb/Work PB1144 leg 2 - the corresponding table ELEMENTS of a variable-length group MOVE (ISO
      *> 14.9.25.4 GR9) are moved by the elementary MOVE rules, not as character images: 14.6.9.2,
      *> "Correspondingly numbered elements are moved according to the rules of the MOVE statement
      *> specified in 14.9.25, MOVE statement". 8.5.1.12.3 matches two tables when "the byte length of
      *> their elements is equal and their elements are compatible", so any pair of equal byte length
      *> that 14.9.25.3 admits is converted.
      *> A: 9(4) 1234 / 5678 into 99V99, dynamic and fixed receivers - "the data is aligned by decimal
      *>    point and is transferred to the receiving digits with zero fill or truncation on either end"
      *>    (14.6.8.2 4)): 34.00 / 78.00, displayed 3400 7800 (as character images they read 12.34/56.78).
      *> B: one MOVE to two receivers. S9(4) -12 into 9(4): "the absolute value of the sending value is
      *>    used" (14.9.25.4 GR6 d)2.b.) -> 0012; 9(6) into ZZ,ZZ9 is edited; 9(4) into X(4) moves the
      *>    digits. The dynamic receivers take the sender's capacity (RT raised to its FROM 3 minimum);
      *>    the fixed FR keeps its own counts: "superfluous elements are not moved" (14.6.9.2 1)) - FT(1)
      *>    only - and "all the remaining elements of the receiving table are space filled" (14.6.9.2 2)).
      *> C: a FIXED sender into a dynamic receiver (14.6.9.1 treats the fixed table as one of capacity 2):
      *>    99V99 12.34 / 56.78 into 9(4) -> 0012 0056.
      *> D: the implicit MOVEs of WRITE ... FROM (14.9.51.4) and READ ... INTO (14.9.30.4) are MOVEs too:
      *>    9(4) into the record's 99V99 -> 3400 7800; the record back into an identically described
      *>    9(2)V9(2) table (no conversion to do) -> 3400 7800, and into 9(4) -> 0034 0078.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1144-DYN-MOVE-ELEMENTS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB1144EL.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 REC.
          05 XH PIC X(2).
          05 XR PIC 99V99 OCCURS 2 TIMES.
       WORKING-STORAGE SECTION.
       01 GS.
          05 SH PIC X(2) VALUE "HH".
          05 ST PIC 9(4) OCCURS DYNAMIC CAPACITY IN CA FROM 2.
       01 GR.
          05 QH PIC X(2).
          05 RT PIC 99V99 OCCURS DYNAMIC CAPACITY IN CR FROM 1.
       01 FR.
          05 FH PIC X(2).
          05 FT PIC 99V99 OCCURS 2 TIMES.
       01 G2.
          05 H2 PIC X(2) VALUE "H2".
          05 S2 PIC S9(4) OCCURS DYNAMIC CAPACITY IN C2 FROM 1.
          05 U2 PIC 9(6) OCCURS DYNAMIC CAPACITY IN CU2 FROM 1.
          05 X2 PIC 9(4) OCCURS DYNAMIC CAPACITY IN CX2 FROM 1.
       01 GR2.
          05 HR2 PIC X(2).
          05 SR2 PIC 9(4) OCCURS DYNAMIC CAPACITY IN CS3 FROM 3.
          05 UR2 PIC ZZ,ZZ9 OCCURS DYNAMIC CAPACITY IN CU3 FROM 1.
          05 XR2 PIC X(4) OCCURS DYNAMIC CAPACITY IN CX3 FROM 1.
       01 FR2.
          05 HF2 PIC X(2).
          05 SF2 PIC 9(4) OCCURS 1 TIMES.
          05 UF2 PIC ZZ,ZZ9 OCCURS 3 TIMES.
          05 XF2 PIC X(4) OCCURS 2 TIMES.
       01 FS.
          05 HS PIC X(2) VALUE "FS".
          05 TS PIC 99V99 OCCURS 2 TIMES.
       01 DR.
          05 HD PIC X(2).
          05 TD PIC 9(4) OCCURS DYNAMIC CAPACITY IN CD.
       01 GI.
          05 HI PIC X(2).
          05 TI PIC 9(2)V9(2) OCCURS DYNAMIC CAPACITY IN CI.
       01 GJ.
          05 HJ PIC X(2).
          05 TJ PIC 9(4) OCCURS DYNAMIC CAPACITY IN CJ.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 1234 TO ST (1).
           MOVE 5678 TO ST (2).
           MOVE GS TO GR.
           DISPLAY "A1=" CR " " RT (1) " " RT (2).
           MOVE GS TO FR.
           DISPLAY "A2=" FT (1) " " FT (2).
           MOVE -12 TO S2 (1).
           MOVE 34 TO S2 (2).
           MOVE 1234 TO U2 (1).
           MOVE 98765 TO U2 (2).
           MOVE 42 TO X2 (1).
           MOVE G2 TO GR2 FR2.
           DISPLAY "B1=" CS3 " " SR2 (1) " " SR2 (2).
           DISPLAY "B2=" CU3 " [" UR2 (1) "][" UR2 (2) "]".
           DISPLAY "B3=" CX3 " [" XR2 (1) "]".
           DISPLAY "B4=" SF2 (1) " [" UF2 (1) "][" UF2 (2) "]["
               UF2 (3) "]".
           DISPLAY "B5=[" XF2 (1) "][" XF2 (2) "]".
           MOVE 12.34 TO TS (1).
           MOVE 56.78 TO TS (2).
           MOVE FS TO DR.
           DISPLAY "C=" CD " " TD (1) " " TD (2).
           OPEN OUTPUT F.
           WRITE REC FROM GS.
           CLOSE F.
           OPEN INPUT F.
           READ F INTO GI.
           DISPLAY "D1=" XR (1) " " XR (2).
           DISPLAY "D2=" CI " " TI (1) " " TI (2).
           CLOSE F.
           OPEN INPUT F.
           READ F INTO GJ.
           DISPLAY "D3=" CJ " " TJ (1) " " TJ (2).
           CLOSE F.
           STOP RUN.
