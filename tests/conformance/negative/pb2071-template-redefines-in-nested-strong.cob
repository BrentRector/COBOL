      *> reject-at: 2002 2014 2023
      *> kb/Work PB2071 - an UNUSED TYPEDEF template. ISO 13.18.44.3 SR14 (cite.py OK): data-name-2 shall not be "an item
      *> subordinate to a strongly-typed group item". Inside U, G is a TYPE subject naming the STRONG T2 (strongly typed
      *> by ISO 8.5.3.1, cite.py OK), and the copy of T2's TB REDEFINES TA that G holds names TA, subordinate to G. T2's
      *> own entry conforms: T2's level-1 entry carries TYPEDEF, not a TYPE clause. No TYPE clause references the
      *> template, which has no storage (ISO 13.18.58.4 GR2, cite.py OK), but a syntax rule binds the source as written
      *> (ISO 4.3), so the template is non-conforming. Before kb/Work PB2071 ResolveRedefines walked the storage forest
      *> only, so this compiled clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W37BPB2071SR14S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T2 IS TYPEDEF STRONG.
          05 TA PIC X(2).
          05 TB REDEFINES TA PIC X(2).
       01 U IS TYPEDEF STRONG.
          05 G TYPE T2.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "R10".
           STOP RUN.
