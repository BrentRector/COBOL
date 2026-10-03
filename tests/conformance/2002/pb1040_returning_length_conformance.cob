      *> kb/Work PB1040 - ISO 14.8.3.3: when the sending operand is not an object reference, "the receiving
      *> operand shall have the same ALIGN, BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE
      *> clauses", so a RETURNING pair whose PICTUREs differ in length does not conform. 14.9.4.4 GR3 d): "If a
      *> violation of these rules is detected, the EC-PROGRAM-ARG-MISMATCH exception condition is set to exist
      *> if checking for it is enabled in both the activated program and activating runtime element, the program
      *> call is not successful, and execution continues as specified in General rule 3h", and GR3 h) 1. sends
      *> an EC-PROGRAM condition to the ON EXCEPTION phrase. "Not successful" means the callee never runs.
      *> Measured before PB1040 (a dynamic Format-1 CALL has no bind-time screen): the call ran, and the PIC X(5)
      *> receiver took the 3-character image raw, displaying [ABC] with FUNCTION LENGTH still 5.
      *>
      *> Unchecked (either half off), the standard leaves the content to the implementor; the receiver's item
      *> is its whole image, so the result is stored in the receiver's OWN width by the alphanumeric receiving
      *> rule 14.6.8.5 ("aligned at the leftmost character position in the data item with space fill or
      *> truncation to the right"), and a numeric receiver reads the number the sender held.
      *>
      *> EXPECTED OUTPUT, derived line by line (site and callee both checked unless said otherwise):
      *>   A-MISMATCH / EX= / R1=[#####]  X(3) result into X(5): ON EXCEPTION runs, the last exception status
      *>        names EC-PROGRAM-ARG-MISMATCH, and the receiver is untouched because the call never ran.
      *>   B-OK / R2=[ABC]                the CONFORMING pair X(3) into X(3) is delivered unchanged.
      *>   C-MISMATCH / R3=[99999]        9(3) into 9(5): the PICTUREs differ in length.
      *>   D-MISMATCH / R4=[abcdef]       a 4-character group result into a 6-character group.
      *>   E-OK / R5=[ABC  ]              the CALLEE is unchecked: no exception, the result is space-filled to 5.
      *>   F-OK / R6=[ABC  ]              the CALL STATEMENT is unchecked (>>TURN OFF just before it): the same.
      *>   G-OK / R7=[AB]                 unchecked callee, X(3) into X(2): truncated on the right, 2 wide.
      *>   H-OK / R8=[00123]              unchecked callee, 9(3) 123 into 9(5): the value, in the receiver's width.
       >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1040A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R1 PIC X(5) VALUE "#####".
       01 R2 PIC X(3) VALUE "###".
       01 R3 PIC 9(5) VALUE 99999.
       01 R4.
          05 R4A PIC X(2) VALUE "ab".
          05 R4B PIC X(4) VALUE "cdef".
       01 R5 PIC X(5) VALUE "#####".
       01 R6 PIC X(5) VALUE "#####".
       01 R7 PIC X(2) VALUE "##".
       01 R8 PIC 9(5) VALUE 99999.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "PB1040X" RETURNING R1
               ON EXCEPTION DISPLAY "A-MISMATCH"
               NOT ON EXCEPTION DISPLAY "A-OK"
           END-CALL
           DISPLAY "EX=" FUNCTION EXCEPTION-STATUS(1:23)
           DISPLAY "R1=[" R1 "]"
           CALL "PB1040X" RETURNING R2
               ON EXCEPTION DISPLAY "B-MISMATCH"
               NOT ON EXCEPTION DISPLAY "B-OK"
           END-CALL
           DISPLAY "R2=[" R2 "]"
           CALL "PB1040N" RETURNING R3
               ON EXCEPTION DISPLAY "C-MISMATCH"
               NOT ON EXCEPTION DISPLAY "C-OK"
           END-CALL
           DISPLAY "R3=[" R3 "]"
           CALL "PB1040G" RETURNING R4
               ON EXCEPTION DISPLAY "D-MISMATCH"
               NOT ON EXCEPTION DISPLAY "D-OK"
           END-CALL
           DISPLAY "R4=[" R4 "]"
           CALL "PB1040U" RETURNING R5
               ON EXCEPTION DISPLAY "E-MISMATCH"
               NOT ON EXCEPTION DISPLAY "E-OK"
           END-CALL
           DISPLAY "R5=[" R5 "]"
       >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING OFF
           CALL "PB1040X" RETURNING R6
               ON EXCEPTION DISPLAY "F-MISMATCH"
               NOT ON EXCEPTION DISPLAY "F-OK"
           END-CALL
       >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON
           DISPLAY "R6=[" R6 "]"
           CALL "PB1040U" RETURNING R7
               ON EXCEPTION DISPLAY "G-MISMATCH"
               NOT ON EXCEPTION DISPLAY "G-OK"
           END-CALL
           DISPLAY "R7=[" R7 "]"
           CALL "PB1040V" RETURNING R8
               ON EXCEPTION DISPLAY "H-MISMATCH"
               NOT ON EXCEPTION DISPLAY "H-OK"
           END-CALL
           DISPLAY "R8=[" R8 "]"
           STOP RUN.
       END PROGRAM PB1040A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1040X.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(3).
       PROCEDURE DIVISION RETURNING L.
           MOVE "ABC" TO L
           GOBACK.
       END PROGRAM PB1040X.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1040N.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 9(3).
       PROCEDURE DIVISION RETURNING L.
           MOVE 123 TO L
           GOBACK.
       END PROGRAM PB1040N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1040G.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L.
          05 LA PIC X(3).
          05 LB PIC X(1).
       PROCEDURE DIVISION RETURNING L.
           MOVE "WXYZ" TO L
           GOBACK.
       END PROGRAM PB1040G.
       >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING OFF
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1040U.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(3).
       PROCEDURE DIVISION RETURNING L.
           MOVE "ABC" TO L
           GOBACK.
       END PROGRAM PB1040U.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1040V.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 9(3).
       PROCEDURE DIVISION RETURNING L.
           MOVE 123 TO L
           GOBACK.
       END PROGRAM PB1040V.
