      *> kb/Work PB1303 - A TYPE-NAME DESCRIBED WITH A GLOBAL CLAUSE IS A GLOBAL NAME, INHERITED BY EVERY CONTAINED PROGRAM.
      *>   13.18.58.4 GR3: "The GLOBAL clause applies to the scope of the type-name."
      *>   8.4.6.2.2: "A constant-name, file-name, record-name, report-name, screen-name, or type-name described with a
      *>     GLOBAL clause is a global name."  8.4.6.2.1 1) / 3): a global name of source element A is in the set of
      *>     names of every source element A contains, directly or indirectly, and a name declared in the referencing
      *>     element itself is the one referenced.
      *>   MID (directly contained) uses OUTER's T and declares its OWN GLOBAL U; LEAF (indirectly contained in OUTER,
      *>   directly in MID) uses T from OUTER (two levels out) and U from MID, NOT OUTER's U (the nearer declaration).
      *>   SHADOW1303 declares its OWN data item T (five characters): 8.4.6.2.1 3 a) - the name declared in the element
      *>   itself is the one referenced - so FUNCTION LENGTH (T) is 5, not the 3 positions of OUTER's type T.
      *>   Each leg can fail: an inherited type is refused as undefined (COBOLNET1530), or LEAF's U resolves to OUTER's
      *>   "OUT" instead of MID's "MID", or T's group description (the 88 and the numeric member) is lost in the copy.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1303OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF GLOBAL.
          05 A PIC X(2) VALUE "GL".
          05 B PIC 9 VALUE 7.
             88 B-SEVEN VALUE 7.
       01 U TYPEDEF GLOBAL.
          05 UA PIC X(3) VALUE "OUT".
       01 OWN-T TYPE T.
       PROCEDURE DIVISION.
           DISPLAY "O=" A OF OWN-T B OF OWN-T
           CALL "MID"
           CALL "SHADOW1303"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MID.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U TYPEDEF GLOBAL.
          05 UA PIC X(3) VALUE "MID".
       01 M TYPE T.
       PROCEDURE DIVISION.
           DISPLAY "M=" A OF M B OF M
           IF B-SEVEN OF M
              DISPLAY "M88"
           END-IF
           CALL "LEAF"
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LEAF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L TYPE U.
       01 L2 TYPE T.
       PROCEDURE DIVISION.
           DISPLAY "L=" UA OF L " " A OF L2
           GOBACK.
       END PROGRAM LEAF.
       END PROGRAM MID.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SHADOW1303.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
           DISPLAY "S=" FUNCTION LENGTH (T)
           GOBACK.
       END PROGRAM SHADOW1303.
       END PROGRAM PB1303OK.
