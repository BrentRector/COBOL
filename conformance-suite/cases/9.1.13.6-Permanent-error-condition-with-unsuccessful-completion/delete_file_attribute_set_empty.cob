      *>--- conformance-case
      *> id: delete_file_attribute_set_empty
      *> rules: [GR-9.1.13.6-7, GR-14.9.10.4-18, GR-14.9.10.4-19,
      *>    DOC-A.1-50]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DELFAES19.
      *> §14.9.10.4 GR19: "The implementor shall define which of the
      *> fixed-file attributes are validated during the execution of the
      *> DELETE FILE statement." COBOL.NET's definition is THE EMPTY SET
      *> (docs/CONFORMANCE.md §7, Annex A.1 item 50 — required and
      *> required to be documented). An empty set is a legal definition:
      *> GR19 requires the implementor to DEFINE the set, never that it
      *> be non-empty.
      *>
      *> The consequence GR18 states is what this golden pins. GR18 sets
      *> '39' when "the attributes of the file connector referenced by
      *> file-name-1 and the fixed file attributes of the physical file"
      *> do not match. With no attribute validated, no mismatch is
      *> detectable, so '39' is unreachable from DELETE FILE BY
      *> DEFINITION rather than by omission — and a DELETE FILE issued
      *> through a connector whose attributes contradict the physical
      *> file's must still succeed.
      *>
      *> F-KILL contradicts F-MAKE in TWO of the attributes §9.1.6 names
      *> as fixed: the ORGANIZATION ("the primary attribute") and the
      *> logical record size in bytes (20 vs 40).
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-MAKE ASSIGN TO "dfaes.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS MK-ST.
           SELECT F-KILL ASSIGN TO "dfaes.dat"
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS KL-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F-MAKE.
       01 MK-REC PIC X(20).
       FD F-KILL.
       01 KL-REC PIC X(40).
       WORKING-STORAGE SECTION.
       01 MK-ST PIC XX.
       01 KL-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
      *> Create the physical file through the 20-byte record-sequential
      *> connector. §9.1.13.2: successful completion is '00'.
           OPEN OUTPUT F-MAKE. DISPLAY "OPENOUT=" MK-ST.
           MOVE "EMPTY ATTRIBUTE SET " TO MK-REC. WRITE MK-REC.
           DISPLAY "WRITE=" MK-ST.
      *> GR13 requires the connector not be open at the DELETE FILE.
           CLOSE F-MAKE. DISPLAY "CLOSE=" MK-ST.
      *> THE SUBJECT. Two fixed file attributes contradict the physical
      *> file, and the file is deleted anyway with GR20 a)'s '00' — the
      *> empty validated set means GR18 finds nothing to compare.
           DELETE FILE F-KILL. DISPLAY "DELMISMATCH=" KL-ST.
      *> The deletion was real, not merely reported: GR14's '05' is the
      *> SUCCESSFUL status for an absent file, and an OPEN INPUT of a
      *> non-optional absent file is '35' (§9.1.13.4).
           DELETE FILE F-KILL. DISPLAY "DELAGAIN=" KL-ST.
           OPEN INPUT F-MAKE. DISPLAY "OPENGONE=" MK-ST.
           STOP RUN.
