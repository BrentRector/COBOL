*>--- conformance-case
*> id: pb331-read-ignoring-with-lock
*> rules: [SR-14.9.30.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.30.3 SR3: "The LOCK phrase shall not be specified in the same READ
      *> statement as the IGNORING LOCK phrase."
      *> !! NOTHING IMPLEMENTED THIS RULE BEFORE kb/Work PB331. The pair was unwritable
      *> only because the grammar had merged 14.9.30.2's TWO printed lock brackets into
      *> one slot - a collapse that ALSO rejected the LEGAL pair IGNORING LOCK WITH NO
      *> LOCK, which 14.9.30.4 GR11 b)/d) distinguish by naming "the NO LOCK phrase" and
      *> "the LOCK phrase" as different phrases. Splitting the brackets is what makes
      *> this file necessary: without the COBOLNET1818 check the split would have opened
      *> a silent hole. 2002/pb331_read_lock_brackets is its permitted-pair twin.
      *> The rule is edition-invariant, hence all four editions; below 2002 the phrases
      *> additionally draw the COBOLNET0900 introduction gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB331SR3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQF ASSIGN TO "pb331sr3.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD SQF.
       01 SQ-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT SQF.
           READ SQF NEXT RECORD IGNORING LOCK WITH LOCK
               AT END CONTINUE
           END-READ.
           CLOSE SQF.
           STOP RUN.
