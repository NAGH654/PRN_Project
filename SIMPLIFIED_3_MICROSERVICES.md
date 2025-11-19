# 🎯 Simplified 3-Microservice Architecture (Beginner Friendly)

## Overview

Instead of 6 complex microservices, we'll create **3 simple, logical microservices** that are easier to learn and manage.

## Architecture Diagram

```
                    ┌─────────────────────────────────┐
                    │     SQL Server (Shared DB)      │
                    │                                 │
                    │  [Identity]  [Core]  [Storage] │
                    └────────┬──────┬──────┬──────────┘
                             │      │      │
        ┌────────────────────┼──────┼──────┼────────────────────┐
        │                    │      │      │                    │
   ┌────▼────┐        ┌──────▼──────▼──┐  │    ┌───────────────▼──┐
   │ Service │        │   Service 2    │  │    │    Service 3     │
   │    1    │        │                │  │    │                  │
   │Identity │        │  Core/Exam     │  │    │  File/Storage    │
   │         │        │                │  │    │                  │
   │ Port    │        │   Port 5002    │  │    │    Port 5003     │
   │ 5001    │        │                │  │    │                  │
   └─────────┘        └────────────────┘  │    └──────────────────┘
                                          │
```

## 3 Microservices Breakdown

### 🔐 Service 1: Identity Service (DONE ✅)
**Port:** 5001  
**Database Schema:** `[Identity]`  
**Responsibility:** Authentication & User Management

**What it does:**
- User registration
- Login & JWT tokens
- Refresh tokens
- User profile management

**Entities:**
- Users

**APIs:**
```
POST   /api/auth/register
POST   /api/auth/login
POST   /api/auth/refresh
GET    /api/auth/users/{id}
```

---

### 📚 Service 2: Core/Exam Service (NEW)
**Port:** 5002  
**Database Schema:** `[Core]`  
**Responsibility:** Exam Management & Grading

**What it does:**
- Manage subjects, semesters, exams
- Manage rubrics (grading criteria)
- Grade submissions
- Assign examiners
- View grades and reports

**Entities:**
- Subjects
- Semesters
- Exams
- Rubrics
- ExamSessions
- Grades
- ExaminerAssignments
- AuditLogs

**APIs:**
```
# Subjects
GET    /api/subjects
POST   /api/subjects
PUT    /api/subjects/{id}
DELETE /api/subjects/{id}

# Exams
GET    /api/exams
POST   /api/exams
GET    /api/exams/{id}
PUT    /api/exams/{id}

# Grades
GET    /api/grades
POST   /api/grades
GET    /api/grades/{submissionId}

# Reports
GET    /api/reports/dashboard
GET    /api/reports/exam/{examId}
```

**Why combine these?**
- Exams and Grades are tightly coupled
- Reports need data from both
- Reduces inter-service communication
- Simpler for beginners

---

### 📁 Service 3: File/Storage Service (NEW)
**Port:** 5003  
**Database Schema:** `[Storage]`  
**Responsibility:** File Upload & Processing

**What it does:**
- Upload submission files (RAR/ZIP)
- Extract and process files with 7-Zip
- Detect violations (copied code, missing files)
- Extract images from submissions
- Store file metadata

**Entities:**
- Submissions
- SubmissionImages
- Violations

**APIs:**
```
# Submissions
POST   /api/submissions/upload
GET    /api/submissions
GET    /api/submissions/{id}
GET    /api/submissions/session/{sessionId}
DELETE /api/submissions/{id}

# Files
GET    /api/files/{submissionId}/images
GET    /api/files/{submissionId}/violations
GET    /api/files/download/{submissionId}
```

**Why separate storage?**
- File processing is resource-intensive
- Can scale independently (large uploads)
- Isolates 7-Zip processing
- Clear separation of concerns

---

## Comparison: 6 vs 3 Microservices

| Aspect | 6 Microservices | 3 Microservices (Simplified) |
|--------|-----------------|------------------------------|
| **Complexity** | High | Low ✅ |
| **Learning Curve** | Steep | Gentle ✅ |
| **Inter-service calls** | Many | Few ✅ |
| **Services to manage** | 6 | 3 ✅ |
| **Deployment complexity** | High | Medium ✅ |
| **Good for beginners** | ❌ | ✅ |
| **Real microservices** | ✅ | ✅ |

## Database Schema Strategy

### Shared Database with 3 Schemas

```sql
-- Identity Service
[Identity].[Users]

-- Core/Exam Service  
[Core].[Subjects]
[Core].[Semesters]
[Core].[Exams]
[Core].[Rubrics]
[Core].[ExamSessions]
[Core].[Grades]
[Core].[ExaminerAssignments]
[Core].[AuditLogs]

-- File/Storage Service
[Storage].[Submissions]
[Storage].[SubmissionImages]
[Storage].[Violations]
```

## Communication Between Services

### Service Dependencies

```
┌─────────────────┐
│ Identity Service│
└────────┬────────┘
         │ provides JWT tokens
         ▼
┌─────────────────┐      ┌──────────────────┐
│  Core Service   │◄────►│ Storage Service  │
└─────────────────┘      └──────────────────┘
   Needs user ID          Needs exam/session ID
   from token             from Core Service
```

### Example Flow: Student Submits Assignment

1. **Student logs in** → Identity Service (5001)
   - Returns JWT token with userId

2. **Student uploads file** → Storage Service (5003)
   - Token validated (userId extracted)
   - File saved and processed
   - Submission record created in `[Storage].[Submissions]`

3. **Examiner views submissions** → Storage Service (5003)
   - Query by sessionId
   - Returns list of submissions

4. **Examiner grades submission** → Core Service (5002)
   - Creates grade in `[Core].[Grades]`
   - Links to submissionId from Storage Service

## 3-Layer Architecture (Applied to All Services)

Each microservice follows this structure:

```
ServiceName/
├── Controllers/          # Presentation Layer
│   └── *Controller.cs   # API endpoints, HTTP handling
│
├── Services/            # Business Logic Layer
│   ├── I*Service.cs    # Service interfaces
│   └── *Service.cs     # Business logic implementation
│
├── Repositories/        # Data Access Layer
│   ├── I*Repository.cs # Repository interfaces
│   └── *Repository.cs  # Database operations
│
├── Entities/           # Domain Models
│   └── *.cs           # Entity classes
│
├── Data/              # Database Context
│   └── *DbContext.cs  # EF Core context
│
└── Program.cs         # Service configuration
```

### Example: Core Service Structure

```
CoreService/
├── Controllers/
│   ├── SubjectsController.cs
│   ├── ExamsController.cs
│   ├── GradesController.cs
│   └── ReportsController.cs
│
├── Services/
│   ├── IExamService.cs
│   ├── ExamService.cs
│   ├── IGradeService.cs
│   └── GradeService.cs
│
├── Repositories/
│   ├── IExamRepository.cs
│   ├── ExamRepository.cs
│   ├── IGradeRepository.cs
│   └── GradeRepository.cs
│
├── Entities/
│   ├── Exam.cs
│   ├── Subject.cs
│   ├── Grade.cs
│   └── Rubric.cs
│
└── Data/
    └── CoreDbContext.cs  # Uses [Core] schema
```

## Docker Compose Configuration

```yaml
services:
  # SQL Server (Shared)
  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    ports:
      - "1433:1433"

  # Service 1: Identity
  identity-service:
    build: ./Microservices/IdentityService
    ports:
      - "5001:80"
    depends_on:
      - db

  # Service 2: Core/Exam
  core-service:
    build: ./Microservices/CoreService
    ports:
      - "5002:80"
    depends_on:
      - db
      - identity-service

  # Service 3: File/Storage
  storage-service:
    build: ./Microservices/StorageService
    ports:
      - "5003:80"
    depends_on:
      - db
      - identity-service
```

## Migration Plan

### ✅ Phase 1: Identity Service (DONE)
- [x] Created 3-layer architecture
- [x] Repository layer (IUserRepository)
- [x] Service layer (IAuthService)
- [x] Controller uses service
- [x] Builds successfully

### 📋 Phase 2: Core Service (Week 1-2)
1. Create CoreService project
2. Copy entities: Subject, Semester, Exam, Rubric, Grade, etc.
3. Create CoreDbContext with `[Core]` schema
4. Implement repositories for each entity
5. Implement services (ExamService, GradeService)
6. Create controllers (SubjectsController, ExamsController, GradesController)
7. Add to docker-compose

### 📋 Phase 3: Storage Service (Week 3)
1. Create StorageService project
2. Copy entities: Submission, SubmissionImage, Violation
3. Create StorageDbContext with `[Storage]` schema
4. Implement SubmissionRepository
5. Implement SubmissionService (with 7-Zip processing)
6. Create SubmissionsController, FilesController
7. Add file upload handling
8. Add to docker-compose

### 📋 Phase 4: Integration & Testing (Week 4)
1. Test each service independently
2. Test JWT tokens work across services
3. Test file upload → submission → grading flow
4. Add logging and error handling
5. Write integration tests

## Benefits of 3-Microservice Approach

### For Learning
✅ **Manageable Complexity** - 3 services instead of 6  
✅ **Clear Boundaries** - Identity, Business Logic, Storage  
✅ **Less Communication** - Fewer HTTP calls between services  
✅ **Faster Development** - Less code to write  
✅ **Easier Debugging** - Fewer moving parts  

### For Your Project
✅ **Demonstrates Microservices** - Shows you understand the concept  
✅ **Scalable** - Can scale storage service for large uploads  
✅ **Maintainable** - Each service has clear responsibility  
✅ **Production-Ready** - Used by real companies  

## Next Steps

1. **Review this document** - Make sure you understand the 3 services
2. **Test Identity Service** - Ensure it works with 3-layer architecture
3. **Start Core Service** - Follow same pattern as Identity Service
4. **Read CORE_SERVICE_GUIDE.md** - Step-by-step instructions (coming next)

## Questions to Consider

Before starting, think about:

1. **Do you understand 3-layer architecture?**
   - Controller → Service → Repository → Database

2. **Do you understand each service's responsibility?**
   - Identity: Users & Auth
   - Core: Exams & Grades
   - Storage: Files & Processing

3. **Are you comfortable with:**
   - Dependency Injection (IService, IRepository)
   - Async/await
   - Entity Framework
   - Docker

If yes to all → You're ready to continue!  
If no → Review Identity Service code first, it's your template!

---

**Remember:** This is a LEARNING project. 3 microservices is perfect for understanding the concepts without overwhelming complexity. You can always split further later if needed!
