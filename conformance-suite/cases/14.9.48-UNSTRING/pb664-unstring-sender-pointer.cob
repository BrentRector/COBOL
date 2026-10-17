*>--- conformance-case
*> id: pb664-unstring-sender-pointer
*> rules: [SR-14.9.48.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> The UNSTRING half of kb/Work PB664's sweep.  ISO 14.9.48.3 SR2: "Identifier-1, identifier-2,
      *> identifier-3, and identifier-5 shall reference data items of category alphanumeric or national."
      *> A data-pointer item is category data-pointer (ISO 13.18.60.4 GR22/GR23 put it in class pointer), so
      *> neither the sender nor a DELIMITED BY operand may be one.
      *> The screen was written as a list of the categories it REFUSED - numeric, numeric-edited, boolean and
      *> the two edited forms - so the four pointer/object categories passed as if admitted: measured before the
      *> fix, `UNSTRING P DELIMITED BY " " INTO B` stored "Cobo".  It is now the rule's own shape, a whitelist.
      *> USAGE POINTER is a COBOL-2002 introduction, so 85 rejects the DECLARATION instead and is not claimed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB664NEGV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P   USAGE POINTER.
       01 TGT PIC X(4).
       01 B66 PIC X(4).
       PROCEDURE DIVISION.
           SET P TO ADDRESS OF TGT
           UNSTRING P DELIMITED BY " " INTO B66
           DISPLAY "B66=[" B66 "]"
           STOP RUN.
