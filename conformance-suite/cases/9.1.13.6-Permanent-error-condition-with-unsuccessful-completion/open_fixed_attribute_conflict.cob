      *>--- conformance-case
      *> id: open_fixed_attribute_conflict
      *> rules: [GR-9.1.13.6-7, GR-12.4.5.10.3-1, GR-14.9.27.4-10, DOC-A.1-129]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OPNFAT85.
      *> ISO/IEC 1989:2023 §14.9.27.4 GR10 — the OPEN statement's fixed
      *> file attribute comparison, and the Annex A.1 item 129 validated
      *> set it delegates to the implementor (kb/Work PB193).
      *>
      *> GR10: "During the execution of the OPEN statement when the file
      *> connector is matched with the file and the file exists, the
      *> attributes of the file connector as specified in the file
      *> control paragraph and the file description entry are compared
      *> with the fixed file attributes of the file. If the attributes
      *> don't match, a file attribute conflict condition occurs, the
      *> execution of the OPEN statement is unsuccessful, and the I-O
      *> status associated with file-name-1 is set to '39'." It then
      *> delegates: "The implementor defines which of the fixed-file
      *> attributes are validated during the execution of the OPEN
      *> statement. The validation of fixed-file attributes may vary
      *> depending on the organization or storage medium of the file."
      *>
      *> COBOL.NET's determination USES that permission, and this golden
      *> pins BOTH of its arms:
      *>
      *>   PART A (steps 1-9), a RELATIVE subject. A relative store is
      *>   an implementor-defined structure whose layout IS the record
      *>   type and the record sizes, so those are validated along with
      *>   the organization — §9.1.6's "primary attribute", of which
      *>   §9.1.6 names exactly three: "There are three organizations:
      *>   sequential, relative, and indexed". The indexed key
      *>   attributes are the 2023 twin, ..._conflict_ix.
      *>
      *>   PART B (steps 10-13), a FIXED-LENGTH SEQUENTIAL subject.
      *>   Such a file is PLAIN BYTES and records NOTHING, so nothing
      *>   about it is validated: §9.1.7.2 puts its record lengths
      *>   in the data and in the reading program rather than in the
      *>   file — "In record sequential files the length of each
      *>   record is determined by any information the implementor may
      *>   add to the record on the physical storage medium (such as
      *>   record length headers)", and COBOL.NET adds none to a
      *>   fixed-length record sequential file — and the standard
      *>   answers the resulting disagreement with a SUCCESSFUL
      *>   completion: §9.1.13.2 item 3, "I-O status = 04. A READ
      *>   statement is successfully executed but the physical record
      *>   from the file is shorter than or longer than the minimum or
      *>   maximum length of records allowed for the fixed file
      *>   attributes for that file." Step 12 shows that '04' happening
      *>   where a '39' at step 11 would have made it unreachable.
      *>   Step 13 shows the direction the FORMAT CAN see: a plain byte
      *>   stream carries no store header, so a RELATIVE description
      *>   cannot interpret it at all.
      *>
      *>   PART C (steps 14-16), a RECORD VARYING SEQUENTIAL subject.
      *>   §9.1.7.2's "such as record length headers" is exactly what
      *>   COBOL.NET adds to a VARYING record-sequential file — a
      *>   4-byte little-endian length prefix per record, by the method
      *>   §12.4.5.11.4 GR5 leaves to the implementor ("If the RECORD
      *>   DELIMITER clause is not specified, the method used for
      *>   determining the length of a variable-length record is
      *>   specified by the implementor") — so that prefix is the one
      *>   §9.1.6 attribute a sequential file DOES record, and the
      *>   whole of what GR10 compares for one.
      *>
      *> The rule is version-invariant (inventory row GR-14.9.27.4-10:
      *> 85, 2002, 2014, 2023), so it is pinned at the OLDEST edition
      *> here and at the newest by the indexed twin.
      *>
      *> STEP 6 pins the other half of the determination: §14.9.27.4
      *> GR18 — "If the OUTPUT phrase is specified, the successful
      *> execution of the OPEN statement creates the file" — makes an
      *> OPEN OUTPUT a CREATION, and §9.1.6 fixes a file's attributes
      *> "at the time it is created". So OPEN OUTPUT is never judged
      *> against the previous file's attributes; it ESTABLISHES them,
      *> and steps 7 and 8 show the roles of the two FDs swapped.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
      *> PART A's subject: a RELATIVE file of fixed 20-byte records.
           SELECT F-MAKE ASSIGN TO "opnfat85.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS MK-ST.
      *> Same organization and record type, DIFFERENT record size.
           SELECT F-BIG ASSIGN TO "opnfat85.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS BG-ST.
      *> Different ORGANIZATION, same record size.
           SELECT F-SEQ ASSIGN TO "opnfat85.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS SQ-ST.
      *> The MATCHING connector — every validated attribute agrees.
           SELECT F-SAME ASSIGN TO "opnfat85.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS SM-ST.
      *> Same organization and maximum size, different RECORD TYPE.
           SELECT F-VAR ASSIGN TO "opnfat85.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS VR-ST.
      *> PART B's subject: a SEQUENTIAL file of fixed 20-byte records.
           SELECT G-MAKE ASSIGN TO "opnfat85b.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS GM-ST.
      *> Same organization, WIDER record — not a conflict.
           SELECT G-WIDE ASSIGN TO "opnfat85b.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS GW-ST.
      *> Different ORGANIZATION over the sequential file — a conflict.
           SELECT G-REL ASSIGN TO "opnfat85b.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS GR-ST.
      *> PART C. A RECORD VARYING description over PART B's plain
      *> 40-byte file, whose bytes are not a record-length framing.
           SELECT G-VARY ASSIGN TO "opnfat85b.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS GV-ST.
      *> PART C's own subject: a varying file COBOL.NET writes.
           SELECT H-VARY ASSIGN TO "opnfat85c.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS HV-ST.
      *> The same file through a WIDER minimum — the '04' boundary.
           SELECT J-VARY ASSIGN TO "opnfat85c.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS JV-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F-MAKE.
       01 MK-REC PIC X(20).
       FD F-BIG.
       01 BG-REC PIC X(40).
       FD F-SEQ.
       01 SQ-REC PIC X(20).
       FD F-SAME.
       01 SM-REC PIC X(20).
       FD F-VAR
           RECORD IS VARYING IN SIZE FROM 1 TO 40 CHARACTERS
               DEPENDING ON VR-LEN.
       01 VR-REC PIC X(40).
       FD G-MAKE.
       01 GM-REC PIC X(20).
       FD G-WIDE.
       01 GW-REC PIC X(30).
       FD G-REL.
       01 GR-REC PIC X(20).
       FD G-VARY
           RECORD IS VARYING IN SIZE FROM 5 TO 40 CHARACTERS
               DEPENDING ON GV-LEN.
       01 GV-REC PIC X(40).
       FD H-VARY
           RECORD IS VARYING IN SIZE FROM 5 TO 40 CHARACTERS
               DEPENDING ON HV-LEN.
       01 HV-REC PIC X(40).
       FD J-VARY
           RECORD IS VARYING IN SIZE FROM 30 TO 40 CHARACTERS
               DEPENDING ON JV-LEN.
       01 JV-REC PIC X(40).
       WORKING-STORAGE SECTION.
       01 MK-ST PIC XX.
       01 BG-ST PIC XX.
       01 SQ-ST PIC XX.
       01 SM-ST PIC XX.
       01 VR-ST PIC XX.
       01 VR-LEN PIC 9(4).
       01 GM-ST PIC XX.
       01 GW-ST PIC XX.
       01 GR-ST PIC XX.
       01 GV-ST PIC XX.
       01 GV-LEN PIC 9(4).
       01 HV-ST PIC XX.
       01 HV-LEN PIC 9(4).
       01 JV-ST PIC XX.
       01 JV-LEN PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
      *> 1. Create PART A's subject. GR18: the OPEN OUTPUT creates the
      *>    file, so §9.1.6 fixes its attributes here — RELATIVE, fixed
      *>    record type, minimum and maximum logical record size 20.
           OPEN OUTPUT F-MAKE
           MOVE "ALPHA" TO MK-REC
           WRITE MK-REC
           MOVE "BRAVO" TO MK-REC
           WRITE MK-REC
           CLOSE F-MAKE
           DISPLAY "S1-MAKE=" MK-ST
      *> 2. Record size 40 against a RELATIVE file created with 20 —
      *>    §9.1.6's "minimum and maximum logical record size in bytes"
      *>    are validated for a relative store, whose slot layout IS
      *>    those sizes, so GR10's comparison fails: '39' (§9.1.13.6
      *>    item 7).
           OPEN INPUT F-BIG
           DISPLAY "S2-BIGGER=" BG-ST
      *> 3. ORGANIZATION SEQUENTIAL against a RELATIVE file, and it is
      *>    NOT a conflict: '00'. — THIS IS WHAT OWNER DECISION
      *>    2026-09-07 GIVES UP (kb/Work PB802: "Let's match GNUCobol's
      *>    implementation in spirit. No sidecar of any type."), and it
      *>    is pinned so that it stays a DECISION rather than becoming
      *>    a regression nobody noticed. GR10 compares the connector
      *>    with "the fixed file attributes of the file", and a
      *>    fixed-length record sequential file is plain bytes that
      *>    record none — so nothing in this file contradicts this
      *>    description. The catalog sidecar that used to answer '39'
      *>    here was a second artifact beside the data file; with it
      *>    gone, what survives is step 13's REVERSE direction (a plain
      *>    file opened through a RELATIVE FD has no store header and
      *>    is still '39') and RecordLayoutNotice's stderr report that
      *>    the byte count is not a whole multiple of 20.
           OPEN INPUT F-SEQ
           DISPLAY "S3-SEQUENTIAL=" SQ-ST
      *>    What the successful OPEN then admits is the STORE'S OWN
      *>    BYTES read as 20-character records — the misread this
      *>    rule used to prevent, and the one RecordLayoutNotice
      *>    reports on stderr above. ⛔ The CLOSE is load-bearing: an
      *>    OPEN that was unsuccessful left NO connector open, so
      *>    step 6's OPEN OUTPUT on this same physical file would
      *>    otherwise meet a §9.1.15 sharing conflict ('61') instead
      *>    of GR18's creation.
           READ F-SEQ
               AT END DISPLAY "S3-UNEXPECTED-AT-END"
           END-READ
           DISPLAY "S3B-SEQ-READ=" SQ-ST
           CLOSE F-SEQ
      *> 4. Every validated attribute agrees: the OPEN succeeds ('00')
      *>    and the file reads normally. Without this direction the
      *>    check could pass by rejecting everything.
           OPEN INPUT F-SAME
           DISPLAY "S4-MATCH=" SM-ST
           READ F-SAME
               AT END DISPLAY "S4-UNEXPECTED-AT-END"
           END-READ
           DISPLAY "R1=[" SM-REC "]"
           CLOSE F-SAME
      *> 5. GR10 is not INPUT-only: it fires whenever the file exists,
      *>    and an EXTEND that appends 40-byte records to a 20-byte
      *>    relative store would corrupt it permanently.
           OPEN EXTEND F-BIG
           DISPLAY "S5-EXTEND=" BG-ST
      *> 6. GR18: OPEN OUTPUT CREATES the file, so it is not judged
      *>    against the old attributes — it establishes new ones.
           OPEN OUTPUT F-BIG
           DISPLAY "S6-OUTPUT-40=" BG-ST
           MOVE "CHARLIE" TO BG-REC
           WRITE BG-REC
           CLOSE F-BIG
      *> 7. The file's fixed attributes are now the 40-byte ones, so
      *>    the connector that matched in step 4 conflicts.
           OPEN INPUT F-SAME
           DISPLAY "S7-OLD-20=" SM-ST
      *> 8. ... and the connector that conflicted in step 2 matches.
           OPEN INPUT F-BIG
           DISPLAY "S8-NEW-40=" BG-ST
           READ F-BIG
               AT END DISPLAY "S8-UNEXPECTED-AT-END"
           END-READ
           DISPLAY "R2=[" BG-REC "]"
           CLOSE F-BIG
      *> 9. §9.1.6's "record type (fixed or variable)": a RECORD IS
      *>    VARYING connector over a relative file created with
      *>    fixed-length records conflicts even though its MAXIMUM
      *>    size agrees — the record type and the minimum do not.
           OPEN INPUT F-VAR
           DISPLAY "S9-VARYING=" VR-ST
      *> 10. PART B. Create the SEQUENTIAL subject the same way: two
      *>     fixed 20-byte records, a 40-byte plain byte stream with
      *>     no length information of COBOL.NET's on it at all.
           OPEN OUTPUT G-MAKE
           MOVE "ABCDEFGHIJKLMNOPQRST" TO GM-REC
           WRITE GM-REC
           MOVE "UVWXYZ0123456789----" TO GM-REC
           WRITE GM-REC
           CLOSE G-MAKE
           DISPLAY "S10-MAKE-SEQ=" GM-ST
      *> 11. THE DETERMINATION. A 30-byte record description over a
      *>     sequential file created with 20-byte records is NOT a
      *>     file attribute conflict: '00'. §9.1.7.2 makes the record
      *>     length a property of the reading program here, and the
      *>     standard's answer to the disagreement is step 12's '04',
      *>     which a '39' at this step would make unreachable.
           OPEN INPUT G-WIDE
           DISPLAY "S11-SEQ-WIDER=" GW-ST
      *> 12. §9.1.13.2 item 3. The file holds 40 bytes; read through a
      *>     30-byte description that is one full 30-byte record
      *>     ('00', the length equals the maximum) and then a 10-byte
      *>     remainder, which is SHORTER than the minimum — '04', a
      *>     SUCCESSFUL completion, the record still delivered and
      *>     right-space-filled into the 30-byte record area.
           READ G-WIDE
               AT END DISPLAY "S12A-UNEXPECTED-AT-END"
           END-READ
           DISPLAY "S12A=" GW-ST " [" GW-REC "]"
           READ G-WIDE
               AT END DISPLAY "S12B-UNEXPECTED-AT-END"
           END-READ
           DISPLAY "S12B=" GW-ST " [" GW-REC "]"
           READ G-WIDE
               AT END DISPLAY "S12C=EOF"
           END-READ
           CLOSE G-WIDE
      *> 13. The direction the FORMAT records: a RELATIVE description
      *>     over a plain byte stream finds no store header, so it
      *>     cannot interpret the file at all — '39'.
           OPEN INPUT G-REL
           DISPLAY "S13-SEQ-AS-RELATIVE=" GR-ST
      *> 14. PART C. A RECORD VARYING description over PART B's plain
      *>     40-byte file. Its first four bytes are the DATA "ABCD",
      *>     which as a little-endian length prefix name 1,145,258,561
      *>     bytes the 40-byte file does not hold — so the file is not
      *>     framed, the connector's declared VARIABLE record type
      *>     contradicts the medium, and GR10's comparison fails: '39'
      *>     (§9.1.13.6 item 7).
           OPEN INPUT G-VARY
           DISPLAY "S14-VARY-FOREIGN=" GV-ST
      *> 15. The other direction, without which the check could pass by
      *>     refusing every varying file: a varying file COBOL.NET
      *>     itself wrote parses, so the OPEN succeeds and the record
      *>     comes back at the length it was written (§13.18.43 GR13,
      *>     reported by GR15 in the DEPENDING ON item).
           OPEN OUTPUT H-VARY
           MOVE 5 TO HV-LEN
           MOVE "HELLO" TO HV-REC
           WRITE HV-REC
           CLOSE H-VARY
           DISPLAY "S15-VARY-MAKE=" HV-ST
           OPEN INPUT H-VARY
           DISPLAY "S15-VARY-OPEN=" HV-ST
           READ H-VARY
               AT END DISPLAY "S15-UNEXPECTED-AT-END"
           END-READ
           DISPLAY "S15-VARY-READ=" HV-ST " LEN=" HV-LEN
           CLOSE H-VARY
      *> 16. THE BOUNDARY OF THE RULE. The same file through a
      *>     description whose MINIMUM is 30: the prefix still PARSES
      *>     (it names 5 bytes and the file holds them), so there is no
      *>     file attribute conflict and the OPEN succeeds. The
      *>     disagreement is the READ's, and the standard answers it
      *>     with §9.1.13.2 item 3's '04' — a SUCCESSFUL completion
      *>     that a '39' at this OPEN would have made unreachable.
           OPEN INPUT J-VARY
           DISPLAY "S16-VARY-BOUNDS-OPEN=" JV-ST
           READ J-VARY
               AT END DISPLAY "S16-UNEXPECTED-AT-END"
           END-READ
           DISPLAY "S16-VARY-BOUNDS-READ=" JV-ST
           CLOSE J-VARY
           STOP RUN.
