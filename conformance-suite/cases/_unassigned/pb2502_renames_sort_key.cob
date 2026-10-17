      *>--- conformance-case
      *> id: pb2502_renames_sort_key
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2502 - a level-66 RENAMES item named as a SORT key data-name. The same record-layout reader
      *> that places an indexed file's RECORD KEY answered "no position" for a RENAMES alias, which the SORT
      *> key screen read as "subject to an OCCURS clause" (COBOLNET1757) and refused legal source.
      *>   cite.py --check 14.9.40.3 "The same byte positions that are referenced by a key data-name in one record description entry are taken as the key in all records of the file"
      *>     -> OK 14.9.40.3 6) e)   (SR6 a): the key is "described in records associated with file-name-1"; a
      *>        RENAMES entry follows the record's last data description entry, 13.18.45.3 SR2)
      *>   cite.py --check 14.9.40.4 "the record containing the key data item with the lower value is returned first"
      *>     -> OK 14.9.40.4 8) a)
      *>   cite.py --check 14.9.40.4 "the record containing the key data item with the higher value is returned first"
      *>     -> OK 14.9.40.4 8) b)
      *> SK (K1 THRU K2, bytes 3-7, an alphanumeric group item: 13.18.45.4 GR2) is the major key, ascending; SE
      *> (D, the alias without THROUGH, which is data-name-2's own area: 13.18.45.4 GR1) the minor key, descending.
      *>
      *> EXPECTED VALUES, DERIVED: the four records are "aaZZzzzdddd", "bbAAaaadddd", "ccMMmmmdddd" and
      *> "ddAAaaaeeee". By SK ascending the order is AAaaa, AAaaa, MMmmm, ZZzzz (the characters compare in the
      *> program collating sequence, uppercase letters before lowercase). The two AAaaa records tie on SK, so the
      *> descending minor key SE decides: eeee (record dd) is higher than dddd (record bb), and the higher is
      *> returned first. The order is therefore dd, bb, cc, aa.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2502RENSORT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "pb2502-renames-sort.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD SF.
       01 SR.
          05 P PIC X(2).
          05 K1 PIC X(2).
          05 K2 PIC X(3).
          05 D PIC X(4).
       66 SK RENAMES K1 THRU K2.
       66 SE RENAMES D.
       WORKING-STORAGE SECTION.
       01 EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-SECTION SECTION.
       MAIN.
           SORT SF ON ASCENDING KEY SK ON DESCENDING KEY SE
               INPUT PROCEDURE IS LOADING
               OUTPUT PROCEDURE IS UNLOADING.
           STOP RUN.
       LOADING SECTION.
       LOAD-PARA.
           MOVE "aaZZzzzdddd" TO SR.
           RELEASE SR.
           MOVE "bbAAaaadddd" TO SR.
           RELEASE SR.
           MOVE "ccMMmmmdddd" TO SR.
           RELEASE SR.
           MOVE "ddAAaaaeeee" TO SR.
           RELEASE SR.
       UNLOADING SECTION.
       UNLOAD-PARA.
           PERFORM UNTIL EOF-FLAG = "Y"
               RETURN SF
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END DISPLAY P " " SK " " SE
               END-RETURN
           END-PERFORM.
