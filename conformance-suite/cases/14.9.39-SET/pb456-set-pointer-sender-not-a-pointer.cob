*>--- conformance-case
*> id: pb456-set-pointer-sender-not-a-pointer
*> rules: [SR-14.9.39.3-17]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.39.3 SR17, SECOND SENTENCE - "Identifier-6 shall be of category data-pointer." The twin of
      *> pb456-set-pointer-sender-numeric-receiver, which witnesses the FIRST sentence ("Identifier-5 shall
      *> reference a data item of category data-pointer"): here the receiving operand is the legal one and the
      *> SENDER is not, so the two halves of one syntax rule are each measured by a case of their own rather
      *> than one case standing for both.
      *> A PIC 9(4) item is of category numeric (8.5.2.1 Table 2), so it is neither the predefined address NULL,
      *> nor another data-pointer item, nor an ADDRESS OF identifier - the three sending alternatives Format 7
      *> prints.
      *> Rejected from 2002: USAGE POINTER and Format 7 are COBOL-2002 introductions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB456N11.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P1 USAGE POINTER.
       01 N4 PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET P1 TO N4
           STOP RUN.
