*>--- conformance-case
*> id: l1-allocate-returning-program-pointer
*> rules: [SR-13.18.60.3-8, SR-14.9.3.3-3]
*> source-format: free
*> portable: unverified
*>---
*> ISO 14.9.3.3 SR3 - "Data-name-2 shall reference a data item of category data-pointer."  FORMAT-2 arm
*> (ALLOCATE data-name-1 ... RETURNING data-name-2, 14.9.3.2), and the SIBLING-CATEGORY leg: L1B satisfies
*> SR1 (it is described with the BASED clause), so the only defect is the RETURNING receiver.  L1PP is
*> category PROGRAM-POINTER - 13.18.60.4 GR24, "A data description entry that specifies the USAGE
*> PROGRAM-POINTER clause specifies a program-pointer data item" - which is a DIFFERENT category from the
*> data-pointer GR23 defines, even though both are class pointer (8.5.2).  SR3 names data-pointer exactly,
*> so a program-pointer (and, by the same reasoning, an object reference) is not admissible here.
IDENTIFICATION DIVISION.
PROGRAM-ID. L1ALC02.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 L1B PIC X(5) BASED.
01 L1PP USAGE PROGRAM-POINTER.
PROCEDURE DIVISION.
MAIN.
    ALLOCATE L1B RETURNING L1PP.
    STOP RUN.
