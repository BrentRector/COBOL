*>--- conformance-case
*> id: pb140-delete-file-sd
*> rules: [SR-14.9.10.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.10.3 SR3: the file description entry associated with a
      *> DELETE FILE statement shall not be a sort-merge file description
      *> entry; 13.4.6.3 SR3 is the general ban (kb/Work PB140).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB140N3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S ASSIGN TO "s.dat".
       DATA DIVISION.
       FILE SECTION.
       SD S.
       01 SREC PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           DELETE FILE S
           STOP RUN.
