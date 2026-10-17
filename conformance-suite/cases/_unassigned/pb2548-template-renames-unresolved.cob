*>--- conformance-case
*> id: pb2548-template-renames-unresolved
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2548 - an UNUSED TYPEDEF template. ISO 13.18.45.3 SR4 (cite.py OK): "Data-name-2 and data-name-3 shall
      *> be names of elementary items or groups of elementary items in the same record". NOSUCH names no item of the
      *> template, and RENAMES clauses "are part of the type declaration" (ISO 13.18.58.4 GR1, cite.py OK), so the
      *> record is the declaration. No TYPE clause references it and a type declaration has no storage (ISO 13.18.58.4
      *> GR2), but a syntax rule binds the source as written (ISO 4.3). Before kb/Work PB2548 the RENAMES resolver walked
      *> the storage forest only, so this compiled clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2548RENAMES.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U IS TYPEDEF.
          05 A PIC X(2).
          05 B PIC X(2).
          66 R RENAMES A THRU NOSUCH.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "R01".
           STOP RUN.
