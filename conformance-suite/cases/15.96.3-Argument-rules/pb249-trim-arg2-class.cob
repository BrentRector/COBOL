*>--- conformance-case
*> id: pb249-trim-arg2-class
*> rules: [AR-15.96.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB249 - ISO 15.96.3 r2's SECOND sentence at the repeating tail: "When argument-1 is class
      *> alphabetic or alphanumeric, argument-2 shall be a single character that is either class alphabetic or
      *> class alphanumeric. When argument-1 is class national, argument-2 shall be a single character of class
      *> national." Argument-1 here is class alphanumeric and the SECOND argument-2 is a national literal, which
      *> neither sentence admits. The cross-argument screen reached the declared positions only (the schema had
      *> no tail), so this bound clean and mixed the two families inside one delete set.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGTRIMARG2C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X3    PIC X(3) VALUE "ABC".
       01 A20   PIC X(20).
       PROCEDURE DIVISION.
           MOVE FUNCTION TRIM(X3 "A" N"X") TO A20.
           STOP RUN.
