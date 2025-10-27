## Detailed Use Case Specifications

I'll break down each use case with concrete, step-by-step flows that your team can implement. Each use case includes specific user interactions, system responses, and business rules to make them actionable rather than abstract.

### **Use Case 1: Exam Setup & Management**
**Goal:** Create and configure programming exams with rubrics and examiner assignments.

**Primary Actor:** Manager (Admin can also perform this)

**Secondary Actors:** System (validation), Examiners (assigned)

**Preconditions:**
- User is logged in with Manager/Admin role
- At least one Subject and Semester exist in the system

**Main Success Scenario:**
1. Manager navigates to "Create Exam" page in web dashboard
2. System displays form with dropdowns for Subject and Semester selection
3. Manager enters exam details: name, date, duration (e.g., "PRN232 Final Exam", 2024-12-15, 120 minutes)
4. Manager sets total marks (e.g., 100 points)
5. Manager adds rubric criteria:
   - Clicks "Add Rubric Item"
   - Enters "Code Quality" with 30 points max
   - Adds description: "Evaluate code structure, comments, and best practices"
   - Repeats for additional criteria (e.g., "Functionality" 40 points, "Documentation" 30 points)
6. Manager saves exam draft
7. System validates exam date falls within selected semester dates
8. System creates Exam record and associated Rubric records
9. Manager assigns examiners:
   - Searches for available examiners by name/email
   - Selects 2-3 examiners from list
   - System creates ExaminerAssignment records
10. System sends email notifications to assigned examiners
11. Manager publishes exam (changes status to "Active")

**Alternative Flows:**
- **Invalid Date:** If exam date outside semester, show error "Exam date must be within semester period"
- **Duplicate Assignment:** If examiner already assigned to exam, show warning "Examiner already assigned"
- **No Rubrics:** Cannot publish exam without at least one rubric item

**Postconditions:**
- Exam exists with status "Active"
- Rubrics are defined and linked to exam
- Examiners are assigned and notified
- Exam appears in submission upload interfaces

**Business Rules:**
- Exam duration must be between 30-300 minutes
- Total rubric points must equal exam's total marks
- Maximum 5 examiners per exam
- Cannot modify exam after first submission is uploaded

### **Use Case 2: Submission Upload & Processing**
**Goal:** Extract and validate student submissions from RAR files, detect violations.

**Primary Actor:** System (automated) or Examiner (manual upload)

**Secondary Actors:** File processing service, Violation detection service

**Preconditions:**
- Exam session is active (current time within session start/end)
- RAR file contains student submissions
- File size < 500MB

**Main Success Scenario:**
1. Examiner uploads RAR file via WPF client or web interface
2. System validates file format (.rar extension)
3. System extracts RAR contents to temporary directory
4. For each extracted file:
   - Parses filename for student ID (e.g., "SE123456_Exam1.docx")
   - Validates naming convention against pattern "StudentID_ExamName.ext"
   - Calculates file hash for duplicate detection
   - If Word document: extracts embedded images to separate folder
5. System checks for duplicate content:
   - Compares file hashes within same exam session
   - Flags submissions with identical hashes as duplicates
6. System scans for content violations:
   - Searches code files for prohibited patterns (e.g., "System.out.println" in restricted areas)
   - Checks Word documents for copied content using similarity algorithms
7. System creates Submission records with status "Processing"
8. System creates Violation records for detected issues
9. System updates submission status to "Pending" (ready for grading)
10. System sends SignalR notification to relevant managers/examiners

**Alternative Flows:**
- **Corrupt RAR:** Show error "File is corrupted or password protected"
- **Invalid Naming:** Create violation record with type "Naming"
- **Large File:** If >100MB, process asynchronously and show progress bar
- **No Images:** Skip image extraction step

**Postconditions:**
- All submissions extracted and stored
- Violations detected and recorded
- Images extracted and linked to submissions
- Notifications sent to stakeholders

**Business Rules:**
- Student ID must match pattern: 2 letters + 6 digits (e.g., SE123456)
- Maximum 1000 submissions per RAR file
- Duplicate detection only within same exam session
- Images stored as separate files with naming convention "submissionId_imageIndex.ext"

### **Use Case 3: Grading Workflow**
**Goal:** Examiners grade submissions using defined rubrics with support for double grading.

**Primary Actor:** Examiner

**Secondary Actors:** Moderator (for verification), System (validation)

**Preconditions:**
- Examiner is assigned to the exam
- Submission status is "Pending" or "Processing"
- Rubrics are defined for the exam

**Main Success Scenario:**
1. Examiner logs into web dashboard
2. System displays assigned exams with submission counts
3. Examiner selects exam and views submission list
4. Examiner clicks on specific submission to grade
5. System displays submission details and file download link
6. For each rubric item:
   - Examiner enters points (0 to MaxPoints)
   - Adds optional comments (e.g., "Good code structure but missing comments")
7. Examiner saves grade (creates Grade records)
8. System validates total points don't exceed rubric maximums
9. If this is the second examiner grading the same submission:
   - System calculates average score
   - Flags for moderator review if difference >20%
10. System updates submission status to "Graded"
11. System sends real-time notification to other examiners/managers

**Alternative Flows:**
- **Zero Score Due to Violations:** If submission has violations, examiner can mark as zero and add justification
- **Disagreement:** If two examiners' scores differ significantly, moderator is automatically assigned for review
- **Incomplete Rubric:** Cannot submit grade until all rubric items are scored

**Postconditions:**
- Grade records exist for each rubric item per examiner
- Submission status updated
- Notifications sent for review if needed

**Business Rules:**
- Each examiner can only grade assigned submissions
- Double grading required for submissions with violations
- Moderator must verify zero-score submissions before finalization
- Grades are editable until moderator approval

### **Use Case 4: Dashboard & Reporting**
**Goal:** Provide OData-powered dashboards for querying exam data and exporting reports.

**Primary Actor:** Admin/Manager/Moderator

**Secondary Actors:** System (OData processing), Excel export service

**Preconditions:**
- User has appropriate role permissions
- Exam data exists in system

**Main Success Scenario:**
1. User accesses dashboard with OData endpoint
2. User applies filters: `$filter=ExamDate gt 2024-01-01 and Status eq 'Graded'`
3. User requests aggregation: `$apply=groupby((SubjectName), aggregate(Score with average as AvgScore))`
4. System processes OData query and returns filtered data
5. User selects "Export to Excel" option
6. System generates Excel file with:
   - Student names and IDs
   - Individual rubric scores
   - Final grades
   - Violation flags
7. System applies formatting (headers, conditional coloring for violations)
8. User downloads Excel file

**Alternative Flows:**
- **Large Dataset:** If >10,000 records, system paginates with `$top` and `$skip`
- **Complex Query:** System validates OData syntax and returns error for invalid queries
- **Permission Denied:** If user lacks role access, return 403 Forbidden

**Postconditions:**
- Excel file downloaded with complete grading data
- Audit log records export action

**Business Rules:**
- OData supports filtering, sorting, paging, and aggregation
- Excel export includes all rubric details and violation information
- Export limited to 50,000 records per request

### **Use Case 5: Real-time Notifications**
**Goal:** Provide live updates when submissions are uploaded, graded, or flagged.

**Primary Actor:** System (event-driven)

**Secondary Actors:** All users with active SignalR connections

**Preconditions:**
- User is logged into web dashboard
- SignalR connection established

**Main Success Scenario:**
1. Submission uploaded successfully
2. System broadcasts "SubmissionUploaded" event with submission count
3. Manager's dashboard updates submission counter in real-time
4. Examiner grades a submission
5. System broadcasts "SubmissionGraded" event with exam ID and examiner name
6. Other examiners see updated progress bars
7. Violation detected during processing
8. System broadcasts "ViolationDetected" event with severity level
9. Moderators receive notification for review
10. User acknowledges notification (dismisses or takes action)

**Alternative Flows:**
- **Connection Lost:** System queues notifications and sends when reconnected
- **High Volume:** System throttles notifications to prevent spam (max 1 per minute per user)
- **Filtered Notifications:** Users can subscribe to specific exam notifications only

**Postconditions:**
- Users receive timely updates without page refresh
- Notification history maintained for 30 days

**Business Rules:**
- Notifications based on user role and exam assignments
- Real-time updates for critical events (violations, grading completion)
- Connection maintained via SignalR persistent connection

### **Use Case 6: Security & Access Control**
**Goal:** Authenticate users and enforce role-based permissions across all operations.

**Primary Actor:** Any user

**Secondary Actors:** JWT service, API Gateway

**Preconditions:**
- User has valid credentials
- System is configured with JWT settings

**Main Success Scenario:**
1. User enters username/password on login page
2. System validates credentials against Users table
3. System generates JWT token with user ID, role, and expiration
4. User includes token in API requests
5. API Gateway validates token and extracts user context
6. For each API call:
   - System checks role permissions (e.g., only Manager can assign examiners)
   - System logs access in AuditLogs table
7. User performs authorized actions
8. System refreshes token before expiration

**Alternative Flows:**
- **Invalid Credentials:** Return 401 Unauthorized with "Invalid username or password"
- **Expired Token:** Return 401 with "Token expired" and refresh token option
- **Insufficient Permissions:** Return 403 Forbidden with specific permission required

**Postconditions:**
- User authenticated and authorized for requested operations
- All actions logged for audit purposes

**Business Rules:**
- JWT expires after 8 hours
- Passwords hashed with bcrypt
- Role hierarchy: Admin > Manager > Moderator > Examiner
- All API endpoints require authentication except login/register

### **Use Case 7: WPF Client Upload (Optional)**
**Goal:** Provide desktop tool for examiners to upload and validate submissions locally.

**Primary Actor:** Examiner

**Secondary Actors:** Local file system, Cloud API

**Preconditions:**
- WPF application installed on examiner's machine
- Internet connection available
- User logged in with valid credentials

**Main Success Scenario:**
1. Examiner launches WPF application
2. Application prompts for login (username/password)
3. System validates credentials via API call
4. Examiner selects active exam session from dropdown
5. Examiner browses and selects RAR file from local drive
6. Application validates file (size, format, not password protected)
7. Application shows preview of extracted contents
8. Examiner clicks "Upload" button
9. Application streams file to cloud API with progress indicator
10. Cloud processes file and returns success confirmation
11. Application shows completion message with submission count

**Alternative Flows:**
- **Network Error:** Application queues upload and retries automatically
- **Large File:** Shows progress bar with estimated time
- **Validation Failure:** Highlights specific files with naming violations before upload

**Postconditions:**
- Submissions uploaded to cloud system
- Local application logs upload history

**Business Rules:**
- WPF client handles files up to 2GB
- Automatic retry on network failures (up to 3 attempts)
- Local validation matches cloud validation rules

These detailed use cases provide concrete implementation steps that your team can follow. Each includes specific UI interactions, validation rules, and error handling to guide development.
