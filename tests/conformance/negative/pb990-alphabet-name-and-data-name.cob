      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB990 - ISO 8.3.2.2: "Within a source element, a given user-defined word may be used as only one type
      *> of user-defined word" (cite.py --check 8.3.2.2 "Within a source element, a given user-defined word may be
      *> used as only one type of user-defined word"). ZQ is an alphabet-name AND a data-name; neither is among the
      *> exceptions (a compilation-variable-name, a level-number beside a paragraph- or section-name, and the
      *> constant-name / data-name / property-name / record-key-name / record-name group). It compiled and ran.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB990A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET ZQ IS NATIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ZQ PIC X VALUE "A".
       PROCEDURE DIVISION.
           DISPLAY ZQ
           STOP RUN.
