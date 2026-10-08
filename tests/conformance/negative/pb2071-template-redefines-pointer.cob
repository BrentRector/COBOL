      *> reject-at: 2002 2014 2023
      *> kb/Work PB2071 - an UNUSED TYPEDEF template. ISO 13.18.44.3 SR14 (cite.py OK): "Data-name-2 shall not be of
      *> class object, message-tag, or pointer, a strongly-typed group item, or an item subordinate to a strongly-typed
      *> group item." P is of class pointer. No TYPE clause references the template, which has no storage (ISO 13.18.58.4
      *> GR2, cite.py OK), but a syntax rule binds the source as written (ISO 4.3), so the template is non-conforming.
      *> Before kb/Work PB2071 ResolveRedefines walked the storage forest only, so this compiled clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W37BPB2071SR14P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U IS TYPEDEF STRONG.
          05 P USAGE POINTER.
          05 Q REDEFINES P PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "P9".
           STOP RUN.
