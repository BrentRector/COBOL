      *> kb/Work PB1042 — a dynamic-capacity table in an EXTERNAL record, shared by two programs.
      *> ISO/IEC 1989:2023 §8.5.1.9.1 3): a dynamic-capacity table "may be defined in any place,
      *> other than the file section, in which a fixed-capacity table may be defined", and an
      *> EXTERNAL record of the working-storage section is such a place. §13.18.22.4 GR1: every
      *> program describing PB1042XR references the ONE run-unit record, so the occurrences one
      *> program creates (§8.5.1.9.3, a receiving subscript past the current capacity) exist in
      *> the other. A group MOVE recreates the receiving table (§14.6.9.2); INITIALIZE leaves the
      *> capacity unchanged (§14.9.20.4 GR10); SEARCH bounds at the current capacity.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1042XA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PB1042XR EXTERNAL.
          05 XH PIC X(3).
          05 T OCCURS DYNAMIC CAPACITY IN XC FROM 1 TO 5
               INDEXED BY TX.
             10 TA PIC X(2).
             10 TN PIC 9(3).
          05 XZ PIC X(2).
       01 YR.
          05 YH PIC X(3).
          05 Y OCCURS DYNAMIC CAPACITY IN YC FROM 1 TO 5.
             10 YA PIC X(2).
             10 YN PIC 9(3).
          05 YZ PIC X(2).
       01 N PIC 9(4).
       PROCEDURE DIVISION.
           MOVE "HHH" TO XH
           MOVE "ZZ" TO XZ
           MOVE "AA" TO TA(1)
           MOVE 11 TO TN(1)
           MOVE "BB" TO TA(2)
           MOVE 22 TO TN(2)
           DISPLAY "A1 " XC " [" PB1042XR "]"
           CALL "PB1042XB"
           DISPLAY "A2 " XC " [" PB1042XR "]"
           MOVE PB1042XR TO YR
           DISPLAY "A3 " YC " [" YR "]"
           MOVE FUNCTION LENGTH(PB1042XR) TO N
           DISPLAY "A4 " N " " FUNCTION SUM(TN(ALL))
           SET TX TO 1
           SEARCH T
              AT END DISPLAY "A5 NOT FOUND"
              WHEN TA(TX) = "DD" DISPLAY "A5 FOUND " TN(TX)
           END-SEARCH
           SET XC TO 1
           DISPLAY "A6 " XC " [" PB1042XR "]"
           MOVE "QQQ" TO YH
           SET YC TO 3
           MOVE "KK" TO YA(3)
           MOVE 777 TO YN(3)
           MOVE YR TO PB1042XR
           DISPLAY "A7 " XC " [" PB1042XR "] " TA(2) TN(3)
           INITIALIZE PB1042XR
           DISPLAY "A8 " XC " [" PB1042XR "]"
           STOP RUN.
       END PROGRAM PB1042XA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1042XB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PB1042XR EXTERNAL.
          05 XH PIC X(3).
          05 T OCCURS DYNAMIC CAPACITY IN XC FROM 1 TO 5.
             10 TA PIC X(2).
             10 TN PIC 9(3).
          05 XZ PIC X(2).
       PROCEDURE DIVISION.
           DISPLAY "B1 " XC " " TA(2) " " TN(2)
           MOVE "DD" TO TA(4)
           MOVE 44 TO TN(4)
           GOBACK.
       END PROGRAM PB1042XB.
