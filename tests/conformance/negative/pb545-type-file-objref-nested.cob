      *> reject-at: 2002 2014 2023
      *> ISO §13.18.57.3 8) - a file-section TYPE whose type holds an object reference, one TYPE level down
      *> "When the TYPE clause is specified in the file section, the description of type-name-1, including
      *> its subordinate data items, shall not contain a data item described with a USAGE OBJECT REFERENCE
      *> clause."
      *> cite.py --check 13.18.57.3 "When the TYPE clause is specified in the file section, the description
      *>   of type-name-1, including its subordinate data items, shall not contain a data item described
      *>   with a USAGE OBJECT REFERENCE clause" -> OK §13.18.57.3 8)
      *> THE SUBORDINATE ARM, AT DEPTH: FR's type FT holds N, of type OT, whose subordinate OT-R is USAGE
      *> OBJECT REFERENCE (legal in working-storage under a STRONG type declaration). The direct-USAGE
      *> screen (§13.18.60.3 15), COBOLNET1725) never sees OT-R - it is a working-storage declaration - and
      *> the SAME AS twin (§13.18.49.3 6), COBOLNET1556) is a different clause: before kb/Work PB545 this
      *> compiled clean and ran. With OT-R described as PIC X the program compiles clean. TYPEDEF and TYPE
      *> are COBOL 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB545NEG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB545NEG.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 FR TYPE FT.
       WORKING-STORAGE SECTION.
       01 OT TYPEDEF STRONG.
          05 OT-K PIC X.
          05 OT-R USAGE OBJECT REFERENCE.
       01 FT TYPEDEF STRONG.
          05 FT-K PIC X.
          05 N TYPE OT.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
