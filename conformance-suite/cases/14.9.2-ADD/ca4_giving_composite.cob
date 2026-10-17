*>--- conformance-case
*> id: ca4_giving_composite
*> rules: [SR-14.9.2.3-1, SR-14.9.2.3-4, GR-14.9.2.4-2, SR-14.9.44.3-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> CA4 (CONFORMANCE-FIX-QUEUE): the ADD/SUBTRACT Format-2 composite of operands EXCLUDES the data items that
      *> follow GIVING (ISO 13.18.63... 14.9.2.3 SR1b / 14.9.44.3 SR1b; the superimposition rule 14.7.7 rule 2).
      *> A and B are 25 integer digits (composite 25 <= 31) so both statements are LEGAL; pre-fix the wide GIVING
      *> receiver C (6 int + 10 frac) was wrongly superimposed, giving a 35-digit composite and a spurious
      *> COBOLNET0805 rejection. Runtime: 1+2 = 3 and 5-2 = 3, each stored in PIC 9(6)V9(10) -> 0000030000000000.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CA4ADD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(25) VALUE 1.
       01 B PIC 9(25) VALUE 2.
       01 D PIC 9(25) VALUE 5.
       01 C PIC 9(6)V9(10).
       PROCEDURE DIVISION.
           ADD A B GIVING C.
           DISPLAY C.
           SUBTRACT B FROM D GIVING C.
           DISPLAY C.
           STOP RUN.
