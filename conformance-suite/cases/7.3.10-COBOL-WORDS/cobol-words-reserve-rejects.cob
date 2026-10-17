*>--- conformance-case
*> id: cobol-words-reserve-rejects
*> rules: [GR-7.3.10.4-5]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 7.3.10.4 GR5 - a >>COBOL-WORDS RESERVE'd word shall not be used as a
*> user-defined word: FOO is reserved for this group, so 01 FOO is rejected (COBOLNET0901).
       >>COBOL-WORDS RESERVE "FOO"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CWRSV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FOO PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY FOO.
           STOP RUN.
