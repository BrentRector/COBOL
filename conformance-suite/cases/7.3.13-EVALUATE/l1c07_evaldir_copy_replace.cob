      *>--- conformance-case
      *> id: l1c07_evaldir_copy_replace
      *> rules: [GR-7.3.13.4-1]
      *> accept: [2002]
      *> files: [l1c07cb.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.13.4 GR1 — text words inside EVALUATE directive text
      *> are subject to COPY and REPLACE matching and replacing.
      *> RULE 7.3.13.4 GR1: "Text-1 and text-2 are not part of the
      *>   EVALUATE compiler directive line. Any text words in text-1 or
      *>   text-2 that do not form a compiler directive line are subject
      *>   to the matching and replacing rules of the COPY statement and
      *>   the REPLACE statement."
      *> cite.py:
      *>   OK  §7.3.13.4 1)  (General rules)
      *>  OK  §7.3.13.4 4) b)  (General rules)  [GR4 tail]
      *> Support file: l1c07cb.cpy = one line "DISPLAY ZZTAG."
      *> EXPECTED OUTPUT, DERIVED:
      *>   GR1-REPLACE-OK  subject 1 matches >>WHEN 1, so its text-1 is
      *>                   included; the REPLACE statement in it acts on
      *>                   the following text word FOOTAG (GR1).
      *>   GR1-COPY-OK     the COPY ... REPLACING in text-1 is processed
      *>                   and replaces ZZTAG in the library text (GR1).
      *>   GR1-REPLACE-OK  after END-EVALUATE the REPLACE from text-1 is
      *>                   still in effect; the REPLACE in the omitted
      *>                   WHEN OTHER text-2 never enters the resultant
      *>                   text, so it does not supersede it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C07C.
       PROCEDURE DIVISION.
       MAIN-PARA.
       >>EVALUATE 1
       >>WHEN 1
           REPLACE ==FOOTAG== BY =="GR1-REPLACE-OK"==.
           DISPLAY FOOTAG.
           COPY l1c07cb REPLACING ==ZZTAG== BY =="GR1-COPY-OK"==.
       >>WHEN OTHER
           REPLACE ==FOOTAG== BY =="OMITTED-REPLACE"==.
       >>END-EVALUATE
           DISPLAY FOOTAG.
           STOP RUN.
