      *>--- conformance-case
      *> id: pb894-prototype-contained
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 10.6.1: a program-definition contains only
      *> [ program-definition ] ... -- a program prototype is a source
      *> unit of the compilation group itself and is never contained.
      *> kb/Work PB894.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PGNCO.
       PROCEDURE DIVISION.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PGNCI IS PROTOTYPE.
       PROCEDURE DIVISION.
       END PROGRAM PGNCI.
       END PROGRAM PGNCO.
