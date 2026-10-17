      *>--- conformance-case
      *> id: open_fixed_attribute_conflict_ix
      *> rules: [GR-9.1.13.6-7, GR-12.4.5.6.4-3, GR-12.4.5.12.4-3, GR-14.9.27.4-10, DOC-A.1-129]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OPNFATIX.
      *> ISO/IEC 1989:2023 §14.9.27.4 GR10 — the INDEXED half of the
      *> Annex A.1 item 129 validated set (kb/Work PB193). §9.1.6 names
      *> "prime record key, alternate record keys, SUPPRESS WHEN
      *> attribute ... the collating sequence of the keys for indexed
      *> files" among the attributes fixed when a file is created;
      *> COBOL.NET records and validates the KEY COUNT and each key's
      *> window, DUPLICATES, SUPPRESS WHEN value and collating
      *> sequence. The organization / record-size / record-type
      *> attributes are the 85 twin, open_fixed_attribute_conflict.
      *>
      *> A key-geometry mismatch is the sharpest case GR10 exists for:
      *> the index of an indexed file IS its key descriptors, so a
      *> connector that slices a different window reads records by a
      *> key the file was never built on — silently, and with no
      *> record-length arithmetic to notice.
      *>
      *> WHERE the file records them is the store's OWN HEADER, in
      *> the data file, as a real ISAM file keeps its key definitions
      *> — owner decision 2026-09-07 (kb/Work PB802: "Let's match
      *> GNUCobol's implementation in spirit. No sidecar of any
      *> type."), which replaced the catalog sidecar PB193 landed.
      *> The expectations below did NOT move when it did: the key
      *> table changed homes and the answers did not.
      *>
      *> The key half of the set — and only the key half — has a
      *> documented runtime opt-out, COBOLNET_KEYCHECK=OFF
      *> (CONFORMANCE.md DOC-A.1-129), which no COBOL program can
      *> set; FixedFileAttributeFormatTests pins both of its arms.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
      *> The subject: one prime key, a 4-byte window at offset 0.
           SELECT IX-A ASSIGN TO "opnfatix.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS A-KEY
               FILE STATUS IS A-ST.
      *> Same offset, a SIX-byte prime key window.
           SELECT IX-B ASSIGN TO "opnfatix.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS B-KEY
               FILE STATUS IS B-ST.
      *> The same prime key, plus an ALTERNATE record key the file was
      *> never built with — the key COUNT differs.
           SELECT IX-C ASSIGN TO "opnfatix.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS C-KEY
               ALTERNATE RECORD KEY IS C-ALT WITH DUPLICATES
               FILE STATUS IS C-ST.
      *> The MATCHING connector — every validated attribute agrees.
           SELECT IX-D ASSIGN TO "opnfatix.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS D-KEY
               FILE STATUS IS D-ST.
       DATA DIVISION.
       FILE SECTION.
       FD IX-A.
       01 A-REC.
           05 A-KEY  PIC X(4).
           05 A-DATA PIC X(16).
       FD IX-B.
       01 B-REC.
           05 B-KEY  PIC X(6).
           05 B-DATA PIC X(14).
       FD IX-C.
       01 C-REC.
           05 C-KEY  PIC X(4).
           05 C-ALT  PIC X(5).
           05 C-REST PIC X(11).
       FD IX-D.
       01 D-REC.
           05 D-KEY  PIC X(4).
           05 D-DATA PIC X(16).
       WORKING-STORAGE SECTION.
       01 A-ST PIC XX.
       01 B-ST PIC XX.
       01 C-ST PIC XX.
       01 D-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN-P.
      *> 1. §14.9.27.4 GR18 — the OPEN OUTPUT creates the file, so
      *>    §9.1.6 fixes its key attributes here: ONE key, the 4-byte
      *>    window at offset 0, no duplicates, no SUPPRESS WHEN, the
      *>    native collating sequence (§12.4.5.3 GR6 — no COLLATING
      *>    SEQUENCE clause applies).
      *>    The two records are released in ASCENDING prime key order
      *>    because the access mode is sequential: §9.1.13.5 item 1
      *>    makes a violation of "the ascending sequence requirements
      *>    for successive record key values" the '21' sequence error,
      *>    and each WRITE's status is displayed so a golden can never
      *>    pass with a record that silently never reached the file.
           OPEN OUTPUT IX-A
           MOVE "K001" TO A-KEY
           MOVE "FIRST" TO A-DATA
           WRITE A-REC
           DISPLAY "I1-W1=" A-ST
           MOVE "K002" TO A-KEY
           MOVE "SECOND" TO A-DATA
           WRITE A-REC
           DISPLAY "I1-W2=" A-ST
           CLOSE IX-A
           DISPLAY "I1-MAKE=" A-ST
      *> 2. A six-byte prime key window over a file whose prime key is
      *>    four bytes: the key descriptors differ, so GR10's
      *>    comparison fails and the OPEN is unsuccessful with '39'.
           OPEN INPUT IX-B
           DISPLAY "I2-KEYLEN=" B-ST
      *> 3. The prime key agrees but the file has no alternate index:
      *>    the number of record keys differs — '39'.
           OPEN INPUT IX-C
           DISPLAY "I3-ALTCOUNT=" C-ST
      *> 4. Every validated attribute agrees: '00', and the file reads
      *>    in prime-key order (§14.9.27.4 GR14 — the file position
      *>    indicator is set to the lowest collating position and the
      *>    prime record key is the key of reference).
           OPEN INPUT IX-D
           DISPLAY "I4-MATCH=" D-ST
           READ IX-D
               AT END DISPLAY "I4-UNEXPECTED-AT-END"
           END-READ
           DISPLAY "K=[" D-KEY "] D=[" D-DATA "]"
           CLOSE IX-D
           STOP RUN.
