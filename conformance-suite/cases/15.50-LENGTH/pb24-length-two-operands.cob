*>--- conformance-case
*> id: pb24-length-two-operands
*> rules: [FMT-15.50.2]
*> source-format: free
*> portable: unverified
*>---
      *> FUNCTION LENGTH takes argument-1 and, optionally, the PHYSICAL keyword (15.50.2). TWO operands is not a
      *> form the general format admits. Consuming PHYSICAL as a keyword must not degrade into accepting anything.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB24LEN2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(4).
       01 B PIC X(4).
       01 R PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION LENGTH(A B)
           STOP RUN.
