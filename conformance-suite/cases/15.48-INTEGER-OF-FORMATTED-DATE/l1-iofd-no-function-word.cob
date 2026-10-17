*>--- conformance-case
*> id: l1-iofd-no-function-word
*> rules: [FMT-15.48.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.48.2 general format — FUNCTION INTEGER-OF-FORMATTED-DATE
      *> ( argument-1 argument-2 ): FUNCTION is UNDERLINED, so it is a required word
      *> of this format, exactly as INTEGER-OF-FORMATTED-DATE itself is.
      *>
      *> THE ARGUMENT-COUNT HALF OF THIS FORMAT IS CLOSED IN BOTH DIRECTIONS
      *> (2014/l1_iofd_two_argument_format accepts two; l1-iofd-arity1 and
      *> l1-iofd-arity3 reject one and three). The required-WORD half was tested
      *> only in the accepting direction, which is not a test of the requirement —
      *> the sibling row FMT-15.25.2 was overturned CONFORMS -> PARTIAL for that
      *> precise omission, and this row's own recorded evidence rests partly on the
      *> keyword-omitted routing.
      *>
      *> §8.4.3.2.3 SR2 grants the only omission permission — "If intrinsic-
      *> function-name-1 or the ALL phrase is specified in the REPOSITORY paragraph
      *> … the word FUNCTION may be omitted from the function-identifier; otherwise
      *> the word FUNCTION is required" — and this unit has NO REPOSITORY paragraph,
      *> so the permission is withheld and the required word is missing. No data
      *> item of this name is declared either, so the parenthesised group is not a
      *> subscript on anything.
      *>
      *> ⚠ 2014 AND UP because that is where §15.48 itself begins; below it the NAME
      *> is rejected first (COBOLNET1502, pinned elsewhere), which is a different
      *> rule about a different clause.
      *> The accepting side of the PERMISSION is
      *> 2014/l1_repository_bare_intrinsic_names, which writes this same reference
      *> bare under REPOSITORY. FUNCTION ALL INTRINSIC. and requires 0143951.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1NEGIOFDNOFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D8 PIC X(8) VALUE "19950215".
       01 R  PIC 9(7).
       PROCEDURE DIVISION.
           COMPUTE R = INTEGER-OF-FORMATTED-DATE("YYYYMMDD" D8)
           STOP RUN.
       END PROGRAM L1NEGIOFDNOFN.
