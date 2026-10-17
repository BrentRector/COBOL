      *>--- conformance-case
      *> id: program_pointer
      *> rules: [SR-13.18.60.3-8, GR-13.18.60.4-24, SR-14.9.4.3-1, SR-14.9.39.3-21, GR-14.9.39.4-16, GR-14.9.39.4-17, DOC-A.1-175, DOC-A.1-217]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.60 GR24 — USAGE PROGRAM-POINTER (2002): a data item
      *> that may contain the address of a program (for a COBOL program,
      *> an OUTERMOST program). Covers: the initial NULL state
      *> (§13.18.63); SET Format 9 (§14.9.39 SR21) with the §8.4.3.13
      *> program-address-identifier sender — ENTRY literal and ENTRY
      *> identifier (GR1a) — a program-pointer sender, and NULL;
      *> §8.8.4.2.16 pointer relations (= / NOT = / NULL); CALL through
      *> the pointer (§14.9.4 SR1 — identifier-1 references a
      *> program-pointer; GR — the item contains the location of the
      *> program being called); and the §8.4.3.13 GR4 not-found leg
      *> (EC-PROGRAM-NOT-FOUND set to exist, the value becomes NULL —
      *> checking not enabled here, so execution continues per
      *> §14.6.13.1.4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PPMAINP10PT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PP-1 USAGE PROGRAM-POINTER.
       01 PP-2 USAGE PROGRAM-POINTER.
       01 W-NAME PIC X(12) VALUE "PPSUBP10PT".
       PROCEDURE DIVISION.
       MAIN.
           IF PP-1 = NULL DISPLAY "INIT-NULL" END-IF.
           SET PP-1 TO ENTRY "PPSUBP10PT".
           IF PP-1 NOT = NULL DISPLAY "SET-LIT" END-IF.
           CALL PP-1.
           SET PP-2 TO ENTRY W-NAME.
           IF PP-1 = PP-2 DISPLAY "SAME" END-IF.
           SET PP-2 TO PP-1.
           IF PP-2 NOT = NULL DISPLAY "COPIED" END-IF.
           CALL PP-2.
           SET PP-1 TO NULL.
           IF PP-1 = NULL DISPLAY "NULLED" END-IF.
           SET PP-1 TO ENTRY "NOSUCHPROG".
           IF PP-1 = NULL DISPLAY "NOTFOUND-NULL" END-IF.
           STOP RUN.
       END PROGRAM PPMAINP10PT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PPSUBP10PT.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "SUB-CALLED".
           GOBACK.
       END PROGRAM PPSUBP10PT.
