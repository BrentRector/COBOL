*>--- conformance-case
*> id: pb232-entry-convention-name
*> rules: [GR-11.9.7.4-3, GR-14.9.4.4-10, DOC-A.1-64]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 §11.9.7.4 GR3: "When entry-convention-name-1 is specified, the meaning of the entry
      *> convention is implementor-defined." COBOL.NET defines no entry-convention-name (docs/CONFORMANCE.md
      *> §7, Annex A.1 item 64); the one convention provided is COBOL. A named convention is refused by name
      *> (COBOLNET2385) instead of being silently activated with the COBOL convention (kb/Work PB232) - which
      *> is what happened before: the clause was parsed and read by nothing but the edition gate. Below 2014
      *> the clause does not exist (the edition gate, COBOLNET0900), so only 2014 and 2023 are named here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W56ECNEG.
       OPTIONS.
           ENTRY-CONVENTION IS STDCALL.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHED"
           STOP RUN.
