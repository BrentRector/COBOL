      *>--- conformance-case
      *> id: l1-formatted-time-format-boolean-literal
      *> rules: [AR-15.41.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 15.41.3 1): argument-1 shall be a national or
      *> ALPHANUMERIC literal. 8.3.3.4.1: "Boolean literals are of the
      *> class and category boolean." A boolean literal IS a literal,
      *> and it is neither national nor alphanumeric, so it is the one
      *> shape the rule's CLASS half has to turn away on its own - the
      *> literal-ness half cannot see it. That is why this fixture
      *> exists alongside the non-literal one: the two halves of a
      *> single sentence cover disjoint shapes, and deleting either
      *> would leave legal-looking source silently accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGL1FTBOOLIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION FORMATTED-TIME(B"0101" 45296) TO R.
           STOP RUN.
