*>--- conformance-case
*> id: pb971_program_arg_omitted
*> rules: [GR-14.9.4.4-12]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB971 - ISO 14.9.4.4 GR12: "If a parameter for which the
      *> omitted-argument condition is true is referenced in a called program,
      *> except as an argument or in the omitted-argument condition, the
      *> EC-PROGRAM-ARG-OMITTED exception condition is set to exist." Table 13:
      *> Fatal. With checking ON, EVERY reference raises it - an elementary
      *> formal (X), a subordinate of a group formal (G1, G2), a redefinition of
      *> a formal (R9), and a GLOBAL formal referenced from a contained program
      *> (P971N) - and the called program's own declarative (14.6.13.1.3 item 5)
      *> reports it; RESUME AT NEXT STATEMENT abandons the interrupted statement
      *> (so its DISPLAY/MOVE has no effect). The two exemptions raise nothing:
      *> the IS OMITTED condition, and X/G passed as ARGUMENTS to P971F, where
      *> the omission is transitive (8.8.4.8.4 GR1c). The second CALL supplies
      *> every argument: no reference raises, so N stays 00.
      >>TURN EC-PROGRAM-ARG-OMITTED CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P971M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WA PIC X(4) VALUE "ABCD".
       01 WG.
          05 WG1 PIC X(2) VALUE "GH".
          05 WG2 PIC 9(2) VALUE 42.
       01 WR PIC X(4) VALUE "0123".
       PROCEDURE DIVISION.
           CALL "P971C" USING OMITTED OMITTED OMITTED
           DISPLAY "M-SECOND"
           CALL "P971C" USING WA WG WR
           DISPLAY "M-END WG1=" WG1
           STOP RUN.
       END PROGRAM P971M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P971C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4).
       01 N PIC 9(2).
       LINKAGE SECTION.
       01 X PIC X(4).
       01 G GLOBAL.
          05 G1 PIC X(2).
          05 G2 PIC 9(2).
       01 R PIC X(4).
       01 R9 REDEFINES R PIC 9(4).
       PROCEDURE DIVISION USING OPTIONAL X OPTIONAL G OPTIONAL R.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-PROGRAM-ARG-OMITTED.
       H-P.
           ADD 1 TO N
           DISPLAY "  C-CAUGHT " N "=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M-P.
           MOVE 0 TO N
           IF X IS OMITTED DISPLAY "X-OMITTED" END-IF
           CALL "P971F" USING X G
           DISPLAY "X=[" X "]"
           MOVE X TO W
           MOVE "ZZ" TO G1
           DISPLAY "G2=" G2
           DISPLAY "R9=" R9
           CALL "P971N"
           DISPLAY "N=" N
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P971N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       HN SECTION.
           USE AFTER EXCEPTION CONDITION EC-PROGRAM-ARG-OMITTED.
       HN-P.
           ADD 1 TO K
           DISPLAY "  N-CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       N-P.
           DISPLAY "NESTED G1=[" G1 "]"
           DISPLAY "NESTED K=" K
           GOBACK.
       END PROGRAM P971N.
       END PROGRAM P971C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P971F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 FX PIC X(4).
       01 FG PIC X(4).
       PROCEDURE DIVISION USING OPTIONAL FX OPTIONAL FG.
           IF FX IS OMITTED
               DISPLAY "F-X-OMITTED"
           ELSE
               DISPLAY "F-X=" FX
           END-IF
           IF FG IS OMITTED DISPLAY "F-G-OMITTED" END-IF
           GOBACK.
       END PROGRAM P971F.
