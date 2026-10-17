*>--- conformance-case
*> id: pb345-indexed-no-record
*> rules: [SR-13.4.5.3-7]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 §13.4.5.3 syntax rule 7: "Format 2 is the file description entry for
*> a relative file or an indexed file. For an indexed file, one or more record description
*> entries shall be associated with the file description entry."
*> SR3's permission to omit them is a FORMATS 1 AND 2 rule; SR7 takes it back for INDEXED
*> alone, because §12.4.5.12.3 SR2 requires the RECORD KEY operand to "reference a data
*> item ... within a record description entry associated with the file-name specified in
*> this file control entry" -- and there is none here.
*> The RELATIVE twin of this same shape is LEGAL and is exercised positively by
*> conformance/2023/pb345_record_less_fd_area. kb/Work PB345 -> COBOLNET1837.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB345N3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT I1 ASSIGN TO "pb345n3.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS IK
               FILE STATUS IS IS-1.
       DATA DIVISION.
       FILE SECTION.
       FD  I1 RECORD CONTAINS 10 CHARACTERS.
       WORKING-STORAGE SECTION.
       01  IK   PIC X(4).
       01  IS-1 PIC XX.
       01  W    PIC X(10).
       PROCEDURE DIVISION.
           OPEN INPUT I1.
           READ I1 INTO W INVALID KEY CONTINUE END-READ.
           CLOSE I1.
           STOP RUN.
