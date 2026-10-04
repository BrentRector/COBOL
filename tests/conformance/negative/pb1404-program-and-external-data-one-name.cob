      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1404 - 8.3.2.2: "The following user-defined words shall be externalized to the operating
      *>   environment: 1) program-names of outermost programs, ... 2) data-names, file-names, and record-names of
      *>   items described with the EXTERNAL attribute." and "Within a run unit, all instances of a given name that
      *>   is externalized to the operating environment shall identify the same kind of entity or item."
      *>   KCSUB1404 is externalized as an outermost PROGRAM-ID and, by the EXTERNAL clause, as a data item.
      *>   COBOLNET2213 (duplicate-externalized-definition). The positive twin is
      *>   conformance/2002/pb1404_external_same_kind_is_one_instance.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1404N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 KCSUB1404 PIC X(5) EXTERNAL.
       PROCEDURE DIVISION.
           CALL "KCSUB1404"
           STOP RUN.
       END PROGRAM PB1404N1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. KCSUB1404.
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           GOBACK.
       END PROGRAM KCSUB1404.
