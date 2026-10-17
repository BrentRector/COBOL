*>--- conformance-case
*> id: pb1082-pcs-class-name-is-no-alphabet
*> rules: [SR-12.3.6.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1082 - ISO 12.3.6.3 SR1 (cite.py --check 12.3.6.3 "Alphabet-name-1 shall reference an alphabet that
      *> defines an alphanumeric collating sequence"): HEXD below is a
      *> CLASS-name - HEXD is declared, but not as an alphabet, so alphabet-name-1 refuses it (the alphabet-name-2
      *> slot always refused such a word; this one used to be inert).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB1082B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. ANY-COMPUTER PROGRAM COLLATING SEQUENCE IS HEXD.
       SPECIAL-NAMES.
           CLASS HEXD IS "0" THRU "9" "A" THRU "F".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A PIC X VALUE "A".
       PROCEDURE DIVISION.
           STOP RUN.
