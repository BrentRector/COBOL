*>--- conformance-case
*> id: pb692-access-random-line-sequential
*> rules: [SR-12.4.5.5.2-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.5.2 syntax rule 2 on the OTHER sequential organization. That a LINE
*> SEQUENTIAL file IS "a sequential file" in SR2's sense is DERIVED, not assumed: 12.4.5.10.3
*> general rule 2 puts the LINE SEQUENTIAL phrase in the ORGANIZATION clause, that clause is
*> written only in the Format 3 file control entry, and 12.4.5.2 syntax rule 11 says "Format 3
*> shall be specified only for a sequential file or a report file" -- so RANDOM is closed out
*> of it exactly as it is for record sequential (and Format 3's own general format admits only
*> [ ACCESS MODE IS SEQUENTIAL ]).
*> reject-at names 2023 ALONE. ORGANIZATION IS LINE SEQUENTIAL is a COBOL-2023 introduction
*> (the Foreword's list of the main changes over ISO/IEC 1989:2014; constructs.json row
*> file-organization-line-sequential-2023), so at 85, 2002 and 2014 this source is rejected by
*> the edition gate on the ORGANIZATION clause (COBOLNET0900, kb/Work PB688) and 12.4.5.5.2
*> syntax rule 2 never gets to speak. 2023 is the only edition at which the organization exists,
*> so it is the only edition at which this rule is the operative one. COBOLNET1858 (kb/Work PB692).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB692N4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb692n4.dat"
               ORGANIZATION IS LINE SEQUENTIAL
               ACCESS MODE IS RANDOM.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  F-REC PIC X(6).
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT F.
           CLOSE F.
           STOP RUN.
