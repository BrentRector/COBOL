      *> reject-at: 2002 2014 2023
      *> kb/Work PB2071 - an UNUSED TYPEDEF template. ISO 13.18.44.3 SR5 (cite.py OK): "The data description entry for
      *> data-name-2 shall not contain an OCCURS clause." A carries OCCURS 4. No TYPE clause references the template,
      *> which has no storage (ISO 13.18.58.4 GR2, cite.py OK), but a syntax rule binds the source as written (ISO 4.3),
      *> so the template is non-conforming. Before kb/Work PB2071 ResolveRedefines walked the storage forest only, so
      *> this compiled clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W37BPB2071SR5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U IS TYPEDEF.
          05 A PIC X OCCURS 4.
          05 B REDEFINES A PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "P11".
           STOP RUN.
