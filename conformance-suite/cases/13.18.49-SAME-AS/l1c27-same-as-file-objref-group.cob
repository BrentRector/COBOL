      *>--- conformance-case
      *> id: l1c27-same-as-file-objref-group
      *> rules: [SR-13.18.49.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.49.3 6) — file-section SAME AS a group holding an
      *> object reference
      *> "When the SAME AS clause is specified in the file section, the
      *> description of data-name-1, including its subordinate data
      *> items, shall not contain a data item described with a USAGE
      *> OBJECT REFERENCE clause."
      *> cite.py --check 13.18.49.3 "When the SAME AS clause is
      *>   specified in the file section, the description of
      *>   data-name-1, including its subordinate data items, shall not
      *>   contain a data item described with a USAGE OBJECT REFERENCE
      *>   clause" -> OK §13.18.49.3 6)
      *> THE SUBORDINATE ARM ("including its subordinate data items"):
      *> data-name-1 OGI is a level-1 group (SR7) of the STRONG type OG,
      *> whose subordinate OG-R is USAGE OBJECT REFERENCE (legal under a
      *> STRONG type declaration, §13.18.60.3 14), --check OK). With FR
      *> described as PIC X, the program compiles clean. SAME AS and
      *> TYPEDEF STRONG are COBOL 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C27K.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "L1C27K.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 FR SAME AS OGI.
       WORKING-STORAGE SECTION.
       01 OG TYPEDEF STRONG.
          05 OG-K PIC X.
          05 OG-R USAGE OBJECT REFERENCE.
       01 OGI TYPE OG.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
