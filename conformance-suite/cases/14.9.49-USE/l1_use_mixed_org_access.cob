*>--- conformance-case
*> id: l1_use_mixed_org_access
*> rules: [SR-14.9.49.3-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.49.3 SR5 — "The files implicitly or explicitly referenced in the USE statement
      *> need not all have the same organization or access." This is the FORMAT 1 half, and it
      *> pins BOTH words of "implicitly or explicitly": one explicitly-referencing declarative
      *> (ON F1 F2) and one implicitly-referencing declarative (ON INPUT), each serving a
      *> SEQUENTIAL file and an INDEXED / ACCESS DYNAMIC file at once.
      *> DERIVATION — every expected line follows from the rule text, nothing from the compiler.
      *>  · SR5 is a RELAXATION, so the only conforming behaviour is to ACCEPT: this program must
      *>    compile, and each declarative must serve both of its files.
      *>  · F1 / F2 legs: §14.9.6.4 GR1 — "If the file connector is not open, the CLOSE statement
      *>    is unsuccessful and the I-O status indicator for the file connector is set to '42'"
      *>    (§9.1.13.7 rule 2 names the same value). §14.9.49.4 GR6 a) then executes the
      *>    file-scoped procedure "when the condition described in the USE statement occurs", so
      *>    the explicit handler runs once for the SEQUENTIAL F1 and once for the INDEXED F2.
      *>  · F3 / F4 legs: §14.9.27.4 GR4 Table 18 — for a non-optional file that is unavailable
      *>    the INPUT row reads "Open is unsuccessful" — and §9.1.13.6 rule 5 gives the value: an
      *>    OPEN with the INPUT phrase on a file "not described as optional" whose physical file
      *>    "is not present" sets I-O status 35. The corpus runner executes each program in a
      *>    fresh empty directory and nothing creates these two files, so 35 is deterministic. §14.9.49.4 GR6 b) then executes the
      *>    mode-scoped procedure "for any file open in the input mode or in the process of being
      *>    opened in the input mode" — again once for a SEQUENTIAL and once for an INDEXED file.
      *>  · The two declaratives cannot be confused: §14.9.49.4 GR5 — "a USE statement specifying
      *>    file-name-1 takes precedence over any USE statements specifying an INPUT, OUTPUT,
      *>    I-O, or EXTEND phrase" — and F3/F4 appear in no file-scoped USE at all.
      *>  · Control returns from each handler by §14.9.33.4 GR2 a): RESUME AT NEXT STATEMENT
      *>    transfers to an implicit CONTINUE immediately following the failing statement, so the
      *>    DISPLAY after it reports the status the failed statement left behind.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1USE5A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "l1use5a-1.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST1.
           SELECT F2 ASSIGN TO "l1use5a-2.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS F2-KEY
               FILE STATUS IS ST2.
           SELECT F3 ASSIGN TO "l1use5a-3.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST3.
           SELECT F4 ASSIGN TO "l1use5a-4.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS F4-KEY
               FILE STATUS IS ST4.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(8).
       FD F2.
       01 R2.
          05 F2-KEY PIC X(4).
          05 F2-DATA PIC X(4).
       FD F3.
       01 R3 PIC X(8).
       FD F4.
       01 R4.
          05 F4-KEY PIC X(4).
          05 F4-DATA PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST1 PIC XX.
       01 ST2 PIC XX.
       01 ST3 PIC XX.
       01 ST4 PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
      *> ONE explicit declarative over a SEQUENTIAL/SEQUENTIAL and an INDEXED/DYNAMIC file.
       EXPLICIT-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1 F2.
       EXPLICIT-PARA.
           DISPLAY "EXPLICIT-USE-FIRED"
           RESUME AT NEXT STATEMENT.
      *> ONE implicit declarative — it names no file at all — over the other such pair.
       IMPLICIT-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       IMPLICIT-PARA.
           DISPLAY "IMPLICIT-INPUT-USE-FIRED"
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN.
           CLOSE F1
           DISPLAY "F1-SEQ=" ST1
           CLOSE F2
           DISPLAY "F2-IDX=" ST2
           OPEN INPUT F3
           DISPLAY "F3-SEQ=" ST3
           OPEN INPUT F4
           DISPLAY "F4-IDX=" ST4
           DISPLAY "DONE"
           STOP RUN.
