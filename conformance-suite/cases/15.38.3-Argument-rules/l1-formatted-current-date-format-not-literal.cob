*>--- conformance-case
*> id: l1-formatted-current-date-format-not-literal
*> rules: [AR-15.38.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.38.3 r1 — "Argument-1 shall be a national or alphanumeric LITERAL." W-F is a data item, not a
      *> literal, so the reference violates r1 however well-formed its CONTENT is: the value here is the very
      *> combined date and time format §15.38.3 r2 requires (§15.3.3.7), which is the point — this file screens
      *> literal-ness alone, with the content rule satisfied, so a diagnostic about the format's kind could not
      *> be mistaken for enforcement of r1. The class half of r1 is pinned separately by
      *> pb124-formatted-alphabetic (a PIC A item, category alphabetic).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1NFCDLIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-F PIC X(15) VALUE "YYYYMMDDThhmmss".
       01 W-S PIC X(15).
       PROCEDURE DIVISION.
           MOVE FUNCTION FORMATTED-CURRENT-DATE(W-F) TO W-S
           STOP RUN.
       END PROGRAM L1NFCDLIT.
