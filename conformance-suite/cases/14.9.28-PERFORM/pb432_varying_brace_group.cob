      *>--- conformance-case
      *> id: pb432_varying_brace_group
      *> rules: [SR-14.9.28.3-3, SR-14.9.28.3-4, SR-14.9.28.3-5,
      *>    FMT-14.9.28.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB432 — the §14.9.28.2 varying-phrase FROM/BY brace group, every alternative it prints,
      *> at the edition the phrase has existed at since. Rendered from the printed page 683 / PDF 713:
      *>     VARYING {identifier-2|index-name-1} FROM {identifier-3|index-name-2|literal-1}
      *>             [ BY {identifier-4|literal-2} ] UNTIL condition-1
      *>
      *> EXPECTED VALUES, DERIVED FROM THE RULES AND NOT FROM A RUN:
      *>
      *> A  literal-1 written as the FIGURATIVE ZERO. §14.9.28.3 SR3 restricts every literal in this phrase to
      *>    NUMERIC, which is exactly the precondition of §8.3.3.6.3 SR1 ("If the literal is restricted to a
      *>    numeric literal, the only figurative constant permitted is ZERO"), so ZERO is admitted and denotes 0
      *>    (§8.3.3.6.4). §14.9.28.4 GR13 a) initializes I to 0; GR13 e) tests I > 3 before each iteration and augments by
      *>    1 after it, so the body runs for I = 0,1,2,3 => NA = 4. This spelling used to be a hard PARSE ERROR.
      *> B  the plural spelling ZEROS is the same figurative constant (§8.3.3.6.2 Format 1) => NB = 3 (I = 0,1,2).
      *> C  a SIGNED literal-2. §8.3.3.3.2 rule 2 makes the sign part of the literal, so -1 is literal-2 and not
      *>    an expression; GR12's augment value is then -1 and the induction variable DECREMENTS: I = 5,4,3,2,1
      *>    => NC = 5.
      *> D  index-name-2 in the FROM phrase. GR12: "the occurrence number corresponding to the value of the index
      *>    referenced by the index-name ... is referred to as the initialization value", and SET IX2 TO 2 makes
      *>    that occurrence number 2, so IX1 runs 2,3,4 => ND = 3.
      *> E  identifier-3 and identifier-4. GR12 re-reads the BY operand at every augment, and nothing changes it
      *>    here, so I = 4,8,12 and the test I > 12 ends it => NE = 3.
      *>
      *>   NA=0004 NB=0003 NC=0005 ND=0003 NE=0003
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB432BRACE85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC X(4) OCCURS 10 TIMES INDEXED BY IX1 IX2.
       01 I PIC S9(4).
       01 P4 PIC 9(4) VALUE 4.
       01 NA PIC 9(4) VALUE 0.
       01 NB PIC 9(4) VALUE 0.
       01 NC PIC 9(4) VALUE 0.
       01 ND PIC 9(4) VALUE 0.
       01 NE PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM VARYING I FROM ZERO BY 1 UNTIL I > 3
               ADD 1 TO NA
           END-PERFORM.
           PERFORM VARYING I FROM ZEROS BY 1 UNTIL I > 2
               ADD 1 TO NB
           END-PERFORM.
           PERFORM VARYING I FROM 5 BY -1 UNTIL I < 1
               ADD 1 TO NC
           END-PERFORM.
           SET IX2 TO 2.
           PERFORM VARYING IX1 FROM IX2 BY 1 UNTIL IX1 > 4
               ADD 1 TO ND
           END-PERFORM.
           PERFORM VARYING I FROM P4 BY P4 UNTIL I > 12
               ADD 1 TO NE
           END-PERFORM.
           DISPLAY "NA=" NA " NB=" NB " NC=" NC " ND=" ND " NE=" NE.
           STOP RUN.
