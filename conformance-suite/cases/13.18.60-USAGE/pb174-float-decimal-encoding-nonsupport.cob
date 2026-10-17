      *>--- conformance-case
      *> id: pb174-float-decimal-encoding-nonsupport
      *> rules: [GR-13.18.60.4-16, GR-13.18.60.4-17, GR-13.18.60.4-18, GR-13.18.60.4-20, DOC-A.1-47, DOC-A.1-48]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> THE STAGE THE REJECT COMES FROM IS THE POINT OF THIS FIXTURE.
      *> "USAGE FLOAT-DECIMAL-16 BINARY-ENCODING" IS LEGAL 2014+ SOURCE
      *> - ISO 13.18.60.2 prints FLOAT-DECIMAL-16 with a bracketed
      *> choice-indicator group over { encoding-phrase,
      *> endianness-phrase }, and 5.2.6.4 makes each alternative
      *> optional and at most once. Before kb/Work PB174 the grammar
      *> carried no tail at all, so this failed as a raw PARSE error at
      *> the wrong stage. The honest reject is the DOCUMENTED
      *> non-support of the usage itself: Annex A.3 item 19 makes
      *> FLOAT-DECIMAL-16 processor-dependent and 13.18.60.4 GR17 pins
      *> it to 60559:2020 decimal64, which .NET has no type for.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB174N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D USAGE FLOAT-DECIMAL-16 BINARY-ENCODING.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
