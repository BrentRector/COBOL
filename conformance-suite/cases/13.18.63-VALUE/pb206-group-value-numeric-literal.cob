*>--- conformance-case
*> id: pb206-group-value-numeric-literal
*> rules: [SR-13.18.63.3-4, SR-13.18.63.3-13]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR13, sentence 1: "If the VALUE clause is specified at the group level, literal-1 shall
      *> be of the same category as the group item or shall be a figurative constant that is permitted in a MOVE
      *> statement to a receiving item of that category."  GN carries no GROUP-USAGE clause, is not strongly
      *> typed and is not a variable-length group, so it IS an alphanumeric group item (13.18.29.4 GR3) and
      *> 8.5.2.1 gives it "class and category alphanumeric".  1234 is a NUMERIC literal and is not a figurative
      *> constant, so it is neither of the two things SR13 admits.  (SR4's first sentence says the same of the
      *> same subject - "If the item is of category alphabetic, alphanumeric, or alphanumeric-edited literals in
      *> the VALUE clause shall be alphanumeric literals" - and SR16 carries SR13 to the format 2 VALUE.)
      *> MEASURED BEFORE THIS SCREEN (kb/Work PB206, on 1d949007): this program compiled CLEAN and left N1 and
      *> N2 both SPACES - the VALUE silently lost, because 13.18.63.4 GR5's area deposit is defined over the
      *> operand's CHARACTERS and a numeric literal has none.  That is also why this arm is an error on BOTH
      *> dialect axes where the ELEMENTARY SR4-sentence-1 violation is a --permissive warning: the leniency
      *> promises the literal's digits are stored as MOVE would store them, and over a group area they are not.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB206N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GN VALUE 1234.
          05 N1 PIC X(2).
          05 N2 PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY N1
           STOP RUN.
