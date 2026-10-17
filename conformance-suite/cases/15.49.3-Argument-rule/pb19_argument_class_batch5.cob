*>--- conformance-case
*> id: pb19_argument_class_batch5
*> rules: [AR-15.49.3-1, AR-15.56.3-1, RV-15.57.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB19 - the 15.3 argument-CLASS screen for the 15.45-15.57 batch. Every function below was absent from
      *> IntrinsicArgumentRules.Verified, so CheckArgumentClasses returned at its TryGetValue guard and the rule
      *> was enforced NOWHERE. This golden pins the LEGAL side; the rejections are negative fixtures.
      *>
      *> ⛔ THE REF-MODIFIED BOOLEAN IS THE POINT OF THIS FILE. 15.45.3 r1 is "Argument-1 shall be of class
      *> boolean" - the only rule in the catalogue naming that class - and Annex D's own worked example passes a
      *> REFERENCE-MODIFIED bit item. 8.4.3.3.4 GR6 preserves the category, so the slice is still class boolean.
      *> Under the rule that stood until PB20 (every ref-mod result typed class ALPHANUMERIC, on the authority of
      *> "8.4.2.4" - a clause that DOES NOT EXIST) this screen REJECTED the standard's own sample program. That
      *> was verified by temporarily restoring the old rule, not assumed: PB20 had to land first.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB19CLASS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BITS PIC 1(8) VALUE B"10101010".
       01 N    PIC 9(8) VALUE 20210616.
       01 D    PIC 9(7) VALUE 2021167.
       01 F    PIC S9(5)V99 VALUE -12.75.
       01 T    PIC X(12) VALUE "mIxEd CaSe".
       01 R    PIC S9(9)V9(4).
       01 I    PIC 9(9).
       01 S    PIC X(12).
       PROCEDURE DIVISION.
      *> 15.45.3 r1 - class boolean, plain and REFERENCE-MODIFIED (the Annex D shape).
           COMPUTE I = FUNCTION INTEGER-OF-BOOLEAN(BITS)
           DISPLAY "BOOL=" I
           COMPUTE I = FUNCTION INTEGER-OF-BOOLEAN(BITS(1:6))
           DISPLAY "BOOL-REFMOD=" I
      *> 15.46.3 r1 / 15.47.3 r1 - an integer argument.
           COMPUTE I = FUNCTION INTEGER-OF-DATE(N)
           DISPLAY "IOD=" I
           COMPUTE I = FUNCTION INTEGER-OF-DAY(D)
           DISPLAY "IODAY=" I
      *> 15.49.3 r1 - class numeric, and a NEGATIVE value to prove INTEGER-PART truncates toward zero.
           COMPUTE R = FUNCTION INTEGER-PART(F)
           DISPLAY "IPART=" R
      *> 15.55.3 r1 / 15.56.3 r1 - class numeric.
           COMPUTE R = FUNCTION LOG(1)
           DISPLAY "LOG1=" R
           COMPUTE R = FUNCTION LOG10(1000)
           DISPLAY "LOG10-1000=" R
      *> 15.57.3 r1 / 15.97.3 r1 - class alphabetic, alphanumeric or national.
           MOVE FUNCTION LOWER-CASE(T) TO S
           DISPLAY "LOWER=[" S "]"
           MOVE FUNCTION UPPER-CASE(T) TO S
           DISPLAY "UPPER=[" S "]"
           STOP RUN.
