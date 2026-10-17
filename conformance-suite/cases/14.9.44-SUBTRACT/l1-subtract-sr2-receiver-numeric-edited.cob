      *>--- conformance-case
      *> id: l1-subtract-sr2-receiver-numeric-edited
      *> rules: [SR-14.9.44.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.44.3 SR2 (FORMATS 1 AND 2) — "Identifier-1 and
      *> identifier-2 shall reference numeric data items."
      *> THE RECEIVING ARM, and the axis that separates SR2 from SR4.
      *> In format 1 identifier-2 is the in-place receiver.  NE is
      *> described PIC ZZZ9, which §8.5.2.13 makes "a data item of
      *> category numeric-edited … referred to as a numeric-edited
      *> data item"; §8.5.2.12's closed list of what is a numeric data
      *> item does not contain it.  SR2 says NUMERIC, full stop, so
      *> `SUBTRACT 1 FROM NE` is illegal.
      *> THE DERIVATION IS INTERNAL TO THE CLAUSE AND NEEDS NO OTHER
      *> AUTHORITY: SR4, three rules later in the same syntax-rule
      *> list, says "Identifier-3 shall reference a numeric data item
      *> OR A NUMERIC-EDITED DATA ITEM" for the GIVING resultant.  A
      *> standard that spells the second category out at SR4 and omits
      *> it at SR2 has excluded it at SR2.  The positive control for
      *> the admitting side is 2023/l1_subtract_giving_resultant_-
      *> categories, whose `SUBTRACT 1 FROM 20 GIVING G NE` stores
      *> into this very PICTURE and must COMPILE AND RUN.
      *> Its sibling arms are
      *> negative/l1-subtract-sr2-sending-alphanumeric (format 1's
      *> identifier-1) and
      *> negative/l1-subtract-sr2-format2-minuend-alphanumeric
      *> (format 2's SENDING identifier-2, which SR2's "FORMATS 1
      *> AND 2" heading also governs).
      *> Reject-at names every edition: SR2 carries no edition
      *> condition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SBSR2B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NE PIC ZZZ9.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 20 TO NE.
           SUBTRACT 1 FROM NE.
           STOP RUN.
