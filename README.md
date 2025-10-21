Part 2 introduces functionality, persistence, and logic, transforming the UI into a working prototype.
Each feature was implemented as a development task tracked in GitHub commits.

 Implemented Features / Tasks
1. Lecturer Claim Submission Form

Task: “Build lecturer claim submission form.”
Fix: Implemented a functional form for lecturers to submit claims. Data is stored in the database using Entity Framework Core.
Includes validation for hours worked, rate, and total auto-calculation.

2. Supporting Document Upload & Encryption

Task: “Enable secure document upload.”
Fix: Added file upload with AES encryption via the new EncryptionService.
Documents are encrypted before saving and decrypted when downloaded for review.

3. UI Design Improvements

Task: “UI design can be improved.”
Fix: Updated Razor Views for cleaner layout, spacing, and accessibility.

Added consistent navigation bar

Improved form alignment

Added confirmation and alert messages

4. Lecturer Claim Tracking Dashboard

Task: “Show submitted claims with status.”
Fix: Added a dynamic “My Claims” page that displays claim history, status (Draft / Submitted / Approved), and uploaded document links.

5. Programme Coordinator / Academic Manager Dashboard

Task: “Add claim approval management.”
Fix: Added dashboards where coordinators can view pending claims, approve or reject them, and add comments.

6. Database Integration (EF Core)

Task: “Connect system to a working database.”
Fix: Linked all models (Lecturer, Claim, Document, Approval) to a real ApplicationDbContext using Entity Framework Core with migrations.

7. Antiforgery & Security Enhancements

Task: “Secure all form submissions.”
Fix: Added antiforgery tokens in all views and controllers to prevent CSRF attacks.
Implemented secure key management via environment variables.

8. Delete & Download Document Functionality

Task: “Allow lecturers to manage their uploaded documents.”
Fix: Added DeleteDocument and DownloadDocument controller actions with proper file validation and JSON responses for feedback.

9. Unit Testing

Task: “Add tests for document and claim workflows.”
Fix: Added xUnit test cases for LecturerController (including file deletion, encryption validation, and mock environment).

⚙️ Technologies Used

ASP.NET Core MVC (.NET 6)

Entity Framework Core (In-Memory + SQLite)

Razor Views

HTML / CSS / Bootstrap

AES Encryption (System.Security.Cryptography)

xUnit Testing

🧠 How It Works (Application Flow)

Lecturer logs in → Submits claim via form.

Uploaded documents are encrypted and stored.

Lecturer can track submitted claims via “My Claims”.

Programme Coordinator reviews claims in dashboard.

Manager approves or rejects with feedback.

All actions logged in database for traceability.
