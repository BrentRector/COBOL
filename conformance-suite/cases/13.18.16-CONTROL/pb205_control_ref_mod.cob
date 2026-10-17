*>--- conformance-case
*> id: pb205_control_ref_mod
*> rules: [SR-13.18.16.3-1, SR-13.18.16.3-4, SR-13.18.16.3-6, GR-13.18.16.4-1, GR-13.18.16.4-3, GR-13.18.16.4-5, SR-13.18.54.3-8, SR-13.18.57.3-10]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.16.3 SR4: "Data-name-1 may be reference-modified. If it is,
      *> leftmost-position and length shall be integer literals." 13.18.16.4 GR3
      *> then defines the prior control as "having the same data description as
      *> the corresponding data item" - for a reference-modified operand that is
      *> the 8.4.3.3.4 GR5 unique data item, THE SLICE. So a control break is
      *> sensed on the slice, never on the whole item.
      *>
      *> This witness puts TWO control operands over ONE physical item, which
      *> SR6 expressly permits ("Two or more instances of data-name-1 in the
      *> same clause may, however, refer to the same physical data item or to
      *> overlapping data items"), and it is the shape that separates the rule
      *> from the defect: before kb/Work PB205 the ref-mod was dropped at
      *> capture, so BOTH operands bound to the whole 6-character CX, every
      *> GENERATE broke, and both TYPE CF operands matched control level 0 by
      *> NAME - the minor footing never printed at all.
      *>
      *> It also exercises the two sibling clauses that name a control operand
      *> the same way: 13.18.57.3 SR10 ("Each data-name-1 ... shall be the same
      *> as one of the operands of the CONTROL clause"; ref-mod permitted, same
      *> integer-literal restriction) on each TYPE CF, and 13.18.54.3 SR8 on
      *> SUM ... RESET ON - whose operand is the MAJOR control while the counter
      *> prints in the MINOR footing, the level ordering SR8 requires ("If the
      *> current report group is a control footing, its level of control shall
      *> be a lower level than that of data-name-3").
      *>
      *> The omitted length CX(4:) is 8.4.3.3.2's bracketed [ length ]: the
      *> slice "extends from and includes the position identified by
      *> leftmost-position up to and including the rightmost position"
      *> (8.4.3.3.4 rule 5c), i.e. positions 4-6.
      *>
      *> NO PAGE CLAUSE, DELIBERATELY: 13.18.39.4 GR2a - "If integer-1 is not
      *> specified, the report consists of a single page of indefinite length" -
      *> so no page-fit or FIRST DETAIL geometry enters the
      *> expected output and every line below is derived from the CONTROL / SUM
      *> rules alone. The readback SKIPS ALL-SPACE LINES for the same reason, and
      *> that is not cosmetic: this compiler currently places the FIRST body group
      *> one line too low - 13.18.35.4 GR5c gives an unpaged relative group the
      *> line number LINE-COUNTER + integer-2, and 14.9.21.4 GR1b sets
      *> LINE-COUNTER to zero at INITIATE, so LINE PLUS 1 is line 1 and there is
      *> no blank line above it, while the emitted file carries one (the paged
      *> twin of the same +1 is visible in 2002/rw_suppress_bare.out, where
      *> FIRST DETAIL 3 lands the first group on line 4 against GR5b3). That
      *> placement bug is a DIFFERENT mechanism; a witness that leans on a second
      *> gap stops witnessing the moment that gap closes, so this one asserts the
      *> sequence of PRINTED lines and nothing about their vertical position.
      *>
      *> Expected, derived (LINE PLUS 1 per group; counters M = SUM reset at its
      *> own group per 13.18.54.4 GR2, R = SUM RESET ON the major control, J =
      *> the major footing's SUM; adds happen with the detail, after the break
      *> processing of 14.9.16.4 GR5a):
      *>   G1 AAA|111 10  first GENERATE saves the priors      -> AMT=10
      *>   G2 AAA|222 20  minor breaks (111 -> 222)            -> MIN=010 RUN=010
      *>                                                          AMT=20
      *>   G3 AAA|222 05  no break                             -> AMT=05
      *>   G4 BBB|111 30  major breaks (AAA -> BBB): footings
      *>                  print minor then major (13.14)       -> MIN=025 RUN=035
      *>                                                          MAJ=035
      *>                                                          AMT=30
      *>   G5 BBB|111 40  no break                             -> AMT=40
      *>   TERMINATE      as a highest-level break (GR5)       -> MIN=070 RUN=070
      *>                                                          MAJ=070
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB205CTL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb205-control-ref-mod.rpt".
           SELECT RBACK ASSIGN TO "pb205-control-ref-mod.rpt"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       FD RBACK.
       01 RB-REC PIC X(30).
       WORKING-STORAGE SECTION.
       01 CX      PIC X(6) VALUE SPACES.
       01 WS-AMT  PIC 99   VALUE 0.
       01 WS-EOF  PIC 9    VALUE 0.
       REPORT SECTION.
       RD R-1 CONTROLS ARE CX(1:3) CX(4:).
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(4) VALUE "AMT=".
          02 COLUMN 5 PIC 99 SOURCE IS WS-AMT.
       01 TYPE CF CX(4:) LINE PLUS 1.
          02 COLUMN 1 PIC X(4) VALUE "MIN=".
          02 COLUMN 5 PIC 999 SUM WS-AMT.
          02 COLUMN 9 PIC X(4) VALUE "RUN=".
          02 COLUMN 13 PIC 999 SUM WS-AMT RESET ON CX(1:3).
       01 TYPE CF CX(1:3) LINE PLUS 1.
          02 COLUMN 1 PIC X(4) VALUE "MAJ=".
          02 COLUMN 5 PIC 999 SUM WS-AMT.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           MOVE "AAA111" TO CX
           MOVE 10 TO WS-AMT
           GENERATE DET-A
           MOVE "AAA222" TO CX
           MOVE 20 TO WS-AMT
           GENERATE DET-A
           MOVE "AAA222" TO CX
           MOVE 5 TO WS-AMT
           GENERATE DET-A
           MOVE "BBB111" TO CX
           MOVE 30 TO WS-AMT
           GENERATE DET-A
           MOVE "BBB111" TO CX
           MOVE 40 TO WS-AMT
           GENERATE DET-A
           TERMINATE R-1
           CLOSE RPT
           OPEN INPUT RBACK
           PERFORM UNTIL WS-EOF = 1
               READ RBACK
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END
                       IF RB-REC NOT = SPACES
                           DISPLAY "L=" RB-REC
                       END-IF
               END-READ
           END-PERFORM
           CLOSE RBACK
           STOP RUN.
