*>--- conformance-case
*> id: pb920-false-value-crosses-edited-image
*> rules: [SR-13.18.63.3-27]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB920 - ISO 1989:2023 13.18.63.3 SR27, the CROSS-CLASS arm. "The value of literal-4 shall not be
*> equal to the value of any occurrence of literal-2." The rule says VALUE. It does not say SPELLING and it
*> does not say CLASS - and on a NUMERIC-EDITED subject one value has two spellings, so a screen that
*> compared only same-kind pairs could not see this entry at all.
*> SR6: "If the item is of category numeric-edited, then, subject to Syntax rules 2 and 3, literals in
*> formats 1, 2, and 4 of the VALUE clause may be numeric when they shall be converted to their
*> numeric-edited forms according to the rules for the MOVE statement" - so the numeric literal 10 over
*> PIC ZZ9.99 IS the six characters " 10.00", which is exactly what literal-2 spells.
*> 8.8.4.2.1 NOTE states the consequence the screen has to predict: "All comparisons involving
*> numeric-edited data items are alphanumeric or national comparisons, including when the associated VALUE
*> clause is a numeric literal", and 8.8.4.5.3 GR2 makes the condition-name test one of those comparisons.
*> MEASURED before the fix: this program compiled with NO diagnostic, ran, and `SET X-TEN TO FALSE` left the
*> condition TRUE (it printed X-STILL-TRUE and XF=[ 10.00]) - a wrong answer under a row that read CONFORMS.
*> THE BAND IS 2002+, WHICH IS WHERE THE PHRASE EXISTS. The WHEN SET TO FALSE phrase and SET condition-name
*> TO FALSE are both COBOL-2002 additions, so at --std 85 this entry draws the two COBOLNET0900 gates ON TOP
*> of SR27 (MEASURED: three diagnostics) - a reject that would pass for the wrong reason, so 85 is left out.
*> MEASURED at 2002 and 2014: COBOLNET2048 alone. Note that a numeric literal-2 on a numeric-edited subject
*> is NOT edition-gated in format 3 the way the format-1 twin is (negative/pb560-numeric-edited-value-below-
*> 2023 pins that one at COBOLNET0900); that asymmetry is a REGISTERED lead, and it cannot make this case
*> pass for the wrong reason either way, because the SR27 screen runs at every edition alongside any gate.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB920A.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X PIC ZZ9.99.
   88 X-TEN VALUE " 10.00" WHEN SET TO FALSE IS 10.
PROCEDURE DIVISION.
    SET X-TEN TO FALSE.
    STOP RUN.
