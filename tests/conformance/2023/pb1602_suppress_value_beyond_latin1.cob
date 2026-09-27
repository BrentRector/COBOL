      *> kb/Work PB1602 - a SUPPRESS WHEN literal holding characters above U+00FF
      *> (ISO 12.4.5.6.3 SR7: an alphanumeric or national literal; the repertoire
      *> is UTF-16) is recorded exactly in the indexed file's fixed attributes, so
      *> the program can reopen the file it wrote. 12.4.5.3 1) k): the alternate
      *> record keys are fixed file attributes, and OPEN (14.9.27.4) compares them.
      *> Before the fix, the header stored "€€" as "??" and every reopen returned
      *> '39'. The third key's NX"D800" is a lone surrogate, a legal national
      *> literal that an encoder would have replaced with U+FFFD.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1602SV.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1602sv.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS R-KEY
               ALTERNATE RECORD KEY IS R-AX WITH DUPLICATES
                   SUPPRESS WHEN "€€"
               ALTERNATE RECORD KEY IS R-NA WITH DUPLICATES
                   SUPPRESS WHEN N"€€"
               ALTERNATE RECORD KEY IS R-SU WITH DUPLICATES
                   SUPPRESS WHEN NX"D800"
               FILE STATUS IS WS-FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC.
          05 R-KEY PIC X(4).
          05 R-AX  PIC X(6).
          05 R-NA  PIC N(2).
          05 R-SU  PIC N(1).
       WORKING-STORAGE SECTION.
       01 WS-FS PIC XX.
       PROCEDURE DIVISION.
           OPEN OUTPUT F
           DISPLAY "OUTPUT " WS-FS
           MOVE "K001" TO R-KEY
           MOVE "ABCDEF" TO R-AX
           MOVE N"XY" TO R-NA
           MOVE N"Z" TO R-SU
           WRITE F-REC
           DISPLAY "WRITE " WS-FS
           CLOSE F
           OPEN INPUT F
           DISPLAY "REOPEN INPUT " WS-FS
           MOVE "K001" TO R-KEY
           READ F KEY IS R-KEY
           DISPLAY "READ " WS-FS " " R-AX
           CLOSE F
           OPEN I-O F
           DISPLAY "REOPEN I-O " WS-FS
           CLOSE F
           STOP RUN.
