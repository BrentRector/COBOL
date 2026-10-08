      *> kb/Work PB165 - a CALL whose activated program the activating
      *> element holds no signature for checks every argument against
      *> the activated program's formal parameter at run time.
      *> PROGRAM P165Q4 (below) names no definition that precedes it
      *> and no prototype, so 12.3.8.4 GR10 c): "the details are taken
      *> from the external repository for the program with the same
      *> name as the externalized name of the program prototype".
      *> 14.9.4.4 GR3 d): "the rules for conformance specified in
      *> 14.8.2, Parameters and 14.8.3, Returning items apply. If a
      *> violation of these rules is detected, the
      *> EC-PROGRAM-ARG-MISMATCH exception condition is set to exist if
      *> checking for it is enabled in both the activated program and
      *> activating runtime element". The element HAS a
      *> program-specifier for each program, so 14.8.2.3.2 rule 2
      *> applies BY REFERENCE: "the definition of the formal parameter
      *> and the definition of the argument shall have the same ALIGN,
      *> BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, and
      *> USAGE clauses" (cases 1-3); and 14.8.2.3.3 rule 2 BY CONTENT:
      *> a numeric formal is "the same as for a COMPUTE statement"
      *> (cases 6, 7, 10-13), any other "the same as for a MOVE
      *> statement" (cases 8, 9; Table 16: numeric noninteger to
      *> alphanumeric is "No"). 14.8.2.2 1): a group formal "shall be
      *> described with the same number or a smaller number of bytes
      *> as the corresponding argument" (cases 4, 5). A CALL by
      *> data-name reaches a program the element has a specifier for
      *> (case 14, rule 2: the COMPUTE converts) or one it has none
      *> for (case 15, rule 1: "the formal parameter shall be of the
      *> same length as the corresponding argument"). RETURNING,
      *> 14.8.3.3: "the receiving operand shall have the same ALIGN,
      *> BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, and
      *> USAGE clauses" (cases 16-18). Each case prints EXC or OK.
       >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P165QM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P165Q4
           PROGRAM P165Q3
           PROGRAM P165QX6
           PROGRAM P165QG10
           PROGRAM P165QG8
           PROGRAM P165QRN
           PROGRAM P165QRX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A4 PIC X(4) VALUE "ABCD".
       01 N4 PIC 9(4) VALUE 1234.
       01 D2 PIC 9V9 VALUE 1.5.
       01 G8.
          05 G8A PIC X(4) VALUE "WXYZ".
          05 G8B PIC X(4) VALUE "WXYZ".
       01 G10.
          05 G10A PIC X(5) VALUE "VWXYZ".
          05 G10B PIC X(5) VALUE "VWXYZ".
       01 TGT PIC X(8).
       01 RX4 PIC X(4).
       01 RA4 PIC A(4).
       01 RN4 PIC 9(4) VALUE 7.
       PROCEDURE DIVISION.
           CALL P165Q4 USING BY REFERENCE A4
               ON EXCEPTION DISPLAY "01 X4->94 REF EXC"
               NOT ON EXCEPTION DISPLAY "01 X4->94 REF OK"
           END-CALL
           CALL P165Q4 USING BY REFERENCE N4
               ON EXCEPTION DISPLAY "02 94->94 REF EXC"
               NOT ON EXCEPTION DISPLAY "02 94->94 REF OK"
           END-CALL
           CALL P165QX6 USING BY REFERENCE A4
               ON EXCEPTION DISPLAY "03 X4->X6 REF EXC"
               NOT ON EXCEPTION DISPLAY "03 X4->X6 REF OK"
           END-CALL
           CALL P165QG10 USING BY REFERENCE G8
               ON EXCEPTION DISPLAY "04 G8->G10 REF EXC"
               NOT ON EXCEPTION DISPLAY "04 G8->G10 REF OK"
           END-CALL
           CALL P165QG8 USING BY REFERENCE G10
               ON EXCEPTION DISPLAY "05 G10->G8 REF EXC"
               NOT ON EXCEPTION DISPLAY "05 G10->G8 REF OK"
           END-CALL
           CALL P165Q4 USING BY CONTENT A4
               ON EXCEPTION DISPLAY "06 X4->94 CONTENT EXC"
               NOT ON EXCEPTION DISPLAY "06 X4->94 CONTENT OK"
           END-CALL
           CALL P165Q3 USING BY CONTENT N4
               ON EXCEPTION DISPLAY "07 94->93 CONTENT EXC"
               NOT ON EXCEPTION DISPLAY "07 94->93 CONTENT OK"
           END-CALL
           CALL P165QX6 USING BY CONTENT A4
               ON EXCEPTION DISPLAY "08 X4->X6 CONTENT EXC"
               NOT ON EXCEPTION DISPLAY "08 X4->X6 CONTENT OK"
           END-CALL
           CALL P165QX6 USING BY CONTENT D2
               ON EXCEPTION DISPLAY "09 9V9->X6 CONTENT EXC"
               NOT ON EXCEPTION DISPLAY "09 9V9->X6 CONTENT OK"
           END-CALL
           CALL P165Q3 USING BY CONTENT "ABC"
               ON EXCEPTION DISPLAY "10 'ABC'->93 CONTENT EXC"
               NOT ON EXCEPTION DISPLAY "10 'ABC'->93 CONTENT OK"
           END-CALL
           CALL P165Q3 USING BY CONTENT 12
               ON EXCEPTION DISPLAY "11 12->93 CONTENT EXC"
               NOT ON EXCEPTION DISPLAY "11 12->93 CONTENT OK"
           END-CALL
           CALL P165Q3 USING BY CONTENT ZERO
               ON EXCEPTION DISPLAY "12 ZERO->93 CONTENT EXC"
               NOT ON EXCEPTION DISPLAY "12 ZERO->93 CONTENT OK"
           END-CALL
           CALL P165Q3 USING BY CONTENT SPACE
               ON EXCEPTION DISPLAY "13 SPACE->93 CONTENT EXC"
               NOT ON EXCEPTION DISPLAY "13 SPACE->93 CONTENT OK"
           END-CALL
           MOVE "P165Q3" TO TGT
           CALL TGT USING BY CONTENT N4
               ON EXCEPTION DISPLAY "14 TGT=P165Q3 94->93 EXC"
               NOT ON EXCEPTION DISPLAY "14 TGT=P165Q3 94->93 OK"
           END-CALL
           MOVE "P165QU3" TO TGT
           CALL TGT USING BY CONTENT N4
               ON EXCEPTION DISPLAY "15 TGT=P165QU3 94->93 EXC"
               NOT ON EXCEPTION DISPLAY "15 TGT=P165QU3 94->93 OK"
           END-CALL
           CALL P165QRN RETURNING RX4
               ON EXCEPTION DISPLAY "16 RET 94->X4 EXC"
               NOT ON EXCEPTION DISPLAY "16 RET 94->X4 OK"
           END-CALL
           CALL P165QRX RETURNING RA4
               ON EXCEPTION DISPLAY "17 RET X4->A4 EXC"
               NOT ON EXCEPTION DISPLAY "17 RET X4->A4 OK"
           END-CALL
           CALL P165QRN RETURNING RN4
               ON EXCEPTION DISPLAY "18 RET 94->94 EXC"
               NOT ON EXCEPTION DISPLAY "18 RET 94->94 OK " RN4
           END-CALL
           STOP RUN.
       END PROGRAM P165QM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P165Q4.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 9(4).
       PROCEDURE DIVISION USING L.
           DISPLAY "   Q4 GOT " L
           GOBACK.
       END PROGRAM P165Q4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P165Q3.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 9(3).
       PROCEDURE DIVISION USING L.
           GOBACK.
       END PROGRAM P165Q3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P165QU3.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 9(3).
       PROCEDURE DIVISION USING L.
           GOBACK.
       END PROGRAM P165QU3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P165QX6.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(6).
       PROCEDURE DIVISION USING L.
           DISPLAY "   QX6 GOT [" L "]"
           GOBACK.
       END PROGRAM P165QX6.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P165QG10.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L.
          05 LA PIC X(5).
          05 LB PIC X(5).
       PROCEDURE DIVISION USING L.
           GOBACK.
       END PROGRAM P165QG10.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P165QG8.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L.
          05 LA PIC X(4).
          05 LB PIC X(4).
       PROCEDURE DIVISION USING L.
           DISPLAY "   QG8 GOT [" L "]"
           GOBACK.
       END PROGRAM P165QG8.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P165QRN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR PIC 9(4).
       PROCEDURE DIVISION RETURNING LR.
           MOVE 42 TO LR
           GOBACK.
       END PROGRAM P165QRN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P165QRX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR PIC X(4).
       PROCEDURE DIVISION RETURNING LR.
           MOVE "WXYZ" TO LR
           GOBACK.
       END PROGRAM P165QRX.
