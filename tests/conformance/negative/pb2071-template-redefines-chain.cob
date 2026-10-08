      *> reject-at: 2002 2014 2023
      *> kb/Work PB2071 - an UNUSED TYPEDEF template. ISO 13.18.44.3 SR7 (cite.py OK): "Multiple redefinitions of the
      *> same storage area shall each specify as data-name-2 the data-name of the entry that originally defined the
      *> area." C names B, itself a redefinition. No TYPE clause references the template, which has no storage (ISO
      *> 13.18.58.4 GR2, cite.py OK), but a syntax rule binds the source as written (ISO 4.3), so the template is
      *> non-conforming. Before kb/Work PB2071 ResolveRedefines walked the storage forest only, so this compiled clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W37BPB2071SR7.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U IS TYPEDEF.
          05 A PIC X(2).
          05 B REDEFINES A PIC X(2).
          05 C REDEFINES B PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "R02".
           STOP RUN.
