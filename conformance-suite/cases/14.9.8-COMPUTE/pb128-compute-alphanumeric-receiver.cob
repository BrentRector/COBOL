*>--- conformance-case
*> id: pb128-compute-alphanumeric-receiver
*> rules: [SR-14.9.8.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.8.3 SR1: COMPUTE's identifier-1 shall reference an elementary numeric or numeric-edited
      *> item. A PIC X receiver compiled clean and died in StoreArith's RUN-TIME loud before the PB128
      *> screen (4.2.2 requires a compile-time mechanism); the sending side has had DA6's screen for weeks.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB128NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XR PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE XR = 1
           STOP RUN.
