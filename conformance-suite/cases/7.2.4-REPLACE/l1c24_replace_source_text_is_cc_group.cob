      *>--- conformance-case
      *> id: l1c24_replace_source_text_is_cc_group
      *> rules: [GR-7.2.4.4-1]
      *> accept: [2002]
      *> files: [l1c24ca.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.2.4.4 GR1 — REPLACE acts on the conditionally-processed
      *> compilation group (after >>IF selection and COPY REPLACING)
      *> RULE §7.2.4.4 GR1: "In subsequent general rules of the REPLACE
      *>   statement, 'source text' refers to the
      *>   conditionally-processed
      *>   compilation group."
      *>   cite.py --check 7.2.4.4 "'source text' refers to the
      *>   conditionally-processed compilation group" -> OK §7.2.4.4 1)
      *> What that group is (§7.2.1): Step 1 incorporates library text;
      *> false-path lines of an IF directive may be omitted; Step 2
      *> processes IF directives and "the replacing actions of COPY
      *> statements"; Step 3 then applies REPLACE.
      *>   cite.py --check 7.2.1 "Step 3: The conditionally-processed
      *>   compilation group is read and the replacing actions of
      *>   REPLACE statements are applied in order." -> OK §7.2.1
      *>   cite.py --check 7.2.1 "the replacing actions of COPY
      *>   statements" -> OK §7.2.1 (Step 2 d))
      *>   cite.py --check 7.2.4.4 "A format 1 REPLACE statement without
      *>   the ALSO phrase cancels the active REPLACE statement"
      *>     -> OK §7.2.4.4 (7) b); the tool labels the line 7) a) 2.)
      *>   cite.py --check 7.3.8.4.4 "A defined condition using the IS
      *>   DEFINED" -> OK §7.3.8.4.4 1)
      *> Support file: l1c24ca.cpy = one line "DISPLAY CPWORD."
      *> EXPECTED OUTPUT, DERIVED:
      *>   BASE-REPLACE-KEPT  L1C24NONE is never defined, so the >>IF is
      *>     false; its REPLACE is not in the conditionally-processed
      *>     group, so it neither cancels (7) b)) nor supersedes the
      *>     base
      *>     REPLACE: TAGA still becomes "BASE-REPLACE-KEPT".
      *>   TRUE-PATH-REPLACE  the >>ELSE path is in the group; its
      *>     REPLACE
      *>     cancels the base one and replaces TAGB.
      *>   COPY-TEXT-REPLACED library text is part of the group: CPWORD
      *>     in the copybook is replaced by the active REPLACE.
      *>   AFTER-COPY-REPLACING  COPY ... REPLACING CPWORD BY MIDWORD
      *>     is a Step 2 action, so REPLACE (Step 3) sees MIDWORD and
      *>     replaces
      *>     it. Applying REPLACE first would print COPY-TEXT-REPLACED.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C24I.
       PROCEDURE DIVISION.
       MAIN-P.
           REPLACE ==TAGA== BY =="BASE-REPLACE-KEPT"==.
       >>IF L1C24NONE IS DEFINED
           REPLACE ==TAGA== BY =="FALSE-PATH-REPLACE"==.
       >>END-IF
           DISPLAY TAGA.
       >>IF L1C24NONE IS DEFINED
           DISPLAY "FALSE-PATH-TEXT".
       >>ELSE
           REPLACE ==TAGB== BY =="TRUE-PATH-REPLACE"==
                   ==CPWORD== BY =="COPY-TEXT-REPLACED"==
                   ==MIDWORD== BY =="AFTER-COPY-REPLACING"==.
       >>END-IF
           DISPLAY TAGB.
           COPY l1c24ca.
           COPY l1c24ca REPLACING ==CPWORD== BY ==MIDWORD==.
           STOP RUN.
