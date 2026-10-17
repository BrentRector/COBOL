      *> reject-at: 2002 2014 2023
      *> kb/Work PB1784 - ISO/IEC 1989:2023 section 11.6.3 SR6: "A given interface-name shall not appear
      *> more than once in an INHERITS clause."
      *>   cite.py: OK  11.6.3 6)
      *> SR6 is a rule on the WRITTEN list, asked before any name resolves (the class twin, 11.3.3 SR7, is):
      *> I1784Q names I1784Z twice and no interface I1784Z is declared, so the repetition is reported as
      *> SR6 (COBOLNET2791) beside the unresolved name (COBOLNET0840), not only as two unresolved names.
       IDENTIFICATION DIVISION.
       INTERFACE-ID. I1784Q INHERITS FROM I1784Z I1784Z.
       END INTERFACE I1784Q.
