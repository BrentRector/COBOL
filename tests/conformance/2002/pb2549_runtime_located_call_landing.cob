      *> kb/Work PB2549 - a CALL whose activating element holds no
      *> signature of the activated program lands each BY CONTENT and
      *> BY VALUE argument into the formal's description at run time.
      *> 14.2.3 GR9, BY CONTENT: for "a program for which there is no
      *> program-specifier in the REPOSITORY paragraph of the
      *> activating runtime element" the argument "is moved to this
      *> allocated record without conversion" (case 6: the image 1234
      *> of 12.34); for "a program for which there is a
      *> program-specifier" the record is "a data item with the same
      *> description and the same number of bytes as the formal
      *> parameter" and "if the formal parameter is numeric, a COMPUTE
      *> statement without the ROUNDED phrase" fills it (cases 1-5, 7).
      *> 14.2.3 GR10, BY VALUE: "a data item of the same description
      *> as the formal parameter", filled by the same COMPUTE (case 8).
      *> Every PROGRAM specifier names a definition that FOLLOWS it,
      *> so 12.3.8.4 GR10 c) takes its details "from the external
      *> repository"; a CALL by data-name has no compile-time
      *> signature either. 14.7.5: the COMPUTE has no SIZE ERROR
      *> phrase, so with EC-SIZE-TRUNCATION checking off the low-order
      *> digits are stored (CONFORMANCE DOC-A.1-70: 1234 into 9(3) is
      *> 234), and with it on the activating element's condition is
      *> raised before the program is entered (K1, E1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2549M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P2549N3
           PROGRAM P2549N4
           PROGRAM P2549S5
           PROGRAM P2549V4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N4 PIC 9(4) VALUE 1234.
       01 SN PIC S9(3)V99 VALUE -12.34.
       01 D4 PIC 9(2)V99 VALUE 12.34.
       01 TGT PIC X(8).
       PROCEDURE DIVISION.
           DISPLAY "01"
           CALL P2549N3 USING BY CONTENT N4
           DISPLAY "02"
           CALL P2549N3 USING BY CONTENT 12
           DISPLAY "03"
           CALL P2549N3 USING BY CONTENT ZERO
           DISPLAY "04"
           CALL P2549S5 USING BY CONTENT SN
           DISPLAY "05"
           MOVE "P2549N3" TO TGT
           CALL TGT USING BY CONTENT N4
           DISPLAY "06"
           MOVE "P2549U4" TO TGT
           CALL TGT USING BY CONTENT D4
           DISPLAY "07"
           CALL P2549N4 USING BY CONTENT D4
           DISPLAY "08"
           CALL P2549V4 USING BY VALUE D4
           CALL "P2549K"
           CALL "P2549E"
           STOP RUN.
       END PROGRAM P2549M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2549N3.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 9(3).
       PROCEDURE DIVISION USING L.
           DISPLAY "   N3 GOT " L
           GOBACK.
       END PROGRAM P2549N3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2549N4.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 9(4).
       PROCEDURE DIVISION USING L.
           DISPLAY "   N4 GOT " L
           GOBACK.
       END PROGRAM P2549N4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2549U4.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 9(4).
       PROCEDURE DIVISION USING L.
           DISPLAY "   U4 GOT " L
           GOBACK.
       END PROGRAM P2549U4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2549S5.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC S9(5)V9 SIGN LEADING SEPARATE.
       PROCEDURE DIVISION USING L.
           DISPLAY "   S5 GOT " L
           GOBACK.
       END PROGRAM P2549S5.
       >>TURN EC-SIZE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2549K.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P2549N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BIG PIC 9(4) VALUE 1234.
       01 FITS PIC 9(4) VALUE 0123.
       01 TGT PIC X(8) VALUE "P2549N3".
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE.
       H-P.
           DISPLAY "   EC=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           DISPLAY "K1"
           CALL TGT USING BY CONTENT BIG
           DISPLAY "K2"
           CALL TGT USING BY CONTENT FITS
           GOBACK.
       END PROGRAM P2549K.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2549E.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P2549V4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BIG PIC 9(5) VALUE 12345.
       01 FITS PIC 9(2)V99 VALUE 12.34.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE.
       H-P.
           DISPLAY "   EC=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           DISPLAY "E1"
           CALL P2549V4 USING BY VALUE BIG
           DISPLAY "E2"
           CALL P2549V4 USING BY VALUE FITS
           GOBACK.
       END PROGRAM P2549E.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2549V4.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 9(4).
       PROCEDURE DIVISION USING BY VALUE L.
           DISPLAY "   V4 GOT " L
           GOBACK.
       END PROGRAM P2549V4.
