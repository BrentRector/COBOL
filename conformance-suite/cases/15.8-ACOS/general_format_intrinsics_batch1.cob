*>--- conformance-case
*> id: general_format_intrinsics_batch1
*> rules: [FMT-15.8.2, FMT-15.15.2, FMT-15.18.2, FMT-15.20.2,
*>    FMT-15.24.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> The GENERAL FORMAT (FMT) rows of the traceability inventory that were adjudicated CONFORMS but had no
      *> SPEC-DERIVED test, so each stayed a GAP: FMT-15.8.2 ACOS, FMT-15.15.2 CHAR, FMT-15.18.2 CONCAT,
      *> FMT-15.20.2 COS, FMT-15.24.2 DAY-OF-INTEGER. A CONFORMS verdict is a claim that the grammar accepts
      *> EXACTLY the printed general format; this is that claim written as a test, so the row can close.
      *>
      *> Each call below is written in the form the standard prints - FUNCTION <name> ( argument-1 ) - and the
      *> expected value is derived from the function's own returned-value rule, never from what the compiler
      *> happens to emit:
      *>   15.8.4 r1  - ACOS returns the arccosine, 0 <= value <= pi.  arccos(1) = 0.
      *>   15.15.4 r1 - CHAR returns the character at the ORDINAL POSITION given by argument-1 in the
      *>                alphanumeric program collating sequence. Ordinal positions are 1-based, so under the
      *>                native sequence position 66 is the character with code 65, "A".
      *>   15.18.4 r1 - CONCAT returns all characters of argument-1 followed by all of argument-2; r4 repeats
      *>                that pairwise for a third argument.
      *>   15.20.4 r1 - COS returns the cosine, -1 <= value <= +1.  cos(0) = 1.
      *>   15.24.4 r2 - DAY-OF-INTEGER returns an integer of the form YYYYDDD; 15.5.2 fixes the integer date
      *>                form's starting date at Monday, 1 January 1601, so integer 1 is 1601001.
      *>
      *> ACOS and COS are the 15.4.1 floating-math family, whose result is an implementor-defined APPROXIMATION,
      *> so those two are pinned by an inclusive RANGE rather than by printing our own double back at ourselves
      *> - the PB7 convention. The exact cases (CHAR, CONCAT, DAY-OF-INTEGER) are pinned exactly.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GENFMTINTR1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R    PIC S9V9(6).
       01 C    PIC X.
       01 S    PIC X(6).
       01 D    PIC 9(7).
       PROCEDURE DIVISION.
      *> FMT-15.8.2 - arccos(1) = 0 exactly, and 15.8.4 bounds the result to [0, pi].
           COMPUTE R = FUNCTION ACOS ( 1 )
           IF R >= 0 AND R <= 0.000001 DISPLAY "ACOS=0"
              ELSE DISPLAY "ACOS=BAD" END-IF

      *> FMT-15.20.2 - cos(0) = 1 exactly, and 15.20.4 bounds the result to [-1, +1].
           COMPUTE R = FUNCTION COS ( 0 )
           IF R >= 0.999999 AND R <= 1 DISPLAY "COS=1"
              ELSE DISPLAY "COS=BAD" END-IF

      *> FMT-15.15.2 - the ordinal position is 1-based (15.15.4 r1).
           MOVE FUNCTION CHAR ( 66 ) TO C
           DISPLAY "CHAR=" C

      *> FMT-15.18.2 - two arguments, then the r4 pairwise repeat with a third.
           MOVE FUNCTION CONCAT ( "AB" "CD" ) TO S
           DISPLAY "CONCAT2=" S
           MOVE FUNCTION CONCAT ( "AB" "CD" "EF" ) TO S
           DISPLAY "CONCAT3=" S

      *> FMT-15.24.2 - 15.5.2's epoch makes integer 1 the first day of 1601.
           COMPUTE D = FUNCTION DAY-OF-INTEGER ( 1 )
           DISPLAY "DAYOFINT=" D
           STOP RUN.
