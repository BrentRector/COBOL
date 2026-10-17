      *>--- conformance-case
      *> id: l1c32_system_name_formation
      *> rules: [DOC-A.1-198]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §A.1 198) / §8.3.2.3.1 — documented system-name formation (DOC-A.1-198)
      *>
      *> THE RULE. §8.3.2.3.1: "A system-name is used to communicate with the operating
      *> environment. The implementor may define rules for the formation of a system-name
      *> that add restrictions to the rules for formation of a COBOL word." A computer-name
      *> is one of the listed system-names.
      *>   cite.py: OK  §8.3.2.3.1   (General)
      *>   cite.py: OK  §8.3.2.3.1   (General)  [— computer-name]
      *>   cite.py: OK  §A.1 198)  (Implementor-defined language element list)
      *> §8.3.2.1: a COBOL word is letters, digits, hyphen (and underscore), and "The
      *> hyphen or underscore shall not appear as the first or last character in such
      *> words."  cite.py: OK  §8.3.2.1   (General)
      *> The "at least one basic letter" requirement binds USER-DEFINED words only:
      *>   cite.py: OK  §8.3.2.2 3)  (User-defined words)  [the sentence after list item
      *>            3); cite.py may mislabel list items - PB1554]
      *>
      *> THE DOCUMENTED CHOICE. docs/CONFORMANCE.md#DOC-A.1-198: "Provided - one
      *> restriction": a system-name shall not consist of digits only (then it is an
      *> integer literal; see the companion negative l1c32-system-name-all-digits), and
      *> "Unlike a user-defined word, a system-name need not contain a letter otherwise:
      *> `123ABC`, `MY_BOX-9` and `1-2` are all accepted as computer-names."
      *>
      *> This program pins the PERMISSIVE half: `1-2` (no letter at all) and `123ABC`
      *> (leading digits) are legal computer-names. An implementation applying the
      *> user-defined-word letter rule to system-names rejects `1-2`.
      *> DERIVATION: the program compiles and runs; its one line is
      *>   SYSTEM-NAMES ACCEPTED
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C32H.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. 1-2.
       OBJECT-COMPUTER. 123ABC.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SYSTEM-NAMES ACCEPTED".
           STOP RUN.
