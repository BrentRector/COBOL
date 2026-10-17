*>--- conformance-case
*> id: pb456-set-object-sender-numeric-receiver
*> rules: [SR-14.9.39.3-8]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.39.3 SR8 - "Identifier-3 shall be any item of class object that is permitted as a receiving
      *> item." SET N4 TO U writes an item of class object as the SENDER, and no other general format can hold
      *> that sender: Format 1's sending brace is { arithmetic-expression-1 | index-name-2 | identifier-2 },
      *> 8.8.1.1 admits only numeric operands in an arithmetic expression, and SR2 makes identifier-2 "a data
      *> item of class index". Format 5 is therefore the only format this statement can be, and its receiving
      *> operand rule refuses N4 - a PIC 9(4) item is not of class object.
      *> ⛔ It used to reach the ARITHMETIC screen instead (COBOLNET0844, "'U' ... is not a numeric operand"),
      *> because the format was selected from the receiving list alone and Format 1's identifier-1 brace admits
      *> any identifier. The diagnostic was true of 8.8.1.1 and silent about the rule the program broke. Two
      *> OoSpineTests cases measured it, and they are what dropped this cluster from train 40.
      *> Rejected from 2002: USAGE OBJECT REFERENCE and Format 5 are COBOL-2002 introductions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB456N8.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U USAGE OBJECT REFERENCE.
       01 N4 PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET N4 TO U
           STOP RUN.
