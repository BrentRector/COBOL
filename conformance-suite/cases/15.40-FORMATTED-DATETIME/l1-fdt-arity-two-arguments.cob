*>--- conformance-case
*> id: l1-fdt-arity-two-arguments
*> rules: [FMT-15.40.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.40.2's general format is FUNCTION FORMATTED-DATETIME
      *> ( argument-1 argument-2 argument-3 [ argument-4 ] ): THREE arguments
      *> are required and only the fourth is bracketed. Two arguments is not
      *> a form the function has. The rules confirm it position by position -
      *> §15.40.3 r1 the format, r3 the date, r4 the time - and §15.40.4 r1
      *> makes the returned value a combination of BOTH values, so there is
      *> nothing for a two-argument reference to return.
      *> This is the reject side of 2023/l1_fdt_general_format, which pins
      *> the accepting arities: a catalog row reading 2..4 would satisfy
      *> every line of that golden and still be wrong here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FDTNEG2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(40).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss" 143951)
               TO R
           STOP RUN.
