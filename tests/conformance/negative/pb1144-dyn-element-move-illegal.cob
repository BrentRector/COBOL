*> reject-at: 2014 2023
*> kb/Work PB1144 - ISO 14.6.9.2: the corresponding table elements of a variable-length group MOVE (14.9.25.4
*> GR9) "are moved according to the rules of the MOVE statement specified in 14.9.25". ST (PIC 9(4)) and RT
*> (PIC A(4)) match under 8.5.1.12.3 - four bytes each - but a numeric sending operand does not move to an
*> alphabetic receiver (14.9.25.3 SR10, Table 16), so the element MOVE the group MOVE implies is invalid and
*> COBOLNET0819 names it at the MOVE. (The positive half is tests/conformance/2014/pb1144_dyn_move_elements.)
IDENTIFICATION DIVISION.
PROGRAM-ID. N1144EL.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 GS.
   05 SH PIC X(2) VALUE "HH".
   05 ST PIC 9(4) OCCURS DYNAMIC CAPACITY IN CA FROM 1.
01 GR.
   05 QH PIC X(2).
   05 RT PIC A(4) OCCURS DYNAMIC CAPACITY IN CR FROM 1.
PROCEDURE DIVISION.
    MOVE GS TO GR
    STOP RUN.
