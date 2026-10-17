*>--- conformance-case
*> id: pb46_call_format2_as_nested
*> rules: [SR-14.9.4.3-15, SR-14.9.4.3-17, GR-14.9.4.4-2, GR-14.9.4.4-6,
*>    GR-14.9.4.4-8, FMT-14.9.4.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.2 Format 2 (program-prototype CALL):
      *>   CALL { identifier-1 | literal-1 } AS { NESTED | program-prototype-name-1 }
      *> The AS phrase is a SYNTACTIC discriminator - Format 1 has no AS at all -
      *> and its absence from the grammar is why Format 2 was unreachable.
      *>
      *> ⛔ PB46's NOTE HAD THIS WRONG. It asserted "the formats are NOT
      *> distinguishable at parse time ... what selects Format 2 is whether the
      *> called name resolves to a program-prototype - a SEMANTIC question", and
      *> concluded the whole CALL half was blocked on the P13 prototype registry.
      *> Reading the RENDERED general format refutes both halves of that: the AS
      *> phrase decides it syntactically, and its NESTED arm needs no registry.
      *>
      *> The brace has two arms with DIFFERENT dependencies:
      *>   AS NESTED             14.9.4.3 SR13/SR15 - a COMMON or directly
      *>                         contained program, named by literal-1. Here.
      *>   AS prototype-name     SR16 - requires a PROGRAM specifier in the
      *>                         REPOSITORY paragraph (12.3.8.2), which the
      *>                         grammar has no entry for. Still P13; the
      *>                         negative fixture pins that it says so by name.
      *>
      *> AND FORMAT 2 IS WHAT MAKES THE EXPRESSION OPERAND LEGAL. Format 1's
      *> BY CONTENT is `{ identifier-2 } ...` and nothing else; Format 2's adds
      *> arithmetic-expression-1. Assertion 5 is the one that could not be
      *> written before, and pb46-call-format1-content-expression pins that the
      *> same operand stays illegal without the AS phrase.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB46CALLF2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC S9(4) VALUE 5.
       PROCEDURE DIVISION.
       MAIN.
      *> 1-2 - Format 1 controls: the bare and BY CONTENT identifier forms, which
      *> must be untouched by a change that widens the shared callByContent rule.
           CALL "PB46IN" USING N.
           CALL "PB46IN" USING BY CONTENT N.
      *> 3 - Format 2 with the same identifier operand.
           CALL "PB46IN" AS NESTED USING BY CONTENT N.
      *> 4 - Format 2 with a literal operand.
           CALL "PB46IN" AS NESTED USING BY CONTENT 42.
      *> 5 - THE SUBJECT: arithmetic-expression-1 under Format 2's BY CONTENT.
           CALL "PB46IN" AS NESTED USING BY CONTENT (N + 1).
           CALL "PB46IN" AS NESTED USING BY CONTENT N * 2 - 3.
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB46IN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P PIC S9(4).
       PROCEDURE DIVISION USING P.
       M.
           DISPLAY "GOT " P.
       END PROGRAM PB46IN.
       END PROGRAM PB46CALLF2.
