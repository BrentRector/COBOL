*>--- conformance-case
*> id: l1_move_overlap_same_entry
*> rules: [GR-14.9.25.4-10]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.25.4 GR10 MOVE statement — "Additional rules and explanations relative to this
      *> statement are given in 14.6.10, Overlapping operands."
      *>   python scripts/spec/cite.py --check 14.9.25.4 "Additional rules and explanations relative
      *>   to this statement are given in 14.6.10, Overlapping operands."  ->  OK  §14.9.25.4 10)
      *>
      *> GR10 IS A CROSS-REFERENCE, SO WHAT IT OBLIGES IS §14.6.10 — held through a MOVE. §14.6.10
      *> has two normative rules and only ONE of them can be pinned:
      *>   rule 1  "When the data items are not described by the same data description entry, the
      *>           result of the statement is undefined." — an UNCONSTRAINED case; every behaviour
      *>           conforms and no golden can exist for it (a REDEFINES window over the sender is
      *>           exactly this case, and its result would pin an implementation choice).
      *>   rule 2  "When the data items are described by the same data description entry, the result
      *>           of the statement is the same as if the data items shared no part of their
      *>           respective storage areas." — a DEFINITE result, and the whole content of this
      *>           golden.
      *>   python scripts/spec/cite.py --check 14.6.10 "When the data items are described by the
      *>   same data description entry, the result of the statement is the same as if the data items
      *>   shared no part of their respective storage areas."  ->  OK  §14.6.10 2)
      *>
      *> ⛔ WHICH MOVES §14.6.10 ACTUALLY REACHES. Its opening sentence applies only where "the rules
      *> for the statement do not provide for a specific result"
      *>   python scripts/spec/cite.py --check 14.6.10 "the rules for the statement do not provide
      *>   for a specific result"  ->  OK  §14.6.10  (Overlapping operands)
      *> and §14.9.25.4 GR6 b) already provides one for two same-item shapes: an alphanumeric-edited
      *> or national-edited operand is UNDEFINED (GR6 b) 1., cite.py --check 14.9.25.4 -> OK) and a
      *> variable-length operand goes through a temporary (GR6 b) 2.). Every case below is therefore
      *> chosen to fall OUTSIDE both, so that §14.6.10 rule 2 is what governs it. NUMERIC-EDITED is
      *> deliberately included precisely because GR6 b) 1. names only the two EDITED ALPHANUMERIC /
      *> NATIONAL categories and not it.
      *> GR6 c) is NOT the specific result §14.6.10 defers to: §14.6.10's opening asks whether the
      *> rules for the statement provide a result IN THE OVERLAP CIRCUMSTANCE, and GR6 b) is the only
      *> MOVE rule written about a sending and a receiving operand that reference the same data item.
      *> GR6 c) would in any case give the same four values, so nothing here turns on which reading
      *> is preferred.
      *>   python scripts/spec/cite.py --check 14.9.25.4 "When the receiving data item is described
      *>   with the same usage specification as the sending operand, the data in the sending operand
      *>   is transferred to the receiving data item without change."  ->  OK  §14.9.25.4 6) 2.
      *>
      *> EXPECTED OUTPUT, derived from rule 2 — "as if the data items shared no part of their
      *> storage areas" means the result equals a move of the sender's ORIGINAL content into a
      *> distinct item of the same description, i.e. the content is preserved exactly:
      *>   ELEM   fixed-length alphanumeric, sender and receiver the same entry. Not GR6 b) 1. (not
      *>          edited), not GR6 b) 2. (not variable-length), so rule 2 governs: [ABCDE].
      *>   GRP    a group receiver is not an elementary move, so GR4 treats it "exactly as if it
      *>          were an alphanumeric to alphanumeric elementary move, except that there is no
      *>          conversion of data from one form of internal representation to another"
      *>          (cite.py --check 14.9.25.4 -> OK, rule 4). Equal length, rule 2: [PQ|456].
      *>   NUM    the numeric store path. Rule 2 preserves the value, read out through PIC -999.99
      *>          (§13.18.40.5 rule 5, Table 8: '-' renders as the minus character for a negative
      *>          value) with the fraction aligned by decimal point: [-012.34].
      *>   EDIT   numeric-edited to itself. GR5 — "De-editing takes place only when the sending
      *>          operand is a numeric-edited data item and the receiving item is a numeric or a
      *>          numeric-edited data item" (cite.py --check 14.9.25.4 -> OK, rule 5) — so the move
      *>          de-edits to -123 and re-edits; rule 2 requires the same answer a non-overlapping
      *>          pair would give, which by Table 8 is [-123].
      *> §14.6.10 rule 2's only reachable circumstance is the SELF-MOVE: two data items described by
      *> the same data description entry can share storage only by being the same occurrence, since
      *> the clause's closing paragraph excludes reference modification ("the unique data item
      *> produced by reference modification is not considered to be the same data description entry
      *> as any other data description entry", cite.py --check 14.6.10 -> OK, rule 2) and distinct
      *> OCCURS elements do not overlap. What this golden pins is that all four store paths — fixed-
      *> length alphanumeric, group, numeric and numeric-edited — leave a self-move's content EXACTLY
      *> as a non-overlapping pair would, and that none of them is diverted into GR6 b)'s undefined
      *> arm. (Note that these four are full self-identities, so the golden does NOT discriminate
      *> against a byte-by-byte in-place copy, which is the identity on each of them.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1MVOVL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X    PIC X(5)     VALUE "ABCDE".
       01 W-N    PIC S9(3)V99 VALUE -12.34.
       01 W-NE   PIC -999.
       01 W-E    PIC -999.99.
       01 W-G.
           05 W-GP PIC X(2) VALUE "PQ".
           05 W-GQ PIC 9(3) VALUE 456.
       PROCEDURE DIVISION.
       MAIN.
           MOVE W-X TO W-X
           DISPLAY "ELEM=[" W-X "]"
           MOVE W-G TO W-G
           DISPLAY "GRP=[" W-GP "|" W-GQ "]"
           MOVE W-N TO W-N
           MOVE W-N TO W-E
           DISPLAY "NUM=[" W-E "]"
           MOVE -123 TO W-NE
           MOVE W-NE TO W-NE
           DISPLAY "EDIT=[" W-NE "]"
           STOP RUN.
