*>--- conformance-case
*> id: pb721-record-bytes-below-2023
*> rules: [SR-13.18.43.3-2]
*> source-format: free
*> portable: unverified
*>---
*> The BYTES spelling of the RECORD clause's { BYTES | CHARACTERS } brace group is a COBOL-2023
*> ADDITION, and below 2023 it is refused by name (COBOLNET0900, the introduction band).
*> THE EDITION FACT IS DERIVED, NOT ASSUMED. Annex E.3.3 item 13 lists BYTES among the words that
*> "have either been added to the list of context-sensitive words or the context in which they
*> are reserved has been expanded" - an OR - and 8.10 gives BYTES exactly ONE construct, the
*> "RECORD clause", so there is no earlier context for 2023 to have EXPANDED and the first arm is
*> the one that applies: the spelling did not exist below 2023. SECONDS sits in the same item-13
*> list and CONTINUE AFTER ... SECONDS is gated the same way.
*> The CHARACTERS spelling of the very same clause is 1985-continuous and draws nothing, which is
*> what tests/conformance/85/pb721_zero_length_record_85.cob compiles; and the WORD stays a
*> user-defined word at every edition, which tests/conformance/85/pb721_bytes_user_word_85.cob
*> compiles. Together those three programs say exactly what is gated and what is not.
*> kb/Work PB721.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB721BYGATE.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "pb721bygate.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD CONTAINS 10 BYTES.
01 F-REC PIC X(10).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT F
    CLOSE F
    STOP RUN.
