*>--- conformance-case
*> id: pb458-set-size-literal-over-limit-bare
*> rules: [SR-14.9.39.3-34]
*> source-format: free
*> portable: unverified
*>---
      *> The SIZE-OF-absent arm of the SAME rule as pb458-set-size-literal-over-limit. ISO 14.9.39.2 Format 16 is
      *> SET [ SIZE OF ] data-name-3 TO { integer-2 | arithmetic-expression-5 }: SIZE OF is a BRACKET, so both
      *> spellings are the same format and 14.9.39.3 SR34 governs both - "Integer-2 shall be non-negative, and
      *> shall be equal to or less than the maximum size of data-name-3, as specified in 8.5.1.10."
      *> ⛔ ONE RULE, TWO ARMS, AND THE SECOND ARM IS THE ONE THAT GOES MISSING (feedback_two_arm_dispatch). The
      *> bare arm was a separate peek with its own receiver resolution, which is how it came to drop a QUALIFIED
      *> receiver as well; this fixture pins that the screen is now written once, for both. kb/Work PB458.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB458N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D PIC X DYNAMIC LENGTH LIMIT IS 8.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ABC" TO D
           SET D TO 99
           DISPLAY D
           STOP RUN.
