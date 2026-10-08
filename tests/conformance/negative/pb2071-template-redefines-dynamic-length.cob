      *> reject-at: 2014 2023
      *> kb/Work PB2071 - an UNUSED TYPEDEF template. ISO 13.18.44.3 SR17 (cite.py OK): "Neither data-name-2 nor the
      *> subject of the entry shall be a variable-length group or a dynamic-length elementary item." A is described with
      *> DYNAMIC LENGTH (2014 on). No TYPE clause references the template, which has no storage (ISO 13.18.58.4 GR2,
      *> cite.py OK), but a syntax rule binds the source as written (ISO 4.3), so the template is non-conforming. Before
      *> kb/Work PB2071 ResolveRedefines walked the storage forest only, so this compiled clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W37BPB2071SR17.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U IS TYPEDEF.
          05 A PIC X DYNAMIC LENGTH.
          05 B REDEFINES A PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "R06".
           STOP RUN.
