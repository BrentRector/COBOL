*>--- conformance-case
*> id: pb74_sdidi_store_size_error
*> rules: [GR-14.7.5-3, GR-14.7.5-7]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB74 - the SDIDI final transfer under ON SIZE ERROR / EC-SIZE checking (ISO 14.7.5). Case 3: "if,
      *> after radix point alignment and any applicable rounding specifications, the result of an arithmetic
      *> statement is further from zero than permitted for the associated resultant data item" the size
      *> error condition exists; storing rule 1: the resultant items REMAIN UNCHANGED and control goes to the
      *> imperative; no-phrase rule 4 names the condition EC-SIZE-TRUNCATION. The defect: CobolDec.ToUnscaled's
      *> widening arm kept "only the digits a <=38-digit store could use" - 0 for 10**100 - and
      *> TryStore(CobolDec) capacity-checked THAT 0, so 10 ** 100 into PIC 9(5) ran NOT ON SIZE ERROR and
      *> overwrote the receiver with 0. The checked transfer (ToUnscaledChecked) now raises; the unchecked
      *> MOVE/no-phrase transfer keeps its low-order-digit disposition.
      *> P100/P40/P37/PE100/PF40: overflow legs - SE fires and X5 / Y5 / ED keep their prior contents.
      *> P30: the control that always worked (10**30 fits Int128 at ws 0 and trips the capacity check).
      *> P4: a fitting value takes NOT ON SIZE ERROR and stores. PROH: a ROUNDED MODE PROHIBITED inexact
      *> transfer to an EDITED receiver latches EC-SIZE-TRUNCATION (14.7.4.3 r7) - it latched the default
      *> EC-SIZE-OVERFLOW; ECOV: the case-3 overflow latches EC-SIZE-TRUNCATION (no-phrase rule 4).
      >>TURN EC-SIZE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB74SDSTORE.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X5   PIC 9(5) VALUE 12345.
       01 Y5   PIC 9(5)V99 VALUE 12345.67.
       01 ED   PIC ZZZZ9.99 VALUE 4.5.
       PROCEDURE DIVISION.
           COMPUTE X5 = 10 ** 100
               ON SIZE ERROR DISPLAY "P100=SE X5=" X5
               NOT ON SIZE ERROR DISPLAY "P100=NOSE X5=" X5
           END-COMPUTE.
           COMPUTE X5 = 10 ** 40
               ON SIZE ERROR DISPLAY "P40=SE X5=" X5
               NOT ON SIZE ERROR DISPLAY "P40=NOSE X5=" X5
           END-COMPUTE.
           COMPUTE X5 = 10 ** 30
               ON SIZE ERROR DISPLAY "P30=SE X5=" X5
               NOT ON SIZE ERROR DISPLAY "P30=NOSE X5=" X5
           END-COMPUTE.
           COMPUTE Y5 = 10 ** 37
               ON SIZE ERROR DISPLAY "P37=SE Y5=" Y5
               NOT ON SIZE ERROR DISPLAY "P37=NOSE Y5=" Y5
           END-COMPUTE.
           COMPUTE ED = 10 ** 100
               ON SIZE ERROR DISPLAY "PE100=SE ED=" ED
               NOT ON SIZE ERROR DISPLAY "PE100=NOSE ED=" ED
           END-COMPUTE.
           COMPUTE X5 = FUNCTION NUMVAL-F("1E+40")
               ON SIZE ERROR DISPLAY "PF40=SE X5=" X5
               NOT ON SIZE ERROR DISPLAY "PF40=NOSE X5=" X5
           END-COMPUTE.
           COMPUTE X5 = 10 ** 4
               ON SIZE ERROR DISPLAY "P4=SE X5=" X5
               NOT ON SIZE ERROR DISPLAY "P4=NOSE X5=" X5
           END-COMPUTE.
           COMPUTE ED ROUNDED MODE IS PROHIBITED = 0.355
               ON SIZE ERROR DISPLAY "PROH=" FUNCTION EXCEPTION-STATUS
               NOT ON SIZE ERROR DISPLAY "PROH=NOSE"
           END-COMPUTE.
           DISPLAY "ED=" ED.
           COMPUTE Y5 = 10 ** 60
               ON SIZE ERROR DISPLAY "ECOV=" FUNCTION EXCEPTION-STATUS
               NOT ON SIZE ERROR DISPLAY "ECOV=NOSE"
           END-COMPUTE.
           STOP RUN.
       END PROGRAM PB74SDSTORE.
