*>--- conformance-case
*> id: pb1938-through-alias-element-move-illegal
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB1938 - ISO 14.6.9.2: the corresponding table elements of a variable-length group MOVE (14.9.25.4
*> GR9) "are moved according to the rules of the MOVE statement specified in 14.9.25", and that holds when the
*> group operand is a level-66 THROUGH alias (13.18.45.4 GR2 makes it a group item). ST (PIC 9(4)) and RT
*> (PIC A(4)) match under 8.5.1.12.3 - four bytes each - but a numeric sending operand does not move to an
*> alphabetic receiver (14.9.25.3 SR10, Table 16), so the element MOVE the group MOVE implies is invalid and
*> COBOLNET0819 names it at the MOVE. (The positive half is tests/conformance/2014/pb1938_through_alias_vlg_element_moves.)
IDENTIFICATION DIVISION.
PROGRAM-ID. N1938EL.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 GS.
   05 S1 PIC X(2) VALUE "HH".
   05 ST PIC 9(4) OCCURS 2.
   05 S3 PIC X(2) VALUE "TT".
66 SALIAS RENAMES S1 THROUGH S3.
01 GR.
   05 QH PIC X(2).
   05 RT PIC A(4) OCCURS DYNAMIC CAPACITY IN CR FROM 1.
   05 QT PIC X(2).
PROCEDURE DIVISION.
    MOVE SALIAS TO GR
    STOP RUN.
