*>--- conformance-case
*> id: l1c32-system-name-all-digits
*> rules: [DOC-A.1-198]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §A.1 198) / §8.3.2.3.1 — documented restriction: an all-digit system-name
      *>
      *> §8.3.2.3.1: "The implementor may define rules for the formation of a system-name
      *> that add restrictions to the rules for formation of a COBOL word."
      *>   cite.py: OK  §8.3.2.3.1   (General)
      *>   cite.py: OK  §A.1 198)  (Implementor-defined language element list)
      *> docs/CONFORMANCE.md#DOC-A.1-198 documents ONE added restriction: "a system-name
      *> shall not consist of digits only, because such a character-string is always read
      *> as an integer literal (`OBJECT-COMPUTER. 12345.` is a syntax error, `COBOL0001`)".
      *> `12345` is otherwise a legal COBOL word (§8.3.2.1: digits allowed, no hyphen at
      *> either end; the letter requirement of §8.3.2.2 binds user-defined words only), so
      *> this documented restriction is the only reason to reject. The companion positive
      *> l1c32_system_name_formation shows `1-2` and `123ABC` accepted. The diagnostic is
      *> the generic parse code, which is exactly what the documentation names.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C32I.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X-1.
       OBJECT-COMPUTER. 12345.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE".
           STOP RUN.
