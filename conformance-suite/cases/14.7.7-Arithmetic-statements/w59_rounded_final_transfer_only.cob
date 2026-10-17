*>--- conformance-case
*> id: w59_rounded_final_transfer_only
*> rules: [GR-14.7.7-3]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.7.7 GR3 (kb/Work R43 / PB1151, the LIVE half of GR-14.7.7-3): "When standard-decimal or
      *> standard-binary arithmetic is in effect, each arithmetic statement is defined in terms of one or more
      *> arithmetic expressions. Unless specified otherwise in rules, transfer of data from an intermediate form
      *> into a resultant-identifier shall be according to the specifications in 14.6.8" — and its NOTE: "The
      *> ROUNDED phrase applies only to this transfer of data". The standard-binary half is declined (Annex A.3
      *> item 2, COBOLNET0806); this program runs the standard-decimal half.
      *> EXPECTED VALUES, from the rule. Under STANDARD-DECIMAL the intermediate is a 34-digit decimal rounded
      *> by the implied INTERMEDIATE ROUNDING mode (§11.9.11.2 GR3 a: "If the INTERMEDIATE ROUNDING clause is
      *> not specified, the NEAREST-AWAY-FROM-ZERO phrase is implied"): 1 / 3 is
      *> 0.3333333333333333333333333333333333 and times 3 is 0.9999999999999999999999999999999999.
      *>  - WS-R ROUNDED (PIC 9V9): the ONE transfer is rounded -> 1.0, displayed "10".
      *>    Had ROUNDED reached the intermediate 1 / 3 (-> 0.3, times 3 -> 0.9), it would display "09".
      *>  - WS-T without ROUNDED: the transfer truncates (§14.6.8) -> 0.9, displayed "09".
      *>  - 2 / 3 * 3 ROUNDED: 0.6666666666666666666666666666666667 times 3 is
      *>    2.0000000000000000000000000000000001, rounded into PIC 9V9 -> 2.0, displayed "20"; an intermediate
      *>    rounded to the receiver's one decimal place (0.7) would give 2.1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W59RNDFT.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-R PIC 9V9.
       01 WS-T PIC 9V9.
       01 WS-U PIC 9V9.
       PROCEDURE DIVISION.
           COMPUTE WS-R ROUNDED = 1 / 3 * 3.
           COMPUTE WS-T = 1 / 3 * 3.
           COMPUTE WS-U ROUNDED = 2 / 3 * 3.
           DISPLAY "R=" WS-R " T=" WS-T " U=" WS-U.
           STOP RUN.
