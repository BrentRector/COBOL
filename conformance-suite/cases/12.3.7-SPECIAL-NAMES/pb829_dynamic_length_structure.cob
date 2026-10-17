*>--- conformance-case
*> id: pb829_dynamic_length_structure
*> rules: [GR-12.3.7.4-18, SR-13.18.19.3-2, FMT-12.3.7.2]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB829 (finisher) - the SPECIAL-NAMES dynamic-length-structure-clause, ISO 12.3.7.2:
      *>   DYNAMIC LENGTH [STRUCTURE] dynamic-length-structure-name-1 [IS]
      *>       { |[SIGNED] [SHORT] PREFIXED| |DELIMITED| } | physical-structure-name-1
      *> (STRUCTURE and IS un-underlined; PREFIXED/DELIMITED in choice indicators - one or more, any
      *> order).  It had no grammar at all: `DYNAMIC LENGTH STRUCTURE DLS1 IS PREFIXED.` was COBOL0001,
      *> and a DYNAMIC LENGTH clause naming a structure was refused as "not yet supported".
      *> What the declaration decides is OBSERVABLE: ISO 8.5.1.10.1 makes an item's maximum size the
      *> smallest of its LIMIT phrase, "the largest integer that can be stored in an item of the usage
      *> specified in the PREFIXED phrase", and the implementor maximum; 12.3.7.4 GR18 sizes the length
      *> field (SHORT PREFIXED 65535, SIGNED SHORT PREFIXED 32767, PREFIXED / SIGNED PREFIXED past any
      *> 70000).  A 70000-character sender therefore stores 65535, 32767, all 70000 (DELIMITED has no
      *> length field) and 70000 (PREFIXED + DELIMITED, 32-bit field) characters, and a LIMIT phrase
      *> below the field's capacity still wins (10).  The contained program references a name its
      *> container declares (ISO 8.4.6.1 - configuration-section names reach contained elements).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB829DL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DYNAMIC LENGTH STRUCTURE DS-SHORT IS SHORT PREFIXED
           DYNAMIC LENGTH DS-SS SIGNED SHORT PREFIXED
           DYNAMIC LENGTH STRUCTURE DS-DELIM DELIMITED
           DYNAMIC LENGTH STRUCTURE DS-BOTH IS DELIMITED PREFIXED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BIG     PIC X(70000) VALUE ALL "Z".
       01 D-SHORT PIC X DYNAMIC LENGTH DS-SHORT.
       01 D-SS    PIC X DYNAMIC LENGTH DS-SS.
       01 D-DELIM PIC X DYNAMIC LENGTH DS-DELIM.
       01 D-BOTH  PIC X DYNAMIC LENGTH DS-BOTH.
       01 D-LIM   PIC X DYNAMIC LENGTH DS-SHORT LIMIT IS 10.
       01 N-OUT   PIC 9(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE BIG TO D-SHORT D-SS D-DELIM D-BOTH D-LIM.
           MOVE FUNCTION LENGTH(D-SHORT) TO N-OUT.
           DISPLAY "SHORT PREFIXED        " N-OUT.
           MOVE FUNCTION LENGTH(D-SS) TO N-OUT.
           DISPLAY "SIGNED SHORT PREFIXED " N-OUT.
           MOVE FUNCTION LENGTH(D-DELIM) TO N-OUT.
           DISPLAY "DELIMITED             " N-OUT.
           MOVE FUNCTION LENGTH(D-BOTH) TO N-OUT.
           DISPLAY "PREFIXED DELIMITED    " N-OUT.
           MOVE FUNCTION LENGTH(D-LIM) TO N-OUT.
           DISPLAY "LIMIT 10              " N-OUT " [" D-LIM "]".
           CALL "PB829DLN".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB829DLN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N-BIG   PIC X(40000) VALUE ALL "Q".
       01 N-D     PIC X DYNAMIC LENGTH DS-SS.
       01 N-OUT   PIC 9(5).
       PROCEDURE DIVISION.
           MOVE N-BIG TO N-D.
           MOVE FUNCTION LENGTH(N-D) TO N-OUT.
           DISPLAY "CONTAINED DS-SS       " N-OUT.
           GOBACK.
       END PROGRAM PB829DLN.
       END PROGRAM PB829DL.
