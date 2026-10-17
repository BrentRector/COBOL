*>--- conformance-case
*> id: apply-commit-open-sharing
*> rules: [SR-14.9.27.3-7]
*> source-format: free
*> portable: unverified
*>---
*> THE WITNESS FOR SR-14.9.27.3-7, one of the seven vacuously-satisfied rules kb/Work PB371 raised (owner
*> decision 2026-09-02: CONFORMS, witnessed by the refusal of the antecedent).
*> THE RULE (ISO 14.9.27.3 SR7): "The sharing phrase shall not be specified for a file subject to an APPLY
*> COMMIT clause."
*> ⚠ THE PHRASE ORDER IS THE PRINTED ONE and is easy to get wrong: 14.9.27.2 Format 1 is
*> `OPEN { {mode} [ sharing-phrase ] [ retry-phrase ] { file-name-1 [ WITH NO REWIND ] } ... } ...` - the
*> sharing phrase precedes the FILE LIST, not the file name. A fixture written `OPEN INPUT F SHARING ...`
*> draws a bare COBOL0001 and would have "witnessed" the refusal by accident, which is not a witness at all.
*> The OPEN statement and its SHARING phrase are CLAIMED and supported; the APPLY COMMIT clause is Annex
*> A.4.3 item 2 and is refused by name (COBOLNET1709). Writing both is the rule's own forbidden shape, and it
*> cannot compile - so no OPEN in this implementation can violate SR7.
*> ⚠ SR-14.9.27.3-8 IS NOT WITNESSED HERE and must not be read into this fixture. Its antecedent is the
*> COMPLEMENT - "When file-name-1 is NOT subject to an APPLY COMMIT clause" - which is the ONLY state this
*> implementation has, so its content is live and it carries its own (DIVERGES) verdict.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. APCOPENS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "apcopens.dat"
               ORGANIZATION IS SEQUENTIAL
               LOCK MODE IS AUTOMATIC.
       I-O-CONTROL.
           APPLY COMMIT ON F.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       PROCEDURE DIVISION.
           OPEN INPUT SHARING WITH ALL OTHER F.
           CLOSE F.
           STOP RUN.
