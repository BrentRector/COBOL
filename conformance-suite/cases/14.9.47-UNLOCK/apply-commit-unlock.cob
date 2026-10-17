*>--- conformance-case
*> id: apply-commit-unlock
*> rules: [SR-14.9.47.3-2]
*> source-format: free
*> portable: unverified
*>---
*> THE WITNESS FOR SR-14.9.47.3-2, one of the seven vacuously-satisfied rules kb/Work PB371 raised (owner
*> decision 2026-09-02: CONFORMS, witnessed by the refusal of the antecedent).
*> THE RULE (ISO 14.9.47.3 SR2): "File-name-1 shall not be a file specified in an APPLY COMMIT clause."
*> UNLOCK (14.9.47) is a CLAIMED statement - and that asymmetry is exactly what kb/Work PB371's question was
*> about: 4.2.7 discharges non-support through user documentation that declines COMMIT AND ROLLBACK, never
*> UNLOCK, so this row may not be stamped DOCUMENTED-NON-SUPPORT. It records CONFORMS because the APPLY
*> COMMIT clause (Annex A.4.3 item 2) is refused by name (COBOLNET1709) and no file can be specified in one.
*> ⚠ THE FORMAT IS `UNLOCK file-name-1 [ RECORD | RECORDS ]` - RENDERED at PDF p798 / folio 768, square
*> BRACKETS (the phrase is optional) and NO `ALL`. `UNLOCK F ALL RECORDS` is a GnuCOBOL extension, not ISO,
*> and drawing its parse error here would have passed the negative runner for the wrong reason.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. APCUNLCK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "apcunlck.dat"
               ORGANIZATION IS SEQUENTIAL.
       I-O-CONTROL.
           APPLY COMMIT ON F.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       PROCEDURE DIVISION.
           OPEN INPUT F.
           UNLOCK F RECORDS.
           CLOSE F.
           STOP RUN.
