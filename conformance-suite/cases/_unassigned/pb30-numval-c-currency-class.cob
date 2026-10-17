*>--- conformance-case
*> id: pb30-numval-c-currency-class
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.68.3 r1: "Argument-1 shall be of category alphanumeric or
      *> national." r2: "Argument-2, if specified, shall be of the same class as
      *> argument-1." A numeric argument-2 is neither.
      *>
      *> NUMVAL-C is one of the eight functions PB30 found absent from the
      *> screen, and one of the eight whose bespoke binder RETURNS BEFORE the
      *> generic path reaches CheckArgumentClasses — so no table row could have
      *> screened it until the screen was called from the binder itself.
      *> The screen runs BEFORE the 15.68.3 r3 default-currency injection, so a
      *> one-argument call is never reported against an argument-2 the source
      *> does not contain.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB30NEGNVC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC S9(9)V99.
       PROCEDURE DIVISION.
           COMPUTE N = FUNCTION NUMVAL-C("#1" 5)
           STOP RUN.
