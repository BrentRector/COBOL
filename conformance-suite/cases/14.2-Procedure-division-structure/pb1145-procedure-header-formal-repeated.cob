*>--- conformance-case
*> id: pb1145-procedure-header-formal-repeated
*> rules: [SR-14.2.2-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1145 - ISO 14.2.2 SR1: "A particular user-defined word
      *>   shall not appear more than once as data-name-1." A repeated
      *>   USING parameter used to reach the backend as a duplicate carrier
      *>   member (CS0102); it is now a COBOLNET2648 diagnostic.
      *> cite.py --check 14.2.2 "A particular user-defined word shall not
      *>   appear more than once as data-name-1" -> OK  14.2.2 1)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1145A.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC 9(3).
       PROCEDURE DIVISION USING A A.
           GOBACK.
