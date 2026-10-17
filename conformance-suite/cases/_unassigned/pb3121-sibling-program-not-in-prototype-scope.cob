      *>--- conformance-case
      *> id: pb3121-sibling-program-not-in-prototype-scope
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3121 - the converse of 2002/pb3121_containing_
      *> definition_scope. 8.4.6.8 (cite.py --check 8.4.6.8 -> OK):
      *> "Program-prototype-names referenced within a source element
      *> shall be either the program-name of a containing program
      *> definition or a program-prototype-name declared in the
      *> REPOSITORY paragraph". "Containing" reaches the element's own
      *> definition and each one enclosing it, never a SIBLING: NSIBA
      *> and NSIBB are both contained in NSIBOUT, so inside NSIBB the
      *> word NSIBA is neither spelling, and TO NSIBA names no
      *> checkable signature (13.18.60.4 GR25). COBOLNET1958.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NSIBOUT.
       PROCEDURE DIVISION.
       NSIBOUT-P.
           CALL "NSIBB"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NSIBA.
       PROCEDURE DIVISION.
       NSIBA-P.
           GOBACK.
       END PROGRAM NSIBA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NSIBB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TSIB IS TYPEDEF USAGE PROGRAM-POINTER TO NSIBA.
       01 Q TYPE TSIB.
       PROCEDURE DIVISION.
       NSIBB-P.
           SET Q TO NULL
           GOBACK.
       END PROGRAM NSIBB.
       END PROGRAM NSIBOUT.
