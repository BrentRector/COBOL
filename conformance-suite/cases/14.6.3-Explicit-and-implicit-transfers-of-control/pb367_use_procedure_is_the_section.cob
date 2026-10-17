*>--- conformance-case
*> id: pb367_use_procedure_is_the_section
*> rules: [GR-14.6.3-1, GR-14.9.49.4-3, GR-14.9.49.4-7, GR-14.9.49.4-13]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB367 - A USE PROCEDURE IS THE WHOLE REMAINDER OF ITS DECLARATIVE SECTION.
      *> Both declaratives below are written in the shape that used to truncate them: named
      *> paragraphs, then a paragraph whose only statement is a bare EXIT, then MORE paragraphs,
      *> the last of which terminates the run unit. A handler end derived from that SHAPE ended the
      *> bounded dispatch at the bare-EXIT paragraph, so the selected declarative executed only in
      *> part - on every format's selection path.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN:
      *>  . 14.9.49.3 SR1 - "The remainder of the section shall consist of zero, one, or more
      *>    procedural paragraphs that define the procedures to be used": the use procedure IS the
      *>    remainder of the section, all of it.
      *>  . 14.4.2 - a section "ends immediately before the next section or at the end of the
      *>    procedure division or, in the declaratives portion of the procedure division, at the
      *>    keywords END DECLARATIVES", so D1 ends at D2's header and D2 at END DECLARATIVES.
      *>  . 14.6.3 rule 1 - a procedure executed under the control of another COBOL statement "such
      *>    as PERFORM, USE, SORT, and MERGE" transfers back to the control mechanism from the last
      *>    statement of "the last procedure in the range of the controlling statement".
      *>  . 14.9.14.4 GR7 + its NOTE - EXIT SECTION passes control "to an unnamed empty paragraph
      *>    immediately following the last paragraph of the current section, preceding any return
      *>    mechanisms for that section", and the NOTE names USE as one of those return mechanisms:
      *>    the USE return point sits AFTER the section's last paragraph, nowhere earlier.
      *>  . 14.9.14.4 GR1 - a format-1 EXIT "serves only to enable the user to assign a
      *>    procedure-name to a given point in a procedure division. Such an EXIT statement has no
      *>    other effect", so D1-EX / D2-EX do NOT end anything; control falls to the next paragraph.
      *>  . 14.9.14.4 GR7 again, as an EXECUTED statement this time - D1-LAST's EXIT SECTION lands on
      *>    the unnamed empty paragraph after D1's LAST paragraph, "preceding any return mechanisms
      *>    for that section", so it reaches D1's USE return WITHOUT running D1-TAIL. The two
      *>    MUST-NOT-RUN paragraphs are the negative half of the assertion: they are the CCVS
      *>    termination-tail shape that made the deleted heuristic fire, and a conforming run neither
      *>    truncates the handler before them nor falls into them.
      *>
      *> LINE BY LINE:
      *>  . The second READ hits the end of the file with no AT END phrase, so 9.1.13 sets I-O
      *>    status 10 and 14.9.49.4 GR6 a) selects the file-name declarative D1.
      *>    D1 therefore runs D1-P1, D1-EX, D1-P2 and D1-LAST => F1-A FS=10 / F1-B / F1-C.
      *>  . 14.9.49.4 GR7 b) - the status is not a fatal EC-I-O, so control returns to an implicit
      *>    CONTINUE following the READ => AFTER-READ FS=10.
      *>  . 8.4.2.3.4 GR2 - a subscript "greater than the highest permissible occurrence number"
      *>    sets EC-BOUND-SUBSCRIPT; T has 3 occurrences and IDX is 5. With checking ON,
      *>    14.9.49.4 GR3 e) selects the format-3 declarative D2 (no file, level-3 exception-name).
      *>    D2 runs D2-P1, D2-EX, D2-P2 and D2-LAST => F3-A / F3-B / F3-C.
      *>  . 14.9.33.4 GR2 a) - RESUME AT NEXT STATEMENT returns control to the statement following
      *>    the one that raised the condition, so D2-TAIL is never reached, the MOVE did not store,
      *>    and R keeps its VALUE => AFTER-SUB R=00.
      *>  . 14.6.13.1.1 - "All user-defined exception conditions shall be nonfatal", so EC-USER-DEMO is
      *>    NF (Table 13 spells the same). 14.9.49.4 GR3 e) selects D3, SR1 runs D3-P1 / D3-EX / D3-P2 /
      *>    D3-LAST => NF-A / NF-B, and D3-LAST's EXIT SECTION reaches the return mechanism with the
      *>    declarative COMPLETING NORMALLY - no RESUME. 14.9.49.4 GR13 a) then returns control to an
      *>    implicit CONTINUE following the RAISE => AFTER-RAISE. That is the GR13 a) arm; the GR13 b) arm
      *>    (fatal + normal completion => 14.6.12 abnormal termination) is pinned by
      *>    conformance-test:ExceptionConditionConformanceTests.UseF3_UseProcedureIsTheWholeSection_
      *>    ThenGr13bTerminates, which cannot live here because its run does not end normally.
      *>  . NOTHING here is edition-specific except the spelling: the 85 witness of the same rule is
      *>    tests/conformance/85/pb367_use_procedure_is_the_section_85.cob, which reaches D1's return
      *>    through 14.9.14.4 GR2 (EXIT PROGRAM outside a calling runtime element is a CONTINUE)
      *>    because EXIT SECTION and the EXCEPTION CONDITION format are both 2002-and-later.
       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
       >>TURN EC-USER-DEMO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB367USE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb367-use.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS1.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 F1-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01 FS1 PIC XX VALUE "00".
       01 G.
          05 T PIC 9(2) OCCURS 3 TIMES.
       01 IDX PIC 9(2) VALUE 5.
       01 R  PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       D1-P1.
           DISPLAY "F1-A FS=" FS1.
       D1-EX.
           EXIT.
       D1-P2.
           DISPLAY "F1-B".
       D1-LAST.
           DISPLAY "F1-C".
           EXIT SECTION.
       D1-TAIL.
           DISPLAY "F1-TAIL-MUST-NOT-RUN".
           STOP RUN.
       D2 SECTION.
           USE AFTER EC EC-BOUND-SUBSCRIPT.
       D2-P1.
           DISPLAY "F3-A".
       D2-EX.
           EXIT.
       D2-P2.
           DISPLAY "F3-B".
       D2-LAST.
           DISPLAY "F3-C".
           RESUME AT NEXT STATEMENT.
       D2-TAIL.
           DISPLAY "F3-TAIL-MUST-NOT-RUN".
           STOP RUN.
       D3 SECTION.
           USE AFTER EC EC-USER-DEMO.
       D3-P1.
           DISPLAY "NF-A".
       D3-EX.
           EXIT.
       D3-P2.
           DISPLAY "NF-B".
       D3-LAST.
           EXIT SECTION.
       D3-TAIL.
           DISPLAY "NF-TAIL-MUST-NOT-RUN".
           STOP RUN.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT F1.
           WRITE F1-REC FROM "AAA".
           CLOSE F1.
           OPEN INPUT F1.
           READ F1.
           READ F1.
           DISPLAY "AFTER-READ FS=" FS1.
           CLOSE F1.
           MOVE T (IDX) TO R.
           DISPLAY "AFTER-SUB R=" R.
           RAISE EXCEPTION EC-USER-DEMO.
           DISPLAY "AFTER-RAISE".
           STOP RUN.
