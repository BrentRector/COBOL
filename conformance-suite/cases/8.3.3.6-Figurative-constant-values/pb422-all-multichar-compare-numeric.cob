*>--- conformance-case
*> id: pb422-all-multichar-compare-numeric
*> rules: [SR-8.3.3.6.3-3]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 8.3.3.6.3 SR3: "If the length of literal-1 is greater than one, it is not permitted to be
*> associated with a numeric or numeric-edited item." 8.3.3.6.4 GR2's NOTE 1: "A figurative constant is
*> associated with a data item or literal when, for example, the figurative constant is moved to it, compared
*> with it, or paired with it in a binary operation."
*> kb/Work PB422: the COMPARISON arm of the association was screened at no edition (the MOVE arm rode SR5's
*> 2023 row); a numeric-EDITED item is asked the same. Edge DERIVED (VCR Table 7 row 7.13): an obsolete
*> ANSI-85 element deleted by ISO 2002 - the 85 positive twin is 85/pb422_all_multichar_numeric_85.
*> COBOLNET0902, from the one relation checkpoint (IF / EVALUATE / PERFORM UNTIL / SEARCH WHEN).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB422NCP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NED PIC ZZ9.
       PROCEDURE DIVISION.
           MOVE 575 TO NED
           IF NED = ALL "57"
               DISPLAY "EQ"
           ELSE
               DISPLAY "NE"
           END-IF
           STOP RUN.
