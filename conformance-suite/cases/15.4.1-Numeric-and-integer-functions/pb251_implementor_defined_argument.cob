*>--- conformance-case
*> id: pb251_implementor_defined_argument
*> rules: [RV-15.4.1-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB251 (row RV-15.4.1-2) - an implementor-defined ARGUMENT flows into the enclosing equivalent
      *> arithmetic expression UNCHANGED, and the enclosing function's own rule then applies to it.
      *> 15.4.1 NOTE 1 item 2 makes an EAE's result implementor-defined when one or more of its ARGUMENTS are.
      *> That is a LATITUDE statement, so the only way to contradict it is to OVER-DEFINE such a result - to
      *> narrow, quantize or replace the argument's implementor-defined value on the way in. Every leg below is
      *> therefore a DETERMINISTIC consequence of the argument's own DOMAIN rule, which is exactly what an
      *> intake that preserved the value must produce and an intake that narrowed it need not.
      *>
      *> The two numeric sources of an implementor-defined VALUE (not merely an implementor-defined
      *> representation, which a COMP-2 item has and this rule is not about):
      *>   RANDOM               15.75.4 r1 - "The returned value is greater than or equal to zero and less
      *>                        than one"; r3 leaves the distinct-sequence subset to the implementor.
      *>   SECONDS-PAST-MIDNIGHT 15.80.3 r2 - the current local time expressed in seconds past midnight;
      *>                        r3 leaves the PRECISION to the implementor, and r4 admits a value >= 86400
      *>                        only under the LEAP-SECOND directive with ON, which this program does not use.
      *>
      *> IPR: INTEGER-PART is this rule's own shape - 15.49.4 r1 IS an equivalent arithmetic expression,
      *>      "(FUNCTION SIGN (argument-1) * FUNCTION INTEGER (FUNCTION ABS (argument-1)))", so NOTE 1 item 2
      *>      applies to it directly. Over [0,1) that expression is 0 for every value in the domain: SIGN is 0
      *>      or 1 and INTEGER(ABS(v)) is 0. An implementor-defined argument, a determined result.
      *> INR: 15.44.4 r1 - "the greatest integer less than or equal to the value of argument-1" - likewise 0.
      *> MXR/MNR: 15.59.4 r1 / 15.63.4 r1 are pure SELECTION ("the content of the argument-1 having the
      *>      greatest [least] value"), so the returned value IS one argument's content - the plainest form of
      *>      the propagation this rule describes - and a bound outside the domain is chosen whatever the draw.
      *> MXS/MNS: the same selection over the seconds domain [0, 86400).
      *> SPM: INTEGER-PART of the seconds value lands in [0, 86399] - the enclosing integer function neither
      *>      widened the domain nor collapsed it to a constant.
      *> ⛔ WHY THIS PAGE EXISTS. RV-15.4.1-2 was adjudicated from ONE of the renderer's argument intakes, on
      *> the stated premise that it was the only one, and the sentence that settled it - "it renders an argument
      *> WITHOUT redefining its value" - is true of RawArg and FALSE of Arg, the intake that lands an SDIDI
      *> operand at a compile-time working scale. Every intake now declares its value contract in the renderer
      *> and IntrinsicArgumentIntakeContractDriftTests holds that true; these legs are the runtime half.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB251IDARG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R    PIC S9(9)V9(9).
       01 E    PIC -(9)9.9(9).
       01 S    PIC S9(9).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION INTEGER-PART(FUNCTION RANDOM).
           MOVE R TO E.
           DISPLAY "IPR=" E.
           COMPUTE R = FUNCTION INTEGER(FUNCTION RANDOM).
           MOVE R TO E.
           DISPLAY "INR=" E.
           COMPUTE R = FUNCTION MAX(FUNCTION RANDOM, 2).
           MOVE R TO E.
           DISPLAY "MXR=" E.
           COMPUTE R = FUNCTION MIN(FUNCTION RANDOM, -1).
           MOVE R TO E.
           DISPLAY "MNR=" E.
           COMPUTE R = FUNCTION MAX(FUNCTION SECONDS-PAST-MIDNIGHT, 86400).
           MOVE R TO E.
           DISPLAY "MXS=" E.
           COMPUTE R = FUNCTION MIN(FUNCTION SECONDS-PAST-MIDNIGHT, -1).
           MOVE R TO E.
           DISPLAY "MNS=" E.
           COMPUTE S = FUNCTION INTEGER-PART(FUNCTION SECONDS-PAST-MIDNIGHT).
           IF S >= 0 AND S <= 86399
               DISPLAY "SPM=OK"
           ELSE
               DISPLAY "SPM=" S
           END-IF.
           STOP RUN.
       END PROGRAM PB251IDARG.
