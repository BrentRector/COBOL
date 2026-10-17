*>--- conformance-case
*> id: pb183-usage-objref-file-section
*> rules: [SR-13.18.60.3-15]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.60.3 SR15: "The USAGE OBJECT REFERENCE clause shall not be specified in the file
*> section." THE DIRECT DECLARATION ARM - a textbook two-arm dispatch with only the indirect arm
*> enforced: its SAME AS twin (13.18.49.3 SR6, "a SAME AS clause in the file section shall not
*> reference an item whose description contains a USAGE OBJECT REFERENCE data item") has been
*> screened as COBOLNET1556 since the SAME AS landing, while writing the clause directly on an FD
*> record was accepted. R is at level 1, so SR14 admits it and SR15 alone rejects it - which is what
*> makes this fixture attributable to its own arm.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB183G.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "pb183g.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F.
01 R USAGE OBJECT REFERENCE.
WORKING-STORAGE SECTION.
01 W PIC X.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
