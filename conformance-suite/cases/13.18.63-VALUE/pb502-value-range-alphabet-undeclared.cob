*>--- conformance-case
*> id: pb502-value-range-alphabet-undeclared
*> rules: [GR-13.18.63.4-18]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR31 names alphabet-name-1 in the VALUE clause, and 12.3.7 is where an alphabet-name
      *> is declared - the ALPHABET clause of SPECIAL-NAMES. A word that declares no alphabet supplies no
      *> collating sequence, so 14.7.8 rule 2's "the collating sequence defined by that alphabet" has nothing
      *> to resolve to. The VALUE-clause arm of the same screen the sibling negative
      *> pb398-range-alphabet-undeclared pins on EVALUATE: 14.7.8 opens "this specification applies to
      *> THROUGH phrases specified in the VALUE clause and the EVALUATE statement", so one resolver answers
      *> both and BOTH arms need a witness (kb/Work PB502 - only the EVALUATE arm had one).
      *> Refused rather than quietly ordered natively: silently weighing a range in a sequence the program
      *> did not ask for is exactly the wrong answer PB502 recorded.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB502NEG1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-C PIC X VALUE "C".
           88 C-RANGE VALUE "A" THRU "M" IN NO-SUCH-ALPHA.
       PROCEDURE DIVISION.
       MAIN-P.
           IF C-RANGE DISPLAY "IN" ELSE DISPLAY "OUT" END-IF
           STOP RUN.
