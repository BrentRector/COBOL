      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB990 - ISO 8.3.2.2 (cite.py --check 8.3.2.2 "Within a source element, a given user-defined word may
      *> be used as only one type of user-defined word"): DIGITS is a class-name (SPECIAL-NAMES) AND a paragraph-name
      *> (PROCEDURE DIVISION). Two kinds of declaration, two registries, and nothing crossed them.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB990B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CLASS DIGITS IS "0" THRU "9".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X PIC X VALUE "5".
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM DIGITS
           STOP RUN.
       DIGITS.
           IF X IS DIGITS DISPLAY "D" END-IF.
