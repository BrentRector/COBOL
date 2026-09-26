       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1098OP.
      *> ISO/IEC 1989:2023 §12.4.5.10.3 GR1, GR4, GR5 — the file
      *> organization is permanent (kb/Work PB1098).
      *>
      *> GR1: "The ORGANIZATION clause specifies the logical structure
      *> of a file. The file organization is established at the time a
      *> physical file is created and cannot subsequently be changed."
      *> GR4: "Relative organization is a permanent logical file
      *> structure in which each record is uniquely identified by an
      *> integer value greater than zero, that specifies the record's
      *> logical ordinal position in the file."
      *> GR5: "Indexed organization is a permanent logical file
      *> structure in which each record is identified by the value of
      *> one or more keys within that record."
      *> §14.9.27.4 GR10 makes a disagreement with the physical file's
      *> fixed file attributes the file attribute conflict, '39', and
      *> leaves WHICH attributes are validated to the implementor
      *> (docs/CONFORMANCE.md DOC-A.1-129): a relative or indexed store
      *> records its organization in its own header; a SEQUENTIAL
      *> description that would WRITE into such a file — OPEN I-O or
      *> EXTEND — is refused with '39', because its REWRITE would
      *> overwrite the header and its records would follow the frames
      *> as bytes no store can parse; OPEN INPUT changes nothing and is
      *> not validated (kb/Work PB802's determination).
      *>
      *> Why each leg can fail:
      *>  SQ-IO / SQ-EXT - '39'. The defect answered '00', and the I-O
      *>          REWRITE then destroyed the store (it opened only with
      *>          '39' afterwards under its own RELATIVE description).
      *>  SQ-IN  - '00' (unvalidated, and harmless).
      *>  RL-*   - the store is intact: RRN 7 is read back by its key
      *>          (GR4's ordinal identity), RRN 5 holds no record ('23'),
      *>          a key of 0 is not a relative record number ('34',
      *>          §14.9.51.4 GR29 b)).
      *>  IX-OVER-RL / RL-OVER-IX - a relative store through an INDEXED
      *>          description and an indexed store through a RELATIVE
      *>          one: '39' both ways.
      *>  IX-*   - the indexed store's records are identified by their
      *>          prime key (GR5): K001 is read by key, K009 is '23'.
      *>  SQ-IX  - a sequential I-O description over the INDEXED store
      *>          is '39' too.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RL ASSIGN TO "pb1098op-r.dat"
               ORGANIZATION RELATIVE ACCESS DYNAMIC
               RELATIVE KEY RK FILE STATUS FR.
           SELECT SQ ASSIGN TO "pb1098op-r.dat"
               ORGANIZATION SEQUENTIAL FILE STATUS FS.
           SELECT XR ASSIGN TO "pb1098op-r.dat"
               ORGANIZATION INDEXED ACCESS DYNAMIC
               RECORD KEY XR-K FILE STATUS FX.
           SELECT IX ASSIGN TO "pb1098op-i.dat"
               ORGANIZATION INDEXED ACCESS DYNAMIC
               RECORD KEY IX-K FILE STATUS FI.
           SELECT RI ASSIGN TO "pb1098op-i.dat"
               ORGANIZATION RELATIVE ACCESS DYNAMIC
               RELATIVE KEY RK FILE STATUS FJ.
           SELECT SI ASSIGN TO "pb1098op-i.dat"
               ORGANIZATION SEQUENTIAL FILE STATUS FT.
       DATA DIVISION.
       FILE SECTION.
       FD RL.
       01 RR PIC X(8).
       FD SQ.
       01 SR PIC X(8).
       FD XR.
       01 XR-R.
          05 XR-K PIC X(4).
          05 XR-D PIC X(4).
       FD IX.
       01 IX-R.
          05 IX-K PIC X(4).
          05 IX-D PIC X(4).
       FD RI.
       01 RI-R PIC X(8).
       FD SI.
       01 SI-R PIC X(8).
       WORKING-STORAGE SECTION.
       01 FR PIC XX.
       01 FS PIC XX.
       01 FX PIC XX.
       01 FI PIC XX.
       01 FJ PIC XX.
       01 FT PIC XX.
       01 RK PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RL.
           MOVE 1 TO RK. MOVE "REL1AAAA" TO RR. WRITE RR.
           MOVE 7 TO RK. MOVE "REL7BBBB" TO RR. WRITE RR.
           DISPLAY "RL-MAKE=" FR.
           CLOSE RL.
           OPEN I-O SQ.
           DISPLAY "SQ-IO=" FS.
           OPEN EXTEND SQ.
           DISPLAY "SQ-EXT=" FS.
           OPEN INPUT SQ.
           DISPLAY "SQ-IN=" FS.
           CLOSE SQ.
           OPEN I-O RL.
           DISPLAY "RL-OPEN=" FR.
           MOVE 7 TO RK. READ RL.
           DISPLAY "RL-READ7=" FR " " RR.
           MOVE 5 TO RK. READ RL INVALID KEY DISPLAY "RL-READ5 INVALID".
           DISPLAY "RL-READ5=" FR.
           MOVE 0 TO RK. MOVE "ZERO0000" TO RR.
           WRITE RR INVALID KEY DISPLAY "RL-KEY0 INVALID".
           DISPLAY "RL-KEY0=" FR.
           CLOSE RL.
           DISPLAY "RL-CLOSE=" FR.
           OPEN INPUT XR.
           DISPLAY "IX-OVER-RL=" FX.
           OPEN OUTPUT IX.
           MOVE "K002SEC " TO IX-R. WRITE IX-R.
           MOVE "K001FST " TO IX-R. WRITE IX-R.
           DISPLAY "IX-MAKE=" FI.
           CLOSE IX.
           OPEN I-O RI.
           DISPLAY "RL-OVER-IX=" FJ.
           OPEN I-O SI.
           DISPLAY "SQ-IX=" FT.
           OPEN INPUT IX.
           MOVE "K001" TO IX-K. READ IX.
           DISPLAY "IX-READ1=" FI " " IX-D.
           MOVE "K009" TO IX-K. READ IX INVALID KEY DISPLAY "IX-READ9 INVALID".
           DISPLAY "IX-READ9=" FI.
           CLOSE IX.
           STOP RUN.
