*>--- conformance-case
*> id: pb920-false-value-crosses-edited-image-mirrored
*> rules: [SR-13.18.63.3-27]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB920 - the MIRRORED spelling of negative/pb920-false-value-crosses-edited-image, and it is here
*> because it is the arm that answers "which arm did you fix?". literal-2 is now the NUMERIC literal and
*> literal-4 the alphanumeric one, so a screen keyed on literal-2's class alone - SR27 a) versus b) - would
*> take the OTHER branch and could still miss the pair. ISO 13.18.63.3 SR6 gives the numeric literal 10 the
*> edited image " 10.00" over PIC ZZ9.99, so the two operands name ONE value whichever way round they are
*> written, and SR27's unconditional first sentence forbids it: "The value of literal-4 shall not be equal
*> to the value of any occurrence of literal-2."
*> THE BAND IS 2002+, for the same reason as its twin: WHEN SET TO FALSE and SET condition-name TO FALSE are
*> COBOL-2002 additions, so at --std 85 the two COBOLNET0900 edition gates fire on top of SR27 and the case
*> would pass for the wrong reason. MEASURED at 2002 and 2014: COBOLNET2048 alone.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB920B.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X PIC ZZ9.99.
   88 X-TEN VALUE 10 WHEN SET TO FALSE IS " 10.00".
PROCEDURE DIVISION.
    SET X-TEN TO FALSE.
    STOP RUN.
