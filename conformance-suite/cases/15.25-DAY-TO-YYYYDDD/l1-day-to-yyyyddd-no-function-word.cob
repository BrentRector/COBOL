*>--- conformance-case
*> id: l1-day-to-yyyyddd-no-function-word
*> rules: [FMT-15.25.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.25.2 general format — FUNCTION DAY-TO-YYYYDDD ( … ): the word
      *> FUNCTION is UNDERLINED, so it is a required word of this format.
      *>
      *> The one permission to omit it is §8.4.3.2.3 SR2 — "If intrinsic-function-
      *> name-1 or the ALL phrase is specified in the REPOSITORY paragraph … the
      *> word FUNCTION may be omitted from the function-identifier" — and this
      *> compilation unit has NO REPOSITORY paragraph, so the permission is not in
      *> effect and the required word is missing. Nothing else can resolve the
      *> reference either: no data item of this name is declared, so the parenthes-
      *> ised group is not a subscript on anything.
      *> This is the required-WORD half of the format; the argument-COUNT half is
      *> negative/l1-day-to-yyyyddd-arity4, and the accepting side (all three legal
      *> counts, with the word written) is 2002/l1_day_to_yyyyddd_format.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1NEGDTYNOFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(7).
       PROCEDURE DIVISION.
           COMPUTE R = DAY-TO-YYYYDDD(85365 10 1900)
           STOP RUN.
       END PROGRAM L1NEGDTYNOFN.
