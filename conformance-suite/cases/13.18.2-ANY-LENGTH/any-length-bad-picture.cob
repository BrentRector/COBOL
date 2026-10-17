*>--- conformance-case
*> id: any-length-bad-picture
*> rules: [SR-13.18.2.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.2.3 SR1: the PICTURE character-string of an ANY LENGTH entry shall be exactly ONE
*> instance of the picture symbol 'N', 'X', or '1' - PIC X(2) violates SR1 (COBOLNET1542), even in the
*> otherwise SR2-legal contained-program placement.
IDENTIFICATION DIVISION.
PROGRAM-ID. ALNBPP9AL.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
IDENTIFICATION DIVISION.
PROGRAM-ID. ALNBPCP9AL.
DATA DIVISION.
LINKAGE SECTION.
01 L PIC X(2) ANY LENGTH.
PROCEDURE DIVISION USING L.
M.
    EXIT PROGRAM.
END PROGRAM ALNBPCP9AL.
END PROGRAM ALNBPP9AL.
