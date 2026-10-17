*>--- conformance-case
*> id: r03_hexadecimal_national_and_boolean_literals
*> rules: [GR-8.3.3.2.4-1, GR-8.3.3.2.4-4, GR-8.3.3.4.4-5, GR-8.3.3.4.4-6, GR-8.3.3.4.4-7, GR-8.3.3.5.4-4, AR-15.66.3-3, FMT-8.3.3.2.2, FMT-8.3.3.5.2, DOC-A.1-122]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.3.3.5.2 Format 2 (hexadecimal-national, NX"...") and 8.3.3.4.2
      *> Format 2 (hexadecimal-boolean, BX"...") had NO lexer rule at all
      *> (fix-queue R03). The lexer's own comment said so: "NX"..." (hex
      *> national) is deferred" / "BX"..." (hex boolean) is deferred".
      *>
      *> THE FAILURE WAS A SILENT WRONG ANSWER, not a parse error. `NX` lexed as
      *> an IDENTIFIER and `"0041"` as a separate string, so in an argument list
      *> one literal became TWO operands. With a data item actually named NX in
      *> scope, nothing complained at all:
      *>     01 NX PIC X(4) VALUE "ZZZZ".
      *>     MOVE FUNCTION MAX(NX"0041") TO R.     *> gave ZZZZ, silently
      *> which is MAX(NX, "0041") - an unrelated item compared against a string.
      *>
      *> THE FIX FOLDS FORMAT 2 INTO THE EXISTING TOKEN rather than adding a new
      *> one, because that is what the standard says: 8.3.3.5.4 GR2 and 8.3.3.4.4
      *> GR2 are ALL-FORMATS rules putting both formats of each literal in one
      *> class and category. 32 call sites already route NATLIT to national and
      *> BOOLLIT to boolean, and all of them stay correct untouched - including
      *> the COBOL-2002 introduction gate, which assertions 9-10 of the negative
      *> fixture r03-hex-national-literal-at-85 pin.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R03HEXLITERALS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NA PIC N(4).
       01 BB PIC 1(8) USAGE BIT.
       01 L  PIC 9(4).
      *> THE TRAP ITEM: a data item named NX is what made the old degradation
      *> silent instead of merely wrong. It stays declared here on purpose.
       01 NX PIC X(4) VALUE "ZZZZ".
       PROCEDURE DIVISION.
       MAIN.
      *> 1-2 - 8.3.3.5.4 GR4: each group of hex digits is one national character.
      *> 8.3.3.5.3 SR5 leaves the digits-per-character to the implementor; D-N1
      *> stores one UTF-16 code unit per national position, so it is FOUR.
           MOVE NX"00410042" TO NA.
           DISPLAY "1=[" NA "]".
           MOVE FUNCTION LENGTH(NX"00410042") TO L.
           DISPLAY "2=" L.
      *> 3 - 8.3.3.4.4 GR5 spells the boolean mapping out digit by digit:
      *> '5' is B"0101" and 'A' is B"1010".
           MOVE BX"5A" TO BB.
           DISPLAY "3=[" BB "]".
      *> 4 - THE REGRESSION CONTROL. One argument, not two - and the NX item
      *> above must NOT be what comes back.
           MOVE FUNCTION MAX(NX"0041") TO NA.
           DISPLAY "4=[" NA "]".
      *> 5 - the keyword-OMITTED function form, which re-parses its argument text
      *> through a different path (FunctionArgFragment) and so is asserted apart.
           MOVE LENGTH(NX"00410042") TO L.
           DISPLAY "5=" L.
      *> 6 - 8.8.3 concatenation of two Format-2 literals.
           MOVE NX"0041" & NX"0042" TO NA.
           DISPLAY "6=[" NA "]".
      *> 7 - lowercase digits and the apostrophe delimiter, both of which the
      *> general format admits.
           MOVE NX'00610062' TO NA.
           DISPLAY "7=[" NA "]".
      *> 8-9 - FORMAT 1 CONTROLS: the existing national and boolean literals must
      *> be untouched by a change that shares their token.
           MOVE N"CD" TO NA.
           DISPLAY "8=[" NA "]".
           MOVE B"11110000" TO BB.
           DISPLAY "9=[" BB "]".
      *> 10 - and the trap item itself still reads as an ordinary data item.
           DISPLAY "10=[" NX "]".
      *> 11-13 - BX HAS NO GROUPING RULE, and that asymmetry is the point.
      *> 8.3.3.4.3 r3 says only "Hexadecimal-digit-1 shall be a hexadecimal
      *> digit", because 8.3.3.4.4 GR5 maps EACH digit independently to four
      *> boolean characters. So an odd digit count is well formed here, while
      *> the same count in X"..." or NX"..." is a COBOLNET1635 error (see the
      *> negative fixture r03-hex-literal-digit-grouping). A grouping check
      *> written "for symmetry" would reject all three of these.
           MOVE BX"5" TO BB.
           DISPLAY "11=[" BB "]".
           MOVE BX"5AB" TO BB.
           DISPLAY "12=[" BB "]".
      *> 13-14 - zero length, which all three formats admit in a NOTE
      *> (8.3.3.2.3 NOTE 2, 8.3.3.5.3 NOTE 2, 8.3.3.4.3 NOTE). X"" used to be
      *> refused by the lexer's `+` and split like the others. LENGTH of a
      *> zero-length literal is ZERO (8.5.4 item 8 - "a literal whose ...
      *> length at runtime is zero"; 15.50.4 r2/r3 count its positions): the
      *> value 1 this golden first pinned was the fold's Math.Max(1, ...)
      *> clamp, observed rather than derived - corrected with PB61.
           MOVE FUNCTION LENGTH(X"") TO L.
           DISPLAY "13=" L.
           MOVE FUNCTION LENGTH(NX"") TO L.
           DISPLAY "14=" L.
      *> 15-16 - well-formed X"..." controls, the format that already existed.
           MOVE FUNCTION LENGTH(X"4142") TO L.
           DISPLAY "15=" L.
           MOVE BX"" TO BB.
           DISPLAY "16=[" BB "]".
      *> 17-18 - the APOSTROPHE delimiter (8.3.3.2.2 Format 2 prints BOTH
      *> delimiters with the hex sequence OPTIONAL - page rendered, PB59). The
      *> apostrophe arm's `+` used to make X'' split into IDENTIFIER + a
      *> zero-length Format-1 literal; row 17 pins the same LENGTH answer the
      *> X"" row 13 pins, row 18 the well-formed control.
           MOVE FUNCTION LENGTH(X'') TO L.
           DISPLAY "17=" L.
           MOVE FUNCTION LENGTH(X'4142') TO L.
           DISPLAY "18=" L.
           STOP RUN.
