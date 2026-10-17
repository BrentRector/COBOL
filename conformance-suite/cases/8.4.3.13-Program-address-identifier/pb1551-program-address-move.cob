*>--- conformance-case
*> id: pb1551-program-address-move
*> rules: [SR-8.4.3.13.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1551 - ISO 8.4.3.13.3 SR4: "This identifier format shall not
      *> be specified as a receiving operand." (cite.py: OK 8.4.3.13.3 4))
      *> A program-address-identifier as MOVE's receiving operand. MOVE of a
      *> pointer is also 14.9.25.3 SR1's (COBOLNET0809, for the sender); the
      *> receiver is refused by SR4 (COBOLNET3979), no longer by 8.9.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1551PA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PP USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION.
       MAIN-P.
           SET PP TO ADDRESS OF PROGRAM "PB1551PA"
           MOVE PP TO ADDRESS OF PROGRAM "PB1551PA"
           STOP RUN.
       END PROGRAM PB1551PA.
