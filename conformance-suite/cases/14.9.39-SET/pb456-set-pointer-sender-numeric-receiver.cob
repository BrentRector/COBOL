*>--- conformance-case
*> id: pb456-set-pointer-sender-numeric-receiver
*> rules: [SR-14.9.39.3-17]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.39.3 SR17 - Format 7's receiving operand identifier-5 is of category data-pointer. The SIBLING
      *> of pb456-set-object-sender-numeric-receiver, and the reason the repair is a TABLE COLUMN rather than an
      *> arm for class object: a data-pointer sender is admissible in no Format-1 sending position either
      *> (8.8.1.1 numeric operands, SR2's class index), and exactly one printed format names it - Format 7's
      *> identifier-6. So SET N4 TO P1 is a Format 7 statement whose receiving operand SR17 refuses, and the
      *> diagnostic names that rule instead of reporting "'P1' ... is not a numeric operand" (COBOLNET0844)
      *> about an arithmetic expression the program never wrote. Formats 8 and 9 answer the same way for a
      *> function-pointer and a program-pointer sender.
      *> Rejected from 2002: USAGE POINTER and Format 7 are COBOL-2002 introductions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB456N9.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P1 USAGE POINTER.
       01 N4 PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET N4 TO P1
           STOP RUN.
