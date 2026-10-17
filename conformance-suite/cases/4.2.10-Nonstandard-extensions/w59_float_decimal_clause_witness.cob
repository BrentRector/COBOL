      *>--- conformance-case
      *> id: w59_float_decimal_clause_witness
      *> rules: [GR-4.2.10-2, SR-11.9.9.3-1, SR-11.9.9.3-2,
      *>    SR-11.9.9.3-3, SR-11.9.9.3-4, SR-11.9.9.3-5, SR-11.9.9.3-6,
      *>    FMT-11.9.9.2, DOC-A.1-47]
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work R43 / PB579 — the OPTIONS paragraph's FLOAT-DECIMAL
      *> clause (§11.9.9) is DECLINED. Annex A.3 item 13: "The
      *> FLOAT-DECIMAL clause is dependent both on the capabilities of
      *> the processor and on support for the standard decimal
      *> floating-point usages", and FLOAT-DECIMAL-16 / FLOAT-DECIMAL-34
      *> are not provided (A.3 item 19, refused COBOLNET1564). §4.2.6
      *> makes the compile-time warning mandatory for a declined
      *> processor-dependent element: "An implementation shall provide a
      *> warning mechanism at compile time to indicate use of
      *> syntactically-detectable processor-dependent language elements
      *> not supported by that implementation." That warning is
      *> COBOLNET2424, pinned by conformance-test
      *> DocumentedNonSupportWitnessTests; until kb/Work R43 the clause
      *> compiled with no diagnostic at all.
      *> THE OUTPUT IS THE INERT HALF. Every rule the clause states
      *> (§11.9.9.3 SR1-SR6) implies a phrase for "any data item
      *> described with a standard decimal floating-point usage", and no
      *> such item can be declared, so the program's other items are
      *> untouched: the FLOAT-BINARY-32 item below keeps its own
      *> behaviour and the numeric item computes as without the clause.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W59FDC01.
       OPTIONS.
           FLOAT-DECIMAL DEFAULT IS BINARY-ENCODING HIGH-ORDER-RIGHT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-F USAGE FLOAT-BINARY-32 VALUE 1.5.
       01 WS-N PIC 9(3) VALUE 40.
       01 WS-R PIC 9(3).
       PROCEDURE DIVISION.
           COMPUTE WS-R = WS-N + WS-F * 2.
           DISPLAY "R=" WS-R.
           STOP RUN.
