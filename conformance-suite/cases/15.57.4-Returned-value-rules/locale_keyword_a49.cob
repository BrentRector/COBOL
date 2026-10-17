*>--- conformance-case
*> id: locale_keyword_a49
*> rules: [RV-15.57.4-2, FMT-15.94.2]
*> source-format: free
*> portable: unverified
*>---
      *> The LOCALE keyword of NUMVAL-C (ISO 15.68.3 r5a) and TEST-NUMVAL-C (15.94.3 r1) is LIVE since kb/Work
      *> PB64 T6, and locale-name-1 'shall be associated with a locale in the SPECIAL-NAMES paragraph' - this
      *> program has NO SPECIAL-NAMES paragraph at all, so LOC1 is UNDECLARED and each reference draws the ONE
      *> undeclared-locale-name diagnostic, COBOLNET1664 (never the deleted by-name refusal COBOLNET1518 - the
      *> A.4.9 module is claimed whole). LOCALE is not a LEXER TOKEN here, so the keyword parses as an extra
      *> argument and is recognized BY NAME (it IS a reserved word from 2002 per 8.9 / reserved-words.json; not
      *> tokenizing it is a deliberate choice, because a token would break that by-name recognition. PB25.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P11LOCKW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RN PIC 9(4)V99.
       01 RT PIC 9(2).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE RN = FUNCTION NUMVAL-C("12.34" LOCALE LOC1)
           COMPUTE RT = FUNCTION TEST-NUMVAL-C("12" LOCALE LOC1)
           STOP RUN.
