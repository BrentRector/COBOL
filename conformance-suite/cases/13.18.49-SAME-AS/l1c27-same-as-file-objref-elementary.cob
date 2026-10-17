      *>--- conformance-case
      *> id: l1c27-same-as-file-objref-elementary
      *> rules: [SR-13.18.49.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.49.3 6) — file-section SAME AS an OBJECT REFERENCE
      *> item
      *> "When the SAME AS clause is specified in the file section, the
      *> description of data-name-1, including its subordinate data
      *> items, shall not contain a data item described with a USAGE
      *> OBJECT REFERENCE clause."
      *> cite.py --check 13.18.49.3 "When the SAME AS clause is
      *>   specified in the file section, the description of
      *>   data-name-1, including its subordinate data items, shall not
      *>   contain a data item described with a USAGE OBJECT REFERENCE
      *>   clause" -> OK §13.18.49.3 6)
      *> THE ELEMENTARY ARM: data-name-1 OREF is itself the OBJECT
      *> REFERENCE item, a level-1 working-storage item (legal there;
      *> SR7 holds). The arm where the object reference is SUBORDINATE
      *> to data-name-1 is l1c27-same-as-file-objref-group. With FR
      *> described as PIC X, the program compiles clean. SAME AS is new
      *> in COBOL 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C27J.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "L1C27J.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 FR SAME AS OREF.
       WORKING-STORAGE SECTION.
       01 OREF USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
