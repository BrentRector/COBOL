*>--- conformance-case
*> id: pb24_length_variable_length_group
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> A VARIABLE-LENGTH GROUP'S LENGTH IS 15.50.4 RULE 7's SUM, AND IT IS A RUNTIME VALUE.
      *> r7: the value returned is the sum of (a) the lengths of all subordinate non-variable-length data items,
      *> (b) the CURRENT lengths of all subordinate dynamic-length elementary items, and (c) subordinate
      *> dynamic-capacity tables at their current capacity.
      *>
      *> THIS GOLDEN EXISTS BECAUSE THE ANSWER WAS SILENTLY WRONG (fix-queue PB24). BindLengthFold had arms for
      *> ref-mod, OCCURS DEPENDING, ANY LENGTH, national dynamic-length and dynamic-length ELEMENTARY items - and
      *> NO arm for a GROUP with a dynamic descendant. Such a group fell through to the fixed fold, and
      *> DataItem.ImageWidth contributes ZERO for a dynamic-length child (8.5.1.10 - its width is a runtime fact).
      *> MEASURED before the fix: X(4) plus a DYNAMIC LENGTH child holding "XYZ" returned 4, not 7. No diagnostic.
      *> A missing arm in a dispatch - this compiler's most reproducible defect shape, in its silent form.
      *>
      *> THE CASES BELOW ARE THE ONES A SHALLOW FIX PASSES AND A CORRECT ONE MUST TOO: a fixed group must still
      *> fold unchanged; TWO dynamic children must both count; a dynamic leaf NESTED in a subordinate group must
      *> count (r7 says "all subordinate", not "all immediate children"); and the value must TRACK THE CONTENT at
      *> run time - regrowing the child must change the answer, which is what proves this is not a new constant.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB24VLG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-FIXED.
          05 F1 PIC X(4).
          05 F2 PIC X(6).
       01 WS-ONE.
          05 A1 PIC X(4).
          05 D1 PIC X DYNAMIC LENGTH.
       01 WS-TWO.
          05 B1 PIC X(2).
          05 D2 PIC X DYNAMIC LENGTH.
          05 B2 PIC X(3).
          05 D3 PIC X DYNAMIC LENGTH.
       01 WS-NEST.
          05 N1 PIC X(5).
          05 N-SUB.
             10 N2 PIC X(2).
             10 D4 PIC X DYNAMIC LENGTH.
       01 R PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION LENGTH(WS-FIXED)
           DISPLAY "fixed group        = " R "  (expect 0010)"
           MOVE "XYZ" TO D1
           COMPUTE R = FUNCTION LENGTH(WS-ONE)
           DISPLAY "one dynamic  4+3   = " R "  (expect 0007)"
           MOVE "AB" TO D2
           MOVE "CDEF" TO D3
           COMPUTE R = FUNCTION LENGTH(WS-TWO)
           DISPLAY "two dynamic 2+2+3+4= " R "  (expect 0011)"
           MOVE "Q" TO D4
           COMPUTE R = FUNCTION LENGTH(WS-NEST)
           DISPLAY "nested      5+2+1  = " R "  (expect 0008)"
      *> It must be a RUNTIME value, not a fold: change the content and the answer must change.
           MOVE "LONGERSTRING" TO D1
           COMPUTE R = FUNCTION LENGTH(WS-ONE)
           DISPLAY "after regrow 4+12  = " R "  (expect 0016)"
           MOVE SPACES TO D1
           MOVE "" TO D1
           COMPUTE R = FUNCTION LENGTH(WS-ONE)
           DISPLAY "after empty  4+0   = " R "  (expect 0004)"
           STOP RUN.
