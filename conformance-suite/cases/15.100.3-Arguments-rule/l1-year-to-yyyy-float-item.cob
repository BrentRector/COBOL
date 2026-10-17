*>--- conformance-case
*> id: l1-year-to-yyyy-float-item
*> rules: [AR-15.100.3-3, AR-15.100.3-6, RV-15.100.4-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.100.3 r1 read through §15.3's argument TYPES — YEAR-TO-YYYY's argument-1 on a FLOATING-POINT
      *> carrier, which is where three of this function's inventory rows recorded their residue.
      *> "Argument-1 shall be a nonnegative integer that is less than 100."
      *> (cite.py --check 15.100.3 "Argument-1 shall be a nonnegative integer that is less than 100" -> OK,
      *> §15.100.3 rule 1.)
      *>
      *> §15.3's type 6 (Integer) admits exactly two things — "an arithmetic expression that will ALWAYS
      *> result in an integer value" or "an integer data item". A floating-point item is neither: §14.6.8.3
      *> sets its content to "the algebraic value of the sending operand", so its DECLARED value set
      *> contains non-integers and no reference to it is provably integral, whatever this run happens to
      *> store. It is a TYPE test, not a value test — the same argument the scale arm has applied to a
      *> PIC 9V9 since kb/Work PB40, extended to the float flag by PB248.
      *>
      *> ⛔ WHY THIS FIXTURE IS WRITTEN ON YEAR-TO-YYYY SPECIFICALLY. The rows AR-15.100.3-3, AR-15.100.3-6
      *> and RV-15.100.4-1 each recorded their open half as "the float carrier": FUNCTION YEAR-TO-YYYY(F)
      *> over a COMP-2 F, whose Real-lane body once defaulted the omitted argument-3 to a literal 0 and so
      *> answered the §15.3 default 0 on source r3 requires to window against the execution year. That
      *> body's overload split is fixed (kb/Work PB119), and this fixture pins the OTHER half of the
      *> disposition: for CONFORMING source the lane is not reachable at all, because the argument is
      *> refused at bind. Both halves matter — a compiler that stopped rejecting here would silently reopen
      *> the lane, and nothing else in the corpus would notice.
      *>
      *> ⚠ IT IS A CONFORMANCE REJECTION, NOT A CAPABILITY GAP: --permissive accepts the same reference as a
      *> documented coercion extension and the Real-lane body then answers correctly. The rules' own
      *> windowing content is measured on the lane conforming source DOES reach, by
      *> conformance:cases/15.100.3-Arguments-rule/l1_year_to_yyyy_defaults_and_sum_window.
      *> Expected: COBOLNET1627, the intrinsic-argument-class diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGL1Y2YF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WF USAGE COMP-2.
       01 R  PIC S9(9).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 76 TO WF.
           COMPUTE R = FUNCTION YEAR-TO-YYYY(WF).
           DISPLAY R.
           STOP RUN.
       END PROGRAM NEGL1Y2YF.
