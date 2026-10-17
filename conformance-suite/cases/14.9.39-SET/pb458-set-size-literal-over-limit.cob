*>--- conformance-case
*> id: pb458-set-size-literal-over-limit
*> rules: [SR-14.9.39.3-34]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.39.3 SR34 - "Integer-2 shall be non-negative, and shall be equal to or less than the maximum
      *> size of data-name-3, as specified in 8.5.1.10, Dynamic-length elementary items." 8.5.1.10.1 makes that
      *> maximum the smallest of "the value declared in the LIMIT phrase", "the largest integer that can be stored
      *> in an item of the usage specified in the PREFIXED phrase", "the maximum permitted by the implementor", and
      *> 13.18.19.4 GR2 makes the LIMIT phrase's integer-1 the declared ceiling - so with LIMIT IS 8 the bound is
      *> 8 and integer-2 = 99 is decidably out of range at compile time.
      *> SR34 is a SYNTAX rule over Format 16's LITERAL alternative; 14.9.39.4 GR37/GR38's EC-STORAGE-NOT-AVAIL
      *> and clamp are the rules for arithmetic-expression-5 and are untouched. GR38 is unconditional for
      *> integer-2 precisely BECAUSE SR34 has already guaranteed the literal is in range.
      *> ⛔ The clamp used to stand in for the screen, in BOTH arms: SET SIZE OF D TO 99 and the bare SET D TO 99
      *> each compiled, ran, and left a length of 8. CobolDynString's own comment and docs/CONFORMANCE.md both
      *> asserted this screen existed. kb/Work PB458.
      *> Rejected at 2023 only: Format 16 is a COBOL-2023 introduction and takes the edition-band code below it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB458N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D PIC X DYNAMIC LENGTH LIMIT IS 8.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ABC" TO D
           SET SIZE OF D TO 99
           DISPLAY D
           STOP RUN.
