*>--- conformance-case
*> id: pb836-strong-record-implicitly-redefined
*> rules: [SR-13.18.57.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB836 - ISO 13.18.57.3 SR4: "If type-name-1 is described with the STRONG phrase, the subject of
      *> the entry shall not be implicitly or explicitly redefined in whole or in part." 13.18.33.4 GR3 makes an
      *> FD's multiple level-1 entries implicit redefinitions of one area, so a STRONG-typed record beside any
      *> other record is refused - by SR4 (COBOLNET1532, citing 13.18.33.4 GR3), and in EITHER order. The
      *> strong record is SECOND here: before the fix this order drew only 13.18.44.3 SR12, a rule about a
      *> REDEFINES clause the source does not contain, and never SR4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB836STRG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb836strg.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-OTHER PIC X(4).
       01 F-BIG TYPE T1.
       WORKING-STORAGE SECTION.
       01 T1 IS TYPEDEF STRONG.
          05 A PIC X(4).
       PROCEDURE DIVISION.
           STOP RUN.
