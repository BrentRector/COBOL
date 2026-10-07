      *> kb/Work PB1128 (train 1018 lander review) - ISO 14.9.22.4 GR4 d): "If identifier-1 is a signed
      *>   numeric item, the original value of the sign is retained upon completion of the INSPECT
      *>   statement."  (cite.py --check 14.9.22.4 -> OK 4) d))
      *> identifier-1 an ELEMENT OF AN OCCURS DYNAMIC TABLE (13.18.38): the INSPECT store used to crash the
      *> compiler ("numeric item stored natively") once PB1128 made INSPECT a character channel; the
      *> element is now promoted to its character image like any channel receiver (kb/Work PB2004).
      *> X1 - signed member -105, REPLACING ALL "5" BY "7" => digits 107, sign retained => -107 (edited -999).
      *> Y1 - unsigned member 105, the same replacement => 107.
      *> D1 - elementary dynamic element 125, CONVERTING "25" TO "38" => 138.
      *>
      *>   X=-107
      *>   Y=107
      *>   D=138
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1128DYN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 DT OCCURS DYNAMIC.
             10 X PIC S9(3).
             10 Y PIC 9(3).
       01 G2.
          05 DN PIC 9(3) OCCURS DYNAMIC.
       01 I PIC 9 VALUE 1.
       01 XE PIC -999.
       PROCEDURE DIVISION.
           MOVE -105 TO X(1)
           MOVE 105 TO Y(1)
           MOVE 125 TO DN(1)
           INSPECT X(I) REPLACING ALL "5" BY "7"
           INSPECT Y(I) REPLACING ALL "5" BY "7"
           INSPECT DN(I) CONVERTING "25" TO "38"
           MOVE X(1) TO XE
           DISPLAY "X=" XE
           DISPLAY "Y=" Y(1)
           DISPLAY "D=" DN(1)
           STOP RUN.
       END PROGRAM PB1128DYN.
