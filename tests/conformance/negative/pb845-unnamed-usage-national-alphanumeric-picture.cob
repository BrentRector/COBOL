      *> reject-at: 2002 2014 2023
      *> kb/Work PB845. From COBOL-2002 8.9 reserves NATIONAL, so after a
      *> level number it can only be the USAGE clause of an unnamed item
      *> (13.16.3 SR4; 13.18.60.2 prints [ USAGE IS ] as optional), and
      *> 13.18.60.3 SR12: "An elementary data item with usage national
      *> shall be described with a picture character-string that
      *> describes a boolean, national, national-edited, numeric, or
      *> numeric-edited data item" - PIC X is none of those, so the entry
      *> is COBOLNET0881 by that rule. Before the fix the word was read as
      *> the entry's NAME and the error was COBOLNET0901, the wrong rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB845NU.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NATIONAL PIC X.
       PROCEDURE DIVISION.
           STOP RUN.
