*>--- conformance-case
*> id: pb45_function_in_when_object
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.13 - A FUNCTION-IDENTIFIER AS AN EVALUATE SELECTION OBJECT
      *> (fix-queue PB45, the open half). 8.4.3.1.2 Format 1 makes a function-
      *> identifier an identifier, and 14.9.13.2 writes the selection object as
      *> identifier-2 / arithmetic-expression-2 / condition-2 - all sending, so
      *> 8.4.3.2.3 SR1 (receiving only) does not bar it.
      *>
      *> IT WAS THE GRAMMAR'S ARITY, NOT THE LEXER AND NOT THE ALTERNATIVE ORDER.
      *> evaluateWhenGroup was `NOT? evaluateWhenItem+`, but 14.9.13.2's format is
      *> `{ { WHEN selection-object [ ALSO selection-object ] ... } ... }` - objects
      *> repeat ONLY through ALSO, never by juxtaposition (SR2 fixes the count
      *> against the subjects). That unlicensed `+` gave `WHEN FUNCTION SQRT(W-Z) > 1`
      *> a second reading: take `FUNCTION SQRT` as a bare zero-argument object and
      *> re-read the ARGUMENT PARENTHESIS as a second object `(W-Z) > 1`. The correct
      *> reading cannot consume the trailing `> 1` once the item ends, so only the
      *> peel survived - and it bound as a VALUE object under an EVALUATE TRUE
      *> subject: a CLEAN COMPILE that threw at RUN TIME. For an alphanumeric
      *> function (T3) the same peel was a raw parse error instead.
      *> `FUNCTION PI > 1` (T4) always worked - it has no parenthesis to peel, which
      *> is what identified the argument parenthesis as the discriminator.
      *>
      *> WHY BOTH BRANCHES OF EVERY TEST ARE EXERCISED: a WHEN object that wrongly
      *> matches EVERYTHING passes a one-branch golden. T2 is the false case of T1.
      *> T6-T10 pin the arms an alternative REORDER would silently retarget
      *> (14.9.13.4 Table 15 makes the object's legality depend on the SUBJECT), so a
      *> future "just put condition before valueOperand" edit fails here.
      *>
      *> FUNCTION SQRT(4) = 2 throughout.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB45WHENOBJ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-Z    PIC 9(4) VALUE 4.
       01 W-K    PIC 9    VALUE 3.
       01 W-P    PIC X(2) VALUE "ab".
       01 W-FLAG PIC X    VALUE "Y".
           88 W-IS-YES    VALUE "Y".
       PROCEDURE DIVISION.
       MAIN.
      *> T1 - the defect: a numeric function-identifier as a relation's left operand.
           EVALUATE TRUE
               WHEN FUNCTION SQRT(W-Z) > 1
                   DISPLAY "T1=cond"
               WHEN OTHER
                   DISPLAY "T1=other"
           END-EVALUATE.
      *> T2 - the SAME shape that must be FALSE. 2 > 9 is false.
           EVALUATE TRUE
               WHEN FUNCTION SQRT(W-Z) > 9
                   DISPLAY "T2=cond"
               WHEN OTHER
                   DISPLAY "T2=other"
           END-EVALUATE.
      *> T3 - alphanumeric function: this shape was a PARSE ERROR, not a wrong value.
           EVALUATE TRUE
               WHEN FUNCTION UPPER-CASE(W-P) = "AB"
                   DISPLAY "T3=cond"
               WHEN OTHER
                   DISPLAY "T3=other"
           END-EVALUATE.
      *> T4 - the control: no argument parenthesis, so it always worked.
           EVALUATE TRUE
               WHEN FUNCTION PI > 1
                   DISPLAY "T4=cond"
               WHEN OTHER
                   DISPLAY "T4=other"
           END-EVALUATE.
      *> T5 - ALSO, the repetition 14.9.13.2 DOES license (one object per subject).
           EVALUATE TRUE ALSO TRUE
               WHEN FUNCTION SQRT(W-Z) > 1 ALSO W-Z = 4
                   DISPLAY "T5=both"
               WHEN OTHER
                   DISPLAY "T5=other"
           END-EVALUATE.
      *> T6 - a VALUE subject with a function-identifier object: GR5b equality, NOT a
      *> condition. SQRT(16) = 4 = W-Z. A reorder putting condition first breaks this.
           EVALUATE W-Z
               WHEN FUNCTION SQRT(16)
                   DISPLAY "T6=eq"
               WHEN OTHER
                   DISPLAY "T6=other"
           END-EVALUATE.
      *> T7 - a bare condition-name object under EVALUATE TRUE stays a CONDITION
      *> (8.8.4.1.2), the boundary case between the valueOperand and condition arms.
           EVALUATE TRUE
               WHEN W-IS-YES
                   DISPLAY "T7=88"
               WHEN OTHER
                   DISPLAY "T7=other"
           END-EVALUATE.
      *> T8 - a THRU range object still binds as a range, not two objects.
           EVALUATE W-K
               WHEN 1 THRU 5
                   DISPLAY "T8=range"
               WHEN OTHER
                   DISPLAY "T8=other"
           END-EVALUATE.
      *> T9 - a function-identifier as a range BOUND (range-expression admits
      *> arithmetic-expression-3). 2 THRU 5 contains 3.
           EVALUATE W-K
               WHEN FUNCTION SQRT(W-Z) THRU 5
                   DISPLAY "T9=range-fn"
               WHEN OTHER
                   DISPLAY "T9=other"
           END-EVALUATE.
      *> T10 - the group-level NOT still negates one object.
           EVALUATE W-K
               WHEN NOT 9
                   DISPLAY "T10=not"
               WHEN OTHER
                   DISPLAY "T10=other"
           END-EVALUATE.
      *> T11 - ANY.
           EVALUATE W-K
               WHEN ANY
                   DISPLAY "T11=any"
           END-EVALUATE.
      *> T12 - an AND-chain whose FIRST operand is the function-identifier.
           EVALUATE TRUE
               WHEN FUNCTION SQRT(W-Z) > 1 AND W-Z < 9
                   DISPLAY "T12=and"
               WHEN OTHER
                   DISPLAY "T12=other"
           END-EVALUATE.
           STOP RUN.
