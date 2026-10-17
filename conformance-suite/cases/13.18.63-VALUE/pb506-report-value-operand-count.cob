*>--- conformance-case
*> id: pb506-report-value-operand-count
*> rules: [SR-13.18.63.3-35]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.63.3 SR35, sentence 2: "The number of operands of the VALUE clause shall be equal to the
      *> number of repetitions of the repeating entry or the same number multiplied by the number of repetitions
      *> of any number of successive repeating entries at higher levels than the repeating entry."  The entry
      *> below is a repeating entry — §13.15.4 GR3, a COLUMN clause with more than one operand, three repetitions
      *> — and writes TWO operands.  Two is neither three nor three times the repetitions of any higher repeating
      *> entry (there are none), so the clause is not conforming source.  §13.18.63.4 GR23's wrap-around sentence
      *> ("If no further operands remain, assignment begins again from the first operand") is what governs the
      *> LEGAL multi-level case SR35's "multiplied by" admits; it does not license a short list here.
      *> Before kb/Work PB506 this compiled clean and printed `AAA  AAA  AAA`, indistinguishable from the legal
      *> three-operand program.  SR35 carries no version proviso, so it is reported at all four editions; at 85
      *> the COBOLNET0900 introduction gate for the multiple COLUMN clause is reported ALONGSIDE it, not instead
      *> of it (the kb/Work PB505 negative-fixture precedent).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB506N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb506n2.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-N2.
       REPORT SECTION.
       RD  R-N2 PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMNS ARE 1 6 11 PIC X(3) VALUES ARE "AAA" "BBB".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-N2.
           GENERATE DET.
           TERMINATE R-N2.
           CLOSE PRT.
           STOP RUN.
