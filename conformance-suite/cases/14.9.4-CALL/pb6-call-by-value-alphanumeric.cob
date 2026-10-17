*>--- conformance-case
*> id: pb6-call-by-value-alphanumeric
*> rules: [SR-14.9.4.3-22]
*> source-format: free
*> portable: unverified
*>---
*> ISO 14.9.4.3 SR22 - "If identifier-4 or its corresponding formal parameter is specified with a BY VALUE
*> phrase, identifier-4 shall be of class numeric, object, or pointer." GnuCOBOL accepts an alphanumeric
*> operand as an extension and silently assumes BY CONTENT - a DIFFERENT passing mode from the one written.
*> Surfaced by the pre-merge differential: this was already rejected, but by DA6's 8.8.1.1 ARITHMETIC screen,
*> because the grammar production is named arithmeticExpression. Right verdict, wrong rule quoted.
*> (BY VALUE is a COBOL-2002 introduction, so 85 rejects this earlier for a different reason.)
*> PROG2 is CONTAINED because AS NESTED requires it - 14.9.4.3 SR15 sentence 2 fires first otherwise (PB131).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB6BYVALUE.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X PIC X(4).
PROCEDURE DIVISION.
MAIN.
    CALL "PROG2" AS NESTED USING BY VALUE X END-CALL.
    STOP RUN.
IDENTIFICATION DIVISION.
PROGRAM-ID. PROG2.
DATA DIVISION.
LINKAGE SECTION.
01 L-X PIC X(4).
PROCEDURE DIVISION USING L-X.
P.
    EXIT PROGRAM.
END PROGRAM PROG2.
END PROGRAM NEGPB6BYVALUE.
