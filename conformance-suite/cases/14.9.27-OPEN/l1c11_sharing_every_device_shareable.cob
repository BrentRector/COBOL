      *>--- conformance-case
      *> id: l1c11_sharing_every_device_shareable
      *> rules: [GR-14.9.27.4-21, DOC-A.1-52, DOC-A.1-76]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §9.1.15 (A.1 item 76) — which devices allow concurrent
      *>   access: documented choice "all of them" (PRINTER and DISK
      *>   name one shareable host file)
      *> Rule: "A shared physical file shall reside on a device that
      *>   allows concurrent access to the file. The implementor shall
      *>   specify which devices allow concurrent access to a physical
      *>   file."
      *>   cite.py --check 9.1.15 "The implementor shall specify which
      *>   devices allow concurrent access to a physical file"
      *>   -> OK  §9.1.15   (Sharing mode)
      *>   cite.py --check 9.1.15 "The sharing with all other mode
      *>   allows concurrent access to a physical file through other
      *>   file connectors specifying input, I-O, or extend mode"
      *>   -> OK  §9.1.15 3)  (Sharing mode)
      *>   cite.py --check 9.1.15 "Associating this file connector with
      *>   the physical file will be unsuccessful if the physical file
      *>   is currently open through other file connectors"
      *>   -> OK  §9.1.15 1)  (Sharing mode)
      *>   cite.py --check 9.1.13.9 "I-O status = 61"
      *>   -> OK  §9.1.13.9 1)
      *> Documented choice (docs/CONFORMANCE.md DOC-A.1-76): every
      *> physical file allows concurrent access; ASSIGN TO PRINTER "f"
      *> and ASSIGN TO DISK "f" are the same shareable file (DOC-A.1-71
      *> makes the class word select nothing further).
      *> (LOCK MODE accompanies SHARING WITH ALL OTHER, as §14.9.27.3
      *> SR8 requires: cite.py --check 14.9.27.3 "the LOCK MODE clause
      *> shall be specified in the file control entry for file-name-1"
      *> -> OK  §14.9.27.3 8)  (Syntax rules))
      *> Derivation:
      *>   PR-OPEN=00   OPEN OUTPUT of the PRINTER-class connector.
      *>   DK-OPEN=00   OPEN EXTEND of the DISK-class connector while
      *>                the first is open: both ALL OTHER, the device is
      *>                shareable, so rule 3) admits it (a device not
      *>                allowing concurrent access could not be shared).
      *>   EX-OPEN=61   a NO OTHER OPEN while the file is open through
      *>                other connectors is unsuccessful: 61.
      *>   PR-W=00      PRN01 written through the PRINTER connector,
      *>   DK-W=00      then (after PR is closed) DSK01 is appended
      *>                through the DISK connector.
      *>   EX-OPEN=00   the NO OTHER connector alone opens.
      *>   EX-R=PRN01   both records are in ONE physical file, in the
      *>   EX-R=DSK01   order written.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C11L.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-PR ASSIGN TO PRINTER "L1C11L.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS PR-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT F-DK ASSIGN TO DISK "L1C11L.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS DK-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT F-EX ASSIGN TO DISK "L1C11L.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS EX-ST
               SHARING WITH NO OTHER.
       DATA DIVISION.
       FILE SECTION.
       FD F-PR.
       01 PR-REC PIC X(5).
       FD F-DK.
       01 DK-REC PIC X(5).
       FD F-EX.
       01 EX-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 PR-ST PIC XX.
       01 DK-ST PIC XX.
       01 EX-ST PIC XX.
       01 W-EOF PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F-PR.
           DISPLAY "PR-OPEN=" PR-ST.
           OPEN EXTEND F-DK.
           DISPLAY "DK-OPEN=" DK-ST.
           OPEN INPUT F-EX.
           DISPLAY "EX-OPEN=" EX-ST.
           CLOSE F-DK.
           MOVE "PRN01" TO PR-REC.
           WRITE PR-REC.
           DISPLAY "PR-W=" PR-ST.
           CLOSE F-PR.
           OPEN EXTEND F-DK.
           MOVE "DSK01" TO DK-REC.
           WRITE DK-REC.
           DISPLAY "DK-W=" DK-ST.
           CLOSE F-DK.
           OPEN INPUT F-EX.
           DISPLAY "EX-OPEN=" EX-ST.
           MOVE "N" TO W-EOF.
           PERFORM UNTIL W-EOF = "Y"
               READ F-EX
                   AT END MOVE "Y" TO W-EOF
                   NOT AT END DISPLAY "EX-R=" EX-REC
               END-READ
           END-PERFORM.
           CLOSE F-EX.
           STOP RUN.
