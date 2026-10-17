*>--- conformance-case
*> id: pb249-trim-arg2-width
*> rules: [AR-15.96.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB249 - ISO 15.96.3 r2: "argument-2 shall be a SINGLE CHARACTER that is either class alphabetic
      *> or class alphanumeric". 15.96.2 repeats argument-2, and r2 is one sentence about argument-2 - so it
      *> governs the SECOND one exactly as it governs the first. The schema carried the ExactWidth(1) predicate
      *> on the declared position 2 only and had no variadic tail, so FUNCTION TRIM(X3 "A" "BC") bound clean and
      *> the runtime silently kept 'B' and discarded 'C'. The tail predicate rejects it here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGTRIMARG2W.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X3    PIC X(3) VALUE "ABC".
       01 A20   PIC X(20).
       PROCEDURE DIVISION.
           MOVE FUNCTION TRIM(X3 "A" "BC") TO A20.
           STOP RUN.
