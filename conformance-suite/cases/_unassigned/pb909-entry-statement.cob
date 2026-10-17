*>--- conformance-case
*> id: pb909-entry-statement
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989 defines no ENTRY statement at any edition - a secondary entry point is a vendor extension,
*> and this implementation admits none (§4.2.2: "An implementation shall accept the syntax and provide the
*> functionality for all standard language elements"). It used to bind to the DEFERRAL carrier - a COBOLNET1756
*> warning promising a feature the standard does not contain - and the program compiled. The ENTRY sits
*> behind a GO TO: the refusal is a COMPILE-TIME verdict, not a property of the executed path. COBOLNET2269
*> (kb/Work PB909).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB909NENT.
       PROCEDURE DIVISION.
       MAIN.
           GO TO FINISH.
           ENTRY "PB909NENTE".
       FINISH.
           STOP RUN.
