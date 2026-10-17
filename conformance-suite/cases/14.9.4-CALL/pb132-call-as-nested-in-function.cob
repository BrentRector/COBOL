*>--- conformance-case
*> id: pb132-call-as-nested-in-function
*> rules: [SR-14.9.4.3-13]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.3 SR13: the NESTED phrase may be specified only in a program definition - a function
      *> definition contains no programs (kb/Work PB132; the old binder bound it clean).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB132F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-R PIC 9(4).
       PROCEDURE DIVISION RETURNING L-R.
       P.
           CALL "X" AS NESTED
           MOVE 1 TO L-R
           GOBACK.
       END FUNCTION PB132F.
