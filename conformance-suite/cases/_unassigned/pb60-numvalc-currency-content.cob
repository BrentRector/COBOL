*>--- conformance-case
*> id: pb60-numvalc-currency-content
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.68.3 r2: argument-2 "shall not contain any of the digits 0
      *> through 9; the characters '*', '+', '-', ',', or '.'; or the two
      *> consecutive letters 'CR' or 'DB'" in any case. A digit-bearing currency
      *> could consume argument-1 digits as "the currency" and value a wrong
      *> number silently. The literal half screens at bind (PB60 /
      *> AR-15.68.3-2); a data-item argument-2 has the runtime EC twin.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB60NEGCC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X(12) VALUE "1,234.56".
       01 R    PIC S9(9)V99.
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION NUMVAL-C(WS-A "U5D")
           STOP RUN.
