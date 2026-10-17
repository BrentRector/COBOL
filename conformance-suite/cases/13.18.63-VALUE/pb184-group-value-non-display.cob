*>--- conformance-case
*> id: pb184-group-value-non-display
*> rules: [SR-13.18.63.3-14, GR-13.18.63.4-5]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR14: "If a VALUE clause is specified at the group level, subordinate items within that
      *> group shall not be described with a JUSTIFIED or SYNCHRONIZED clause, and all data items subordinate to
      *> an alphanumeric group item shall be explicitly or implicitly described with usage DISPLAY."  GV carries
      *> no GROUP-USAGE clause, is not strongly typed and is not a variable-length group, so it IS an
      *> alphanumeric group item - 13.18.29.4 GR3: "If a GROUP-USAGE clause is not specified or implied for a
      *> group item that is not strongly typed and is not a variable-length group, that group item is an
      *> alphanumeric group item"; 8.5.2.1 - "an alphanumeric group item has class and category alphanumeric".
      *> GB is COMPUTATIONAL.  (All THREE of GR3's conjuncts matter: the two the first landing dropped are
      *> exactly 13.18.63.3 SR1's population, and without them a strongly-typed or variable-length subject
      *> answered the SR14 usage arm - now COBOLNET1703, pb184-group-value-strong-subject and its twin.)
      *> MEASURED BEFORE THIS SCREEN (kb/Work PB184, on 8ca74a3d): this program compiled CLEAN and left every
      *> occurrence of GB at ZERO - the group VALUE dropped on the floor with no diagnostic at all.
      *> PB184 was registered as a 13.18.63.4 GR5 DISTRIBUTION gap, on the reading that the byte-form leaves
      *> should each take their slice of the literal's bytes.  SR14 refutes that premise: the program is not
      *> conforming, so there is no area for the distribution to exist over.  GR5 is implemented, and correct,
      *> for the entire population SR14 admits - where every subordinate is usage DISPLAY, so the character
      *> image IS the byte image (pinned by the pb184_group_value_area golden).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB184N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GV VALUE "40537".
          05 GB PIC 9 COMP OCCURS 5.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY GB(1)
           STOP RUN.
