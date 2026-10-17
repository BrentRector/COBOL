*>--- conformance-case
*> id: l1-test-numval-f-alphabetic-item
*> rules: [AR-15.95.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.95.3 r1 — "Argument-1 shall be an alphanumeric or national literal or a data item of class
      *> alphanumeric or national."
      *> (cite.py --check 15.95.3 "Argument-1 shall be an alphanumeric or national literal or a data item of
      *> class alphanumeric or national" -> OK, §15.95.3 rule 1.)
      *>
      *> CLASS ALPHABETIC IS A CLASS r1 DOES NOT NAME, and the standard treats it as distinct rather than as
      *> a shade of alphanumeric: §8.5.2.1 Table 2 carries its own "Alphabetic | Alphabetic" row, and
      *> §15.96.3 r1 (TRIM) shows the drafters writing "class alphabetic, alphanumeric, or national" when
      *> they mean to include it — §15.95.3 r1 does not. A PIC A(n) item is category alphabetic and
      *> therefore class alphabetic, so it is not "a data item of class alphanumeric or national".
      *>
      *> ⛔ THIS WAS AN UNENFORCEABLE EXCLUSION, NOT A FORGOTTEN ONE, AND THAT IS WHY THE FIXTURE MATTERS.
      *> PicCategory folds PIC A into Alphanumeric because both carriers are one string, so the screen had
      *> no vocabulary for the distinction: CobolClass had no Alphabetic member, ClassOfCategory answered
      *> Alphanumeric for a PIC A item, and this reference bound clean and ran. kb/Work PB124 wave 5 gave the
      *> class its member and routed a PIC A item to it through PicInfo.IsAlphabetic — the datum MoveTable16
      *> and INITIALIZE's §14.9.20 GR5c category matching had been reading all along.
      *>
      *> THE ADMIT SIDE IS conformance:cases/15.95.3-Argument-rule/l1_test_numval_f_class_screen, which requires a value from a
      *> numeric-edited item and from a group item. Both are class alphanumeric by Table 2, so a screen that
      *> closed this hole by narrowing to CATEGORY alphanumeric would turn this fixture green and that one
      *> red — the pair is the measurement, neither half alone.
      *> Expected: COBOLNET1627, the intrinsic-argument-class diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGL1TNVFA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WA PIC A(6) VALUE "ABCDEF".
       01 R  PIC S9(9).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION TEST-NUMVAL-F(WA).
           DISPLAY R.
           STOP RUN.
       END PROGRAM NEGL1TNVFA.
