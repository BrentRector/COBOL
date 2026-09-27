      *> kb/Work PB1126 - ISO 14.9.22.4 GR14, GR15 and GR22 SET EC-RANGE-INSPECT-SIZE.
      *> The exception-condition mechanism (and with it >>TURN, 7.3.25) arrives in 2002, so this is the
      *> introducing edition; the three rules are unchanged text in 2014 and 2023.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN:
      *>
      *> E1  GR14: "The size of literal-3 or the data item referenced by identifier-5 shall be equal to
      *>     the size of literal-1 or the data item referenced by identifier-3. If these sizes are not
      *>     equal, the EC-RANGE-INSPECT-SIZE exception condition is set to exist". P2 is two
      *>     characters, "Q" one => the condition. Table 13 makes it Fatal; the declarative runs and
      *>     RESUME AT NEXT STATEMENT (14.9.33) continues. The replaced content is undefined, so it is
      *>     not displayed. FUNCTION EXCEPTION-STATUS is the name, space-padded to 31 characters.
      *> E2  GR15: "When the CHARACTERS phrase is used, the data item referenced by identifier-5 shall be
      *>     one character in length. If it is not, the EC-RANGE-INSPECT-SIZE exception condition is
      *>     set to exist" - R2 is two characters.
      *> E3  GR22, the CONVERTING twin: F (3) TO T2 (2).
      *> E4  NOT a mismatch: GR14's last sentence sizes a FIGURATIVE literal-3 to identifier-3, so
      *>     REPLACING ALL P2 BY SPACES sets nothing; the declarative does not run and XABX => "X  X".
      *>
      *>   E1=EC-RANGE-INSPECT-SIZE
      *>   E2=EC-RANGE-INSPECT-SIZE
      *>   E3=EC-RANGE-INSPECT-SIZE
      *>   E4=X  X
      *> (each status line padded to 31 characters, trimmed by the comparison)
       >>TURN EC-RANGE-INSPECT-SIZE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1126INSZ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TAG PIC X(2) VALUE SPACES.
       01 S1 PIC X(4) VALUE "XABX".
       01 S2 PIC X(4) VALUE "XABX".
       01 S3 PIC X(6) VALUE "abcdef".
       01 S4 PIC X(4) VALUE "XABX".
       01 P2 PIC X(2) VALUE "AB".
       01 R2 PIC X(2) VALUE "**".
       01 F  PIC X(3) VALUE "abc".
       01 T2 PIC X(2) VALUE "XY".
       PROCEDURE DIVISION.
       DECLARATIVES.
       HSIZE SECTION.
           USE AFTER EXCEPTION CONDITION EC-RANGE-INSPECT-SIZE.
       HSIZE-P.
           DISPLAY TAG "=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE "E1" TO TAG
           INSPECT S1 REPLACING ALL P2 BY "Q"
           MOVE "E2" TO TAG
           INSPECT S2 REPLACING CHARACTERS BY R2
           MOVE "E3" TO TAG
           INSPECT S3 CONVERTING F TO T2
           MOVE "E4" TO TAG
           INSPECT S4 REPLACING ALL P2 BY SPACES
           DISPLAY "E4=" S4
           STOP RUN.
