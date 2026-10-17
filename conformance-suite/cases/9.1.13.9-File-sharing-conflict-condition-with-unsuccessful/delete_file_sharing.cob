*>--- conformance-case
*> id: delete_file_sharing
*> rules: [GR-9.1.13.9-2, GR-14.9.10.4-15, DOC-A.1-51, DOC-A.1-166]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DFSHRP10FL.
      *> P10 Step 8: the DELETE FILE file-sharing conflict (ISO 9.1.13.9
      *> item 2 / 14.9.10 GR15): a DELETE FILE attempted while the
      *> physical file is currently open by ANOTHER file connector is
      *> unsuccessful with I-O status 62 and the file is not deleted; a
      *> RETRY phrase re-attempts and EVERY form exhausts to 62 -- 14.7.9.3
      *> GR4a and that clause's closing paragraph both land "the appropriate
      *> value ... according to the rules for 9.1.13", and 9.1.13.9 defines
      *> no deadlock value for a FILE SHARING conflict (52 is 9.1.13.8's
      *> RECORD-conflict value). FOR 0 SECONDS (GR4a's zero screen) and FOR
      *> 30 SECONDS (GR2's maximum-meaningful clamp, which this
      *> implementation defines as 0) must therefore AGREE. After the other
      *> connector closes: delete -> 00; an absent file -> the SUCCESSFUL
      *> 05 (GR14); the deleted file's OPEN INPUT -> 35. The conflict is
      *> defined over "another file connector" plainly, so two ordinary
      *> (non-SHARING) SELECTs bound to one physical file exercise it.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-A ASSIGN TO "dfshare.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS A-ST.
           SELECT F-B ASSIGN TO "dfshare.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS B-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F-A.
       01 A-REC PIC X(5).
       FD F-B.
       01 B-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 A-ST PIC XX.
       01 B-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F-A.
           MOVE "HELLO" TO A-REC. WRITE A-REC.
      *> The physical file is open through F-A: DELETE FILE via F-B -> 62.
           DELETE FILE F-B. DISPLAY "DEL62=" B-ST.
           DELETE FILE F-B RETRY 2 TIMES. DISPLAY "DELRT=" B-ST.
           DELETE FILE F-B RETRY FOREVER. DISPLAY "DELFV=" B-ST.
           DELETE FILE F-B RETRY FOR 0 SECONDS. DISPLAY "DELSC0=" B-ST.
           DELETE FILE F-B RETRY FOR 30 SECONDS. DISPLAY "DELSC30=" B-ST.
      *> F-A closed: the delete succeeds; a second delete of the now
      *> absent file is the SUCCESSFUL 05; OPEN INPUT reports 35.
           CLOSE F-A.
           DELETE FILE F-B. DISPLAY "DEL00=" B-ST.
           DELETE FILE F-B. DISPLAY "DEL05=" B-ST.
           OPEN INPUT F-B. DISPLAY "OPEN35=" B-ST.
           STOP RUN.
