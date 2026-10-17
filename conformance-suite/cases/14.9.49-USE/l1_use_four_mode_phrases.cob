*>--- conformance-case
*> id: l1_use_four_mode_phrases
*> rules: [SR-14.9.49.3-7]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.49.3 SR7 — "The INPUT, OUTPUT, I-O, and EXTEND phrases may each be specified
      *> only once in the declaratives portion of a given procedure division." This is the
      *> ACCEPTING complement of that rule (its rejecting half is
      *> conformance-suite/cases/14.9.49-USE/l1-use-input-phrase-twice): all FOUR phrases appear, each
      *> exactly once, so the source conforms and each phrase names a DISTINCT open-mode scope.
      *> The four fired handlers are what makes the once-each reading falsifiable: were any two
      *> of the four phrases treated as one scope, this program would have to be rejected as a
      *> duplicate, and were a phrase mapped to the wrong mode the wrong handler line would
      *> appear.
      *> DERIVATION — every expected line follows from the rule text, nothing from the compiler.
      *>  · FI, FIO, FE: §14.9.27.4 GR4 Table 18 — for a NON-optional file that is unavailable,
      *>    "Open is unsuccessful" in the INPUT, I-O and EXTEND rows — and §9.1.13.6 rule 5 gives
      *>    the value: an OPEN "with the INPUT, I-O, or EXTEND phrase … attempted on a file that
      *>    is not described as optional and the physical file is not present" sets I-O status
      *>    35. The corpus runner runs each program in a fresh empty directory and nothing
      *>    creates these three files, so 35 is deterministic.
      *>  · FO: Table 18's OUTPUT row — an unavailable file is CREATED, a normal open — so the
      *>    first OPEN OUTPUT is successful, §9.1.13.2 rule 1 "I-O status = 00". The second is
      *>    §14.9.27.4 GR2, "The file connector referenced by file-name-1 shall not be open. If
      *>    it is open, the execution of the OPEN statement is unsuccessful and the I-O status
      *>    associated with file-name-1 is set to '41'" (§9.1.13.7 rule 1 names the same value).
      *>  · Handler selection is §14.9.49.4 GR6 b) INPUT / c) OUTPUT / d) I-O / e) EXTEND: each
      *>    procedure is executed for a file "open in the <mode> mode or in the process of being
      *>    opened in the <mode> mode". No declarative names a file, so GR3 b) reaches them and
      *>    GR5's file-name precedence never applies.
      *>  · FIO is ORGANIZATION INDEXED so that OPEN I-O is unambiguously an operation the
      *>    organization supports and §9.1.13.6 rule 6 a) 2. (status 37) cannot be the applicable
      *>    rule instead of rule 5.
      *>  · Control returns from each handler by §14.9.33.4 GR2 a) (RESUME AT NEXT STATEMENT).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1USE7P.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FI ASSIGN TO "l1use7p-i.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS STI.
           SELECT FO ASSIGN TO "l1use7p-o.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS STO.
           SELECT FIO ASSIGN TO "l1use7p-io.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS FIO-KEY
               FILE STATUS IS STIO.
           SELECT FE ASSIGN TO "l1use7p-e.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS STE.
       DATA DIVISION.
       FILE SECTION.
       FD FI.
       01 RI PIC X(8).
       FD FO.
       01 RO PIC X(8).
       FD FIO.
       01 RIO.
          05 FIO-KEY PIC X(4).
          05 FIO-DATA PIC X(4).
       FD FE.
       01 RE PIC X(8).
       WORKING-STORAGE SECTION.
       01 STI  PIC XX.
       01 STO  PIC XX.
       01 STIO PIC XX.
       01 STE  PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       MODE-INPUT-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       MODE-INPUT-PARA.
           DISPLAY "H-INPUT"
           RESUME AT NEXT STATEMENT.
       MODE-OUTPUT-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON OUTPUT.
       MODE-OUTPUT-PARA.
           DISPLAY "H-OUTPUT"
           RESUME AT NEXT STATEMENT.
       MODE-IO-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON I-O.
       MODE-IO-PARA.
           DISPLAY "H-IO"
           RESUME AT NEXT STATEMENT.
       MODE-EXTEND-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON EXTEND.
       MODE-EXTEND-PARA.
           DISPLAY "H-EXTEND"
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN.
           OPEN INPUT FI
           DISPLAY "FI=" STI
           OPEN I-O FIO
           DISPLAY "FIO=" STIO
           OPEN EXTEND FE
           DISPLAY "FE=" STE
           OPEN OUTPUT FO
           DISPLAY "FO1=" STO
           OPEN OUTPUT FO
           DISPLAY "FO2=" STO
           CLOSE FO
           DISPLAY "DONE"
           STOP RUN.
