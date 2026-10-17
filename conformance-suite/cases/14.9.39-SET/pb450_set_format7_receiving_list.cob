*>--- conformance-case
*> id: pb450_set_format7_receiving_list
*> rules: [SR-14.9.39.3-17, SR-14.9.39.3-18, SR-14.9.39.3-19,
*>    GR-14.9.39.4-12, GR-14.9.39.4-13, FMT-14.9.39.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> !! SET FORMAT 7'S RECEIVING OPERAND IS A REPEATED BRACE, AND THE TWO SPELLINGS MIX. kb/Work PB450.
      *>
      *> ISO 14.9.39.2 Format 7, RENDERED from the printed page (PDF p760 / folio 730), is
      *>     SET { ADDRESS OF data-name-1 | identifier-5 } ... TO identifier-6
      *> The brace is a PLAIN required choice - exactly one of the two spellings - and the `...` sits OUTSIDE
      *> it, so a Format-7 statement carries one or more receiving operands, each independently either
      *> spelling and mixable in any order, over ONE sender. Its own figure note says so in words: "The braces
      *> are a plain required choice - exactly one of the two alternatives - and the following `...` repeats
      *> the braced receiving operand" (python scripts/spec/cite.py --check 14.9.39.2
      *> "repeats the braced receiving operand" -> OK).
      *>
      *> Before this the grammar held TWO fixed productions split on the SENDER's spelling, each hard-coding
      *> arity one, so every statement below was `error COBOL0001` at every edition and neither SR17 nor SR18
      *> was ever asked about a second operand.
      *>
      *> Expected values, COMPUTED FROM THE STANDARD (not measured):
      *>   14.9.39.4 GR12 - "If identifier-5 is specified, the address identified by identifier-6 is stored in
      *>     each data item referenced by identifier-5 in the order specified" (--check OK).
      *>   14.9.39.4 GR13 - "If data-name-1 is specified, the address identified by identifier-6 is assigned
      *>     to each based item referenced by data-name-1 in the order specified" (--check OK).
      *>   8.4.3.11.4 GR1 - "Data-address-identifier creates a unique data item of class pointer and category
      *>     data-pointer that contains the address of identifier-1" (--check OK), which is what lets
      *>     `ADDRESS OF x` stand as identifier-6, and 8.6.5 makes a based item's own value its implicit
      *>     data-address pointer, so a rebased B1 reads through whatever record its address names.
      *>   T1: P1 and P2 both take &REC-A (GR12); B1 and B2 are both then based at &REC-A (GR13) -> "AAAA AAAA"
      *>   T2: the MIXED statement `SET ADDRESS OF B1 P2 TO P3` gives B1 the address in P3 (&REC-B, GR13) and
      *>       stores that same address into P2 (GR12); B2 then rebases from P2          -> "BBBB BBBB"
      *>   T3: both operands are data-address-identifiers                                 -> "AAAA"
      *>   T4: a THREE-operand mixed list, proving the repetition is not two              -> "BBBB BBBB BBBB"
      *>
      *> !! WHAT THIS GOLDEN DELIBERATELY DOES NOT CLAIM: GR12's "in the order specified" is only OBSERVABLE
      *> through its second sentence ("Item identification of the data item referenced by identifier-5 is done
      *> immediately before the value of that data item is changed"), which needs a SUBSCRIPTED identifier-5.
      *> 13.18.60.3 SR14 makes that unwritable - a USAGE POINTER item may appear only at level 1 or under a
      *> STRONG type declaration, so there is no conforming pointer table to subscript - and one sender
      *> address reaches every receiver, so no ordering difference can show. The claim here is the LIST and the
      *> mixed brace; the ordering sentence has no conforming witness (feedback_verdict_evidence_invariant).
      *>
      *> Format 7 is a COBOL-2002 introduction (constructs.json usage-pointer-2002 / SetAddress2002), which is
      *> why this lives at 2002; negative/pb450-set-address-second-operand-below-2002 pins the edge BELOW it at
      *> the second operand, and negative/pb450-set-format7-second-receiver-not-pointer pins SR17 there.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB450F7LIST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC-A PIC X(4) VALUE "AAAA".
       01 REC-B PIC X(4) VALUE "BBBB".
       01 P1 USAGE POINTER.
       01 P2 USAGE POINTER.
       01 P3 USAGE POINTER.
       01 B1 BASED.
          05 B1A PIC X(4).
       01 B2 BASED.
          05 B2A PIC X(4).
       01 B3 BASED.
          05 B3A PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           SET P1 P2 TO ADDRESS OF REC-A
           SET ADDRESS OF B1 ADDRESS OF B2 TO P1
           DISPLAY "T1 " B1A " " B2A
           SET P3 TO ADDRESS OF REC-B
           SET ADDRESS OF B1 P2 TO P3
           SET ADDRESS OF B2 TO P2
           DISPLAY "T2 " B1A " " B2A
           SET ADDRESS OF B1 TO ADDRESS OF REC-A
           DISPLAY "T3 " B1A
           SET ADDRESS OF B1 P1 ADDRESS OF B3 TO P3
           SET ADDRESS OF B2 TO P1
           DISPLAY "T4 " B1A " " B2A " " B3A
           DISPLAY "DONE"
           STOP RUN.
