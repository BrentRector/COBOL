*>--- conformance-case
*> id: pb225-float-class-not-standard-float
*> rules: [SR-8.8.4.4.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 8.8.4.4.3 SR7: "If the FLOAT-INFINITY, FLOAT-NOT-A-NUMBER, FLOAT-NOT-A-NUMBER-QUIET, or
      *> FLOAT-NOT-A-NUMBER-SIGNALING phrase is specified, identifier-1 shall reference a data item described with
      *> a standard floating-point usage." FLOAT-LONG is floating-point but not STANDARD floating-point (3.166 /
      *> 3.167 name only FLOAT-BINARY-32/-64/-128 and FLOAT-DECIMAL-16/-34) - the same line the SET format 15 twin
      *> draws (14.9.39.3 SR32, COBOLNET1940). COBOLNET2215 (kb/Work PB225). Declared FLOAT-BINARY-64 the
      *> condition is legal - see 2014/pb225_float_class_conditions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB225NOTSTDFLOAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F1 USAGE FLOAT-LONG.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF F1 IS FLOAT-INFINITY DISPLAY "INF" END-IF
           STOP RUN.
