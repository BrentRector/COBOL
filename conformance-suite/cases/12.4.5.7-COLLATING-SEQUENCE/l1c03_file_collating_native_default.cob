      *>--- conformance-case
      *> id: l1c03_file_collating_native_default
      *> rules: [GR-12.4.5.7.4-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.4.5.7.4 4) - with no alphabet-name-1 in the file
      *> control entry, alphanumeric keys collate in the NATIVE sequence
      *>
      *> "4) If alphabet-name-1 is not specified in the file control
      *> entry, the native alphanumeric collating sequence applies to
      *> any record keys of class alphanumeric, whether primary or
      *> alternate, not specified in a key-level format of another
      *> COLLATING SEQUENCE clause."
      *>   cite.py: OK  §12.4.5.7.4 4)  (General rules)
      *> "6) Alphabet-name-3 applies to record keys identified by
      *> data-name-1 or record-key-name-1."
      *>   cite.py: OK  §12.4.5.7.4 6)  (General rules)
      *> Read order: "ascending based on the value of the key of
      *> reference according to the collating sequence of the physical
      *> file"
      *>   cite.py: OK  §9.1.8.2   (Sequential access mode)
      *> START: "The key specified in the KEY phrase ... becomes the key
      *> of reference"; the comparison is made "according to the
      *> collating sequence of the file".
      *>   cite.py: OK  §14.9.41.4 16)  (General rules)
      *>   cite.py: OK  §14.9.41.4 17)  (General rules)
      *> The PROGRAM collating sequence governs relation conditions,
      *> condition-names, CONTROL (§12.3.6.4 GR11) and sort/merge keys
      *> (GR13) - not indexed record keys, which GR4 gives the native
      *> sequence.
      *>   cite.py: OK  §12.3.6.4 11)  (General rules)
      *>   cite.py: OK  §12.3.6.4 13)  (General rules)
      *>
      *> SET-UP. PROGRAM COLLATING SEQUENCE IS REV (Z..A), so a
      *> compiler that wrongly let the program sequence reach a file
      *> key would reverse it. Records (prime/alternate): A/M, M/Z, Z/A.
      *>   KF: key-level COLLATING SEQUENCE OF KF-KEY IS REV only - no
      *>       alphabet-name-1, so the ALTERNATE key KF-ALT is native
      *>       (GR4) while the prime is REV (GR6).
      *>   NF: no COLLATING SEQUENCE clause at all - prime native (GR4).
      *>
      *> DERIVATION of each expected line.
      *>   KF by prime (REV: Z<M<A)       -> Z/A, M/Z, A/M.
      *>       This line pins GR6 and proves REV is honoured at all.
      *>   KF by alternate, START >= SPACE (native: space<A<M<Z)
      *>                                  -> alternates A, M, Z:
      *>                                     Z/A, A/M, M/Z.   (GR4)
      *>   NF by prime (native)           -> A/M, M/Z, Z/A.   (GR4)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C03E.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. L1C03BOX
           PROGRAM COLLATING SEQUENCE IS REV.
       SPECIAL-NAMES.
           ALPHABET REV IS "ZYXWVUTSRQPONMLKJIHGFEDCBA".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT KF ASSIGN TO "L1C03EK.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS KF-KEY
               ALTERNATE RECORD KEY IS KF-ALT
               COLLATING SEQUENCE OF KF-KEY IS REV
               FILE STATUS IS WS-FS.
           SELECT NF ASSIGN TO "L1C03EN.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS NF-KEY
               FILE STATUS IS WS-FS.
       DATA DIVISION.
       FILE SECTION.
       FD  KF.
       01  KF-REC.
           05  KF-KEY  PIC X.
           05  KF-ALT  PIC X.
       FD  NF.
       01  NF-REC.
           05  NF-KEY  PIC X.
           05  NF-ALT  PIC X.
       WORKING-STORAGE SECTION.
       01  WS-FS   PIC XX.
       01  WS-EOF  PIC 9.
       01  WS-OUT  PIC X(20).
       01  WS-P    PIC 99.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT KF NF.
           MOVE "AM" TO KF-REC NF-REC.
           WRITE KF-REC. WRITE NF-REC.
           MOVE "MZ" TO KF-REC NF-REC.
           WRITE KF-REC. WRITE NF-REC.
           MOVE "ZA" TO KF-REC NF-REC.
           WRITE KF-REC. WRITE NF-REC.
           CLOSE KF NF.
      *>   KF by its prime key (key of reference after OPEN).
           OPEN INPUT KF.
           MOVE SPACES TO WS-OUT. MOVE 1 TO WS-P. MOVE 0 TO WS-EOF.
           PERFORM UNTIL WS-EOF = 1
               READ KF NEXT
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END
                       STRING KF-KEY "/" KF-ALT " " DELIMITED BY SIZE
                           INTO WS-OUT WITH POINTER WS-P
               END-READ
           END-PERFORM.
           DISPLAY "KF PRIME " WS-OUT.
           CLOSE KF.
      *>   KF by its alternate key.
           OPEN INPUT KF.
           MOVE SPACE TO KF-ALT.
           START KF KEY IS >= KF-ALT
               INVALID KEY DISPLAY "START INVALID " WS-FS
           END-START.
           MOVE SPACES TO WS-OUT. MOVE 1 TO WS-P. MOVE 0 TO WS-EOF.
           PERFORM UNTIL WS-EOF = 1
               READ KF NEXT
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END
                       STRING KF-KEY "/" KF-ALT " " DELIMITED BY SIZE
                           INTO WS-OUT WITH POINTER WS-P
               END-READ
           END-PERFORM.
           DISPLAY "KF ALT   " WS-OUT.
           CLOSE KF.
      *>   NF by its prime key.
           OPEN INPUT NF.
           MOVE SPACES TO WS-OUT. MOVE 1 TO WS-P. MOVE 0 TO WS-EOF.
           PERFORM UNTIL WS-EOF = 1
               READ NF NEXT
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END
                       STRING NF-KEY "/" NF-ALT " " DELIMITED BY SIZE
                           INTO WS-OUT WITH POINTER WS-P
               END-READ
           END-PERFORM.
           DISPLAY "NF PRIME " WS-OUT.
           CLOSE NF.
           STOP RUN.
