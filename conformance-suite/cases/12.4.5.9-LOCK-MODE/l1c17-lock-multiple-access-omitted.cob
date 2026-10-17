      *>--- conformance-case
      *> id: l1c17-lock-multiple-access-omitted
      *> rules: [SR-12.4.5.9.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.4.5.9.3 SR2 — MULTIPLE vs ACCESS omitted
      *> "The MULTIPLE phrase shall not be specified for a file
      *>  described with sequential organization or sequential access
      *>  mode."
      *> cite.py --check 12.4.5.9.3 "The MULTIPLE phrase shall not be
      *>   specified for a file described with sequential organization
      *>   or sequential access mode." -> OK §12.4.5.9.3 2)
      *> cite.py --check 12.4.5.5.3 "If the ACCESS MODE clause is not
      *>   specified, sequential access is assumed."
      *>   -> OK §12.4.5.5.3 1)
      *> No ACCESS MODE clause is written, so the access mode is
      *> sequential by default and SR2 applies.
      *> The file is INDEXED (not sequential organization), so the
      *> only reason for rejection is the implied sequential access
      *> mode.
      *> Everything else is legal: SHARING WITH ALL OTHER with a LOCK
      *> MODE clause present (§14.9.27.3 SR8 satisfied).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C17E.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "L1C17E.DAT"
               ORGANIZATION IS INDEXED
               RECORD KEY IS R-KEY
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R.
          05 R-KEY  PIC X(4).
          05 R-DATA PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
