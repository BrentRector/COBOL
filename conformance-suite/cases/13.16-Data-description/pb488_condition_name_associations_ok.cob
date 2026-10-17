*>--- conformance-case
*> id: pb488_condition_name_associations_ok
*> rules: [SR-13.16.3-15, SR-13.16.3-24, SR-13.18.60.3-11]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB488 - THE OVER-REJECTION GUARD for the ISO 13.16.3 SR24 condition-name association screen.
      *> SR24 is an EXCLUSION list, so the hazard a screen for it creates is refusing legal COBOL, not
      *> admitting illegal COBOL. Every leg below is a shape SR24 does NOT exclude, chosen because the
      *> neighbouring letter would catch it if the predicate were written one word wider.
      *>
      *> 1) CONSECUTIVE 88s. SR24 a) excludes "another level 88 entry" from BEING a conditional variable,
      *>    which is precisely what makes a RUN of condition-names share one: the rule's own second sentence
      *>    is plural - "The condition-name entries for a particular conditional variable shall immediately
      *>    follow the entry describing the item". P-AB, P-CD and P-AB-OR-CD all associate with P.
      *>    P = "AB", so P-AB is true, P-CD is false, and P-AB-OR-CD (13.18.63.2 format 3's literal list) is
      *>    true on its first literal.
      *> 2) AN ALPHANUMERIC GROUP WHOSE MEMBERS ARE ALL USAGE DISPLAY. SR24 c) excludes only a group
      *>    "containing items with a usage other than display". 8.8.4.2.1 treats AG as an elementary
      *>    alphanumeric item, whose value is its members' concatenation "ABCD", so AG-COND is true.
      *> 3) AN 88 AFTER A LEVEL-66 ALIAS ELSEWHERE IN THE SECTION. SR24 b) excludes the alias itself; it says
      *>    nothing about later entries. Q follows R1 in source but is an ordinary entry, so Q-AB is a normal
      *>    association and is true.
      *> 4) AN ELEMENTARY MEMBER OF A STRONG TYPE DECLARATION. SR24 g)'s second arm excludes "a GROUP item
      *>    subordinate to such a type declaration" - an elementary member is not one. F1 of the TYPE
      *>    reference V is moved "AB", so F1-AB is true.
      *> 5) A GROUP CONTAINING AN OCCURS DEPENDING ON TABLE. SR24 h) excludes "a variable-length group", and
      *>    8.5.1.12.1 defines that as "a group item whose data description has at least one dynamic-length
      *>    elementary item or dynamic-capacity table as a subordinate item". An occurs-depending-on group is
      *>    a DIFFERENT shape - the standard lists the two separately (13.18.12.3 SR2 names "an
      *>    occurs-depending-on group item, a variable-length group, or a dynamic-length elementary item") -
      *>    so ODO-COND is legal. Its literal "ZZZZZ" differs from the group's image in the first character
      *>    position under any extent (8.8.4.2.7 compares left to right), so the answer is N without
      *>    depending on how the ODO extent is measured.
      *> 6) A GROUP CONTAINING A FIXED OCCURS TABLE OF DISPLAY ITEMS. Same c) reasoning as leg 2; the image
      *>    after the two MOVEs is "AABB", so TG-COND is true.
      *> 7) A NATIONAL GROUP. SR24 c) is confined to an ALPHANUMERIC group; 13.18.29.4 GR2 makes NG "an
      *>    elementary item of usage national described with PICTURE N(m)", i.e. not an alphanumeric group at
      *>    all, so its members' usage national does not exclude it. Its as-if image is N"AB", so NG-COND is
      *>    true.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB488-COND-ASSOC-OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P PIC X(2) VALUE "AB".
       88 P-AB VALUE "AB".
       88 P-CD VALUE "CD".
       88 P-AB-OR-CD VALUE "AB" "CD".
       01 AG.
       88 AG-COND VALUE "ABCD".
          05 AGX PIC X(2) VALUE "AB".
          05 AGY PIC X(2) VALUE "CD".
       01 REC.
          05 RA PIC X(2) VALUE "AB".
          05 RB PIC X(2) VALUE "CD".
       66 R1 RENAMES RA THRU RB.
       01 Q PIC X(2) VALUE "AB".
       88 Q-AB VALUE "AB".
       01 T IS TYPEDEF STRONG.
          05 F1 PIC X(2).
       88 F1-AB VALUE "AB".
          05 F2 PIC X(2).
       01 V TYPE T.
       01 ODO-G.
       88 ODO-COND VALUE "ZZZZZ".
          05 ODO-N PIC 9 VALUE 2.
          05 ODO-E PIC X(2) OCCURS 1 TO 3 DEPENDING ON ODO-N.
       01 TG.
       88 TG-COND VALUE "AABB".
          05 TGE PIC X(2) OCCURS 2.
       01 NG GROUP-USAGE NATIONAL.
       88 NG-COND VALUE N"AB".
          05 NGF PIC N(2) VALUE N"AB".
       PROCEDURE DIVISION.
           MOVE "AB" TO F1 OF V
           MOVE "AA" TO TGE(1)
           MOVE "BB" TO TGE(2)
           IF P-AB DISPLAY "P-AB=Y" ELSE DISPLAY "P-AB=N" END-IF
           IF P-CD DISPLAY "P-CD=Y" ELSE DISPLAY "P-CD=N" END-IF
           IF P-AB-OR-CD DISPLAY "P-EITHER=Y" ELSE DISPLAY "P-EITHER=N" END-IF
           IF AG-COND DISPLAY "AG=Y" ELSE DISPLAY "AG=N" END-IF
           IF Q-AB DISPLAY "Q-AB=Y" ELSE DISPLAY "Q-AB=N" END-IF
           IF F1-AB OF V DISPLAY "F1-AB=Y" ELSE DISPLAY "F1-AB=N" END-IF
           IF ODO-COND DISPLAY "ODO=Y" ELSE DISPLAY "ODO=N" END-IF
           IF TG-COND DISPLAY "TG=Y" ELSE DISPLAY "TG=N" END-IF
           IF NG-COND DISPLAY "NG=Y" ELSE DISPLAY "NG=N" END-IF
           STOP RUN.
