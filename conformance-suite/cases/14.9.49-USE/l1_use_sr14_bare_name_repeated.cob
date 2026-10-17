*>--- conformance-case
*> id: l1_use_sr14_bare_name_repeated
*> rules: [SR-14.9.49.3-14]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.49.3 SR14 - "The same pair of exception-name-2 and file-name-2 shall not be
      *> specified in more than one USE statement within the same procedure division." The rule
      *> is scoped to PAIRS. A BARE exception-name in a Format-3 USE is exception-name-1, not
      *> exception-name-2: the §14.9.49.2 Format-3 figure puts exception-name-2 only in the
      *> `exception-name-2 { FILE file-name-2 } ...` alternative, and §14.9.49.4 GR3 splits on
      *> the same axis - c) and d) read "file-name-2 is specified and exception-name-2 ...",
      *> e), f) and g) read "file-name-2 is not specified and exception-name-1 ...". No syntax
      *> rule forbids repeating exception-name-1; GR3 supplies its OUTCOME instead: "A
      *> declarative is selected for execution by analyzing the USE statements in the source
      *> element in the order in which they are specified. The first declarative that satisfies
      *> the selection criteria is executed and no other declaratives are executed."
      *> So this program - two declarative sections whose USE statements both name the bare
      *> EC-I-O-AT-END - is LEGAL, and its OUTPUT is what GR3 prescribes: the FIRST section runs
      *> and the second never does. (The pair-scoped accepting complement is
      *> l1_use_sr14_pair_twice_one_statement; the rejecting half is
      *> conformance-suite/cases/14.9.49-USE/l1-use-sr14-pair-in-two-statements.)
      *> Placed in the 2002 corpus - the earliest edition with Format 3.
      *>
      *> DERIVATION - every expected line follows from the rule text, nothing from the compiler.
      *>  * The >>TURN directive enables the EC-I-O family so a format-3 declarative can be
      *>    selected at all.
      *>  * OPEN OUTPUT of an absent, non-optional file is a normal open (§14.9.27.4 GR4's table,
      *>    OUTPUT row); the WRITE and CLOSE complete successfully.
      *>  * The second READ reaches end of file: §14.9.30.4 GR24 a) - "The I-O status of the file
      *>    connector associated with file-name-1 is set to '10' to indicate the at end condition,
      *>    and, if enabled, the EC-I-O-AT-END exception condition is set to exist" (§9.1.13.4
      *>    rule 1 a) gives the same 10).
      *>  * Neither USE names a file, so the file-scoped tiers GR3 c) and d) find nothing and the
      *>    search reaches GR3 e) - "All format 3 USE statements in which file-name-2 is not
      *>    specified and exception-name-1 is a level-3 exception-name are examined" -
      *>    EC-I-O-AT-END being a level-3 name. BOTH sections qualify there.
      *>  * GR3's opening sentence decides between them: source order, first match wins, "no
      *>    other declaratives are executed". FIRST-SECT precedes SECOND-SECT, so H1 is
      *>    displayed and H2 is not. WHICH=1 states the same fact in a form a wrong selection
      *>    would change even if the DISPLAY order were somehow preserved.
      *>  * RESUME AT NEXT STATEMENT transfers control to an implicit CONTINUE that "immediately
      *>    follows the end of the statement that was executing when control was transferred to
      *>    the exception processing procedure" (§14.9.33.4 GR2 a)), so AFTER is reached.
      *>  * "H1 S: EC-I-O-AT-END" also names the condition through §15.33.1's EXCEPTION-STATUS,
      *>    so the line proves WHICH exception selected the first section, not only that it ran.
>>TURN EC-I-O CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1S14P2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "l1s14p2-1.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST1.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(8).
       WORKING-STORAGE SECTION.
       01 ST1   PIC XX VALUE "??".
       01 WHICH PIC 9  VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       FIRST-SECT SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O-AT-END.
       FIRST-PARA.
           MOVE 1 TO WHICH
           DISPLAY "H1 S: " FUNCTION EXCEPTION-STATUS
           RESUME AT NEXT STATEMENT.
       SECOND-SECT SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O-AT-END.
       SECOND-PARA.
           MOVE 2 TO WHICH
           DISPLAY "H2 S: " FUNCTION EXCEPTION-STATUS
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN.
           OPEN OUTPUT F1
           MOVE "REC-ONE " TO R1
           WRITE R1
           CLOSE F1
           OPEN INPUT F1
           READ F1
           DISPLAY "R1=" R1 " ST1=" ST1
           READ F1
           DISPLAY "AFTER ST1=" ST1
           CLOSE F1
           DISPLAY "WHICH=" WHICH
           STOP RUN.
