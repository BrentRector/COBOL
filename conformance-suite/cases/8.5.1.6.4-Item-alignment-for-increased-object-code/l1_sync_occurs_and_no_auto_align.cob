*>--- conformance-case
*> id: l1_sync_occurs_and_no_auto_align
*> rules: [GR-8.5.1.6.4-1, GR-8.5.1.6.4-2, GR-13.18.55.4-8,
*>    GR-13.18.55.4-10]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.55.4 GR8 + GR10 — the SYNCHRONIZED clause inside a table
      *> applies to EVERY occurrence (and its implicit FILLER is generated per
      *> occurrence), and the OPTIONAL automatic alignment GR10 offers is not
      *> specified by this implementor, so nothing is aligned without the
      *> clause either.
      *>
      *> THE RULES.
      *> §13.18.55.4 GR8: "When the SYNCHRONIZED clause is specified in the
      *> data description entry that contains an OCCURS clause, or in the data
      *> description entry of a data item subordinate to the data description
      *> entry that contains an OCCURS clause, then: a) The SYNCHRONIZED
      *> clause applies to each occurrence of the data item. b) Any implicit
      *> FILLER generated for other data items within that same table is
      *> generated for each occurrence of those data items (see General rule
      *> 9b)."
      *> §13.18.55.4 GR10: "An implementor may optionally specify automatic
      *> alignment for any internal data representations except for bit data
      *> items and, within a record, data items described with usage display
      *> or national. A record itself may be automatically synchronized."
      *>
      *> THE DETERMINATION THESE TWO REST ON, docs/CONFORMANCE.md DOC-A.1-195
      *> (the §13.18.55.4 GR9 obligation, Annex A.1 item 195): COBOL.NET
      *> performs NO physical alignment for SYNCHRONIZED and generates NO
      *> implicit FILLER for it — and "GR10's optional automatic alignment is
      *> not specified, so nothing is aligned without the clause either", and
      *> "GR8 b)'s per-occurrence FILLER is vacuous inside a table". Annex A.1
      *> item 7 makes GR10's documentation obligation optional ("This item is
      *> optional. This item, if provided by the implementor, shall be
      *> documented"), and declining the option is a conforming choice — so
      *> what a golden owes here is the DOCUMENTED choice measured, not a
      *> layout invented to match the code.
      *>
      *> ⚠ WHAT GR8 a) CAN AND CANNOT DISCRIMINATE, SAID PLAINLY. With zero
      *> filler generated anywhere, an occurrence carrying the clause is laid
      *> out exactly like one that does not, so applying GR8 a) to every
      *> occurrence and applying it to none are the same program. This golden
      *> does NOT claim to separate a) from a vacuous a). What it does
      *> separate is the SHAPE: an implementation that aligned the binary leaf
      *> and applied that per occurrence answers SUBORD=12, and one that
      *> applied it to the first occurrence only answers SUBORD=10. The
      *> vacuity itself is earned elsewhere, by the SYNC=3 leg of the shipped
      *> conformance:cases/8.5.1.6.4-Item-alignment-for-increased-object-code/l1_byte_length_implicit_filler, which rules out a
      *> 2-byte alignment for exactly this carrier.
      *>
      *> THE NUMBERS.
      *> SUBORD — GR8's SECOND entry point, the clause on an item SUBORDINATE
      *>   to the OCCURS entry. One occurrence is PIC X + PIC S9(4) COMP =
      *>   1 + 2 (DOC-A.1-209 one byte per character position; DOC-A.1-205 a
      *>   3-4 digit COMP item is 2 bytes) = 3, and three occurrences are 9.
      *>   The second number is the clause-free twin, which GR8 read against
      *>   the item-195 determination requires to agree.
      *> ONOCCURS — GR8's FIRST entry point, the clause written IN the entry
      *>   that carries the OCCURS clause. One byte of prefix plus three
      *>   2-byte COMP occurrences = 1 + 6 = 7, and again its clause-free
      *>   twin. THE ONE-BYTE PREFIX IS WHAT MAKES THIS LEG ABLE TO FAIL —
      *>   without it the occurrences sit on even offsets already (0, 2, 4),
      *>   so an implementation that aligned every occurrence would answer 6
      *>   too and the leg would measure nothing; with the prefix, a 2-byte
      *>   per-occurrence alignment answers 8.
      *> ONE — one occurrence's shape declared OUTSIDE any table: 3. Together
      *>   with SUBORD=9 this is 3 x 3, i.e. the per-occurrence extent inside
      *>   the table is exactly the extent outside it — the arithmetic GR8 b)
      *>   would break if a per-occurrence FILLER were generated.
      *> AUTO — GR10's FIRST sentence, with NO SYNCHRONIZED clause anywhere in
      *>   the group: 1 + 2 + 1 = 4. An implementor who had elected automatic
      *>   alignment for binary would answer 5 or 6 (a pad before the COMP
      *>   item, and a trailing pad if the record were rounded too).
      *> AUTO8 — the same question on the widest carrier, USAGE BINARY-DOUBLE,
      *>   8 bytes (DOC-A.1-207): 1 + 8 + 1 = 10. This is the leg an
      *>   8-byte-boundary alignment policy would answer 17 or 24 on, so it is
      *>   where an automatic-alignment election would be hardest to hide.
      *> REC — GR10's SECOND sentence, "A record itself may be automatically
      *>   synchronized", is NOT observable through a length function, and
      *>   THIS LEG MAKES NO CLAIM ABOUT IT. Automatic synchronization of a
      *>   record is a PLACEMENT (the record begins on a natural boundary),
      *>   and GR6 — "Any adjustment in storage position resulting from the
      *>   SYNCHRONIZED clause does not affect the size of the synchronized
      *>   data item" — keeps the size constant under any such adjustment, so
      *>   an implementor who HAD elected record synchronization would still
      *>   report 3. What REC=3 does exclude is the one shape of that election
      *>   a length CAN see: a policy that padded a record out to a natural
      *>   boundary would answer 4 or 8. Where a record-level rule would
      *>   really show is the file-record legs of
      *>   conformance:cases/13.18.55-SYNCHRONIZED/l1_sync_file_record, which answer 4/4 with no
      *>   padding.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SYN04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1.
          05 T1E OCCURS 3 TIMES.
             10 T1A PIC X.
             10 T1B PIC S9(4) COMP SYNC.
       01 T2.
          05 T2E OCCURS 3 TIMES.
             10 T2A PIC X.
             10 T2B PIC S9(4) COMP.
       01 T3.
          05 T3P PIC X.
          05 T3E PIC S9(4) COMP SYNC OCCURS 3 TIMES.
       01 T4.
          05 T4P PIC X.
          05 T4E PIC S9(4) COMP OCCURS 3 TIMES.
       01 N1.
          05 N1A PIC X.
          05 N1B PIC S9(4) COMP SYNC.
       01 A1.
          05 A1A PIC X.
          05 A1B PIC S9(4) COMP.
          05 A1C PIC X.
       01 A2.
          05 A2A PIC X.
          05 A2B USAGE BINARY-DOUBLE SIGNED.
          05 A2C PIC X.
       01 A3.
          05 A3A PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "SUBORD=" FUNCTION BYTE-LENGTH(T1)
               " " FUNCTION BYTE-LENGTH(T2)
           DISPLAY "ONOCCURS=" FUNCTION BYTE-LENGTH(T3)
               " " FUNCTION BYTE-LENGTH(T4)
           DISPLAY "ONE=" FUNCTION BYTE-LENGTH(N1)
           DISPLAY "AUTO=" FUNCTION BYTE-LENGTH(A1)
           DISPLAY "AUTO8=" FUNCTION BYTE-LENGTH(A2)
           DISPLAY "REC=" FUNCTION BYTE-LENGTH(A3)
           STOP RUN.
