*>--- conformance-case
*> id: pb392-subtract-corresponding-bit-group
*> rules: [SR-14.9.2.3-6, SR-14.9.44.3-6]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB392. ISO 1989:2023 14.9.44.3 SR6 - "Identifier-4 and identifier-5 shall be alphanumeric group
*> items, national group items, variable-length groups, or strongly-typed group items and shall not be
*> described with level-number 66" - names FOUR of the five group kinds, and a BIT group is the one it leaves
*> out: ISO 3.11 defines an alphanumeric group item as a "group item except for a bit group item, a national
*> group item, a strongly-typed group item, or a variable-length group item", so a GROUP-USAGE BIT group is in
*> none of SR6's four.
*>
*> ISO 14.7.6 rule 3 is WHY the rule leaves it out: "In an ADD or SUBTRACT statement, both of the data items
*> are numeric data items", and 13.18.29.4 GR1 b) makes a bit group "an elementary data item of usage bit and
*> class and category boolean", so no implied pair could ever correspond. Before this note the screen asked the
*> boolean DataItem.IsGroup and ADMITTED the statement, which then ran as a silent no-op.
*>
*> The edition band starts at 2002 because the GROUP-USAGE clause is a COBOL-2002 element (13.18.29); below it
*> the clause itself is gated and the program would be rejected for a different reason.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB392BITCORR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC GROUP-USAGE BIT.
          05 P PIC 1(4).
       01 DST GROUP-USAGE BIT.
          05 P PIC 1(4).
       PROCEDURE DIVISION.
           SUBTRACT CORRESPONDING SRC FROM DST
           STOP RUN.
