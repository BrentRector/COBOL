*>--- conformance-case
*> id: declined-apply-commit
*> rules: [SR-12.4.6.3.3-1, SR-12.4.6.3.3-2, SR-12.4.6.3.3-3,
*>    SR-12.4.6.3.3-4, SR-12.4.6.3.3-5, SR-12.4.6.3.3-6,
*>    SR-12.4.6.3.3-7, SR-12.4.6.3.3-8, SR-12.4.6.3.3-9,
*>    SR-12.4.6.3.3-10, SR-12.4.6.3.3-11, GR-12.4.6.3.4-1,
*>    GR-12.4.6.3.4-2, GR-12.4.6.3.4-3, GR-12.4.6.3.4-4,
*>    GR-12.4.6.3.4-5, GR-12.4.6.3.4-6, FMT-12.4.6.3.2, DOC-A.1-28]
*> source-format: free
*> portable: unverified
*>---
*> ISO 12.4.6.3 APPLY COMMIT clause - Annex A.4.3 item 2, the DECLINED commit-and-rollback facility's
*> declaration half and the owner of 17 of the module's 25 solely-conditioned rules. Before COBOLNET1709
*> the word APPLY was swallowed by the I-O-CONTROL genericClause and the parse then died on COMMIT with an
*> unnamed error. Refusing the clause is what keeps the rest of the decline coherent: with no clause ever
*> accepted, no APPLY COMMIT clause is ever ACTIVE, which is the state 14.9.7.4 GR1 / 14.9.36.4 GR1 give
*> COMMIT and ROLLBACK their CONTINUE behaviour in (pinned by conformance:cases/14.9.7-COMMIT/pb137_commit_inert).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLAPPLY.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "dclapply.dat"
               ORGANIZATION IS SEQUENTIAL.
       I-O-CONTROL.
           APPLY COMMIT ON F.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
