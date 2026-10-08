      *> reject-at: 2002 2014 2023
      *> kb/Work PB2071 - an UNUSED TYPEDEF template. ISO 13.18.44.3 SR2 and SR4 (cite.py OK): "The level-numbers of
      *> data-name-2 and the subject of the entry shall be identical", and "No entry having a level-number numerically
      *> lower than the level-number of data-name-2 may occur between" them. The level-10 B names the level-5 A across
      *> the group G. No TYPE clause references the template, which has no storage (ISO 13.18.58.4 GR2, cite.py OK), but
      *> a syntax rule binds the source as written (ISO 4.3), so the template is non-conforming. Before kb/Work PB2071
      *> ResolveRedefines walked the storage forest only, so this compiled clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W37BPB2071SR2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U IS TYPEDEF.
          05 A PIC X(2).
          05 G.
             10 B REDEFINES A PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "R09".
           STOP RUN.
