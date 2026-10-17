*>--- conformance-case
*> id: pb250_cobol_words_multi_spelling
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB250 - the token arm's half. ZERO, ZEROS and ZEROES are three ISO 8.9 reserved words
      *> carried by ONE lexer rule (ZERO : 'ZERO' | 'ZEROS' | 'ZEROES'), and ANTLR publishes a literal
      *> NAME only for a token defined by exactly one literal - so the >>COBOL-WORDS token map, built from
      *> those names alone, could not see any of the three and the directive below was silently inert.
      *> 7.3.10.3 SR3 names ONE COBOL word, so GR3 frees ZERO and leaves ZEROS and ZEROES reserved.
       >>COBOL-WORDS UNDEFINE "ZERO"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB250CWZ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
      *> Legal only because GR3 made ZERO a user-defined word for this compilation group.
       01 ZERO PIC X(5) VALUE "alpha".
      *> ZEROS is untouched, so it is still the 8.9 figurative constant: Y is filled with the digit 0.
       01 Y    PIC 9(3) VALUE ZEROS.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "W=" ZERO
           DISPLAY "Y=" Y
           STOP RUN.
