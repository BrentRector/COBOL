*>--- conformance-case
*> id: inspect_keyword_omitted_function
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R15 (ledger F10) - the keyword-omitted function reference in the INSPECT operand slot.
      *> ISO/IEC 1989:2023 8.4.3.2.3 SR2: under a REPOSITORY declaration "the word FUNCTION may be omitted
      *> from the function-identifier" - the omitted and keyword forms are ONE reference, so INSPECT of
      *> either spelling must bind identically (before R15 the omitted form compiled clean and died at run
      *> time with "INSPECT of unresolvable item"). 14.9.22.4 GR1/GR7 make Format 1 (TALLYING) a SENDING
      *> use, which 8.4.3.2.3 SR1 admits; the REPLACING/CONVERTING receiving bar stays (the negative
      *> fixture inspect-kof-replacing pins COBOLNET1632).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R15INSKOF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N1 PIC 9(4) VALUE 0.
       01 N2 PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
           INSPECT FUNCTION UPPER-CASE("a-b-c") TALLYING N1 FOR ALL "-".
           INSPECT UPPER-CASE("a-b-c") TALLYING N2 FOR ALL "-".
           IF N1 = N2 AND N1 = 2
             DISPLAY "INSPECT-KOF=OK " N1
           ELSE
             DISPLAY "INSPECT-KOF=BAD " N1 " " N2
           END-IF.
           STOP RUN.
