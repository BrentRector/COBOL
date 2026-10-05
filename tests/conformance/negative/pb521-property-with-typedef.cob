      *> reject-at: 2002 2014 2023
      *> kb/Work PB521 -- ISO 13.16.3 SR21 b): the PROPERTY clause shall not be specified in the same data
      *> description entry as a TYPEDEF clause.  The property roster searched Roots for its subject, and a
      *> TYPEDEF entry is a template kept OFF Roots, so the subject was null and the entry was skipped as
      *> "already diagnosed" -- when nothing had diagnosed it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB521NT.
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM PB521NT.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB521CT.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  PT TYPEDEF PIC X(4) PROPERTY.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB521CT.
