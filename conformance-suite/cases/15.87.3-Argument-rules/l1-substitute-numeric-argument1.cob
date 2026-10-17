*>--- conformance-case
*> id: l1-substitute-numeric-argument1
*> rules: [AR-15.87.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.87.3 r1 — "Argument-1 shall be an identifier that references a data item or identifier that is
      *> class alphabetic, alphanumeric, or national, or an alphanumeric or national literal."
      *>   python scripts/spec/cite.py --check 15.87.3 "Argument-1 shall be an identifier that references a data
      *>   item or identifier that is class alphabetic, alphanumeric, or national, or an alphanumeric or national
      *>   literal."  ->  OK  §15.87.3 1)  (Argument rules)
      *>
      *> A class-NUMERIC item is outside that list: §8.5.2.1's Table 2 carries "| Numeric | Numeric |" as its own
      *> row, distinct from the Alphabetic, Alphanumeric and National rows, and r1 ENUMERATES the admitted
      *> classes rather than excluding any — so anything not named is refused.
      *>
      *> ⛔ NO FIXTURE HAD EVER FIRED r1's SCREEN. The SUBSTITUTE negatives in the corpus are r3's
      *> zero-length pair (pb58-subst-zero, pb58-subst-zero2), §15.87.1's result-type case
      *> (pb15-substitute-national-result-to-an), r2's class-pairing case (pb118-substitute-tail-class) and
      *> §15.87.2's keyword-order cases (pb124-substitute-*). The ADMITTED side is 2023/substitute (alphanumeric
      *> item), 2023/pb15_result_type_follows_argument (national item) and 2023/pb124_keyword_positions.
      *> SUBSTITUTE is COBOL-2023 (Annex E.3.3 item 30 — "FUNCTION SUBSTITUTE. The SUBSTITUTE intrinsic function
      *> has been added"), hence the single reject-at year: below 2023 the FUNCTION is gated, which is a
      *> different diagnostic and a different rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SUBNEGN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L1S-N9 PIC 9(4) VALUE 1234.
       01 L1S-R  PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION SUBSTITUTE(L1S-N9 "2" "9") TO L1S-R.
           STOP RUN.
