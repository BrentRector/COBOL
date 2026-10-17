*>--- conformance-case
*> id: external_constant_record_weak_type
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EXTCRWEAK14.
      *> VCR 16 STRENGTH-half continuity witness (ISO 13.16.3 SR13 para 2;
      *> Annex E.2 item 10; P13 review finding C9): below COBOL-2023 a WEAK
      *> (non-STRONG) typedef satisfies TYPE on an EXTERNAL CONSTANT RECORD -
      *> the strongly-typed requirement is the 2023 flip. Per 11.9.10.4 GR7
      *> the constant record initializes at initial state, so WK-A prints its
      *> template VALUE. The reject-at-2023 leg is pinned by the negative
      *> corpus (external-constant-record-weak-type-at-2023).
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WK TYPEDEF.
          05 WK-A PIC X(4) VALUE "WXYZ".
       01 CR IS EXTERNAL CONSTANT RECORD TYPE WK.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY WK-A OF CR.
           STOP RUN.
