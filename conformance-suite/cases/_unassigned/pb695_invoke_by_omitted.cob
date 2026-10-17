*>--- conformance-case
*> id: pb695_invoke_by_omitted
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB695 family 3 - BY is an OPTIONAL WORD in ALL THREE of INVOKE's argument phrases,
      *> and FROM is one in the INHERITS clause.
      *> ISO 14.9.23.2 prints `[BY REFERENCE]`, `[BY CONTENT]` and `[BY VALUE]`. Measured on printed
      *> page 681 / folio 651: REFERENCE (96.3% cover), CONTENT (95.4%), VALUE (92.9%), USING (92.8%)
      *> and RETURNING (96.2%) each carry an underline rectangle, and NONE of the three occurrences of
      *> BY (boxes 325.97-336.30, 325.96-336.30, 325.90-336.24) has any rule in its band. kb/Work
      *> PB130 relaxed only the VALUE arm; REFERENCE and CONTENT still demanded BY until family 3.
      *> ISO 11.3.2 prints `[ INHERITS FROM { object-class-name-2 } ... ]`; measured on printed page
      *> 294 / folio 264, INHERITS carries a rule (95.0%) and FROM's box 140.01-168.09 carries none -
      *> a word REQUIRED INSIDE an optional group, which no optional-word audit can see (PB715).
      *> 8.3.2.4.3 makes every one of them omittable "with no effect on the semantics of the format".
      *>
      *> EXPECTED VALUES, DERIVED:
      *>   ARG - 14.9.23.4 GR7 passes the arguments to the method; the base class supplies MIX through
      *>         11.4/11.8 inheritance, so the subclass instance answers it.
      *>   REF - 14.8.2.3.2 / 14.2.3 GR8: a BY REFERENCE argument names the SAME data item as its
      *>         formal, so the method's store into LREF is visible in the caller's A after the call.
      *>   CON - 14.8.2.3.3: a BY CONTENT argument gives the method a COPY, so the method's store into
      *>         LCON leaves the caller's B unchanged - it still reads "22".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB695INVBY.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB695INVD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB695INVD.
       01 A PIC X(2) VALUE "11".
       01 B PIC X(2) VALUE "22".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB695INVD "NEW" RETURNING O
           INVOKE O "MIX" USING REFERENCE A CONTENT B
           DISPLAY "REF=" A
           DISPLAY "CON=" B
           DISPLAY "DONE"
           STOP RUN.
       END PROGRAM PB695INVBY.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB695INVB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.

       METHOD-ID. MIX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LREF PIC X(2).
       01 LCON PIC X(2).
       PROCEDURE DIVISION USING LREF LCON.
       M.
           DISPLAY "ARG=" LREF LCON
           MOVE "AA" TO LREF
           MOVE "BB" TO LCON
           EXIT METHOD.
       END METHOD MIX.

       END OBJECT.
       END CLASS PB695INVB.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB695INVD INHERITS PB695INVB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB695INVB.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB695INVD.
