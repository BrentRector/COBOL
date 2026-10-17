      *>--- conformance-case
      *> id: pb400_evaluate_condition_subject_forms
      *> rules: [GR-8.8.4.6.3-1, GR-14.9.13.4-4, GR-14.9.39.4-5, FMT-8.8.4.6.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.13.3 SR10 Table 15 marks subject=Condition × object=TRUE-or-FALSE
      *> permissible (and its transpose, subject=TRUE-or-FALSE × object=Condition),
      *> §14.9.13.4 GR3 e) assigns "any selection subject specified by condition-1 …
      *> a truth value according to the rules for evaluating conditional expressions",
      *> and GR4 a) 4. selects the WHEN whose TRUE/FALSE matches that truth value.
      *>
      *> THREE spellings are condition-1, and all three shall behave identically in
      *> both positions: a level-88 condition-name (§8.8.4.2.7 rule 2 — "the bare
      *> condition-name … is a complete condition"), a SWITCH-STATUS condition-name
      *> (§8.8.4.6), and the subject's own class test.  The switch spelling is the one
      *> the compiler used to refuse as "an identifier" on the SUBJECT side alone while
      *> accepting it as an OBJECT (kb/Work PB400) — two per-side classifiers, one arm
      *> fixed.  §14.9.39.4 GR5 fixes what SET … TO ON does: the external switch is
      *> modified "such that the truth value resultant from evaluation of a
      *> condition-name associated with that switch will reflect an on status".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB400CSUBJ.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-1 IS SWM-A
               ON STATUS IS SW-ON
               OFF STATUS IS SW-OFF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-CODE PIC 9 VALUE 1.
          88 W-VALID VALUE 1.
       PROCEDURE DIVISION.
       MAIN-P.
      *> A level-88 as the SUBJECT, then in the transposed cell as the OBJECT.
           EVALUATE W-VALID
               WHEN TRUE
                   DISPLAY "L88-SUBJ=T"
               WHEN FALSE
                   DISPLAY "L88-SUBJ=F"
           END-EVALUATE.
           EVALUATE TRUE
               WHEN W-VALID
                   DISPLAY "L88-OBJ=T"
               WHEN OTHER
                   DISPLAY "L88-OBJ=F"
           END-EVALUATE.
           MOVE 2 TO W-CODE.
           EVALUATE W-VALID
               WHEN TRUE
                   DISPLAY "L88-OFF-SUBJ=T"
               WHEN FALSE
                   DISPLAY "L88-OFF-SUBJ=F"
           END-EVALUATE.
      *> A SWITCH-STATUS condition-name in the identical positions.
           SET SWM-A TO ON.
           EVALUATE SW-ON
               WHEN TRUE
                   DISPLAY "SWON-SUBJ=T"
               WHEN FALSE
                   DISPLAY "SWON-SUBJ=F"
           END-EVALUATE.
           EVALUATE SW-OFF
               WHEN TRUE
                   DISPLAY "SWOFF-SUBJ=T"
               WHEN FALSE
                   DISPLAY "SWOFF-SUBJ=F"
           END-EVALUATE.
           EVALUATE TRUE
               WHEN SW-ON
                   DISPLAY "SWON-OBJ=T"
               WHEN OTHER
                   DISPLAY "SWON-OBJ=F"
           END-EVALUATE.
           SET SWM-A TO OFF.
           EVALUATE SW-ON
               WHEN TRUE
                   DISPLAY "SWON2-SUBJ=T"
               WHEN FALSE
                   DISPLAY "SWON2-SUBJ=F"
           END-EVALUATE.
      *> The subject's own CLASS TEST — the third spelling of the same column.
           EVALUATE W-CODE NUMERIC
               WHEN TRUE
                   DISPLAY "CLASS-SUBJ=T"
               WHEN FALSE
                   DISPLAY "CLASS-SUBJ=F"
           END-EVALUATE.
      *> Table 15 marks Condition x Condition permissible as well (the Condition ROW is
      *> 'Y' under BOTH the Condition and the TRUE-or-FALSE column), and §14.9.13.4
      *> GR4 a) 3. and a) 4. state that cell's analysis in one shared sentence: "If the
      *> truth value of the selection subject and selection object match, the result of
      *> the analysis is true.  If they do not match, the result is false."  GR3 e) is
      *> what makes it meaningful for a condition-1 subject -- "any selection subject
      *> specified by condition-1 is assigned a truth value".  All three combinations
      *> below are reached from the state the preceding blocks left: W-CODE is 2 (so
      *> W-VALID is false) and the switch is OFF (so SW-ON is false).
           EVALUATE W-VALID
               WHEN SW-ON
                   DISPLAY "CC-FF=MATCH"
               WHEN OTHER
                   DISPLAY "CC-FF=NOMATCH"
           END-EVALUATE.
           SET SWM-A TO ON.
           EVALUATE W-VALID
               WHEN SW-ON
                   DISPLAY "CC-FT=MATCH"
               WHEN OTHER
                   DISPLAY "CC-FT=NOMATCH"
           END-EVALUATE.
           MOVE 1 TO W-CODE.
           EVALUATE W-VALID
               WHEN SW-ON
                   DISPLAY "CC-TT=MATCH"
               WHEN OTHER
                   DISPLAY "CC-TT=NOMATCH"
           END-EVALUATE.
      *> The subject's CLASS TEST is condition-1 in that cell too: W-CODE is 1, so it is
      *> NUMERIC and W-VALID is true -- the truth values match.
           EVALUATE W-CODE NUMERIC
               WHEN W-VALID
                   DISPLAY "CLSCC=MATCH"
               WHEN OTHER
                   DISPLAY "CLSCC=NOMATCH"
           END-EVALUATE.
           STOP RUN.
