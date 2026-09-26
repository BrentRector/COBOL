      *> kb/Work PB1072 — the SUPPRESS WHEN literal-1 is the key suppression value in EVERY form ISO §12.4.5.6.3
      *> SR7 admits: "Literal-1 shall be an alphanumeric literal, a national literal, or a figurative constant".
      *> §12.4.5.6.4 GR6: no alternate access path to a record "when the value of data-name-1 ... in that record
      *> is equal to literal-1"; GR4 makes that equality a relation condition, so a figurative / ALL literal is
      *> repeated to the key's size (§8.3.3.6.4 GR2) and a shorter literal space-extended (§8.8.4.2.7 2)).
      *> A constant-name (§13.10.3 SR2) and a symbolic-character (§8.3.3.6.2 Format 7) stand for literal-1.
      *> §14.9.51.4 GR41: suppressed entries never cause a duplicate key condition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1072SW.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS STAR IS 43.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1072sw.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS R-KEY
               ALTERNATE RECORD KEY IS R-SP SUPPRESS WHEN SPACES
               ALTERNATE RECORD KEY IS R-NA SUPPRESS WHEN SPACE
               ALTERNATE RECORD KEY IS R-SH WITH DUPLICATES
                   SUPPRESS WHEN "X"
               ALTERNATE RECORD KEY IS R-AL WITH DUPLICATES
                   SUPPRESS WHEN ALL "*"
               ALTERNATE RECORD KEY IS R-NL WITH DUPLICATES
                   SUPPRESS WHEN N"Q"
               ALTERNATE RECORD KEY IS R-CN WITH DUPLICATES
                   SUPPRESS WHEN K-NONE
               ALTERNATE RECORD KEY IS R-SY WITH DUPLICATES
                   SUPPRESS WHEN STAR
               ALTERNATE RECORD KEY IS R-CC WITH DUPLICATES
                   SUPPRESS WHEN "A" & "B"
               ALTERNATE RECORD KEY IS R-HV WITH DUPLICATES
                   SUPPRESS WHEN HIGH-VALUES
               ALTERNATE RECORD KEY IS R-LT WITH DUPLICATES
                   SUPPRESS WHEN "AB   "
               ALTERNATE RECORD KEY IS R-LX WITH DUPLICATES
                   SUPPRESS WHEN "ABC"
               FILE STATUS IS WS-FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R.
          05 R-KEY PIC X(2).
          05 R-SP  PIC X(3).
          05 R-NA  PIC X(5).
          05 R-SH  PIC X(3).
          05 R-AL  PIC X(3).
          05 R-NL  PIC N(2).
          05 R-CN  PIC X(4).
          05 R-SY  PIC X(2).
          05 R-CC  PIC X(2).
          05 R-HV  PIC X(2).
          05 R-LT  PIC X(2).
          05 R-LX  PIC X(2).
       WORKING-STORAGE SECTION.
       01 K-NONE CONSTANT AS "NONE".
       01 WS-FS  PIC X(2).
       01 EOF    PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
      *>   Record 01: every suppressible key holds its suppression value; R-NA holds the WORD "SPACE".
           MOVE "01" TO R-KEY MOVE SPACES TO R-SP MOVE "SPACE" TO R-NA
           MOVE "X" TO R-SH MOVE ALL "*" TO R-AL MOVE N"Q" TO R-NL
           MOVE "NONE" TO R-CN MOVE ALL STAR TO R-SY MOVE "AB" TO R-CC
           MOVE HIGH-VALUES TO R-HV MOVE "AB" TO R-LT MOVE "AB" TO R-LX
           WRITE R DISPLAY "W1 " WS-FS.
      *>   Record 02: R-SP spaces again (suppressed: no duplicate), R-NA the word "SPACE" again (NOT the
      *>   suppression value, so a duplicate -> '22').
           MOVE "02" TO R-KEY MOVE SPACES TO R-SP
           WRITE R DISPLAY "W2 " WS-FS.
      *>   Record 03: same as 01 but R-NA spaces (suppressed), R-SH "Z" (not suppressed), and R-LX "AB" again
      *>   (a duplicate on a WITH DUPLICATES key that "ABC" never suppresses -> '02').
           MOVE "03" TO R-KEY MOVE SPACES TO R-NA MOVE "Z" TO R-SH
           WRITE R DISPLAY "W3 " WS-FS.
      *>   Record 04: nothing suppressed.
           MOVE "04" TO R-KEY MOVE "ABC" TO R-SP MOVE "OTHER" TO R-NA
           MOVE "Y" TO R-SH MOVE "**A" TO R-AL MOVE N"QQ" TO R-NL
           MOVE "SOME" TO R-CN MOVE "+-" TO R-SY MOVE "AC" TO R-CC
           MOVE "ZZ" TO R-HV MOVE "AC" TO R-LT MOVE "AC" TO R-LX
           WRITE R DISPLAY "W4 " WS-FS.
           CLOSE F.
           OPEN I-O F.
      *>   REWRITE 03 so R-NA LEAVES suppression (spaces -> the word "OTHR1") and R-SH ENTERS it.
           MOVE "03" TO R-KEY READ F
           MOVE "OTHR1" TO R-NA MOVE "X" TO R-SH
           REWRITE R DISPLAY "RW3 " WS-FS.
      *>   REWRITE 04 so R-NA becomes the WORD "SPACE", which record 01 holds: not the suppression value, so
      *>   §14.9.35.4 GR25 c) '22' and the record is left unchanged.
           MOVE "04" TO R-KEY READ F
           MOVE "SPACE" TO R-NA
           REWRITE R DISPLAY "RW4 " WS-FS.
           MOVE "OTHR1" TO R-NA
           READ F KEY IS R-NA INVALID KEY DISPLAY "RNA INVALID " WS-FS
             NOT INVALID KEY DISPLAY "RNA FOUND " R-KEY END-READ.
      *>   Each alternate key walked from its lowest value: only unsuppressed records are delivered.
           MOVE LOW-VALUES TO R-SP START F KEY >= R-SP
           DISPLAY "SP:" WITH NO ADVANCING PERFORM WALK.
           MOVE LOW-VALUES TO R-NA START F KEY >= R-NA
           DISPLAY "NA:" WITH NO ADVANCING PERFORM WALK.
           MOVE LOW-VALUES TO R-SH START F KEY >= R-SH
           DISPLAY "SH:" WITH NO ADVANCING PERFORM WALK.
           MOVE LOW-VALUES TO R-AL START F KEY >= R-AL
           DISPLAY "AL:" WITH NO ADVANCING PERFORM WALK.
           MOVE LOW-VALUES TO R-NL START F KEY >= R-NL
           DISPLAY "NL:" WITH NO ADVANCING PERFORM WALK.
           MOVE LOW-VALUES TO R-CN START F KEY >= R-CN
           DISPLAY "CN:" WITH NO ADVANCING PERFORM WALK.
           MOVE LOW-VALUES TO R-SY START F KEY >= R-SY
           DISPLAY "SY:" WITH NO ADVANCING PERFORM WALK.
           MOVE LOW-VALUES TO R-CC START F KEY >= R-CC
           DISPLAY "CC:" WITH NO ADVANCING PERFORM WALK.
           MOVE LOW-VALUES TO R-HV START F KEY >= R-HV
           DISPLAY "HV:" WITH NO ADVANCING PERFORM WALK.
           MOVE LOW-VALUES TO R-LT START F KEY >= R-LT
           DISPLAY "LT:" WITH NO ADVANCING PERFORM WALK.
           MOVE LOW-VALUES TO R-LX START F KEY >= R-LX
           DISPLAY "LX:" WITH NO ADVANCING PERFORM WALK.
      *>   A random READ by the suppression value itself finds nothing (GR6 NOTE — "as if they did not exist").
           MOVE SPACES TO R-SP
           READ F KEY IS R-SP INVALID KEY DISPLAY "RSP " WS-FS
             NOT INVALID KEY DISPLAY "RSP FOUND " R-KEY END-READ.
           MOVE N"Q" TO R-NL
           READ F KEY IS R-NL INVALID KEY DISPLAY "RNL " WS-FS
             NOT INVALID KEY DISPLAY "RNL FOUND " R-KEY END-READ.
           CLOSE F.
           STOP RUN.
       WALK.
           MOVE 0 TO EOF.
           PERFORM UNTIL EOF = 1
             READ F NEXT AT END MOVE 1 TO EOF
               NOT AT END DISPLAY " " R-KEY WITH NO ADVANCING
             END-READ
           END-PERFORM.
           DISPLAY " .".
