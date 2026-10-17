*>--- conformance-case
*> id: l1-integer-of-date-no-function-word
*> rules: [FMT-15.46.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.46.2 general format — FUNCTION INTEGER-OF-DATE ( argument-1 ): the
      *> word FUNCTION is UNDERLINED, so it is a required word of this format.
      *>
      *> THIS IS THE OTHER HALF OF THAT ONE-LINE FORMAT. l1-integer-of-date-arity2
      *> closes the argument-COUNT half in the rejecting direction; writing the word
      *> in every positive is not a test that OMITTING it is rejected, and the
      *> sibling row FMT-15.25.2 was overturned CONFORMS -> PARTIAL for exactly this
      *> omission.
      *>
      *> The one permission to leave the word out is §8.4.3.2.3 SR2 — "If
      *> intrinsic-function-name-1 or the ALL phrase is specified in the REPOSITORY
      *> paragraph … the word FUNCTION may be omitted from the function-identifier;
      *> otherwise the word FUNCTION is required" — and this compilation unit has NO
      *> REPOSITORY paragraph, so the permission is withheld. Nothing else can
      *> resolve the reference either: no data item of this name is declared, so the
      *> parenthesised group is not a subscript on anything.
      *>
      *> ⚠ 2002 AND UP, NOT 85, and the reason is a rule and not a convenience: the
      *> REPOSITORY FUNCTION specifier SR2 depends on is a COBOL-2002 introduction
      *> (§12.3.8). Below 2002 the omission draws the ordinary unresolved-name
      *> diagnostic instead — a different rule about a different clause, which a
      *> negative pinning THIS one must not silently absorb.
      *> The accepting side of the word (written) is 2023/l1_integer_date_form_
      *> returned; the accepting side of the PERMISSION is
      *> 2014/l1_repository_bare_intrinsic_names.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1NEGIODNOFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(7).
       PROCEDURE DIVISION.
           COMPUTE R = INTEGER-OF-DATE(19950215)
           STOP RUN.
       END PROGRAM L1NEGIODNOFN.
