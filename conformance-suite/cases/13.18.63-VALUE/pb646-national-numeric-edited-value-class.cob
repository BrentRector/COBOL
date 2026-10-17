*>--- conformance-case
*> id: pb646-national-numeric-edited-value-class
*> rules: [SR-13.18.63.3-7]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.63.3 SR7 - "If the item is of category numeric-edited and the literal is of
*> class alphanumeric or national, the class of the literal shall conform to that of the data item".
*> 8.5.2.1 Table 2 puts "Numeric-edited (if usage is national)" in class NATIONAL, so a usage-NATIONAL
*> numeric-edited item takes a NATIONAL literal and this ALPHANUMERIC one does not conform: COBOLNET0898.
*> The conforming spelling is conformance:cases/13.18.40-PICTURE/pb646_national_form_numeric_2023's NED (VALUE N"  7").
*> The opposite direction is conformance:cases/13.18.63-VALUE/pb646-numeric-edited-value-national-class.
*> The SECOND entry is the 13.18.60.4 GR1 group-inherited spelling of the same defect: the verdict is
*> the resolved usage's, so it is refused identically (kb/Work PB646).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB646VC1.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-NE PIC ZZ9 USAGE NATIONAL VALUE "  1".
01 W-GG USAGE NATIONAL.
   05 W-GE PIC ZZ9 VALUE "  1".
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
