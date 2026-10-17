*>--- conformance-case
*> id: pb422-all-multichar-move-numeric
*> rules: [SR-8.3.3.6.3-3]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 8.3.3.6.3 SR3: "If the length of literal-1 is greater than one, it is not permitted to be
*> associated with a numeric or numeric-edited item." 8.3.3.6.4 GR2's NOTE 1 makes a MOVE an association.
*> kb/Work PB422: this is NOT 14.9.25.3 SR5 - SR5's digit-only exception has no length qualifier, so ALL "57"
*> is inside what SR5 permits and only SR3 bars it. It rode SR5's 2023 removal row, which accepted it silently
*> at 2002/2014 and at 2023 named the wrong rule. Edge DERIVED (VCR Table 7 row 7.13): an obsolete ANSI-85
*> element deleted by ISO 2002 - the 85 positive twin is 85/pb422_all_multichar_numeric_85. COBOLNET0902.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB422NMV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(3).
       PROCEDURE DIVISION.
           MOVE ALL "57" TO N
           DISPLAY "N=[" N "]"
           STOP RUN.
