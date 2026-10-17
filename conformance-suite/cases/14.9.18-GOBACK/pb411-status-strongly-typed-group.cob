*>--- conformance-case
*> id: pb411-status-strongly-typed-group
*> rules: [SR-14.9.18.3-6, SR-14.9.42.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB411 — THE EDGE OF 8.5.2.1's GROUP SENTENCE, and the reason the
      *> fix is not "any group is usage display".
      *> 14.9.42.3 syntax rule 2 (and its twin 14.9.18.3 SR6): "Identifier-1 shall
      *> reference an integer data item or a data item with usage display or usage
      *> national." 8.5.2.1 supplies the group answer - "An alphanumeric group item
      *> is treated as though it had a usage of display" - and 3.11 defines that
      *> term BY EXCLUSION: "group item except for a bit group item, a national
      *> group item, a strongly-typed group item, or a variable-length group item".
      *> A strongly-typed group is therefore NOT reached by that sentence; 8.5.2.1
      *> gives it its type-name as class and category and states no usage for it at
      *> all, so it meets neither alternative of SR2 and is in error here.
      *> This fixture REPLACES pb169-status-group-identifier, which pinned the
      *> rejection of a PLAIN alphanumeric group - legal source that this compiler
      *> refused for three editions while a green test held the refusal in place.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB411STG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-TD IS TYPEDEF STRONG.
          05 WS-TD-A PIC X(2).
       01 WS-STRONG TYPE WS-TD.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN WITH ERROR STATUS WS-STRONG.
