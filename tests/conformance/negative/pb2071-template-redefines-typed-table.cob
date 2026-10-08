      *> reject-at: 2002 2014 2023
      *> kb/Work PB2071 - an UNUSED TYPEDEF template. ISO 13.18.44.3 SR5 (cite.py OK): "The data description entry for
      *> data-name-2 shall not contain an OCCURS clause." G's own entry carries OCCURS 2 beside its TYPE clause. No TYPE
      *> clause references the template, which has no storage (ISO 13.18.58.4 GR2, cite.py OK), but a syntax rule binds
      *> the source as written (ISO 4.3), so the template is non-conforming. Before kb/Work PB2071 ResolveRedefines
      *> walked the storage forest only, so this compiled clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W37BPB2071SR5T.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T2 IS TYPEDEF.
          05 TA PIC X(2).
       01 U IS TYPEDEF.
          05 G TYPE T2 OCCURS 2.
          05 H REDEFINES G PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "R08".
           STOP RUN.
