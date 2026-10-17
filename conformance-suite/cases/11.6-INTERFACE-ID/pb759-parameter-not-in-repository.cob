*>--- conformance-case
*> id: pb759-parameter-not-in-repository
*> rules: [SR-11.6.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB759 -- ISO 11.6.3 SR4: "Parameter-name-1 shall be a name specified in a class-specifier or
      *> an interface-specifier in the REPOSITORY paragraph of this interface definition."  PB759NRI's USING
      *> clause names T, and the interface has no REPOSITORY paragraph at all -> COBOLNET2239.  Diagnosed on
      *> the definition itself, although nothing expands it: every expansion would inherit the defect.
       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB759NRI USING T.
       END INTERFACE PB759NRI.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB759NR.
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM PB759NR.
