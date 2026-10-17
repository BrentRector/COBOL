*>--- conformance-case
*> id: pb183_usage_pointer_placement_ok
*> rules: [SR-13.18.60.3-14]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB183 - the POSITIVE witnesses for both arms ISO 13.18.60.3 SR14 admits, plus the
      *> level-77 determination the screen rests on. These matter MORE than the negative fixtures:
      *> the hazard of a placement screen is over-rejection.
      *>
      *> ARM 1 - "an elementary data item at level 1": P01 / OBJ01 / PP01.
      *>
      *> LEVEL 77 SATISFIES ARM 1, and that is a derived determination, not an assumption. SR14 says
      *> "at level 1" and the standard writes "level 1 or level 77" elsewhere when it means both, so
      *> the question is real. It is settled by two texts. 8.5.1.3.2: "Three types of entries exist
      *> for which there is no true concept of level", the second being entries that specify
      *> noncontiguous working-storage, local storage and linkage data items - a 77 entry is not AT a
      *> level greater than 1, it is outside the level system, so SR14's arm cannot exclude it by
      *> level arithmetic. And 13.11.1 declares the two spellings ALTERNATIVES for one thing: "Data
      *> elements that bear no hierarchical relationship to any other data item may be described as
      *> records that are single elementary items. Alternatively, such data elements ... may be
      *> described as separate data description entries having level-number 77." SR14's evident
      *> subject is SUBORDINATION to a non-strong group, and a 77 entry is defined by having no such
      *> relationship. The surveyed implementations agree - GnuCOBOL's testsuite uses `77 ptr USAGE
      *> POINTER.` throughout.
      *>
      *> ARM 2 - "an elementary data item subordinate to a type declaration that includes the STRONG
      *> phrase": SP inside STRONGT. The test is the DECLARATION-side TYPEDEF STRONG, so a pointer
      *> member of a STRONG template is admitted at the template and its TYPE clones inherit the
      *> verdict; the weak spelling is the negative fixture pb183-usage-pointer-in-weak-typedef.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB183PLACEOK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 STRONGT IS TYPEDEF STRONG.
          05 SP USAGE POINTER.
          05 SA PIC X(4) VALUE "strg".
       01 SREC TYPE STRONGT.
       01 P01 USAGE POINTER.
       01 OBJ01 USAGE OBJECT REFERENCE.
       01 PP01 USAGE PROGRAM-POINTER.
       77 P77 USAGE POINTER.
       77 OBJ77 USAGE OBJECT REFERENCE.
       77 PP77 USAGE PROGRAM-POINTER.
       01 W-N PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           SET P01 TO NULL.
           SET OBJ01 TO NULL.
           SET PP01 TO NULL.
           SET P77 TO NULL.
           SET OBJ77 TO NULL.
           SET PP77 TO NULL.
           SET SP IN SREC TO NULL.
           MOVE 0 TO W-N.
           IF P01 = NULL AND P77 = NULL MOVE 1 TO W-N END-IF.
           DISPLAY "LVL1-AND-77=" W-N.
           MOVE 0 TO W-N.
           IF SP IN SREC = NULL MOVE 1 TO W-N END-IF.
           DISPLAY "STRONG-MEMBER=" W-N.
           DISPLAY "SA=" SA IN SREC.
           STOP RUN.
