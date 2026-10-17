*>--- conformance-case
*> id: pb506-report-value-not-repeating
*> rules: [SR-13.18.63.3-35]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.63.3 SR35, sentence 1: "If the VALUE clause has more than one operand, the entry shall be a
      *> repeating entry or shall be subordinate to a repeating entry."  §13.15.4 GR3 defines a repeating entry:
      *> "An entry that contains either an OCCURS clause or a LINE or COLUMN clause with more than one operand is
      *> said to be a repeating entry … The number of repetitions of an entry that is not a repeating entry is
      *> defined to be 1."  The entry below has a SINGLE COLUMN operand, no OCCURS and no multiple LINE clause, so
      *> it is not a repeating entry and is subordinate to none: three VALUE operands are not conforming source.
      *> Before kb/Work PB506 this compiled clean and printed `AAA` — the whole operand list was glued into the
      *> raw text `"AAA""BBB""CCC"`, whose doubled quotes the literal decoder read as escaped ones, and the PIC
      *> then truncated the result.  SR35 carries no version proviso, so it is reported at all four editions;
      *> this program writes nothing edition-gated (ONE absolute COLUMN operand is the COBOL-85 form), so
      *> COBOLNET2012 is the only diagnostic at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB506N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb506n1.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-N1.
       REPORT SECTION.
       RD  R-N1 PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(3) VALUE "AAA" "BBB" "CCC".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-N1.
           GENERATE DET.
           TERMINATE R-N1.
           CLOSE PRT.
           STOP RUN.
