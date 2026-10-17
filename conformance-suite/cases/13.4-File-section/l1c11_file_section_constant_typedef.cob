      *>--- conformance-case
      *> id: l1c11_file_section_constant_typedef
      *> rules: [FMT-13.4.2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.4.2 general format — constant and type-declaration
      *>   entries inside the FILE SECTION, after an FD and an SD
      *> Format: FILE SECTION. [ { file-description-entry
      *>   [constant-entry | record-description-entry |
      *>   type-declaration-entry] ... } |
      *>   { sort-merge-file-description-entry
      *>   {constant-entry | record-description-entry |
      *>   type-declaration-entry} ... } ] ...
      *>   cite.py --check 13.4.2 "constant-entry"
      *>   -> OK  §13.4.2   (General format)
      *>   cite.py --check 13.4.2 "type-declaration-entry"
      *>   -> OK  §13.4.2   (General format)
      *>   cite.py --check 13.4.2 "sort-merge-file-description-entry"
      *>   -> OK  §13.4.2   (General format)
      *>   cite.py --check 13.10.4 "the effect of specifying
      *>   constant-name-1 in other than this entry is as if literal-1
      *>   or the text represented by compilation-variable-name-1 were
      *>   written where constant-name-1 is written"
      *>   -> OK  §13.10.4 1)  (General rules)
      *>   cite.py --check 13.12 "A type declaration entry has no
      *>   storage associated with it"
      *>   -> OK  §13.12   (Type declaration entry)
      *> F-OUT's entry is followed by a constant, a type declaration,
      *> its record and another constant; S-WK's by a constant and
      *> its record. The entries interleave freely and the records
      *> still belong to their files.
      *> Derivation:
      *>   K=07 09 11        the three constants act as their literals.
      *>   T=ABC LEN=3       W-T is TYPE T-3 (PIC X(3)); "ABCDE" is
      *>                     truncated to ABC; LENGTH is 3.
      *>   F=HELLO           F-OUT's record written and read back.
      *>   S=AAAAA           the SD's record: two RELEASEd records are
      *>   S=BBBBB           RETURNed in ascending key order.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C11G.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-OUT ASSIGN TO "L1C11G.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT S-WK ASSIGN TO "L1C11G.SRT".
       DATA DIVISION.
       FILE SECTION.
       FD F-OUT.
       01 K-SEVEN CONSTANT AS 7.
       01 T-3 TYPEDEF PIC X(3).
       01 F-REC PIC X(5).
       01 K-NINE CONSTANT AS 9.
       SD S-WK.
       01 K-ELEVEN CONSTANT AS 11.
       01 S-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 W-T TYPE T-3.
       01 W-K PIC 99.
       01 W-EOF PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE K-SEVEN TO W-K.
           DISPLAY "K=" W-K " " WITH NO ADVANCING.
           MOVE K-NINE TO W-K.
           DISPLAY W-K " " WITH NO ADVANCING.
           MOVE K-ELEVEN TO W-K.
           DISPLAY W-K.
           MOVE "ABCDE" TO W-T.
           DISPLAY "T=" W-T " LEN=" FUNCTION LENGTH(W-T).
           OPEN OUTPUT F-OUT.
           MOVE "HELLO" TO F-REC.
           WRITE F-REC.
           CLOSE F-OUT.
           MOVE SPACES TO F-REC.
           OPEN INPUT F-OUT.
           READ F-OUT AT END MOVE "EOF" TO F-REC END-READ.
           CLOSE F-OUT.
           DISPLAY "F=" F-REC.
           SORT S-WK ON ASCENDING KEY S-REC
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN.
           STOP RUN.
       FEED.
           MOVE "BBBBB" TO S-REC.
           RELEASE S-REC.
           MOVE "AAAAA" TO S-REC.
           RELEASE S-REC.
       DRAIN.
           MOVE "N" TO W-EOF.
           PERFORM UNTIL W-EOF = "Y"
               RETURN S-WK
                   AT END MOVE "Y" TO W-EOF
                   NOT AT END DISPLAY "S=" S-REC
               END-RETURN
           END-PERFORM.
