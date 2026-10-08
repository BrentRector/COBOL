      *> reject-at: 2002 2014 2023
      *> kb/Work PB2071 - an UNUSED TYPEDEF template. ISO 13.18.44.3 SR8 (cite.py OK): "The storage area required for the
      *> subject of the entry shall not be larger than the storage area required for the data item referenced by
      *> data-name-2", unless data-name-2 is level 1 without EXTERNAL. B (4 characters) redefines the 2-character level-5
      *> A. No TYPE clause references the template, which has no storage (ISO 13.18.58.4 GR2, cite.py OK), but a syntax
      *> rule binds the source as written (ISO 4.3), so the template is non-conforming. Before kb/Work PB2071
      *> ResolveRedefines walked the storage forest only, so this compiled clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W37BPB2071SR8.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U IS TYPEDEF.
          05 A PIC X(2).
          05 B REDEFINES A PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "P12".
           STOP RUN.
