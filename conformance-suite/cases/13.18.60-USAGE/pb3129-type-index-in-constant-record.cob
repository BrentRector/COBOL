*>--- conformance-case
*> id: pb3129-type-index-in-constant-record
*> rules: [SR-13.18.60.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3129 review R1-1 - ISO 13.18.60.3 SR4: "The INDEX, MESSAGE-TAG, OBJECT REFERENCE,
      *> POINTER, FUNCTION-POINTER, and PROGRAM-POINTER phrases shall not be specified in a data item
      *> described with the CONSTANT RECORD clause, or in any item subordinate to a data item described
      *> with the CONSTANT RECORD clause." 13.18.57.4 GR1: "The effect of the TYPE clause is as though the
      *> data description identified by type-name-1 had been coded in place of the TYPE clause", so A is
      *> as though 05 A USAGE INDEX were written inside the CONSTANT RECORD CR (the written spelling is
      *> negative pb183-usage-index-constant-record). The type declaration IT is legal at level 1, so only
      *> the TYPE entry breaks the rule. Before the fix this compiled clean and ran.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3129CI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 IT IS TYPEDEF USAGE INDEX.
       01 CR CONSTANT RECORD.
          05 A TYPE IT.
          05 B PIC X VALUE "A".
       PROCEDURE DIVISION.
           DISPLAY B OF CR
           STOP RUN.
