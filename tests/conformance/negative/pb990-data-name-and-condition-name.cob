      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB990 - ISO 8.3.2.2 (cite.py --check 8.3.2.2 "Within a source element, a given user-defined word may
      *> be used as only one type of user-defined word"): ZQ is a data-name (the 01) AND a condition-name (the 88
      *> under another item). condition-name is not in exception 3's group, so one word cannot be both.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB990D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ZQ PIC X VALUE "A".
       01  FLAG PIC X VALUE "N".
           88  ZQ VALUE "Y".
       PROCEDURE DIVISION.
           STOP RUN.
