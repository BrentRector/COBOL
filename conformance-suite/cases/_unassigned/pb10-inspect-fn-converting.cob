*>--- conformance-case
*> id: pb10-inspect-fn-converting
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> PB10 - a FUNCTION-IDENTIFIER as INSPECT identifier-1 in Format 4 (CONVERTING).
*> 8.4.3.1.2 Format 1 makes a function-identifier an IDENTIFIER, so INSPECT identifier-1 admits one - but only
*> where it is SENDING. 14.9.22.4 GR1 concedes only that "for purposes of determining its length, identifier-1
*> is treated as a sending data item", a SCOPED concession that would be unnecessary if it were generally
*> sending. GR20 executes a format 4 AS a format 2 over the same identifier-1, so it is a RECEIVING operand.
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
    INSPECT FUNCTION UPPER-CASE(WS-X) CONVERTING "OL" TO "0L"
    STOP RUN.
