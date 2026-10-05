      *> kb/Work PB1252 - ISO 13.7.2 general format: LINKAGE SECTION. [ { 77-level-description-entry |
      *> constant-entry | record-description-entry | type-declaration-entry } ... ] - any number, any order.
      *> cite.py: OK 13.7.2 (General format) "type-declaration-entry". The constant and type-declaration entries are
      *> COBOL-2002 (13.10, 13.18.58), so 2002 is the introducing edition of the four-kind format.
      *> Expected: the callee adds the constant 7 to the 77-level formal (12 + 7 = 19, PIC 9(3) -> 019) and moves
      *> WORLD into the record typed by the linkage TYPEDEF; both formals are BY REFERENCE (14.2.3), so the caller
      *> sees A=019 B=WORLD. A second, empty linkage section (the caller's) is the zero-entry case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W1020HPB1252MAIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77 A PIC 9(3) VALUE 12.
       01 B.
          05 B-X PIC X(5) VALUE "HELLO".
       LINKAGE SECTION.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "W1020HPB1252SUB" USING A B.
           DISPLAY "A=" A " B=" B.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W1020HPB1252SUB.
       DATA DIVISION.
       LINKAGE SECTION.
       77 L-A PIC 9(3).
       01 L-K CONSTANT AS 7.
       01 L-T TYPEDEF.
          05 L-T-X PIC X(5).
       01 L-B TYPE L-T.
       PROCEDURE DIVISION USING L-A L-B.
       SUB-PARA.
           ADD L-K TO L-A.
           MOVE "WORLD" TO L-T-X OF L-B.
           GOBACK.
       END PROGRAM W1020HPB1252SUB.
       END PROGRAM W1020HPB1252MAIN.
