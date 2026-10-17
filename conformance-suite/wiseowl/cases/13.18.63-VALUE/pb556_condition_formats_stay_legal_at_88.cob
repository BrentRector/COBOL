*>--- conformance-case
*> id: pb556_condition_formats_stay_legal_at_88
*> rules: [SR-13.18.63.3-33]
*> accept: [85]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB556 - the POSITIVE half of ISO 13.18.63.3 SR33, "Formats 3 and 5 may be specified only when
      *> the level-number of the subject of the entry is 88." A screen that rejects a phrase at the wrong level
      *> is only correct if it still ACCEPTS it at the right one, and the three negatives that pin the rejection
      *> (tests/conformance/negative/pb556-value-*) cannot see an over-reject. This is that half.
      *>
      *> The expected values are COMPUTED FROM THE RULES, not measured:
      *>   A  14.7.8 determines the range: `88 X-LOW VALUE 1 THRU 5` is true for a conditional variable holding
      *>      any value from 1 to 5 inclusive, and 8.8.4.5.3 GR3 makes the condition-name condition true when
      *>      "one of the values corresponding to condition-name-1 equals the value of its associated
      *>      conditional variable". X holds 3, so X-LOW is TRUE and X-HIGH (6 THRU 9) is FALSE.
      *>   B  a multi-VALUE list is a set of singletons by the same GR3: Y holds "BB", which is one of them.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB556-SR33-AT-88.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 3.
          88 X-LOW VALUE 1 THRU 5.
          88 X-HIGH VALUE 6 THRU 9.
       01 Y PIC XX VALUE "BB".
          88 Y-SET VALUE "AA" "BB" "CC".
       PROCEDURE DIVISION.
           IF X-LOW DISPLAY "X-LOW-TRUE" ELSE DISPLAY "X-LOW-FALSE" END-IF
           IF X-HIGH DISPLAY "X-HIGH-TRUE" ELSE DISPLAY "X-HIGH-FALSE" END-IF
           IF Y-SET DISPLAY "Y-SET-TRUE" ELSE DISPLAY "Y-SET-FALSE" END-IF
           STOP RUN.
