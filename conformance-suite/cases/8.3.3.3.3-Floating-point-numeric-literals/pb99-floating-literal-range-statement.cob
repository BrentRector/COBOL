*>--- conformance-case
*> id: pb99-floating-literal-range-statement
*> rules: [SR-8.3.3.3.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> THE STATEMENT LANE of 8.3.3.3.3 r3's implementor-defined exponent range, on its own (kb/Work PB276).
      *>
      *> r3: "The maximum permitted value and minimum permitted value of the exponent is implementor-defined".
      *> For a literal in a STATEMENT the literal is its exact 8.3.3.3.3 rule-5 value carried on the SDIDI in
      *> every arithmetic mode, so COBOL.NET's determination (CONFORMANCE.md 7, A.1 item 82) is the decimal128
      *> range, about 1E-6176 to 9.99E+6144.  1.0E+9999 and 1.0E-9999 are past it - and are the widest a legal
      *> literal can be, since SR3 caps the exponent at four digits.  1.0E+400 is INSIDE that range and compiles
      *> (pb156_float_literal_exact_native pins it); before owner decision D-B it was a hard COBOLNET1661 under
      *> native arithmetic and legal under STANDARD-DECIMAL.
      *>
      *> ⛔ WHY THIS IS A SEPARATE FIXTURE. The harness asserts only that SOME diagnostic of the compilation
      *> contains COBOLNET1661.  While these three statements shared a file with the VALUE-clause entries F1-F4,
      *> those entries alone satisfied that assertion and the statement lane was never independently witnessed -
      *> a green negative that would have stayed green if CheckLiteral's screen were deleted outright.  The
      *> VALUE-clause half is pb99-floating-literal-range.cob; nothing in THIS file has a VALUE clause.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB99NRS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F4 USAGE FLOAT-LONG.
       01 N1 PIC 9(5)V99.
       PROCEDURE DIVISION.
           MOVE 1.0E+9999 TO F4
           COMPUTE N1 = 1.0E-9999 * 2
           IF F4 > 1.0E+9999 DISPLAY "X" END-IF
           STOP RUN.
