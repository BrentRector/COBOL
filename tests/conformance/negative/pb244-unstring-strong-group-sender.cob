      *> reject-at: 2002 2014 2023
      *> ISO 14.9.48.3 SR2 (kb/Work PB244): "Identifier-1, identifier-2, identifier-3, and identifier-5 shall reference
      *> data items of category alphanumeric or national." 8.5.2.1: "Both the class and the category of a
      *> strongly-typed group item are the type-name specified in the TYPE clause" - so a strongly-typed group is
      *> neither. The program used to compile, and abort at run time in the Tier-C whole-group island when the group
      *> held a pointer leaf; it is refused at bind (COBOLNET1651).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB244NUNSTR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GPT IS TYPEDEF STRONG.
          05 GA PIC X(3).
          05 GP USAGE POINTER.
       01 WS-GP TYPE GPT.
       01 WS-DST PIC X(40).
       PROCEDURE DIVISION.
       MAIN.
           UNSTRING WS-GP DELIMITED BY SPACE INTO WS-DST
           STOP RUN.
