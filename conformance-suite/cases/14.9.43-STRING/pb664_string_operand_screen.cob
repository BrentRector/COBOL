      *>--- conformance-case
      *> id: pb664_string_operand_screen
      *> rules: [SR-14.9.43.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB664 - THE POSITIVE CONTROL FOR ISO 14.9.43.3 SR1 AND 14.9.48.3 SR2.  The fix those rules
      *> needed is a REJECTION (a class-pointer / COMP / index / numeric-literal operand of STRING or UNSTRING
      *> was accepted and rendered the CLR carrier's type name), and the failure mode of a rejection is that it
      *> reaches source the rule ADMITS.  Every leg below is a spelling the rules admit, and each one names the
      *> clause that admits it.
      *>
      *> ISO 14.9.43.3 SR1 - "All literals shall be described as alphanumeric, boolean, or national literals,
      *>   and all identifiers, EXCEPT identifier-4, shall be described implicitly or explicitly as usage
      *>   display or national."  Identifier-4 is the WITH POINTER item: leg 1 makes it USAGE BINARY on purpose,
      *>   so the exemption is WITNESSED rather than assumed - a screen that over-reached would reject leg 1.
      *> ISO 14.9.43.3 SR7 - identifier-4 "shall be described as an elementary numeric integer data item of
      *>   sufficient size to contain a value equal to 1 plus the size of the data item referenced by
      *>   identifier-3"; PIC 9(4) COMP is one.
      *> ISO 14.9.43.3 SR8 - "Where identifier-1 or identifier-2 is an elementary numeric data item, it shall be
      *>   described as an integer without the symbol 'P' in its picture character-string" - so a usage-DISPLAY
      *>   numeric sender (leg 2) is admitted by SR1 and constrained by SR8, never refused.
      *> ISO 14.9.43.4 GR3 a) - the characters are transferred "in accordance with the MOVE statement rules for
      *>   alphanumeric-to-alphanumeric moves ... except that no space filling is provided"; b) transfer stops
      *>   at the delimiter and "the character(s) specified by literal-2 ... are not transferred".
      *> ISO 14.9.43.4 GR6 - identifier-4 "was increased by one prior to the move of the next character".
      *> ISO 14.9.48.3 SR2 - "Identifier-1, identifier-2, identifier-3, and identifier-5 shall reference data
      *>   items of category alphanumeric or national" - leg 5's sender and delimiter are both alphanumeric.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC:
      *>   L1  A4 = "AB C" delimited by D1 (one space): "AB" lands at position 1 and identifier-4 ends at
      *>       1 + 2 = 3, so OUT1 = "AB" + 28 spaces and PTR4 = 0003.
      *>   L2  a usage-display NUMERIC sender, DELIMITED BY SIZE: its three digit characters, OUT1 = "123".
      *>   L3  a GROUP sender - usage is an elementary property, so SR1's usage test does not reach it and the
      *>       group's own characters go: OUT1 = "XYZ9".
      *>   L4  a NUMERIC-EDITED sender, usage display: its edited image (13.18.63.3 SR7 gives PIC ZZ9
      *>       the image " 42" as the programmer wrote it), so OUT1 = " 42".  SR5 bars an edited item only as identifier-3.
      *>   L5  UNSTRING "AB C" delimited by " " into two PIC X(4) receivers: "AB  " and "C   ".
      *>
      *> EDITIONS: STRING and UNSTRING are complete at COBOL-85 and SR1/SR2 are unchanged since, so this file
      *> lives at the introducing edition and the corpus keeps ONE copy.  The negatives are
      *> tests/conformance/negative/pb664-string-sender-usage, -string-numeric-literal (both reject at every
      *> edition) and -string-sender-pointer, -unstring-sender-pointer (2002+, where USAGE POINTER exists).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB664POS85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A4   PIC X(4) VALUE "AB C".
       01 D1   PIC X(1) VALUE " ".
       01 N3   PIC 9(3) VALUE 123.
       01 G1.
          05 G1A PIC X(2) VALUE "XY".
          05 G1B PIC X(2) VALUE "Z9".
       01 E1   PIC ZZ9 VALUE " 42".
       01 PTR4 PIC 9(4) COMP VALUE 1.
       01 OUT1 PIC X(30).
       01 OUT2 PIC X(4).
       01 OUT3 PIC X(4).
       PROCEDURE DIVISION.
           MOVE SPACES TO OUT1
           STRING A4 DELIMITED BY D1 INTO OUT1 WITH POINTER PTR4
           DISPLAY "L1=[" OUT1 "] " PTR4
           MOVE SPACES TO OUT1
           STRING N3 DELIMITED BY SIZE INTO OUT1
           DISPLAY "L2=[" OUT1 "]"
           MOVE SPACES TO OUT1
           STRING G1 DELIMITED BY SIZE INTO OUT1
           DISPLAY "L3=[" OUT1 "]"
           MOVE SPACES TO OUT1
           STRING E1 DELIMITED BY SIZE INTO OUT1
           DISPLAY "L4=[" OUT1 "]"
           UNSTRING A4 DELIMITED BY " " INTO OUT2 OUT3
           DISPLAY "L5=[" OUT2 "][" OUT3 "]"
           STOP RUN.
