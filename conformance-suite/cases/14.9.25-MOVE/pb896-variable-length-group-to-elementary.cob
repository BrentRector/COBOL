*>--- conformance-case
*> id: pb896-variable-length-group-to-elementary
*> rules: [GR-14.9.25.4-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB896 - THE COMPLEMENT OF tests/conformance/85/pb896_zero_length_group_sender.cob, and the
      *> reason that golden covers ISO 8.5.4 item 1 rather than items 5 and 7.
      *> 8.5.4's four GROUP shapes are all zero-length items, but only item 1 - "A group data item containing
      *> only an occurs-depending table in which the number of occurrences is zero" - can reach an ELEMENTARY
      *> receiving operand. Items 5 and 7 are VARIABLE-LENGTH groups, and ISO 14.9.25.3 SR9 requires the two
      *> operands of such a move to be compatible groups as specified in 8.5.1.12, so a variable-length group
      *> sender with an elementary receiver is refused at compile time and 14.9.25.4 GR1's run-time substitution
      *> is never reached. That is a rule, not a gap: this case is what makes it a MEASURED one.
      *> reject-at names 2014 and 2023 only because OCCURS DYNAMIC (ISO 13.18.38 Format 4) is a COBOL-2014
      *> introduction - below it the entry itself is refused by the edition gate, a different diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB896NEGVLG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 VLG.
          05 VE           PIC X(1) OCCURS DYNAMIC CAPACITY IN VCAP.
       01 R-ALPH          PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
           MOVE VLG TO R-ALPH
           DISPLAY "R=[" R-ALPH "]"
           STOP RUN.
