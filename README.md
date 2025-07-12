# RichKid User Management System

A full-stack .NET application for user management with JWT-based authentication and role-based access control, featuring a RESTful Web API backend, MVC frontend, and shared library architecture.

## 🏗️ Architecture

The solution consists of three main projects:

- **RichKid.API** - RESTful Web API backend with JWT authentication
- **RichKid.Web** - MVC frontend application
- **RichKid.Shared** - Shared library containing models, DTOs, and service interfaces

## ✨ Features

### Authentication & Authorization
- **JWT-based authentication** with detailed error messages
- **Policy-based authorization** using JWT claims
- **Role-based access control** with 4 user groups:
  - **מנהל (Admin)** - Group 1: Full system access (Create, Edit, Delete, View)
  - **עורך (Editor)** - Group 2: Can create and edit users (Create, Edit, View)
  - **משתמש רגיל (Regular User)** - Group 3: Can edit own profile (Edit Own, View)
  - **צפייה בלבד (View Only)** - Group 4: Read-only access (View Only)

### User Management
- Create, read, update, delete users
- User search and filtering by name, email, or phone
- Active/inactive status management
- Comprehensive user data including contact information
- **User-friendly validation** with English error messages
- **Simple password requirements** (minimum 4 characters)

### Security Features
- **JWT token-based authentication** with role claims
- **Cookie authentication** for web sessions
- **Detailed error handling** with specific login failure messages
- **Session management** with automatic timeout
- **CORS protection** configured for web domain
- **Input validation** with clean error messages

## 🚀 Getting Started

### Prerequisites
- .NET 9.0 or later
- Visual Studio 2022 or VS Code
- Git

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd RichKidSol
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the solution**
   ```bash
   dotnet build
   ```

4. **Run the applications**

   **Start the API (Terminal 1):**
   ```bash
   cd RichKid.API
   dotnet run
   ```
   API will run on `http://localhost:5270`

   **Start the Web app (Terminal 2):**
   ```bash
   cd RichKid.Web
   dotnet run
   ```
   Web app will run on `https://localhost:7143`

5. **Access the application**
   - Open your browser and navigate to `https://localhost:7143`
   - Use the test credentials below

## 🔐 Test Users

The system comes with pre-configured test users in `Users.json`:

| Username | Password | Group | Role | Status |
|----------|----------|-------|------|--------|
| Rotem | 1234 | 1 | Admin | Active ✅ |
| DanielaDanon | ab!44 | 2 | Editor | Active ✅ |
| Alon | 1111 | 3 | Regular User | Active ✅ |
| Tuval | 1234 | 3 | Regular User | Active ✅ |
| CharliBrown | 33333333 | 4 | View Only | Inactive ❌ |

### Login Error Messages:
- **Invalid username**: "Username not found"
- **Wrong password**: "Incorrect password"
- **Inactive account**: "Account is inactive. Please contact an administrator"

## 🔌 API Endpoints

### Authentication
- `POST /api/auth/login` - User login with detailed error responses

### Users (JWT Protected)
- `GET /api/users` - Get all users (Requires: CanView)
- `GET /api/users/{id}` - Get user by ID (Requires: CanView)
- `GET /api/users/search` - Search users by name (Requires: CanView)
- `POST /api/users` - Create new user (Requires: CanCreate)
- `PUT /api/users/{id}` - Update user (Requires: CanEdit or self-edit)
- `DELETE /api/users/{id}` - Delete user (Requires: CanDelete)

## 🛡️ Authorization Matrix

| Action | Admin (1) | Editor (2) | Regular (3) | View Only (4) |
|--------|-----------|------------|-------------|---------------|
| View Users | ✅ | ✅ | ✅ | ✅ |
| Create User | ✅ | ✅ | ❌ | ❌ |
| Edit Any User | ✅ | ✅ | ❌ | ❌ |
| Edit Own Profile | ✅ | ✅ | ✅ | ✅ |
| Delete User | ✅ | ❌ | ❌ | ❌ |

## 🔍 API Documentation

When running in development mode, Swagger UI is available at:
`http://localhost:5270/swagger`

The Swagger UI includes JWT Bearer token authentication support.

---

For questions or support, please check the code documentation or create an issue in the repository.