*>--- conformance-case
*> id: l1c23-program-address-zero-length-literal
*> rules: [SR-8.4.3.13.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §8.4.3.13.3 2) — literal-1 of ADDRESS OF PROGRAM shall not be of zero length
      *> Rule: "Literal-1 shall be an alphanumeric or national literal whose length is not zero."
      *>   cite.py: OK  §8.4.3.13.3 2)  (Syntax rules)
      *> "" is a zero-length alphanumeric literal (§8.3.3.1: "If the opening and closing delimiters are
      *> contiguous, the length of the literal is zero"; cite.py: OK  §8.3.3.1).  It is alphanumeric, so
      *> only the length half of SR2 rejects it.  The category half is the sibling negative
      *> l1c23-program-address-boolean-literal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C23E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  PP USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION.
       MAIN.
           SET PP TO ADDRESS OF PROGRAM ""
           STOP RUN.
       END PROGRAM L1C23E.
