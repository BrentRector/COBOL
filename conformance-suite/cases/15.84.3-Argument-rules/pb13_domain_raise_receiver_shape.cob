*>--- conformance-case
*> id: pb13_domain_raise_receiver_shape
*> rules: [AR-15.84.3-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> A FUNCTION'S RETURNED VALUE MUST NOT DEPEND ON THE SHAPE OF ITS RECEIVER.
      *> 15.4 puts a returned value in a temporary elementary data item with the FUNCTION's own characteristics -
      *> nothing in 15.4 or 15.4.1 makes it a property of where the value is going. So the same function
      *> reference, with the same argument, must yield the same value whether it is COMPUTEd into an item or
      *> written bare as a relation operand.
      *>
      *> ⛔ THIS GOLDEN EXISTS BECAUSE PB13 BROKE THAT INVARIANT AND A REVIEW REFUTER CAUGHT IT THE SAME DAY.
      *> PB13 made a RECEIVER-LESS float-family render keep its binary64 value instead of quantizing through
      *> CobolIntrinsics#FromDouble - which was correct, and which also silently moved the EC-ARGUMENT-FUNCTION
      *> raise site out from under that arm, because FromDouble is where an out-of-domain NaN became the 15.3
      *> default result. The two shapes then disagreed:
      *>     COMPUTE R = FUNCTION ACOS(2)   -> the 15.3 default 0     (FromDouble screened the NaN)
      *>     IF FUNCTION ACOS(2) = 0        -> FALSE, a raw NaN       (nothing screened it)
      *> and under EC-ARGUMENT-FUNCTION checking the receiver-less form raised NOTHING AT ALL, which 14.6.13.1
      *> requires. CobolIntrinsics#RealResult restores the screen on the unquantized arms without re-quantizing.
      *>
      *> ACOS(2) is out of domain by 15.8.3 r2 (argument-1 shall be >= -1 and <= +1); the double result is NaN.
      *> (⚠ r2, NOT r1: 15.8.3 r1 is "Argument-1 shall be of class numeric", a different rule. This comment read
      *>  "r1" until 2026-09-02 while quoting r2's text verbatim - the inherited-citation shape CLAUDE.md rule 1
      *>  names, where --check passes on the TEXT and only the NUMBER is wrong. Both re-derived:
      *>  cite.py --check 15.8.3 "The value of argument-1 shall be greater than or equal to" -> OK 15.8.3 2);
      *>  cite.py --check 15.8.3 "Argument-1 shall be of class numeric." -> OK 15.8.3 1).)
      *> 15.3's closing paragraph makes the value implementor-defined while checking is disabled - this golden
      *> pins that the two SHAPES AGREE, which is the part the standard does fix, not the particular default.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB13DOMAIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(9)V9(9).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION ACOS(2)
           IF R = 0
              DISPLAY "WITH-RECEIVER=DEFAULT"
           ELSE
              DISPLAY "WITH-RECEIVER=OTHER"
           END-IF
           IF FUNCTION ACOS(2) = 0
              DISPLAY "RECEIVERLESS=DEFAULT"
           ELSE
              DISPLAY "RECEIVERLESS=OTHER"
           END-IF
      *> The same question through SQRT of a negative (15.84.3 r2 - "The value of argument-1 shall be zero or
      *> positive"; r1 is the class-numeric rule), which reaches NaN by a different body.
           IF FUNCTION SQRT(-1) = 0
              DISPLAY "SQRT-NEG=DEFAULT"
           ELSE
              DISPLAY "SQRT-NEG=OTHER"
           END-IF
      *> ⚠ AND THE IN-DOMAIN CASE MUST STILL BE UNQUANTIZED - the screen must not undo PB13. EXP10(30) and
      *> EXP10(31) are a factor of ten apart and both exceed what the old ws=9 quantization could represent.
           IF FUNCTION EXP10(30) = FUNCTION EXP10(31)
              DISPLAY "EXP10-DISTINCT=NO"
           ELSE
              DISPLAY "EXP10-DISTINCT=YES"
           END-IF
           STOP RUN.
