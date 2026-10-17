      *>--- conformance-case
      *> id: l1-subtract-sr2-format2-minuend-alphanumeric
      *> rules: [SR-14.9.44.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.44.3 SR2 (FORMATS 1 AND 2) — "Identifier-1 and
      *> identifier-2 shall reference numeric data items."
      *> THE FORMAT-2 IDENTIFIER-2 ARM.  SR2's heading names FORMATS 1
      *> AND 2, and identifier-2 is not the same POSITION in the two:
      *> in format 1 it is the in-place RECEIVER, while in format 2's
      *> `SUBTRACT … FROM {identifier-2 | literal-2} GIVING …`
      *> (§14.9.44.2) it is the SENDING minuend.  XM is described
      *> PIC X(4), which §8.5.2.3 makes a data item of category
      *> ALPHANUMERIC and which §8.5.2.12's closed list of numeric data
      *> items does not contain, so `SUBTRACT 1 FROM XM GIVING G`
      *> violates SR2 at that third position.
      *> Literal-1 is the numeric literal 1, so SR3 holds; G is
      *> PIC 9(4), a category SR4 admits outright.  Neither can be the
      *> reason for the rejection.
      *> WHY THE OTHER TWO SR2 FIXTURES DO NOT COVER IT.  SR2's two
      *> identifiers occupy THREE positions and they reach the binder
      *> by THREE different paths: the operand-list walk
      *> (negative/l1-subtract-sr2-sending-alphanumeric), the receiver
      *> screen (negative/l1-subtract-sr2-receiver-numeric-edited) and
      *> the FROM-phrase expression bind (this one).  The grammar's
      *> `subtractFromOperand` takes its FIRST alternative, so a data
      *> reference in the FROM slot parses as a RECEIVER shape even in
      *> format 2 — a screen written for the other two arms misses this
      *> one silently, which is this repository's most reproducible
      *> defect shape.
      *> Reject-at names every edition: SR2 carries no edition
      *> condition and the screen it drives has no edition axis.
      *> NOTE ON THE DIAGNOSTIC.  As with the sending arm, the compiler
      *> reports this through the §8.8.1.1 operand-class screen
      *> (COBOLNET0844) rather than by SR2's number; the REJECTION is
      *> what SR2 obliges.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SBSR2C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XM PIC X(4) VALUE "0020".
       01 G  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           SUBTRACT 1 FROM XM GIVING G.
           STOP RUN.
