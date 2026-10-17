*>--- conformance-case
*> id: l1-substitute-boolean-argument1
*> rules: [AR-15.87.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.87.3 r1 — a class-BOOLEAN argument-1. r1 admits "class alphabetic, alphanumeric, or national,
      *> or an alphanumeric or national literal"; §8.5.2.1's Table 2 carries "| Boolean | Boolean |" as its own
      *> row and r1 does not name it, so a bit item is refused.
      *>   python scripts/spec/cite.py --check 15.87.3 "Argument-1 shall be an identifier that references a data
      *>   item or identifier that is class alphabetic, alphanumeric, or national, or an alphanumeric or national
      *>   literal."  ->  OK  §15.87.3 1)  (Argument rules)
      *>
      *> A SEPARATE ARM FROM THE NUMERIC SIBLING, which is why it is a separate witness: the class model reaches
      *> a bit item through its own PICTURE, not through the category table that keys the numeric case.
      *> §13.18.60.4 GR5: "The USAGE BIT clause specifies that bits shall be used to represent a boolean data
      *> item. A data item described with USAGE BIT is a bit data item."
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SUBNEGB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L1S-B4 PIC 1(4) USAGE BIT VALUE B"1010".
       01 L1S-R  PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION SUBSTITUTE(L1S-B4 "1" "0") TO L1S-R.
           STOP RUN.
