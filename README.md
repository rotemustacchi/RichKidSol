# RichKid User Management System

A full-stack .NET application for user management with JWT-based authentication and role-based access control, featuring a RESTful Web API backend, MVC frontend, and shared library architecture with comprehensive logging.

## 🏗️ Architecture

The solution consists of three main projects:

- **RichKid.API** - RESTful Web API backend with JWT authentication and detailed logging
- **RichKid.Web** - MVC frontend application with enhanced error handling and request tracking
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
- **Enhanced user-friendly validation** with clear English error messages
- **Smart duplicate detection** - prevents username conflicts with helpful messages
- **Simple password requirements** (minimum 4 characters)

### Security Features
- **JWT token-based authentication** with role claims
- **Cookie authentication** for web sessions
- **Enhanced error handling** with user-friendly messages throughout the application
- **Session management** with automatic timeout
- **CORS protection** configured for web domain
- **Robust input validation** with specific error feedback

### Technical Improvements
- **Configuration-based API endpoints** - All API URLs stored in `appsettings.json` for easy maintenance
- **Improved response validation** - Comprehensive HTTP status code handling
- **Better exception management** - User-friendly error messages instead of technical jargon
- **Comprehensive ASP.NET Core logging** - Detailed monitoring and debugging capabilities

## 📊 Logging & Monitoring

### Comprehensive Logging System
The application features a robust logging system that provides complete visibility into all operations:

#### **Startup & Configuration Logging**
- Service initialization tracking
- Configuration validation
- Environment-specific setup
- Database file path verification

#### **Authentication Flow Logging**
- Login attempts with usernames and IP addresses
- JWT token creation and validation events
- Session management activities
- Permission checks and authorization decisions

#### **User Operation Logging**
- All CRUD operations with user context
- Username conflict detection
- Data validation results
- Permission enforcement tracking

#### **HTTP Request Tracking**
- Complete request/response lifecycle
- Request timing and performance metrics
- User context for each operation
- Error response details

#### **Data Layer Logging**
- File I/O operations
- JSON serialization/deserialization
- Data integrity checks
- User statistics and analytics

### **Log Levels & Categories**
- **Debug** (Technical Details): Service initialization, variable values, detailed flow
- **Information** (Normal Operations): User actions, successful operations, system events
- **Warning** (Attention Required): Failed attempts, validation errors, retries
- **Error** (Issues): Exceptions, system errors, critical failures

### **Log Output Examples**
```
[2025-07-12 16:12:15] RichKid API starting up in Development mode
[2025-07-12 16:12:15] JWT Issuer configured as: RichKidAPI
[2025-07-12 16:12:15] Login attempt started for username: Rotem from IP: ::1
[2025-07-12 16:12:15] Authentication successful for user: Rotem
[2025-07-12 16:12:15] User created successfully: NewUser (ID: 7) by User ID: 5
[2025-07-12 16:12:15] HTTP GET /User completed with 200 in 145ms for User ID: 5
```

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

4. **Configure API endpoints (Optional)**
   
   You can customize API endpoints in `RichKid.Web/appsettings.json`:
   ```json
   {
     "ApiSettings": {
       "BaseUrl": "http://localhost:5270/api",
       "Endpoints": {
         "Auth": { "Login": "/auth/login" },
         "Users": {
           "Base": "/users",
           "GetById": "/users/{0}",
           "Search": "/users/search"
         }
       }
     }
   }
   ```

5. **Run the applications**

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

6. **Access the application**
   - Open your browser and navigate to `https://localhost:7143`
   - Use the test credentials below
   - Monitor console outputs for detailed logging information

## 🔐 Test Users

The system comes with pre-configured test users in `Users.json`:

| Username | Password | Group | Role | Status |
|----------|----------|-------|------|--------|
| Rotem | 1234 | 1 | Admin | Active ✅ |
| DanielaDanon | ab!44 | 2 | Editor | Active ✅ |
| Alon | 1111 | 3 | Regular User | Active ✅ |
| Tuval | 1234 | 3 | Regular User | Active ✅ |
| CharliBrown | 33333333 | 4 | View Only | Inactive ❌ |

### User-Friendly Error Messages:
- **Invalid username**: "Username not found"
- **Wrong password**: "Incorrect password"
- **Inactive account**: "Account is inactive. Please contact an administrator"
- **Username conflict**: "This username is already taken. Please choose a different username."
- **Session expired**: "Your session has expired. Please log in again to continue."
- **Permission denied**: "You don't have permission to perform this action. Please contact your administrator if you think this is an error."

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

## 🔧 Configuration

### API Settings
The web application uses configurable endpoints defined in `appsettings.json`. This allows for:
- Easy environment switching (development, staging, production)
- Flexible API endpoint management
- Centralized configuration maintenance

### Logging Configuration
The logging system is highly configurable through `Program.cs`:
- **Console Logging**: Timestamped output with scope information
- **Debug Logging**: Visual Studio integration for development
- **Filtered Logging**: Reduced noise from framework components
- **Structured Logging**: Searchable parameters and context

### Error Handling
The application features comprehensive error handling with:
- User-friendly messages for common scenarios
- Detailed logging for debugging and monitoring
- Graceful degradation when services are unavailable
- Proper HTTP status code handling

## 🔍 API Documentation

When running in development mode, Swagger UI is available at:
`http://localhost:5270/swagger`

The Swagger UI includes JWT Bearer token authentication support.

## 🐛 Troubleshooting & Monitoring

### Debug Information
The application provides detailed console logging for debugging and monitoring:

#### **Real-time Monitoring**
- Authentication flow tracking with success/failure reasons
- API request/response logging with timing information
- User action validation and permission checks
- Data operation success/failure tracking

#### **Performance Metrics**
- HTTP request duration timing
- Database operation performance
- Service method execution time
- Resource usage patterns

#### **Security Auditing**
- Login attempts and outcomes
- Permission violations and access attempts
- Session management events
- JWT token validation results

#### **Data Integrity**
- File I/O operation success/failure
- JSON parsing validation
- User data validation results
- Conflict detection and resolution

### **Log Monitoring Tips**
1. **Watch console outputs** in both API and Web terminals
2. **Filter logs by level** (Debug, Info, Warning, Error)
3. **Track user operations** by User ID in log messages
4. **Monitor timing** for performance optimization
5. **Check error patterns** for system health

### **Common Troubleshooting Scenarios**
- **Authentication Issues**: Check JWT configuration and user credentials in logs
- **Permission Errors**: Review authorization logs for role assignments
- **API Communication**: Monitor HTTP request/response cycles
- **Data Problems**: Examine file I/O and JSON parsing logs
- **Performance Issues**: Analyze request timing and operation duration

---

For questions or support, please check the code documentation, review the detailed console logs, or create an issue in the repository.