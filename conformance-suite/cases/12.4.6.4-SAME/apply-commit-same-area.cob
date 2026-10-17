*>--- conformance-case
*> id: apply-commit-same-area
*> rules: [SR-12.4.6.4.3-11]
*> source-format: free
*> portable: unverified
*>---
*> THE WITNESS FOR SR-12.4.6.4.3-11, one of the seven vacuously-satisfied rules kb/Work PB371 raised (owner
*> decision 2026-09-02: CONFORMS, witnessed by the refusal of the antecedent).
*> THE RULE (ISO 12.4.6.4.3 SR11): "A file or record area that is subject to an APPLY COMMIT clause shall not
*> be specified with another file or record area that is not subject to an APPLY COMMIT clause."
*> Hence the shape below: a SAME RECORD AREA over F and G, with only F in the APPLY COMMIT clause.
*> ⚠ GR-12.4.6.4.4-3 IS A DIFFERENT SHAPE AND HAS ITS OWN FIXTURE - `apply-commit-same-area-shared`. It reads
*> "may not be shared with another SUCH file or record area", i.e. BOTH files subject to commit and rollback,
*> which this program does not write. A first draft claimed both rows here; that would have given GR3 a
*> witness that never writes its antecedent, which is exactly the property these fixtures exist to provide.
*> The SAME clause (12.4.6.4) is CLAIMED and fully supported; only the APPLY COMMIT half is refused
*> (COBOLNET1709, Annex A.4.3 item 2), which is what makes the rule unsatisfiable.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. APCSAMEA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "apcsame1.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT G ASSIGN TO "apcsame2.dat"
               ORGANIZATION IS SEQUENTIAL.
       I-O-CONTROL.
           SAME RECORD AREA FOR F G.
           APPLY COMMIT ON F.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       FD G.
       01 G-REC PIC X(10).
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
