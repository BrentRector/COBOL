*>--- conformance-case
*> id: pb836_implicit_record_redefinition
*> rules: [GR-13.18.33.4-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB836 - an FD's multiple level-1 entries are IMPLICIT redefinitions of one area, not REDEFINES
      *> clauses.
      *>
      *> THE RULE. ISO 13.18.33.4 GR3: "Multiple level 1 entries subordinate to a FD or SD entry represent
      *> implicit redefinitions of the same area." The REDEFINES-clause syntax rules (13.18.44.3) are about a
      *> written clause and its data-name-2; they do not reach this source, which writes none. A WEAK type
      *> (TYPEDEF without STRONG) may be implicitly redefined - 13.18.57.3 SR4 bars only a STRONG one, and the
      *> negative twin tests/conformance/negative/pb836-strong-record-implicitly-redefined pins that side.
      *>
      *> EXPECTED VALUES, DERIVED. The three records share ONE area, left-aligned (13.18.33.4 GR3):
      *>   MOVE "AB12CD" TO R-TEXT sets the six leading positions;
      *>   R-TYPED (TYPE WT: 05 WA PIC X(2), 05 WN PIC 9(2)) then reads WA="AB", WN=12;
      *>   R-NUM (05 NA PIC X(2), 05 NB PIC 9(2), 05 NC PIC X(2)) reads NB+1 = 13 and NC="CD".
      *> COBOL-2002: TYPEDEF / TYPE are 2002 introductions (their own COBOLNET0900 gate covers 85).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB836IMPL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb836impl.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R-TEXT PIC X(6).
       01 R-TYPED TYPE WT.
       01 R-NUM.
          05 NA PIC X(2).
          05 NB PIC 9(2).
          05 NC PIC X(2).
       WORKING-STORAGE SECTION.
       01 WT IS TYPEDEF.
          05 WA PIC X(2).
          05 WN PIC 9(2).
       01 SUM-N PIC 9(2).
       PROCEDURE DIVISION.
           MOVE "AB12CD" TO R-TEXT
           DISPLAY "WA=" WA OF R-TYPED " WN=" WN OF R-TYPED
           COMPUTE SUM-N = NB + 1
           DISPLAY "NB+1=" SUM-N " NC=" NC
           STOP RUN.
