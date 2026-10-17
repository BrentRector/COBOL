*>--- conformance-case
*> id: pb909-write-advancing-indexed
*> rules: [SR-14.9.51.3-3]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 §14.9.51.3 SR3: "If the organization of the write file is indexed or relative, format 2 shall
*> be specified." Format 2 prints no ADVANCING and no END-OF-PAGE phrase, so a print-control phrase on an
*> indexed WRITE is a syntax-rule violation - the mirror of SR2, which already lands on COBOLNET1720. It used
*> to be a COBOLNET1756 deferral ("print-control phrases are for sequential print files") and a run-unit abort.
*> The rule is the same at every edition. COBOLNET1720 (kb/Work PB909).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB909NWAI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO "pb909nwai.dat"
               ORGANIZATION INDEXED ACCESS MODE DYNAMIC
               RECORD KEY IX-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE.
       01  IX-REC.
           05 IX-KEY  PIC X(4).
           05 IX-DATA PIC X(8).
       PROCEDURE DIVISION.
           OPEN OUTPUT IX-FILE.
           MOVE "K001" TO IX-KEY.
           WRITE IX-REC AFTER ADVANCING 1 LINE
               INVALID KEY DISPLAY "DUP"
           END-WRITE.
           CLOSE IX-FILE.
           STOP RUN.
