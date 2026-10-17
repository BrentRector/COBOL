*>--- conformance-case
*> id: pb348-rewrite-from-integer-function
*> rules: [SR-14.9.32.3-2, SR-14.9.35.3-9]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.35.3 syntax rule 9: "If identifier-1 references a function and the FILE phrase
*> is not specified, identifier-1 shall reference an alphanumeric, boolean, or national function."
*> REWRITE's admitted set is the WIDER one -- it admits a BOOLEAN function, where RELEASE
*> (14.9.32.3 SR2) and WRITE (14.9.51.3 SR4) do not -- and REWRITE states no unconditional twin of
*> WRITE's SR4.  That difference is why kb/Work PB348 models the FROM phrase's rules as a ROW PER
*> VERB carrying a SET of admitted categories rather than one "must be alphanumeric" flag: a scalar
*> column here would REJECT LEGAL SOURCE on this arm (feedback_model_the_rule_shape_not_one_case).
*> FUNCTION LENGTH is an integer function, which is outside even the wider set, so this program is
*> refused at every edition.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB348N3.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F-IO ASSIGN TO "pb348n3.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F-IO.
01 F-REC PIC X(8).
WORKING-STORAGE SECTION.
01 WS-A PIC X(8) VALUE "ABCDEFGH".
PROCEDURE DIVISION.
MAIN.
    OPEN I-O F-IO
    REWRITE F-REC FROM FUNCTION LENGTH(WS-A)
    CLOSE F-IO
    STOP RUN.
