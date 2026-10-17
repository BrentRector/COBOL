      *>--- conformance-case
      *> id: pb433_perform_range_one_declarative_section
      *> rules: [SR-14.9.28.3-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.28.3 SR11, whole: "When procedure-name-1 and procedure-name-2 are both specified and either
      *> is the name of a procedure in the declaratives portion of the procedure division, both shall be
      *> procedure-names in the same declarative section."
      *>
      *> The POSITIVE side of that rule — the three shapes it does NOT forbid — so the COBOLNET2186 screen
      *> (kb/Work PB433) cannot be written as "any PERFORM that touches the declaratives". The rule is unchanged
      *> from X3.23-1985, so 85 is its introducing edition and this golden is written here.
      *>
      *> LEG 1 — PERFORM D-P1 THRU D-P3, both ends procedures of the SAME declarative section. SR11's antecedent
      *> is true (both names specified, both in the declaratives portion) and its requirement is met, so the
      *> statement conforms and §14.9.28.4 GR4 defines the set: "all statements beginning with the first
      *> statement of procedure-name-1 and ending with the last statement of procedure-name-2" — D-P1, D-P2 and
      *> D-P3 in that order, 1 + 10 + 100. It FAILS if the SR11 screen refuses a legal range (over-rejection), or
      *> if the range stops short of D-P3 (GR4's end is procedure-name-2's LAST statement).
      *>
      *> LEG 2 — PERFORM D-SEC, ONE procedure-name naming a whole declarative section. SR11 is conditioned on
      *> "procedure-name-1 and procedure-name-2 are BOTH specified", so it says nothing here; §14.9.49.3 SR4
      *> ("Procedure-names within a declarative section may be referenced ... in a nondeclarative procedure only
      *> with a PERFORM statement") is what makes the reference legal. The set is the section's own paragraph
      *> range (§14.9.28.4 GR4 with §14.9.49.3 SR1 — the USE sentence is not part of the use procedure's
      *> statements), so again 1 + 10 + 100. It FAILS if a one-name PERFORM is dragged into the SR11 screen.
      *>
      *> LEG 3 — PERFORM MAIN-A THRU MAIN-B, both ends NONdeclarative. SR11's antecedent is false, so the range
      *> is unconstrained by it: 5 + 50. It FAILS if the screen tests "is this a THRU range" rather than "is
      *> either end in the declaratives portion".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB433DECLRANGE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb433-decl-range.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 F1-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 N PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       D-P1.
           ADD 1 TO N.
       D-P2.
           ADD 10 TO N.
       D-P3.
           ADD 100 TO N.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-P.
           PERFORM D-P1 THRU D-P3
           DISPLAY "L1=" N
           MOVE 0 TO N
           PERFORM D-SEC
           DISPLAY "L2=" N
           MOVE 0 TO N
           PERFORM MAIN-A THRU MAIN-B
           DISPLAY "L3=" N
           STOP RUN.
       MAIN-A.
           ADD 5 TO N.
       MAIN-B.
           ADD 50 TO N.
