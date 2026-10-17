*>--- conformance-case
*> id: pb440_perform_range_composition
*> rules: [GR-14.9.28.4-4, GR-14.9.28.4-5, GR-14.9.28.4-6, GR-14.9.28.4-8, GR-14.9.28.4-9]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB440 - how an out-of-line PERFORM's SPECIFIED SET OF STATEMENTS is composed (ISO 14.9.28.4
      *> GR4/GR5/GR6). Every shape below was previously pinned ONLY by AssertSameAsLegacy differentials in
      *> SectionDifferentialTests, which measure the legacy oracle and can close no row; this program measures
      *> the rule.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN:
      *>
      *> EMPTYPARA - EMPTY-PARA is a paragraph with NO sentences. It still has its own place in the procedure
      *>   sequence, so GR4's set is "all statements beginning with the first statement of procedure-name-1 and
      *>   ending with the last statement of procedure-name-1" over a paragraph that has none: nothing runs, and
      *>   GR5 a)'s return mechanism ("after the last statement of procedure-name-1") fires immediately, so the
      *>   FOLLOWING paragraph PA does NOT run. Three times over nothing is still nothing => N=0000.
      *>   (This is the case that must stay correct after PB440 made the empty SECTION a first-class range.)
      *> PARATHRU  - GR4: PERFORM PA THRU PC is "all statements beginning with the first statement of PA and
      *>   ending with the last statement of PC" => 1 + 2 + 4, and GR5 b) puts the return after PC's last
      *>   statement, so PD does not run => N=0007.
      *> SECTION   - procedure-name-2 omitted and procedure-name-1 a SECTION: the set runs from the first
      *>   statement of its first paragraph through the last statement of its last (GR4 + 14.4.3), and GR5 a)
      *>   returns there, so NEXT-SEC does not run => 10 + 20 = N=0030.
      *> SAMETHRU  - PERFORM WORK-SEC THRU WORK-SEC names the same section twice; GR4's set is unchanged and
      *>   GR5 b)'s return is after the same last statement => N=0030.
      *> INVERTED  - GR6: "There is no necessary relationship between procedure-name-1 and procedure-name-2
      *>   except that a consecutive sequence of operations is to be executed beginning at the procedure named
      *>   procedure-name-1 and ending with the execution of the procedure named procedure-name-2." INV-A
      *>   physically PRECEDES INV-B and is reached by the GO TO written in INV-B, so the sequence is INV-B then
      *>   INV-A => 200 + 100 = N=0300. (NIST NC102A PFM-TEST-F1-10 is the same shape.)
      *> SPAN      - procedure-name-1 is the EMPTY section EMPTY-SEC (14.4.2). It has no first statement, so the
      *>   consecutive sequence GR6 describes begins at what follows it, and GR5 b) puts the return after
      *>   TAIL-SEC's last statement => TAIL-P runs once => N=1000. The composition needs no special case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB440RANGECOMP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN SECTION.
       MAIN-P.
           MOVE 0 TO N
           PERFORM EMPTY-PARA 3 TIMES
           DISPLAY "EMPTYPARA N=" N
           MOVE 0 TO N
           PERFORM PA THRU PC
           DISPLAY "PARATHRU N=" N
           MOVE 0 TO N
           PERFORM WORK-SEC
           DISPLAY "SECTION N=" N
           MOVE 0 TO N
           PERFORM WORK-SEC THRU WORK-SEC
           DISPLAY "SAMETHRU N=" N
           MOVE 0 TO N
           PERFORM INV-B THRU INV-A
           DISPLAY "INVERTED N=" N
           MOVE 0 TO N
           PERFORM EMPTY-SEC THRU TAIL-SEC
           DISPLAY "SPAN N=" N
           STOP RUN.
       EMPTY-PARA.
       PA.
           ADD 1 TO N.
       PB.
           ADD 2 TO N.
       PC.
           ADD 4 TO N.
       PD.
           ADD 8 TO N.
       WORK-SEC SECTION.
       WORK-A.
           ADD 10 TO N.
       WORK-B.
           ADD 20 TO N.
       NEXT-SEC SECTION.
       NEXT-A.
           ADD 40 TO N.
       INV-SEC SECTION.
       INV-A.
           ADD 100 TO N.
       INV-B.
           ADD 200 TO N
           GO TO INV-A.
       EMPTY-SEC SECTION.
       TAIL-SEC SECTION.
       TAIL-P.
           ADD 1000 TO N.
