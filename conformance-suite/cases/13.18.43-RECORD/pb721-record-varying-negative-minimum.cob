*>--- conformance-case
*> id: pb721-record-varying-negative-minimum
*> rules: [SR-13.18.43.3-7]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.43.3 SR7: "Integer-2 shall be greater than or equal to zero."
*> !! THE RULE HAS TWO HALVES AND THIS IS THE REJECTING ONE. The PERMITTING half -
*> that FROM 0 is legal and shall compile - is witnessed by
*> tests/conformance/2023/pb721_zero_length_record.cob and its COBOL-85 twin; a
*> corpus that pinned only the permission would never notice a compiler that
*> accepted a negative minimum too.
*> A negative operand cannot be written at all, and that is the GENERAL FORMAT's
*> doing rather than a screen's: 5.5 rule 1 - "When the term 'integer-n' (n = 1, 2,
*> ...) is used in a general format and associated rules, it refers to a fixed-point
*> integer literal that shall be unsigned and nonzero unless otherwise specified in
*> the associated rules" - makes every integer-n UNSIGNED, so the minus sign is
*> refused where the format writes integer-2. (SR7 is precisely the "unless
*> otherwise specified" override, and it overrides the NONZERO half only.)
*> kb/Work PB721.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB721SR7NEG.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "pb721sr7neg.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM -1 TO 20 DEPENDING ON WS-LEN.
01 F-REC PIC X(20).
WORKING-STORAGE SECTION.
01 WS-LEN PIC 9(4) VALUE 0.
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT F
    CLOSE F
    STOP RUN.
