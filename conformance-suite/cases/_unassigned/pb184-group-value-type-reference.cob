*>--- conformance-case
*> id: pb184-group-value-type-reference
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR14, SECOND CONJUNCT, REACHED THROUGH A TYPE CLAUSE.  13.18.57.4 GR3: "If a VALUE
      *> clause is specified in the data description of the subject of the entry, the content of the literal
      *> associated with that VALUE clause is used for the initial value associated with the subject of the
      *> entry" - and its NOTE sends the reader to the VALUE clause's own syntax rules for exactly this
      *> composition.  So R below is an entry that specifies a group-level VALUE clause, its subordinates are
      *> A and B, and A is COMPUTATIONAL: SR14's usage conjunct is violated.
      *> ⛔ THE POINT OF THIS FIXTURE: the violation is COMPOSED.  R's own entry carries the VALUE and no
      *> usage; T's entries carry the usage and no VALUE.  NEITHER entry carries the violation alone, so a
      *> screen that walks only the entries the programmer wrote AS WRITTEN cannot see it.  The first landing
      *> of COBOLNET1702 walked DataBinder.ConformanceForest, whose final filter drops every item with a
      *> non-null TypeAnchor, and ExpandType sets TypeName on the SUBJECT of the TYPE clause - so R and its
      *> whole clone subtree fell out.  MEASURED: this program compiled CLEAN and ran (A=0000, B=spaces),
      *> while its byte-identical inline spelling `01 R VALUE "ABCD". 05 A PIC 9(4) COMP. 05 B PIC X(2).` was
      *> rejected.  The screen now walks DataBinder.CompositionForest - the entries AS COMPOSED.
      *> Edition band: the TYPE clause is a COBOL-2002 feature, so at --std 85 this source is rejected for the
      *> TYPE clause itself (COBOLNET0900) and never reaches SR14.  The band starts at 2002 for that reason
      *> and not because the rule is edition-dependent - SR14 is unconditional.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB184N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T IS TYPEDEF.
          05 A PIC 9(4) COMP.
          05 B PIC X(2).
       01 R TYPE T VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY A OF R
           STOP RUN.
