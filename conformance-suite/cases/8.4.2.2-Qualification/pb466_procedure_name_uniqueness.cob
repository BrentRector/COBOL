      *>--- conformance-case
      *> id: pb466_procedure_name_uniqueness
      *> rules: [SR-8.4.2.2.1-6, SR-8.4.2.2.3-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB466 - the LEGAL side of procedure-name uniqueness, pinned so that a later "make resolution
      *> more forgiving" edit cannot pass. Every reference below is to a spelling the program declares more
      *> than once, and every one of them is conforming: the standard does not forbid the duplication, it
      *> requires that a REFERENCE to it be unambiguous, and there are exactly two ways to be.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN:
      *> 8.4.2.2.1     - "Identical user-defined names may be specified in a source unit; however, uniqueness
      *>                  shall be established through qualification for each user-defined name explicitly
      *>                  referenced, except as specified in rules 2 through 6."
      *> 8.4.2.2.2 fmt 4 - a paragraph-name is qualified by IN/OF section-name. PERFORM COMMON-P IN S-ONE
      *>                  therefore reaches S-ONE's copy and PERFORM COMMON-P IN S-TWO reaches S-TWO's; the
      *>                  final GO TO TAIL-P IN S-TWO likewise reaches S-TWO's, so TAIL-ONE never prints.
      *> 8.4.2.2.1 r6  - "The name is a paragraph-name and the section containing the reference also contains
      *>                  the named paragraph" - so the UNQUALIFIED PERFORM COMMON-P written inside S-ONE
      *>                  resolves to S-ONE's copy, and the identical statement inside S-TWO resolves to
      *>                  S-TWO's. The two identical sentences deliberately print DIFFERENT lines: that is the
      *>                  whole content of rule 6, and a resolver that answered "the first definition in the
      *>                  program" for both would print ONE twice.
      *> 8.4.2.2.3 SR7 - "IF EXPLICITLY REFERENCED, a paragraph-name shall not be duplicated within a section."
      *>                  UNREF-DUP is declared twice inside S-ONE and once more in S-TWO and is referenced
      *>                  NOWHERE, so the condition SR7 states is not met and the program is conforming. This
      *>                  is why the uniqueness checks live at the reference and not at the declaration; the
      *>                  negative twin is conformance:cases/8.4.2.2-Qualification/pb466-paragraph-name-duplicated-in-section.
      *> 14.9.28.4 GR5 - each out-of-line PERFORM returns after its range, so PERFORM RULE6-ONE runs that one
      *>                  paragraph and the nested PERFORM COMMON-P inside it is a disjoint range (GR2's
      *>                  undefined case is OVERLAPPING ranges, which these are not).
      *> Run at --std 85, the earliest supported edition: 8.4.2.2 is unchanged across all four, so one witness
      *> at the earliest covers them (the version matrix covers the gating).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB466UNIQ85.
       PROCEDURE DIVISION.
       S-DRIVER SECTION.
       P-DRIVE.
           DISPLAY "START".
           PERFORM COMMON-P IN S-ONE.
           PERFORM COMMON-P IN S-TWO.
           PERFORM RULE6-ONE.
           PERFORM RULE6-TWO.
           DISPLAY "END".
           GO TO TAIL-P IN S-TWO.
       S-ONE SECTION.
       COMMON-P.
           DISPLAY "ONE".
       RULE6-ONE.
           DISPLAY "R6-ONE".
           PERFORM COMMON-P.
       TAIL-P.
           DISPLAY "TAIL-ONE-MUST-NOT".
           STOP RUN.
       UNREF-DUP.
           DISPLAY "UNREF-ONE-MUST-NOT".
       UNREF-DUP.
           DISPLAY "UNREF-ONE-AGAIN-MUST-NOT".
       S-TWO SECTION.
       COMMON-P.
           DISPLAY "TWO".
       RULE6-TWO.
           DISPLAY "R6-TWO".
           PERFORM COMMON-P.
       TAIL-P.
           DISPLAY "TAIL-TWO".
           STOP RUN.
       UNREF-DUP.
           DISPLAY "UNREF-TWO-MUST-NOT".
