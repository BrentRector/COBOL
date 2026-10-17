*>--- conformance-case
*> id: pb437_varying_operand_windows
*> rules: [GR-14.9.28.4-12]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB437 — the §14.9.28.4 GR12 evaluation WINDOW of a varying-phrase operand, proved by
      *> CARDINALITY: "Item identification for identifier-3, identifier-4, identifier-6, identifier-7,
      *> index-name-2, and index-name-4 is done each time the content of the data item referenced by the
      *> identifier or the index referenced by the index-name is used in a SETTING or AUGMENTING operation."
      *> Each operand below carries a FUNCTION reference in its SUBSCRIPT (§15.4 — the temporary store is a
      *> pre-op registered exactly like a user-function activation), so an operand read once per STATEMENT and
      *> one read once per OPERATION give DIFFERENT answers, which is what makes this a test.
      *>
      *> Both statements were REJECTED outright before PB437 (COBOLNET1509), in slots the compiler accepted one
      *> operand position over.
      *>
      *> EXPECTED VALUES, DERIVED FROM THE RULES AND NOT FROM A RUN. W-E holds 1,2,3,4,5.
      *>
      *> LEG 1 — the BY operand, per AUGMENT. K starts at 1 and the body adds 1 to it; GR13's closing paragraph
      *>   gives that change immediate effect, and GR12 re-identifies the BY operand at every augment.
      *>     I=1 (GR13 a)   test 1>6 false  body: N=1, K=2   augment BY W-E(2)=2  -> I=3
      *>     test 3>6 false  body: N=2, K=3  augment BY W-E(3)=3  -> I=6
      *>     test 6>6 false  body: N=3, K=4  augment BY W-E(4)=4  -> I=10
      *>     test 10>6 TRUE -> N=0003, and I holds 0010 (captured into I1 before LEG 2 reuses I).
      *>   A once-per-statement read would hold BY at W-E(1)=1 forever and give N=0006.
      *>
      *> LEG 2 — an AFTER level's FROM operand, per outer AUGMENT. GR13 e) 2's TRUE arm re-initializes the inner
      *>   variable from its FROM operand a. BEFORE b./c. augment the outer one, so the reset reads the
      *>   PRE-augment I (GR12 re-identifies the FROM operand at every setting operation).
      *>     GR13 a): I=1, J=W-E(1)=1
      *>     I=1: J=1,2,3 -> M=3;  reset J=W-E(1)=1;  I=2
      *>     I=2: J=1,2,3 -> M=6;  reset J=W-E(2)=2;  I=3
      *>     I=3: J=2,3   -> M=8;  reset J=W-E(3)=3;  I=4
      *>     test 4>3 TRUE -> M=0008.
      *>   A once-per-statement read would hold the AFTER FROM at W-E(1)=1 and give M=0009.
      *>
      *>   N=0003 I=0010 M=0008
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB437WINDOWS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-G.
          05 W-E PIC 9(2) OCCURS 5 TIMES.
       01 K PIC 9(2) VALUE 1.
       01 I PIC 9(4).
       01 I1 PIC 9(4).
       01 J PIC 9(4).
       01 N PIC 9(4) VALUE 0.
       01 M PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 1 TO W-E (1).
           MOVE 2 TO W-E (2).
           MOVE 3 TO W-E (3).
           MOVE 4 TO W-E (4).
           MOVE 5 TO W-E (5).
           PERFORM VARYING I FROM 1 BY W-E (FUNCTION INTEGER(K)) UNTIL I > 6
               ADD 1 TO N
               ADD 1 TO K
           END-PERFORM.
           MOVE I TO I1.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
                   AFTER J FROM W-E (FUNCTION INTEGER(I)) BY 1 UNTIL J > 3
               ADD 1 TO M
           END-PERFORM.
           DISPLAY "N=" N " I=" I1 " M=" M.
           STOP RUN.
