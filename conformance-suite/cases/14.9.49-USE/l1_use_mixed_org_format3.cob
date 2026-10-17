*>--- conformance-case
*> id: l1_use_mixed_org_format3
*> rules: [SR-14.9.49.3-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.49.3 SR5 — "The files implicitly or explicitly referenced in the USE statement
      *> need not all have the same organization or access." The rule is headed FORMATS 1 AND 3;
      *> this is the FORMAT 3 half (the sibling arm of the two-format dispatch, whose Format 1
      *> half is l1_use_mixed_org_access). SR5's subject — "implicitly OR explicitly" — gives the
      *> rule FOUR arms in all, and this program carries the two Format 3 ones:
      *>   1. Format 1 EXPLICIT  (ON F1 F2)                     — l1_use_mixed_org_access
      *>   2. Format 1 IMPLICIT  (ON INPUT, naming no file)     — l1_use_mixed_org_access
      *>   3. Format 3 EXPLICIT  (exception-name-2 FILE …)      — HERE, section F3-SECT
      *>   4. Format 3 IMPLICIT  (a bare exception-name-1)      — HERE, section F3-IMPL-SECT
      *> Arm 4 is a real arm, not a nicety. §14.9.49.2 Format 3's second brace group offers
      *> "exception-name-1" ALONE as its first alternative — a USE with no FILE phrase, whose
      *> files are therefore referenced implicitly — and §14.9.49.4 GR3 f) gives that form its
      *> own selection tier ("All format 3 USE statements in which file-name-2 is not specified
      *> and exception-name-1 is a level-2 exception-name are examined"), distinct from the
      *> GR3 d) tier arm 3 uses.  (cite.py --check on GR3 f)'s text reports "§14.9.49.4 3) e)":
      *> the transcription lost that paragraph's list nesting at a printed page break, so the
      *> tool attributes it to the preceding sibling. The matched paragraph is f).)
      *> DERIVATION — every expected line follows from the rule text, nothing from the compiler.
      *>  · SR5 is a RELAXATION: the only conforming behaviour is to ACCEPT each declarative and
      *>    let it serve files that differ in BOTH organization and access. F1 and F3 are
      *>    SEQUENTIAL / SEQUENTIAL; F2 and F4 are INDEXED / ACCESS DYNAMIC.
      *>  · SR13 is satisfied for arm 3: "Exception-name-2 shall be an exception-name beginning
      *>    with 'EC-I-O'" — EC-I-O is such a name (§14.6.13.1's table lists it as the level-2
      *>    input-output category), so FILE may be written with it. Arm 4 writes no FILE phrase,
      *>    so SR13 does not reach it.
      *>  · SR14 permits the two declaratives to coexist: it forbids only a repeated PAIR of
      *>    exception-name-2 and file-name-2, and arm 4 specifies no file-name-2 at all, so the
      *>    pairs (EC-I-O, F1) and (EC-I-O, F2) are each still written exactly once.
      *>  · §7.3.25.4 GR1 makes the default '>>TURN EC-ALL CHECKING OFF', so EC-I-O checking is
      *>    turned on explicitly; GR3 of the same clause spreads a level-2 name to "all
      *>    exception-names that are subordinate to that level-2 exception-name".
      *>  · EVERY leg: §14.9.6.4 GR1 — a CLOSE of a connector that is not open is unsuccessful
      *>    and sets I-O status '42' (§9.1.13.7 rule 2 names the same value for "a CLOSE or
      *>    UNLOCK statement ... attempted for a file connector that is not in an open mode").
      *>    No file here is ever opened, so no leg depends on a physical file being present or
      *>    absent. §14.6.13.1's table maps I-O status "4x" to EC-I-O-LOGIC-ERROR, which is
      *>    subordinate to the level-2 EC-I-O.
      *>  · Arm 3 legs (F1, F2): §14.9.49.4 GR3 d) selects "format 3 USE statements in which
      *>    file-name-2 is specified and exception-name-2 is a level-2 exception-name" when the
      *>    raised condition matches and is associated with that file-name-2 — so the ONE
      *>    file-scoped declarative runs for the SEQUENTIAL F1 and for the INDEXED F2.
      *>  · Arm 4 legs (F3, F4): no declarative names F3 or F4, so GR3 c) and d) find no
      *>    qualifying statement (their candidates are associated with F1 and F2 only), and e)
      *>    finds none either (EC-I-O is a level-2 name, not level-3). Selection therefore
      *>    reaches GR3 f), where the bare EC-I-O declarative qualifies — so the ONE file-less
      *>    declarative runs for the SEQUENTIAL F3 and for the INDEXED F4. That is precisely
      *>    the "implicitly referenced" half of SR5's subject: F3 and F4 are named nowhere in
      *>    any USE statement, and SR5 still requires them to be served despite differing in
      *>    organization and access.
      *>  · Control returns by §14.9.33.4 GR2 a) (RESUME AT NEXT STATEMENT) in every leg, so the
      *>    DISPLAY after each CLOSE reports the status that CLOSE left.
       >>TURN EC-I-O CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1USE5B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "l1use5b-1.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST1.
           SELECT F2 ASSIGN TO "l1use5b-2.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS F2-KEY
               FILE STATUS IS ST2.
           SELECT F3 ASSIGN TO "l1use5b-3.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST3.
           SELECT F4 ASSIGN TO "l1use5b-4.dat"
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
      *> ARM 3 — Format 3 EXPLICIT: one exception-name file-scoped onto a SEQUENTIAL file and an
      *> INDEXED / ACCESS DYNAMIC file at once (§14.9.49.4 GR3 d)).
       F3-SECT SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O FILE F1 FILE F2.
       F3-PARA.
           DISPLAY "FORMAT3-USE-FIRED"
           RESUME AT NEXT STATEMENT.
      *> ARM 4 — Format 3 IMPLICIT: a bare exception-name-1, naming no file, serving a
      *> SEQUENTIAL file and an INDEXED / ACCESS DYNAMIC file at once (§14.9.49.4 GR3 f)).
       F3-IMPL-SECT SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O.
       F3-IMPL-PARA.
           DISPLAY "FORMAT3-IMPLICIT-USE-FIRED"
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN.
           CLOSE F1
           DISPLAY "F1-SEQ=" ST1
           CLOSE F2
           DISPLAY "F2-IDX=" ST2
           CLOSE F3
           DISPLAY "F3-SEQ=" ST3
           CLOSE F4
           DISPLAY "F4-IDX=" ST4
           DISPLAY "DONE"
           STOP RUN.
