*>--- conformance-case
*> id: l1-national-of-numeric-argument1
*> rules: [AR-15.66.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.66.3 r1 — "Argument-1 shall be of class alphabetic or class alphanumeric." A class-NUMERIC data
      *> item is neither: §8.5.2.1's Table 2 carries "| Numeric | Numeric |" as its own row, distinct from the
      *> Alphabetic and Alphanumeric rows r1 names.
      *>   python scripts/spec/cite.py --check 15.66.3 "Argument-1 shall be of class alphabetic or class
      *>   alphanumeric."  ->  OK  §15.66.3 1)  (Argument rules)
      *>
      *> ⛔ THE ONE NATIONAL-OF REJECTION IN THE CORPUS WAS THE ONE CLASS THE PAIR ITSELF NAMES.
      *> negative/national-of-wrong-category feeds a class-NATIONAL argument, which is exactly what the INVERSE
      *> function DISPLAY-OF (§15.26) takes — so the screen had only ever been asked about its own partner's
      *> class, never about a class outside the pair. r1 excludes numeric, boolean, index, object and pointer
      *> alike; this is the numeric witness.
      *> NATIONAL-OF is COBOL-2002 (the intrinsic catalog's introduction edition), hence the three reject-at
      *> years — at 85 the function itself does not exist and a different diagnostic would fire.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1NOFNEGN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L1N-N9 PIC 9(3) VALUE 123.
       01 L1N-NR PIC N(3).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION NATIONAL-OF(L1N-N9) TO L1N-NR.
           STOP RUN.
