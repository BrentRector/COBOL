      *>--- conformance-case
      *> id: l1c21_page_directive_comment_text
      *> rules: [SR-7.3.19.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.19.3 SR1 — PAGE comment-text-1 may hold any character
      *> "Comment-text-1 may contain any character in the compile-time
      *> computer's coded character set except for control characters
      *> as specified in Clause 6, Reference format, rule 3b."
      *>   cite.py --check 7.3.19.3 -> OK  §7.3.19.3 1)  (Syntax rules)
      *> Companion rules the program leans on (context, not the row):
      *>   §7.3.19.3 SR2 "Comment-text-1 is not checked syntactically."
      *>   cite.py --check 7.3.19.3 -> OK  §7.3.19.3 2)  (Syntax rules)
      *>   §7.3.19.4 GR3 "If a source listing is not being produced, a
      *>   PAGE directive shall have no effect."
      *>   cite.py --check 7.3.19.4 -> OK  §7.3.19.4 3)  (General rules)
      *>   §6.1 3b) (the excluded characters are the implementor's
      *>   line-TERMINATING control characters)
      *>   cite.py --check 6.1 -> OK  §6.1 b)  (General)
      *> The three >>PAGE lines below carry, as comment-text-1, lower
      *> case letters, every printable ASCII special character (an
      *> unbalanced quotation mark and apostrophe, < > \ | ~ ` ^ { } [ ]
      *> @ # % & ! ? $), Latin-1 letters, CJK ideographs, and text that
      *> would be COBOL if it were processed ("STOP RUN.", ">>TURN",
      *> "END PROGRAM"). None is a line-terminating control character,
      *> so SR1 makes every one legal and the program shall compile.
      *> The third directive has no comment-text-1 (it is optional).
      *> Expected output, derived:
      *>   "BEFORE PAGE" — the first DISPLAY.
      *>   "AFTER PAGE"  — the directive between the two DISPLAYs is
      *>     documentation only (§7.3.19.4 GR1/GR3; COBOL.NET produces
      *>     no source listing), so its "STOP RUN." text is not a
      *>     statement and execution reaches the second DISPLAY.
       >>PAGE Start: "unbalanced quote; 'apos (a+b)=c/d*e-f, x.y
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C21A.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "BEFORE PAGE"
       >>PAGE <>\|~`^{}[]@#%&!?$ abc ÀéüßñÆ 漢字 STOP RUN. >>TURN x
       >>PAGE
           DISPLAY "AFTER PAGE"
           STOP RUN.
       END PROGRAM L1C21A.
