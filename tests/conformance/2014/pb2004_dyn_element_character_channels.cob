      *> kb/Work PB2004 - an element of a dynamic-capacity table written through a CHARACTER CHANNEL holds the
      *> characters the channel leaves, exactly as a fixed-table occurrence does.
      *> ISO 14.9.22.4 GR4 d): "the original value of the sign is retained upon completion of the INSPECT
      *>   statement"  (cite.py --check 14.9.22.4 -> OK 4) d)). A negative item whose digits all become
      *>   zeros holds a NEGATIVE zero, which no native value carrier holds: the element read back +0.
      *> N1 - a SIGN LEADING SEPARATE member of a group element, -5, REPLACING ALL "5" BY "0" => -000.
      *> N2 - an elementary SIGN TRAILING SEPARATE element, the same replacement => 000-.
      *> N3 - the default overpunched sign, member and elementary element. Its code is implementor-defined
      *>      (13.18.52.4 GR4); this compiler's EBCDIC-style zone writes "}" for a negative zero: 00} 00}.
      *> N4 - an element CREATED by the INSPECT's own receiving reference (8.5.1.9.3), -57, REPLACING ALL "5"
      *>      BY "1" => 017-, capacity 2.
      *> The other character channels into an element (kb/Work PB2004's sibling sweep):
      *> M  - a group MOVE into an elementary numeric element (14.9.25.4 GR4: "treated exactly as if it were an
      *>      alphanumeric to alphanumeric elementary move, except that there is no conversion") => "A B";
      *>      it used to stop the run unit as a Tier-C island.
      *> C  - a BY REFERENCE argument the called program fills from a group (14.2.3 GR8: "as if the formal
      *>      parameter occupies the same storage area as the argument") => "Q Z"; it read back 000.
      *> R  - a reference-modified store into an element (8.4.3.3.4 GR6, the unique data item is alphanumeric):
      *>      123 with position 2 set to "X" => 1X3; it read back 013.
      *>
      *>   N1=-000
      *>   N2=000-
      *>   N3=00} 00}
      *>   N4=0000000002 017-
      *>   M=[A B]
      *>   C=[Q Z]
      *>   R=[1X3]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2004DYN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 DT OCCURS DYNAMIC CAPACITY IN CG FROM 1.
             10 X PIC S9(3) SIGN LEADING SEPARATE.
             10 XO PIC S9(3).
       01 G2.
          05 DN PIC S9(3) SIGN TRAILING SEPARATE
             OCCURS DYNAMIC CAPACITY IN CN FROM 1.
       01 G3.
          05 DP PIC S9(3) OCCURS DYNAMIC CAPACITY IN CP FROM 1.
       01 SG.
          05 SGX PIC X(3) VALUE "A B".
       01 G4.
          05 DQ PIC 9(3) OCCURS DYNAMIC CAPACITY IN CQ FROM 1.
       01 G5.
          05 DR PIC 9(3) OCCURS DYNAMIC CAPACITY IN CR FROM 2.
       PROCEDURE DIVISION.
           MOVE -5 TO X (1) XO (1) DN (1) DP (1).
           INSPECT X (1) REPLACING ALL "5" BY "0".
           INSPECT XO (1) REPLACING ALL "5" BY "0".
           INSPECT DN (1) REPLACING ALL "5" BY "0".
           INSPECT DP (1) REPLACING ALL "5" BY "0".
           DISPLAY "N1=" X (1).
           DISPLAY "N2=" DN (1).
           DISPLAY "N3=" XO (1) " " DP (1).
           MOVE -57 TO DN (2).
           INSPECT DN (2) REPLACING ALL "5" BY "1".
           DISPLAY "N4=" CN " " DN (2).
           MOVE 5 TO DQ (1).
           MOVE SG TO DQ (1).
           DISPLAY "M=[" DQ (1) "]".
           MOVE 7 TO DR (2).
           CALL "PB2004SUB" USING DR (2).
           DISPLAY "C=[" DR (2) "]".
           MOVE 123 TO DR (1).
           MOVE "X" TO DR (1) (2:1).
           DISPLAY "R=[" DR (1) "]".
           STOP RUN.
       END PROGRAM PB2004DYN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2004SUB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SG.
          05 SGX PIC X(3) VALUE "Q Z".
       LINKAGE SECTION.
       01 L PIC 9(3).
       PROCEDURE DIVISION USING L.
           MOVE SG TO L.
           GOBACK.
       END PROGRAM PB2004SUB.
