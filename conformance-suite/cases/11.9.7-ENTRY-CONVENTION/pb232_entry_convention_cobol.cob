*>--- conformance-case
*> id: pb232_entry_convention_cobol
*> rules: [SR-11.9.7.3-1, GR-11.9.7.4-1, GR-11.9.7.4-2, GR-14.9.4.4-10,
*>    FMT-11.9.7.2, DOC-A.1-64]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 §11.9.7 ENTRY-CONVENTION clause (kb/Work PB232) - the POSITIVE control.
      *> §11.9.7.2: ENTRY-CONVENTION IS { COBOL | entry-convention-name-1 }. §11.9.7.3 SR1 permits the clause
      *> in "a program definition that is not contained within another program" - this outermost program.
      *> §11.9.7.4 GR2: "When COBOL is specified, the naming convention and mapping of method-names and
      *> program-names are as specified in 8.3.2.2" - so the CALL of the contained program by its name in a
      *> different case ("w56ecin") activates it (§8.3.2.2: lowercase and uppercase letters are equivalent).
      *> The contained program carries no ENTRY-CONVENTION (SR1 forbids it there) and is COBOL by GR4 c.
      *> Expected, derived from the rules: the clause is accepted; OUTER then INNER then OUTER-END.
      *> Fails if: COBOL is refused as a convention name (COBOLNET2385 over-reach), or the case-insensitive
      *> name mapping of GR2 / §8.3.2.2 does not locate the contained program.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W56ECOUT.
       OPTIONS.
           ENTRY-CONVENTION IS COBOL.
       PROCEDURE DIVISION.
           DISPLAY "OUTER"
           CALL "w56ecin"
           DISPLAY "OUTER-END"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W56ECIN.
       PROCEDURE DIVISION.
           DISPLAY "INNER"
           GOBACK.
       END PROGRAM W56ECIN.
       END PROGRAM W56ECOUT.
