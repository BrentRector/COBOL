*>--- conformance-case
*> id: pb65_repository_r5_intrinsic_names
*> rules: [SR-8.3.2.1-5, FMT-15.43.2, FMT-15.58.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB65 (FMT-15.43.2 / FMT-15.58.2). ISO §8.3.2.1 rule 5:
      *> "Intrinsic-function-names may be used as user-defined words and
      *> system-names, except for … intrinsic function names identified in a
      *> function-specifier in the REPOSITORY paragraph." So a source unit with
      *> no REPOSITORY may name a table SQRT and an item MOD (§8.4.3.2.3 SR2 —
      *> without the REPOSITORY entry the FUNCTION-less spelling is a data
      *> reference, and FUNCTION SQRT(…) is still the function), while a unit
      *> whose REPOSITORY identifies HIGHEST-ALGEBRAIC makes HIGHEST-ALGEBRAIC(A1)
      *> the function reference — +999 for a PIC S999 argument (§15.43.4 r2),
      *> −999 for LOWEST-ALGEBRAIC (§15.58.4) — and may not declare that name
      *> (the negatives pb65-repository-intrinsic-name-as-data-name /
      *> pb65-repository-all-intrinsic-name-as-user-word). Before this the
      *> declaration compiled clean and the reference silently read the table.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB65R5MAIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
          05 SQRT PIC 9 OCCURS 3 TIMES VALUE 7.
       01 MOD PIC 9 VALUE 4.
       01 R1 PIC 9.
       PROCEDURE DIVISION.
           MOVE SQRT(2) TO R1.
           DISPLAY "T1 SQRT(2) data-name=" R1.
           MOVE MOD TO R1.
           DISPLAY "T2 MOD data-name=" R1.
           MOVE FUNCTION SQRT(16) TO R1.
           DISPLAY "T3 FUNCTION SQRT(16)=" R1.
           CALL "PB65R5SUB".
           STOP RUN.
       END PROGRAM PB65R5MAIN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB65R5SUB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION HIGHEST-ALGEBRAIC INTRINSIC
           FUNCTION LOWEST-ALGEBRAIC INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A1 PIC S999 VALUE 2.
       01 R2 PIC -9(6).9(3).
       PROCEDURE DIVISION.
           MOVE HIGHEST-ALGEBRAIC(A1) TO R2.
           DISPLAY "T4 HIGHEST-ALGEBRAIC(A1)=" R2.
           MOVE LOWEST-ALGEBRAIC(A1) TO R2.
           DISPLAY "T5 LOWEST-ALGEBRAIC(A1)=" R2.
           GOBACK.
       END PROGRAM PB65R5SUB.
