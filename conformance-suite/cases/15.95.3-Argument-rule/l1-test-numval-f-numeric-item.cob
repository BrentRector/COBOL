*>--- conformance-case
*> id: l1-test-numval-f-numeric-item
*> rules: [AR-15.95.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.95.3 r1 — "Argument-1 shall be an alphanumeric or national literal or a data item of class
      *> alphanumeric or national."
      *> (cite.py --check 15.95.3 "Argument-1 shall be an alphanumeric or national literal or a data item of
      *> class alphanumeric or national" -> OK, §15.95.3 rule 1.)
      *>
      *> THE OTHER EXCLUSION: CLASS NUMERIC. §8.5.2.1 Table 2 gives category numeric the class NUMERIC, so a
      *> PIC 9(n) item is neither of the two classes r1 admits. The exclusion is not decorative for THIS
      *> function: TEST-NUMVAL-F exists to report whether a CHARACTER STRING conforms to §15.69.3 r1's
      *> content format, and a numeric item's content is a value with a usage, not a string a program can
      *> have got wrong — which is why the rule takes a literal or a string-classed item and nothing else.
      *>
      *> ⚠ IT IS NOT THE NUMERIC-EDITED CASE. §8.5.2.1 Table 2 puts category numeric-edited with usage
      *> display in class ALPHANUMERIC, so a PIC ZZ9.99 argument IS admitted and
      *> conformance:cases/15.95.3-Argument-rule/l1_test_numval_f_class_screen requires it to return a value. A screen that read
      *> "looks numeric" instead of the Table 2 class column would reject both, and the two fixtures
      *> together are what separates them.
      *> Expected: COBOLNET1627, the intrinsic-argument-class diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGL1TNVFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W9 PIC 9(6) VALUE 123456.
       01 R  PIC S9(9).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION TEST-NUMVAL-F(W9).
           DISPLAY R.
           STOP RUN.
       END PROGRAM NEGL1TNVFN.
