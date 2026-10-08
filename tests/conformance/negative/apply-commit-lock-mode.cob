*> reject-at: 2023
*> THE WITNESS FOR SR-12.4.5.9.3-1, one of the seven VACUOUSLY-SATISFIED rules kb/Work PB371 raised and the
*> owner answered 2026-09-02: a syntax rule that constrains a CLAIMED construct but whose ANTECEDENT can only
*> be created by a DECLINED module records CONFORMS, witnessed by a test pinning that the antecedent is
*> refused with a NAMED diagnostic.
*> THE RULE (ISO 12.4.5.9.3 SR1): "This clause shall not be specified for a file that is the subject of an
*> APPLY COMMIT clause for which there is an implicit LOCK MODE IS AUTOMATIC WITH LOCK ON MULTIPLE RECORDS
*> applied automatically, including for sequential files."
*> THE ANTECEDENT is the APPLY COMMIT clause, Annex A.4.3 item 2, which COBOLNET1709 refuses at every site
*> in the strict lane (A.4.3 Not claimed); under --permissive it is only a warning, so the rule is enforced
*> in its own right there (COBOLNET2974, kb/Work PB666/PB667). This fixture pins the strict lane.
*> ⛔ WHY THIS FIXTURE EXISTS RATHER THAN A SHARED ONE. It writes the rule's OWN forbidden pairing - a LOCK
*> MODE clause AND an APPLY COMMIT clause naming the same file - so the day A.4.3 is claimed, this file stops
*> failing and the row's CONFORMS is forced back to a real adjudication. A witness that only wrote APPLY
*> COMMIT alone would keep passing and would silently leave the verdict standing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. APCLOCKM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "apclockm.dat"
               ORGANIZATION IS SEQUENTIAL
               LOCK MODE IS AUTOMATIC.
       I-O-CONTROL.
           APPLY COMMIT ON F.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
