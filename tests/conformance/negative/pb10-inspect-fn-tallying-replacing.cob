*> reject-at: 85 2002 2014 2023
*> PB10 - a FUNCTION-IDENTIFIER as INSPECT identifier-1 in Format 3 (TALLYING-and-REPLACING).
*> 8.4.3.1.2 Format 1 makes a function-identifier an IDENTIFIER, so INSPECT identifier-1 admits one - but only
*> where it is SENDING. 14.9.22.4 GR1 concedes only that "for purposes of determining its length, identifier-1
*> is treated as a sending data item", a SCOPED concession that would be unnecessary if it were generally
*> sending. THE ONE A NAIVE SCREEN ACCEPTS. A screen keyed on "TALLYING present => Format 1" would admit this, because
*> TALLYING IS present - but the REPLACING phrase still modifies identifier-1, so SR1 bars it. The screen must key on
*> REPLACING-or-CONVERTING, not on TALLYING.
*> 8.4.3.2.3 SR1: "A function-identifier shall not be specified as a receiving operand."
*> The SENDING format (Format 1, TALLYING only) IS legal and is pinned by the positive golden
*> conformance:2023/pb10_function_identifier_sending.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB10INSP.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-X PIC X(11) VALUE "hello world".
01 N PIC 9(3) VALUE 0.
PROCEDURE DIVISION.
MAIN.
    INSPECT FUNCTION UPPER-CASE(WS-X) TALLYING N FOR ALL "O" REPLACING ALL "O" BY "0"
    STOP RUN.
