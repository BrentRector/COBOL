*>--- conformance-case
*> id: pb450-set-format7-second-receiver-not-pointer
*> rules: [SR-14.9.39.3-17, GR-14.9.39.4-12, FMT-14.9.39.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB450 - ISO 14.9.39.3 SR17, "Identifier-5 shall reference a data item of category data-pointer"
*> (python scripts/spec/cite.py --check 14.9.39.3 "Identifier-5 shall reference a data item of category
*> data-pointer. Identifier-6 shall be of category data-pointer" -> OK), asked about the SECOND receiving
*> operand of a Format-7 statement.
*> 14.9.39.2 Format 7 prints the receiving operand as a brace with the ellipsis OUTSIDE it, so a statement
*> may carry any number of them; the grammar held two fixed productions of arity one, so SR17 was
*> STRUCTURALLY unreachable on every operand after the first - the statement died in the parser
*> (`error COBOL0001: unexpected 'WS-N'`) and no syntax rule was ever asked. A rule that cannot be reached
*> is not a rule that passes.
*> The first operand here IS a conforming `ADDRESS OF data-name-1` (SR18 is satisfied), so the only thing
*> this case can be rejected for is the second one.
*> Rejected at every edition where Format 7 exists (2002+); below that the whole data-pointer family draws
*> the introduction gate instead - negative/pb450-set-address-second-operand-below-2002 pins that cell.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB450SR17.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 REC-A PIC X(4) VALUE "AAAA".
01 WS-N PIC 9(4) VALUE 7.
01 P1 USAGE POINTER.
01 B1 BASED.
   05 B1A PIC X(4).
PROCEDURE DIVISION.
MAIN-P.
    SET P1 TO ADDRESS OF REC-A.
    SET ADDRESS OF B1 WS-N TO P1.
    DISPLAY B1A.
    STOP RUN.
