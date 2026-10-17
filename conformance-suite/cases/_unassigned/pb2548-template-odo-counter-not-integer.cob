*>--- conformance-case
*> id: pb2548-template-odo-counter-not-integer
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2548 - an UNUSED TYPEDEF template. ISO 13.18.38.3 SR17 (cite.py OK): "Data-name-1 shall describe an
      *> integer." N is the template's own alphanumeric item, so the DEPENDING ON counter resolves INSIDE the type
      *> declaration and the rule is a fact of the declaration alone. No TYPE clause references the template, which has
      *> no storage (ISO 13.18.58.4 GR2), but a syntax rule binds the source as written (ISO 4.3). Before kb/Work
      *> PB2548 the OCCURS DEPENDING ON resolver walked the storage forest only, so this compiled clean. (A counter the
      *> declaration does not define is NOT refused here: a TYPE clause is "as though the data description ... had been
      *> coded in place" (ISO 13.18.57.4 GR1), so a use site may supply it; see pb2548_template_clauses_resolve.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2548ODO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U IS TYPEDEF.
          05 N PIC X.
          05 T PIC X OCCURS 1 TO 4 DEPENDING ON N.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "R01".
           STOP RUN.
