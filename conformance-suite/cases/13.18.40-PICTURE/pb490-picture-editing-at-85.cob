*>--- conformance-case
*> id: pb490-picture-editing-at-85
*> rules: [GR-13.18.40.4-7]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.40.2 Format 1 / 13.18.40.5 - the PICTURE EDITING phrase is a COBOL-2023
*> introduction (Annex E.3.3 item 19), so below 2023 the version-conformance pass rejects it
*> (COBOLNET0900). The companion POSITIVE golden is 2023/pb490_editing_simple_insertion, which pins the
*> phrase's rendering: an IS-form character-1 is one of the simple insertion editing symbols (rule 3)
*> and a FOR-form one is fixed insertion (rule 5). This fixture's only non-85 construct is the phrase
*> itself.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB490GATE.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 NIS PIC ZT9 EDITING T IS ":".
PROCEDURE DIVISION.
MAIN.
    MOVE 5 TO NIS
    STOP RUN.
