      *>--- conformance-case
      *> id: l1_native_arithmetic_intermediate
      *> rules: [GR-11.9.5.2-1, DOC-A.1-123]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 8.8.1.3 Native arithmetic - the TECHNIQUES and the
      *> INTERMEDIATE DATA ITEM (Annex A.1 item 123; docs/CONFORMANCE.md
      *> row 123).
      *> 8.8.1.3: "Native arithmetic is an implementor-defined method of
      *> evaluating an arithmetic expression, an arithmetic statement,
      *> the SUM clause, and all integer and numeric functions. Native
      *> arithmetic is in effect when the ARITHMETIC IS NATIVE clause is
      *> specified in the OPTIONS paragraph or no ARITHMETIC clause is
      *> specified. The implementor shall specify techniques used for
      *> native arithmetic."  14.7.7 rule 4a names what is being
      *> specified: "If any form of standard arithmetic is in effect, a
      *> standard intermediate data item of the form appropriate to that
      *> mode of arithmetic is used. Otherwise, an implementor-defined
      *> intermediate data item is used."  COBOL.NET's is a scaled
      *> Int128 - a 128-bit two's-complement unscaled value with a
      *> compile-time decimal scale.
      *>
      *> WHY THESE VALUES MEASURE THAT AND NOT SOMETHING WEAKER. 14.7.7
      *> rule 2a fixes the domain the standard REQUIRES native
      *> arithmetic to handle for ADD, DIVIDE, MULTIPLY and SUBTRACT
      *> when no operand is an intrinsic function, a binary- or float-
      *> usage item, or a floating-point literal: "the composite of
      *> operands shall not contain more than 31 digits", the composite
      *> being "a hypothetical data item resulting from the
      *> superimposition of specified operands in a statement aligned on
      *> their decimal points". Every statement below sits exactly ON
      *> that 31-digit boundary with none of the excluded operand kinds,
      *> so an implementation whose intermediate cannot hold the
      *> boundary is non-conforming rather than merely different - and
      *> the values below are the arithmetic identities, not a rendering
      *> of the carrier.
      *> MUL31 (10**16 - 1) * (10**15 - 1) = 10**31 - 11*10**15 + 1, a
      *>       31-digit exact product; composite 16/15/31 = 31 digits. A
      *>       64-bit intermediate wraps; a 28-digit decimal one
      *>       overflows (its ceiling is about 7.9E+28); binary64 loses
      *>       every digit past the 17th. Only an exact carrier of at
      *>       least 104 bits answers 9999999999999989000000000000001.
      *> MULSC the SCALE half: 999999999999.999999 * 999999.9999,
      *>       composite 18 integer plus 10 fraction = 28 digits,
      *>       product exact at 999999999899999999.0000000001. An
      *>       intermediate that carried the value as a float, or that
      *>       rescaled before multiplying, cannot end in ...0000000001.
      *> ADD31 the aligned-sum half at the same boundary: (10**31 - 2) +
      *>       1, composite 31 digits.
      *> The ARITHMETIC clause is written explicitly so the file names
      *> the mode it measures; 11.9.5.2 GR4 ("If the ARITHMETIC clause
      *> is not specified in this source element or a containing source
      *> element, it is as if the ARITHMETIC clause were specified with
      *> the NATIVE phrase") makes the no-clause default the same mode,
      *> and that arm is what the rest of the corpus compiles under.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1NATARI.
       OPTIONS.
           ARITHMETIC IS NATIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A16 PIC 9(16) VALUE 9999999999999999.
       01 B15 PIC 9(15) VALUE 999999999999999.
       01 R31 PIC 9(31).
       01 P18 PIC 9(12)V9(6) VALUE 999999999999.999999.
       01 Q10 PIC 9(6)V9(4) VALUE 999999.9999.
       01 S28 PIC 9(18)V9(10).
       01 T31 PIC 9(31) VALUE 9999999999999999999999999999998.
       PROCEDURE DIVISION.
       MAIN.
           MULTIPLY A16 BY B15 GIVING R31
           DISPLAY "MUL31=" R31
           MULTIPLY P18 BY Q10 GIVING S28
           DISPLAY "MULSC=" S28
           ADD 1 TO T31
           DISPLAY "ADD31=" T31
           STOP RUN.
