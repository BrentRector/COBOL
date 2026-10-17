*>--- conformance-case
*> id: pb452-set-content-not-standard-float
*> rules: [SR-14.9.39.3-32, GR-14.9.39.4-33]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 14.9.39.3 SR32: "If FLOAT-INFINITY, FLOAT-NOT-A-NUMBER, or FLOAT-NOT-A-NUMBER-SIGNALING
      *> is specified, identifier-14 shall reference a data item described with a standard floating-point usage."
      *> "Standard floating-point usage" is a DEFINED TERM and does not mean "any floating-point item": 3.166
      *> names float-binary-32/-64/-128 and 3.167 float-decimal-16/-34, while 21.x separately calls FLOAT-SHORT,
      *> FLOAT-LONG and FLOAT-EXTENDED floating-point numeric data items WITHOUT making them standard ones. Only
      *> a standard usage pins an ISO/IEC 60559:2020 BASIC INTERCHANGE FORMAT, which is what GR33-GR35's
      *> "canonical representation ... for the basic interchange format corresponding to the usage of
      *> identifier-14" has to be taken from; FLOAT-LONG's representation is implementor-defined (13.18.60.4
      *> GR13). COBOLNET1940 (kb/Work PB452). Declaring the same item FLOAT-BINARY-64 makes the statement legal -
      *> see 2014/pb452_set_content_float.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB452NOTSTANDARDFLOAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-LONG USAGE FLOAT-LONG.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET CONTENT OF WS-LONG TO FLOAT-INFINITY
           STOP RUN.
