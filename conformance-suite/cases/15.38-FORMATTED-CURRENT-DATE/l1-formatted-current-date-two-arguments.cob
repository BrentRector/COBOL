*>--- conformance-case
*> id: l1-formatted-current-date-two-arguments
*> rules: [FMT-15.38.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.38.2 prints "FUNCTION FORMATTED-CURRENT-DATE ( argument-1 )" — ONE argument, unbracketed and
      *> without an ellipsis, so the format admits neither a second argument nor a repetition. §15.38.3 supplies
      *> rules for argument-1 only, and §15.38 gives the function no offset-from-UTC argument at all (unlike
      *> §15.40.2 FORMATTED-DATETIME and §15.41.2 FORMATTED-TIME, whose formats print one in brackets). A
      *> two-argument reference does not match the general format and shall be rejected.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1NFCDAR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-S PIC X(15).
       PROCEDURE DIVISION.
           MOVE FUNCTION FORMATTED-CURRENT-DATE("YYYYMMDDThhmmss" 1)
               TO W-S
           STOP RUN.
       END PROGRAM L1NFCDAR.
