*>--- conformance-case
*> id: l1_test_numval_f_class_screen
*> rules: [AR-15.95.3-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.95.3 r1 — TEST-NUMVAL-F's argument-1 class screen, the ADMIT side.
      *> "Argument-1 shall be an alphanumeric or national literal or a data item of class alphanumeric or
      *> national."
      *> (cite.py --check 15.95.3 "Argument-1 shall be an alphanumeric or national literal or a data item of
      *> class alphanumeric or national" -> OK, §15.95.3 rule 1.)
      *>
      *> THE RULE NAMES A CLASS, AND §8.5.2.1 TABLE 2 IS WHAT SAYS WHICH CATEGORIES REACH IT. Class
      *> ALPHANUMERIC covers categories alphanumeric, alphanumeric-edited and — the row easiest to read the
      *> wrong way — NUMERIC-EDITED with usage display. Class NATIONAL covers national and national-edited.
      *> A GROUP item takes its class from its own kind (§8.5.2.1): an alphanumeric group (GRP below: no
      *> GROUP-USAGE clause, not strongly typed, §13.18.29.4 GR3) has class alphanumeric, a bit group
      *> boolean, a national group national. Class ALPHABETIC is a
      *> DISTINCT class with its own Table 2 row ("Alphabetic | Alphabetic") and r1 does not name it, which
      *> is the reject side: conformance:negative/l1-test-numval-f-alphabetic-item. Class NUMERIC is the
      *> other exclusion: conformance:negative/l1-test-numval-f-numeric-item.
      *>
      *> ⛔ WHY THE ADMIT SIDE IS A FIXTURE OF ITS OWN. The compiler could not express class ALPHABETIC at
      *> all until kb/Work PB124 wave 5 — PicCategory folds PIC A into Alphanumeric for storage — so this
      *> rule's exclusion was unenforced and a PIC A(n) argument bound clean. Narrowing the admissible set
      *> to close that hole is exactly the change that can go one step too far: numeric-edited is the
      *> category Table 2 FOLDS INTO class alphanumeric, so a screen rewritten on categories rather than on
      *> the class column would reject the EDIT line below, and a screen that dropped the group arm would
      *> reject GRP. Both would be over-rejections of conforming source, invisible from the two negatives.
      *>
      *> Hand-derived from §15.95.4: the returned value is 0 when argument-1 conforms to the §15.69.3 r1
      *> content format, and §15.69.3 r5 makes leading and trailing spaces ignorable. Every probe below
      *> carries a conforming content, so each answers 0 — the observable that says the reference both BOUND
      *> and EVALUATED, where a rejected argument would not compile at all.
      *>   LIT   an alphanumeric literal, r1's first alternative.
      *>   PICX  PIC X(10) holding " 12.5" — category alphanumeric, class alphanumeric.
      *>   EDIT  PIC ZZ9.99 after MOVE 12.34, so its content is " 12.34" — category NUMERIC-EDITED, which
      *>         Table 2 puts in class ALPHANUMERIC. r1 reaches it through the class, not the category.
      *>   NAT   PIC N(8) USAGE NATIONAL holding "1.5E+3" — class national, r1's other admitted class.
      *>   GRP   a group item whose two children spell "1.5E+3" — a data item of class alphanumeric.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1TNVFCLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WX  PIC X(10) VALUE " 12.5".
       01 WE  PIC ZZ9.99.
       01 WN  PIC N(8) USAGE NATIONAL.
       01 GRP.
          05 G1 PIC X(3) VALUE "1.5".
          05 G2 PIC X(3) VALUE "E+3".
       01 R   PIC S9(9).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 12.34 TO WE
           MOVE "1.5E+3" TO WN
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1.5E+3")
           IF R = 0 DISPLAY "LIT OK" ELSE DISPLAY "LIT BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F(WX)
           IF R = 0 DISPLAY "PICX OK" ELSE DISPLAY "PICX BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F(WE)
           IF R = 0 DISPLAY "EDIT OK" ELSE DISPLAY "EDIT BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F(WN)
           IF R = 0 DISPLAY "NAT OK" ELSE DISPLAY "NAT BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F(GRP)
           IF R = 0 DISPLAY "GRP OK" ELSE DISPLAY "GRP BAD " R END-IF
           STOP RUN.
       END PROGRAM L1TNVFCLS.
