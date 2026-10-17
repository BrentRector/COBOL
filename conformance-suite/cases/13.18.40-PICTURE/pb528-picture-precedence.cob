*>--- conformance-case
*> id: pb528-picture-precedence
*> rules: [SR-13.18.40.3-2, SR-13.18.40.3-25, SR-13.18.40.3-26,
*>    SR-13.18.40.3-27]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB528 - the SECOND obligation of ISO 1989:2023 13.18.40.3 SR2: "Character-string-1 shall
      *> consist of an allowable COMBINATION of characters used as picture symbols. The allowable combinations
      *> of symbols for a PICTURE clause are specified in 13.18.40.6, Precedence rules." Nothing read a
      *> symbol's POSITION, so none of these was ever tested. Each is COBOLNET1935. Table 10's blank cell is
      *> a PROHIBITION, and an 'x' means the column symbol "may precede (but not necessarily immediately)"
      *> the row symbol, so the relation binds NON-ADJACENT pairs too.
      *>
      *> PD01  row 'Z *' left-of-point, column '9' is BLANK: no '9' may precede a zero-suppression symbol.
      *> PD02  row 'V', column 'A X' is BLANK: no 'A' or 'X' may precede the implied decimal point.
      *> PD03  row '9', column 'CR DB' is BLANK - in fact the WHOLE 'CR DB' column is blank, which is how the
      *>     standard says CR/DB is the last symbol. MOVE -1 through this rendered 00CR01.
      *> PD04  the whole 'Z *' right-of-point row is reached only past the point; here 'Z' follows '9' across
      *>     the decimal separator and row 'Z *' right-of-point column '9' is BLANK.
      *> PD05  SR25 needs no rule of its own: a lone '+' is a NON-FLOATING sign, its two roles are "the first
      *>     symbol" and "the last symbol", and 9+9 can be neither - row '+ -' leading is entirely blank
      *>     (nothing may precede a leading sign) and the '+ -' trailing COLUMN is entirely blank (nothing
      *>     may follow a trailing one).
      *> PD06  SR26 likewise: a lone currency symbol is the leftmost symbol "optionally preceded by one of the
      *>     symbols '+' or '-'", or the rightmost "optionally followed by one of '+', '-', 'CR' or 'DB'".
      *>     Row 'cs' leading admits ONLY the '+ -' leading column, so 9$9 has no assignable role.
      *> PD07  SR27 likewise: PIC ++$$$ asks for two floating strings, and row 'cs' floating-left has a BLANK
      *>     '+ -' floating-left column.
      *> PD08  a 'V' between digits and a trailing 'P' string: 13.18.40.4 GR14 puts the assumed point to the
      *>     RIGHT of a rightmost 'P' string, which contradicts the V, and Table 10 refuses the pair - row
      *>     'P' (either row) has a BLANK 'V' column for the left-of-point row and a blank '9' column for the
      *>     right-of-point one. SR19's letter alone does not make this string allowable.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB528PRE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PD01 PIC 9ZZ.
       01 PD02 PIC XV9.
       01 PD03 PIC 99CR99.
       01 PD04 PIC 99.9Z.
       01 PD05 PIC 9+9.
       01 PD06 PIC 9$9.
       01 PD07 PIC ++$$$.
       01 PD08 PIC 99VPP.
       PROCEDURE DIVISION.
           STOP RUN.
