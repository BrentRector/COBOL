*>--- conformance-case
*> id: pb399-evaluate-range-variable-length-group
*> rules: [SR-14.9.13.3-9]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.13.3 SR9: "Neither identifier-3 nor identifier-4 shall reference a variable-length
      *> group."  §8.5.1.12.1 defines the term: "A variable-length group is a group item whose data
      *> description has at least one dynamic-length elementary item or dynamic-capacity table as a
      *> subordinate item."  Both ends here hold a `PIC X DYNAMIC LENGTH` member.
      *>
      *> ⛔ AN OCCURS DEPENDING ON GROUP IS NOT ONE — its size is its maximum — and the legal ODO-ended
      *> range is pinned by `conformance:cases/14.9.13-EVALUATE/pb399_evaluate_selection_set_shapes` leg ODO-RANGE.  kb/Work
      *> PB399's own repro had that premise backwards; a screen written from it would have rejected legal
      *> source and left the shape the rule names untouched.
      *> Measured before the screen: this program compiled clean and aborted at RUN TIME on "whole-group
      *> image of 'WS-G1' with a dynamic-length / dynamic-capacity member".
      *> The DYNAMIC LENGTH clause is a COBOL-2014 introduction.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB399RVLG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-S PIC X(5) VALUE "BBBBB".
       01 WS-G1.
          05 WS-G1-A PIC X(3) VALUE "AAA".
          05 WS-G1-D PIC X DYNAMIC LENGTH.
       01 WS-G2.
          05 WS-G2-A PIC X(3) VALUE "ZZZ".
          05 WS-G2-D PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE WS-S
               WHEN WS-G1 THRU WS-G2
                   DISPLAY "VLG-RANGE"
               WHEN OTHER
                   DISPLAY "VLG-OTHER"
           END-EVALUATE.
           STOP RUN.
