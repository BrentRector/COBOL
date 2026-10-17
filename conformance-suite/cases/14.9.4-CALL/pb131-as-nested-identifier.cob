*>--- conformance-case
*> id: pb131-as-nested-identifier
*> rules: [SR-14.9.4.3-15]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.3 SR15 sentence 1: if the NESTED phrase is specified, literal-1 shall be specified.
      *> An identifier target cannot name a contained program at compile time (kb/Work PB131 pinned the
      *> long-enforced check; the diagnostic moved off the 0899 staging code to COBOLNET1676).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB131NI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-NAME PIC X(8) VALUE "INNER".
       PROCEDURE DIVISION.
       MAIN.
           CALL W-NAME AS NESTED
           STOP RUN.
      *> a contained program exists, so only the identifier spelling is at fault
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER-P.
       PROCEDURE DIVISION.
       P.
           EXIT PROGRAM.
       END PROGRAM INNER-P.
       END PROGRAM PB131NI.
