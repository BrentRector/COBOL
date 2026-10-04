      *> kb/Work PB165 - the DYNAMIC Format-1 CALL checks an argument
      *> against the activated program's formal parameter.
      *> RULE (14.9.4.4 GR3 d): "the rules for conformance specified in
      *> 14.8.2, Parameters ... apply. If a violation of these rules is
      *> detected, the EC-PROGRAM-ARG-MISMATCH exception condition is
      *> set to exist if checking for it is enabled in both the
      *> activated program and activating runtime element".
      *> For a program with no program-specifier and no NESTED phrase,
      *> 14.8.2.3.2 and 14.8.2.3.3 rule 1 say "the formal parameter
      *> shall be of the same length as the corresponding argument" -
      *> BY REFERENCE and BY CONTENT alike. For an alphanumeric group
      *> BY REFERENCE, 14.8.2.2 1) says the other item shall be an
      *> alphanumeric group or an elementary alphanumeric item and "the
      *> formal parameter shall be described with the same number or a
      *> smaller number of bytes as the corresponding argument"; BY
      *> CONTENT, 14.8.2.2 2) gives the rules of a MOVE, which relate
      *> no lengths. Cases 1-9 below; each prints EXC or OK.
      *> A user-defined function and a CALL ... AS NESTED take rule 2
      *> (a conversion by MOVE or COMPUTE, not a length equality), so
      *> a longer or shorter formal is LEGAL for a BY CONTENT argument
      *> and raises nothing (cases 10 and 11).
       >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB165UF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 FARG PIC X(6).
       01 FRES PIC X(6).
       PROCEDURE DIVISION USING FARG RETURNING FRES.
           MOVE FARG TO FRES
           GOBACK.
       END FUNCTION PB165UF.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB165DYN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB165UF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A4 PIC X(4) VALUE "ABCD".
       01 A8 PIC X(8) VALUE "ABCDEFGH".
       01 N4 PIC 9(4) VALUE 1234.
       01 N8 PIC 9(8) VALUE 12345678.
       01 G8.
          05 G8A PIC X(4) VALUE "WXYZ".
          05 G8B PIC X(4) VALUE "WXYZ".
       01 G10.
          05 G10A PIC X(5) VALUE "VWXYZ".
          05 G10B PIC X(5) VALUE "VWXYZ".
       01 UFR PIC X(6).
       PROCEDURE DIVISION.
           CALL "PB165X6" USING BY REFERENCE A4
               ON EXCEPTION DISPLAY "1 X4->X6 REF EXC"
               NOT ON EXCEPTION DISPLAY "1 X4->X6 REF OK"
           END-CALL
           CALL "PB165N3" USING BY REFERENCE N4
               ON EXCEPTION DISPLAY "2 N4->N3 REF EXC"
               NOT ON EXCEPTION DISPLAY "2 N4->N3 REF OK"
           END-CALL
           CALL "PB165X6" USING BY CONTENT A4
               ON EXCEPTION DISPLAY "3 X4->X6 CONTENT EXC"
               NOT ON EXCEPTION DISPLAY "3 X4->X6 CONTENT OK"
           END-CALL
           CALL "PB165X4" USING BY REFERENCE A4
               ON EXCEPTION DISPLAY "4 X4->X4 REF EXC"
               NOT ON EXCEPTION DISPLAY "4 X4->X4 REF OK"
           END-CALL
           CALL "PB165G10" USING BY REFERENCE G8
               ON EXCEPTION DISPLAY "5 G8->G10 REF EXC"
               NOT ON EXCEPTION DISPLAY "5 G8->G10 REF OK"
           END-CALL
           CALL "PB165G8" USING BY REFERENCE G10
               ON EXCEPTION DISPLAY "6 G10->G8 REF EXC"
               NOT ON EXCEPTION DISPLAY "6 G10->G8 REF OK"
           END-CALL
           CALL "PB165X4" USING BY REFERENCE N4
               ON EXCEPTION DISPLAY "7 N4->X4 REF EXC"
               NOT ON EXCEPTION DISPLAY "7 N4->X4 REF OK"
           END-CALL
           CALL "PB165N3" USING BY CONTENT N4
               ON EXCEPTION DISPLAY "8 N4->N3 CONTENT EXC"
               NOT ON EXCEPTION DISPLAY "8 N4->N3 CONTENT OK"
           END-CALL
           CALL "PB165G10" USING BY CONTENT G8
               ON EXCEPTION DISPLAY "9 G8->G10 CONTENT EXC"
               NOT ON EXCEPTION DISPLAY "9 G8->G10 CONTENT OK"
           END-CALL
           MOVE FUNCTION PB165UF("ABCD") TO UFR
           DISPLAY "10 UDF X4->X6 [" UFR "]"
           CALL "PB165OUT" USING BY CONTENT N4
           CALL "PB165G8" USING BY REFERENCE N8
               ON EXCEPTION DISPLAY "12 N8->G8 REF EXC"
               NOT ON EXCEPTION DISPLAY "12 N8->G8 REF OK"
           END-CALL
           CALL "PB165G8" USING BY REFERENCE A8
               ON EXCEPTION DISPLAY "13 X8->G8 REF EXC"
               NOT ON EXCEPTION DISPLAY "13 X8->G8 REF OK"
           END-CALL
           STOP RUN.
       END PROGRAM PB165DYN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB165OUT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R3 PIC 9(3).
       LINKAGE SECTION.
       01 OARG PIC 9(4).
       PROCEDURE DIVISION USING OARG.
           CALL "N3" AS NESTED USING BY CONTENT OARG
           DISPLAY "11 NESTED N4->N3 OK"
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 9(3).
       PROCEDURE DIVISION USING L.
           GOBACK.
       END PROGRAM N3.
       END PROGRAM PB165OUT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB165X6.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(6).
       PROCEDURE DIVISION USING L.
           GOBACK.
       END PROGRAM PB165X6.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB165X4.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(4).
       PROCEDURE DIVISION USING L.
           GOBACK.
       END PROGRAM PB165X4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB165N3.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 9(3).
       PROCEDURE DIVISION USING L.
           GOBACK.
       END PROGRAM PB165N3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB165G10.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L.
          05 LA PIC X(5).
          05 LB PIC X(5).
       PROCEDURE DIVISION USING L.
           GOBACK.
       END PROGRAM PB165G10.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB165G8.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L.
          05 LA PIC X(4).
          05 LB PIC X(4).
       PROCEDURE DIVISION USING L.
           GOBACK.
       END PROGRAM PB165G8.
