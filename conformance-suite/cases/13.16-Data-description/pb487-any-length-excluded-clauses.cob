*>--- conformance-case
*> id: pb487-any-length-excluded-clauses
*> rules: [SR-13.16.3-17]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB487 - ISO 13.16.3 syntax rule 17: "If the ANY LENGTH clause is specified, the only other
      *> clauses permitted are level-number, entry-name, PICTURE, USAGE, and VALUE."
      *> IS GLOBAL is not among them.  The check was an `||` chain over ten decode flags and GLOBAL was not one
      *> of them - deliberately so, in a sense: 13.18.27's clause binds POST-BUILD (CallBindExternalAndGlobal),
      *> so it leaves no flag for an allowlist keyed on decode state to test, and this entry compiled clean.
      *> Reading the WRITTEN clause SET off the parse tree instead of the decode flags is what fixes that whole
      *> CLASS of miss - PROPERTY escaped for the same reason, and ALIGNED, CONSTANT RECORD, DYNAMIC LENGTH,
      *> GROUP-USAGE, SAME AS, SELECT WHEN and the validation clauses escaped simply by never being listed.
      *> Nine of the fourteen clauses SR17 excludes were invisible.  COBOLNET1542.
      *> Rejected from 2002: the ANY LENGTH clause is a COBOL-2002 introduction.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB487AL2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X ANY LENGTH IS GLOBAL.
       PROCEDURE DIVISION USING L.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           EXIT PROGRAM.
