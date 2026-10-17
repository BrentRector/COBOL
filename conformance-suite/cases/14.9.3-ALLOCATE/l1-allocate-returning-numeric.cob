*>--- conformance-case
*> id: l1-allocate-returning-numeric
*> rules: [SR-14.9.3.3-3]
*> source-format: free
*> portable: unverified
*>---
*> ISO 14.9.3.3 SR3 - "Data-name-2 shall reference a data item of category data-pointer."  FORMAT-1 arm
*> (ALLOCATE arithmetic-expression-1 CHARACTERS ... RETURNING data-name-2, 14.9.3.2): the RETURNING receiver
*> here is category NUMERIC, and 13.18.60.4 GR23 makes category data-pointer the property of a USAGE POINTER
*> entry - "A data description entry that specifies the USAGE POINTER clause specifies a data-pointer data
*> item" - so L1N cannot satisfy SR3 and the statement is not a conforming ALLOCATE.  SR2 is satisfied (the
*> RETURNING phrase IS specified, as CHARACTERS requires), so this fixture isolates SR3 alone.
IDENTIFICATION DIVISION.
PROGRAM-ID. L1ALC01.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 L1N PIC 9(4).
PROCEDURE DIVISION.
MAIN.
    ALLOCATE 10 CHARACTERS RETURNING L1N.
    STOP RUN.
