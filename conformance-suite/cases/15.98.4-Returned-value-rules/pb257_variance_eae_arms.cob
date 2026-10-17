*>--- conformance-case
*> id: pb257_variance_eae_arms
*> rules: [RV-15.98.4-1]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB257 - FUNCTION VARIANCE's equivalent arithmetic expression, ISO 15.98.4 r1, under a STANDARD
      *> arithmetic mode, where 15.4.1 makes the obligation exact: "the returned value shall EQUAL the value of
      *> the equivalent arithmetic expression". The rule has THREE lettered arms and only arm (c) was ever
      *> measured, in one of the two carriers; arms (a) and (b) rested on code reading.
      *>   a) one occurrence                   -> (0)
      *>   b) two                              -> ((x1 - MEAN)**2 + (x2 - MEAN)**2) / 2
      *>   c) n                                -> (SUM((xi - MEAN)**2 ...)) / n
      *> MEAN is taken over the VARIANCE call's own argument list (15.60.4 r1), and 8.8.1.5.4 rule 2b makes
      *> `operand ** 2` exactly `(operand * operand)`, so the evaluation is the expression's own operation tree.
      *>   VA1 = FUNCTION VARIANCE(5)        arm (a): literally (0)                            -> 0 (DISPLAYed 000000: 9V9(5), implied point)
      *>   VA2 = FUNCTION VARIANCE(1 3)      arm (b): MEAN = 2; ((1-2)**2 + (3-2)**2)/2 = 1 (100000)
      *>   VA3 = FUNCTION VARIANCE(1 2 3)    arm (c): MEAN = 2; (1 + 0 + 1)/3 = 2/3, stored
      *>                                     truncating into 9V9(5)                            -> 0.66666 (066666)
      *>   SD2 = FUNCTION STANDARD-DEVIATION(1 3)   15.86.4 r1's EAE is literally
      *>                                     (FUNCTION SQRT (FUNCTION VARIANCE (argument-list))) =
      *>                                     SQRT(1) = 1 (100000)
      *> The two clause numbers heading these bodies used to be another function's - 15.97 is UPPER-CASE and
      *> 15.85 is STANDARD-COMPARE - and the wrong number had propagated from the code comment into a golden's
      *> own header. VARIANCE is 15.98 and STANDARD-DEVIATION is 15.86.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB257VARDEC.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 VA1 PIC 9V9(5).
       01 VA2 PIC 9V9(5).
       01 VA3 PIC 9V9(5).
       01 SD2 PIC 9V9(5).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE VA1 = FUNCTION VARIANCE(5)
           DISPLAY "VA1=" VA1
           COMPUTE VA2 = FUNCTION VARIANCE(1, 3)
           DISPLAY "VA2=" VA2
           COMPUTE VA3 = FUNCTION VARIANCE(1, 2, 3)
           DISPLAY "VA3=" VA3
           COMPUTE SD2 = FUNCTION STANDARD-DEVIATION(1, 3)
           DISPLAY "SD2=" SD2
           STOP RUN.
