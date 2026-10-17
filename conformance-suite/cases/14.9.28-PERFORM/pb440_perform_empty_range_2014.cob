*>--- conformance-case
*> id: pb440_perform_empty_range_2014
*> rules: [GR-14.9.28.4-4, GR-14.9.28.4-8, GR-14.9.28.4-9,
*>    GR-14.9.28.4-13]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB440 - PERFORM of an EMPTY procedure range, at --std 2014. The full derivation and both
      *> measured arms are in conformance-suite/cases/14.9.28-PERFORM/pb440_perform_empty_range_2023.cob; this is the 2014
      *> WITNESS, because the rule is edition-INDEPENDENT and the defect had no edition arm.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN:
      *> 14.4.2  - "A section consists of a section header followed by zero, one, or more successive
      *>            paragraphs", so EMPTY-SEC is legal and 14.9.28.4 GR4's specified set is empty.
      *> GR4     - "An inline PERFORM statement and an out-of-line PERFORM statement function identically":
      *>            the two VARYING phrases below are the same phrase written both ways => same answer.
      *> GR13 a) - all induction variables are set to their initialization values BEFORE any transfer, so
      *>            VALUE 7 is erased: A=1,B=1 at entry.
      *> GR13 e) - B walks 1,2,3 and resets to 1 as A is augmented; the statement ends with A=4, B=1.
      *> GR5     - an empty set has no first statement, so no transfer takes place and TAIL-SEC never runs;
      *>            GR8 (the set executed once) and GR9 (three times) both leave N=0.
      *> ⛔ THE INLINE TWIN'S BODY IS `CONTINUE`, AND THAT IS THE RULE, NOT A CONCESSION (kb/Work PB396).
      *> 14.9.28.2 Format 2 prints `PERFORM [ times|until|varying ] imperative-statement-1 END-PERFORM`
      *> with imperative-statement-1 UNBRACKETED, and 5.2.6.2 gives the omission licence to BRACKETED
      *> portions only — so the standard offers NO inline spelling of an empty imperative-statement-1.
      *> The EMPTY procedure RANGE this program is about is an out-of-line fact (14.4.2 lets a section
      *> consist of zero paragraphs); its GR4 inline twin is a body that DOES NOTHING, which 14.9.9 spells
      *> CONTINUE — "a no operation" — so the loop's induction-variable behaviour, and this program's
      *> expected output, are unchanged. Written empty, the statement parsed only because the grammar
      *> said `statementBlock*`; it is now COBOLNET2072.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB440EMPTY14.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 7.
       01 B PIC 9 VALUE 7.
       01 C PIC 9 VALUE 7.
       01 D PIC 9 VALUE 7.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN SECTION.
       MAIN-P.
           PERFORM EMPTY-SEC
                   VARYING A FROM 1 BY 1 UNTIL A > 3
                     AFTER B FROM 1 BY 1 UNTIL B > 3
           PERFORM VARYING C FROM 1 BY 1 UNTIL C > 3
                     AFTER D FROM 1 BY 1 UNTIL D > 3
               CONTINUE
           END-PERFORM
           DISPLAY "OUTOFLINE A=" A " B=" B
           DISPLAY "INLINE    C=" C " D=" D
           MOVE 0 TO N
           PERFORM EMPTY-SEC THRU EMPTY-SEC
           DISPLAY "THRUBASIC N=" N
           MOVE 0 TO N
           PERFORM EMPTY-SEC THRU EMPTY-SEC 3 TIMES
           DISPLAY "THRUTIMES N=" N
           STOP RUN.
       EMPTY-SEC SECTION.
       TAIL-SEC SECTION.
       TAIL-P.
           ADD 1 TO N.
