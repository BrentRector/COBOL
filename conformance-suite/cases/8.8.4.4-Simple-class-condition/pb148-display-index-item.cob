      *>--- conformance-case
      *> id: pb148-display-index-item
      *> rules: [SR-8.8.4.4.3-1, GR-13.18.60.4-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 13.18.60.3 SR10's closed list: an index DATA item may be
      *> referenced only in SEARCH/SET, a relation condition, an
      *> intrinsic/method argument, or a USING phrase - DISPLAY is not
      *> among them. It previously printed an EMPTY zero-digit image
      *> (kb/Work PB148 - the R16/0809 family's unswept third arm).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB148N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 IX USAGE INDEX.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY IX
           STOP RUN.
