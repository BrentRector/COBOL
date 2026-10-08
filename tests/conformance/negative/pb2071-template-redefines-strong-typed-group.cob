      *> reject-at: 2002 2014 2023
      *> kb/Work PB2071 - an UNUSED TYPEDEF template. ISO 13.18.57.3 SR4 (cite.py OK): with a STRONG type-name-1, "the
      *> subject of the entry shall not be implicitly or explicitly redefined in whole or in part". G is the subject of a
      *> TYPE entry naming the STRONG T2, and H redefines it (also ISO 13.18.44.3 SR14, COBOLNET1697: G is a
      *> strongly-typed group item). No TYPE clause references the template, which has no storage (ISO 13.18.58.4 GR2,
      *> cite.py OK), but a syntax rule binds the source as written (ISO 4.3), so the template is non-conforming. Before
      *> kb/Work PB2071 ResolveRedefines walked the storage forest only, so this compiled clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W37BPB2071SR4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T2 IS TYPEDEF STRONG.
          05 TA PIC X(2).
       01 U IS TYPEDEF STRONG.
          05 G TYPE T2.
          05 H REDEFINES G PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "P3".
           STOP RUN.
