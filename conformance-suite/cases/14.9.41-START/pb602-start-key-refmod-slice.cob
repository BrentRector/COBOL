*>--- conformance-case
*> id: pb602-start-key-refmod-slice
*> rules: [SR-14.9.41.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB602 - ISO 14.9.41.3 SR5: "For relative files, data-name-1, if specified, shall be the data
      *> item specified in the RELATIVE KEY clause in the associated file control entry." "Shall BE the data
      *> item" is an IDENTITY over data items, and ISO 8.4.3.3.4 GR5 makes a reference-modified operand a
      *> different one: "Reference modification creates a unique data item that is a subset of the data item
      *> referenced by identifier-1."
      *> It compiled clean while the screen asked the place for the item whose ATTRIBUTES and STORAGE it reads -
      *> which for a slice is still WS-RK - and positioned the file on the relative record number the key's first
      *> two characters spell, where ISO 12.4.5.13.4 GR1 makes 1 the first legal relative record number. A silent
      *> wrong answer at every edition.
      *> The positive control is conformance-suite/cases/14.9.41-START/pb602_start_key_identity.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB602NEGSLICE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RLF ASSIGN TO "pb602ns.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS WS-RK.
       DATA DIVISION.
       FILE SECTION.
       FD RLF.
       01 RL-REC PIC X(2).
       WORKING-STORAGE SECTION.
       01 WS-RK PIC 9(4).
       PROCEDURE DIVISION.
           OPEN INPUT RLF
           START RLF KEY IS = WS-RK(1:2)
               INVALID KEY CONTINUE
           END-START
           CLOSE RLF
           STOP RUN.
