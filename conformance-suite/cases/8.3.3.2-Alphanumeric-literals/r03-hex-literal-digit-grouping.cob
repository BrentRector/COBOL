*>--- conformance-case
*> id: r03-hex-literal-digit-grouping
*> rules: [SR-8.3.3.2.3-6, DOC-A.1-9, DOC-A.1-96, DOC-A.1-98]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.3.3.2.3 rule 6: "Each hex-character-sequence-1 shall consist of
      *> the number of hexadecimal digits that the implementor has specified as
      *> the number of hexadecimal digits that map to an alphanumeric character"
      *> - two, one byte per character. 8.3.3.5.3 rule 5 says the same sentence
      *> for NX"...", where D-N1 stores one UTF-16 code unit per national
      *> position, so it is four.
      *>
      *> BOTH USED TO BE SILENT WRONG ANSWERS (fix-queue R03). The hex decoders
      *> returned "" for a malformed digit count and every caller took that as
      *> the value, so FUNCTION LENGTH(X"414") answered 1 on source the standard
      *> rejects. X"..." had behaved that way since it was introduced; NX"..."
      *> inherited it the moment the token started matching.
      *>
      *> ⚠ THE CHECK IS AT BIND, NOT IN THE LEXER, AND THAT IS DELIBERATE: a
      *> lexer rule refusing an odd digit count would not reject the program - the
      *> token would simply fail to match and `X"414"` would split into an
      *> IDENTIFIER and a STRINGLIT, which is the silent degradation R03 exists to
      *> close. The token must match so that something is left to diagnose.
      *>
      *> BX"..." is NOT here on purpose: 8.3.3.4.3 r3 states no grouping rule,
      *> because 8.3.3.4.4 GR5 maps each digit independently to four boolean
      *> characters. Assertions 11-12 of r03_hexadecimal_national_and_boolean_
      *> literals pin that odd BX counts stay legal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R03GROUPING.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION LENGTH(X"414") TO L.
           DISPLAY "L=" L.
           STOP RUN.
