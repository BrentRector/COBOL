      *>--- conformance-case
      *> id: l1-test-formatted-datetime-format-boolean-literal
      *> rules: [AR-15.92.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 15.92.3 1): argument-1 shall be a national or
      *> ALPHANUMERIC literal. 8.3.3.4.1: "Boolean literals are of the
      *> class and category boolean." A boolean literal survives the
      *> literal-ness half of the rule and has to be turned away by
      *> the CLASS half, so this fixture and its non-literal sibling
      *> cover the two disjoint shapes of one sentence. Nothing
      *> anywhere fed this function a boolean or a numeric argument-1
      *> before, so the class half was unexercised at every edition
      *> even though the schema row for it existed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGL1TFDBOOLIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D PIC X(8) VALUE "20210616".
       01 T PIC 9(2).
       PROCEDURE DIVISION.
           COMPUTE T = FUNCTION TEST-FORMATTED-DATETIME(B"0101" D).
           STOP RUN.
