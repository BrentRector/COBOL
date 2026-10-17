*>--- conformance-case
*> id: apply-commit-same-area-shared
*> rules: [GR-12.4.6.4.4-3]
*> source-format: free
*> portable: unverified
*>---
*> THE WITNESS FOR GR-12.4.6.4.4-3, one of the seven vacuously-satisfied rules kb/Work PB371 raised (owner
*> decision 2026-09-02: CONFORMS, witnessed by the refusal of the antecedent).
*> THE RULE (ISO 12.4.6.4.4 GR3): "The file and record area of a file subject to commit and rollback may not
*> be shared with another such file or record area unless it has been closed, a commit or a rollback has been
*> executed and the APPLY COMMIT clause for the file has been deactivated."
*> ⚠ ITS SHAPE IS NOT SR-12.4.6.4.3-11'S, which is why it has its own fixture. SR11 forbids mixing a
*> commit-subject file with one that is NOT subject; GR3 says "another SUCH file", i.e. BOTH files subject to
*> commit and rollback. `apply-commit-same-area.cob` writes SR11's shape and cannot witness this rule; this
*> one writes GR3's - a SAME RECORD AREA over two files that are both in the APPLY COMMIT clause.
*> The SAME clause (12.4.6.4) is CLAIMED and fully supported; only the APPLY COMMIT half is refused
*> (COBOLNET1709, Annex A.4.3 item 2), so no file is ever subject to commit and rollback and GR3's antecedent
*> is unreachable. 12.4.6.3.2's format is `APPLY COMMIT ON [ [file-name-1] [identifier-1] ] ...` - a
*> repetition, so naming two files in one clause is the printed syntax, not an extension.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. APCSAMSH.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "apcsamsh1.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT G ASSIGN TO "apcsamsh2.dat"
               ORGANIZATION IS SEQUENTIAL.
       I-O-CONTROL.
           SAME RECORD AREA FOR F G.
           APPLY COMMIT ON F G.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       FD G.
       01 G-REC PIC X(10).
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
