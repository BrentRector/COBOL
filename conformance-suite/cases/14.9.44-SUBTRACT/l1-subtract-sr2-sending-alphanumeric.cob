      *>--- conformance-case
      *> id: l1-subtract-sr2-sending-alphanumeric
      *> rules: [SR-14.9.44.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.44.3 SR2 (FORMATS 1 AND 2) — "Identifier-1 and
      *> identifier-2 shall reference numeric data items."
      *> THE SENDING ARM.  In format 1 identifier-1 is the subtrahend,
      *> and XA is described PIC X(4).  §8.5.2.12 enumerates what is a
      *> data item of category numeric and closes with "Such an item
      *> is referred to as a numeric data item"; an alphanumeric
      *> PICTURE is on no line of that list, so XA is not a numeric
      *> data item and `SUBTRACT XA FROM N` violates SR2.  The
      *> receiver N is PIC 9(4), so the receiving half of the same
      *> rule is satisfied and cannot be the reason for the
      *> rejection.
      *> Its sibling arms are
      *> negative/l1-subtract-sr2-receiver-numeric-edited (format
      *> 1's in-place identifier-2) and
      *> negative/l1-subtract-sr2-format2-minuend-alphanumeric
      *> (format 2's SENDING identifier-2).  SR2 heads FORMATS 1
      *> AND 2, so its two identifiers occupy THREE positions on
      *> three different binder paths, and this repository's most
      *> reproducible defect shape is a dispatch of which only one
      *> arm was ever fixed.
      *> Reject-at names every edition: SR2 carries no edition
      *> condition and the screen it drives has no edition axis.
      *> NOTE ON THE DIAGNOSTIC.  The compiler reports this through
      *> the §8.8.1.1 operand-class screen (COBOLNET0844) rather than
      *> by SR2's number; the REJECTION is what SR2 obliges, and
      *> §8.8.1.1 is co-extensive with SR2 in a sending position.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SBSR2A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XA PIC X(4) VALUE "0012".
       01 N  PIC 9(4) VALUE 50.
       PROCEDURE DIVISION.
       MAIN.
           SUBTRACT XA FROM N.
           STOP RUN.
