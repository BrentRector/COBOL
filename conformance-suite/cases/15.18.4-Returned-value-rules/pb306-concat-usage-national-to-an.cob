*>--- conformance-case
*> id: pb306-concat-usage-national-to-an
*> rules: [RV-15.18.4-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB306: FUNCTION CONCAT whose argument-1 is of USAGE national (not CLASS national) returns a NATIONAL
*> value - ISO 15.18.4 r2, "If argument-1 is of class or usage national, the function will return a national
*> value", and the 15.18.1 table's "Numeric usage National -> National" row - so moving it to an alphanumeric
*> receiver is invalid (14.9.25.3 SR10, Table 16: a national sender into an alphanumeric receiver is "No").
*> PIC 9(4) USAGE NATIONAL is legal data (13.18.60.3 SR12) whose CATEGORY is numeric; a category-only reader
*> labelled this result alphanumeric and the MOVE compiled clean. pb15-concat-national-result-to-an pins the
*> CLASS limb; this pins the USAGE limb.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGP306C.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 NN1 PIC 9(4) USAGE NATIONAL VALUE 1234.
01 NN2 PIC 9(4) USAGE NATIONAL VALUE 5678.
01 W-X PIC X(8).
PROCEDURE DIVISION.
MAIN.
    MOVE FUNCTION CONCAT(NN1 NN2) TO W-X.
    STOP RUN.
