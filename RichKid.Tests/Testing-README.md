# RichKid Testing Guide

This document explains how to run and understand the test suite for the RichKid User Management System.

## 🧪 Test Structure

The testing project `RichKid.Tests` contains comprehensive unit and integration tests organized into the following categories:

### **Unit Tests** (`RichKid.Tests.Unit`)
- **UserServiceTests.cs** - Tests business logic for user management operations
- **AuthControllerTests.cs** - Tests JWT authentication and authorization logic
- **DataServiceTests.cs** - Tests file I/O operations and JSON serialization

### **Integration Tests** (`RichKid.Tests.Integration`)
- **ApiIntegrationTests.cs** - End-to-end tests of the complete API workflow

## 🚀 Running the Tests

### **Prerequisites**
- .NET 9.0 SDK installed
- Visual Studio 2022 or VS Code with C# extension
- Both RichKid.API and RichKid.Web projects should build successfully

### **Command Line**
```bash
# Navigate to the test project directory
cd RichKid.Tests

# Restore dependencies
dotnet restore

# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run only unit tests
dotnet test --filter "FullyQualifiedName~Unit"

# Run only integration tests
dotnet test --filter "FullyQualifiedName~Integration"

# Run specific test class
dotnet test --filter "FullyQualifiedName~UserServiceTests"
```

### **Visual Studio**
1. Open the solution in Visual Studio 2022
2. Go to **Test → Test Explorer**
3. Click **Run All Tests** or right-click specific tests to run them individually
4. View results in the Test Explorer window

### **VS Code**
1. Install the ".NET Core Test Explorer" extension
2. Open the solution folder in VS Code
3. Use the Test Explorer in the sidebar to run tests
4. Or use the integrated terminal with `dotnet test` commands

## 📊 Test Coverage

### **Unit Tests Coverage**

#### **UserService Tests (23 tests)**
- ✅ **GetAllUsers**: Verifies user retrieval from data storage
- ✅ **AddUser**: Tests user creation with ID assignment and validation
- ✅ **UpdateUser**: Tests user modification and duplicate username detection
- ✅ **DeleteUser**: Tests user removal from the system
- ✅ **GetUserById**: Tests individual user lookup
- ✅ **SearchByFullName**: Tests search functionality with case-insensitivity

#### **AuthController Tests (8 tests)**
- ✅ **Login with different user roles**: Admin, Editor, Regular User, View-Only
- ✅ **Failed login scenarios**: Invalid username, wrong password, inactive account
- ✅ **JWT token validation**: Proper claims, expiration, issuer/audience
- ✅ **Permission assignment**: Role-based access control verification

#### **DataService Tests (12 tests)**
- ✅ **File operations**: Loading and saving JSON data
- ✅ **Error handling**: Missing files, invalid JSON, permission issues
- ✅ **Data preservation**: Round-trip testing to ensure no data loss
- ✅ **Edge cases**: Special characters, empty data, directory creation

### **Integration Tests Coverage**

#### **API Integration Tests (15 tests)**
- ✅ **Authentication flow**: Login with valid/invalid credentials
- ✅ **User management**: CRUD operations through HTTP API
- ✅ **Authorization**: Permission-based access control
- ✅ **Error scenarios**: Duplicate data, validation errors
- ✅ **Search functionality**: API-level search testing

## 🔍 Key Testing Concepts Demonstrated

### **1. Unit Testing with Mocking**
```csharp
// Example: Testing business logic in isolation
var mockDataService = new Mock<IDataService>();
mockDataService.Setup(x => x.LoadUsers()).Returns(testUsers);

var userService = new UserService(mockDataService.Object, mockLogger.Object);
var result = userService.GetAllUsers();

// Verify business logic worked correctly
Assert.Equal(2, result.Count);
mockDataService.Verify(x => x.LoadUsers(), Times.Once);
```

**Why this matters**: Unit tests verify that each component works correctly in isolation, without depending on external systems like databases or file systems.

### **2. Integration Testing**
```csharp
// Example: Testing the complete API workflow
var loginRequest = new LoginRequest { UserName = "admin", Password = "password" };
var response = await _client.PostAsync("/api/auth/login", content);

Assert.Equal(HttpStatusCode.OK, response.StatusCode);
var token = ExtractTokenFromResponse(response);

// Use token for authenticated requests
_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
var usersResponse = await _client.GetAsync("/api/users");
```

**Why this matters**: Integration tests verify that all components work together correctly, including authentication, authorization, and data flow.

### **3. Test Data Management**
```csharp
// Example: Using temporary files for isolated testing
var testFilePath = Path.Combine(Path.GetTempPath(), $"test_users_{Guid.NewGuid()}.json");
var dataService = new TestDataService(mockLogger.Object, testFilePath);

// Test operations don't affect real data
dataService.SaveUsers(testUsers);
var loadedUsers = dataService.LoadUsers();
```

**Why this matters**: Tests should not interfere with real application data or with each other.

## 🛠️ Test Categories Explained

### **Arrangement, Act, Assert (AAA) Pattern**
All tests follow the AAA pattern for clarity:

```csharp
[Fact]
public void AddUser_WithValidUser_ShouldAssignIdAndSaveUser()
{
    // Arrange - Set up test data and mocks
    var newUser = new User { UserName = "TestUser" };
    mockDataService.Setup(x => x.LoadUsers()).Returns(existingUsers);
    
    // Act - Execute the method being tested
    userService.AddUser(newUser);
    
    // Assert - Verify the expected outcome
    Assert.Equal(2, newUser.UserID);
    mockDataService.Verify(x => x.SaveUsers(It.IsAny<List<User>>()), Times.Once);
}
```

### **Edge Case Testing**
Tests cover not just the "happy path" but also edge cases:

- **Empty data**: What happens with no users in the system?
- **Invalid input**: How does the system handle malformed data?
- **Permission boundaries**: What happens when users try unauthorized actions?
- **File system issues**: How does the system handle read-only files or missing directories?

### **Error Handling Verification**
Tests verify that errors are handled gracefully:

```csharp
[Fact]
public void AddUser_WithDuplicateUsername_ShouldThrowException()
{
    // Verify that the system properly rejects duplicate usernames
    var exception = Assert.Throws<Exception>(() => userService.AddUser(duplicateUser));
    Assert.Contains("Username already exists", exception.Message);
}
```

## 📋 Test Results Interpretation

### **Successful Test Run**
```
Starting test execution, please wait...
A total of 58 test files matched the specified pattern.

Test Run Successful.
Total tests: 58
     Passed: 58
     Failed: 0
    Skipped: 0
 Total time: 12.3456 Seconds
```

### **Failed Test Example**
```
Failed   RichKid.Tests.Unit.UserServiceTests.AddUser_WithDuplicateUsername_ShouldThrowException
Error Message:
   Expected exception with message containing "Username already exists" but got "Different error message"
Stack Trace:
   at RichKid.Tests.Unit.UserServiceTests.AddUser_WithDuplicateUsername_ShouldThrowException() in UserServiceTests.cs:line 123
```

**How to fix**: When tests fail, the error message and stack trace point you to exactly what went wrong and where.

## 🔧 Extending the Tests

### **Adding New Unit Tests**
1. Create a new test method in the appropriate test class
2. Follow the AAA pattern
3. Use descriptive test names that explain the scenario
4. Include both positive and negative test cases

```csharp
[Fact]
public void NewFeature_WithSpecificCondition_ShouldProduceExpectedResult()
{
    // Arrange
    // Act  
    // Assert
}
```

### **Adding New Integration Tests**
1. Add methods to `ApiIntegrationTests.cs`
2. Use the `GetValidJwtTokenAsync()` helper for authenticated requests
3. Test complete workflows, not just individual endpoints
4. Verify both successful operations and error conditions

## 🎯 Benefits of This Test Suite

### **For Developers**
- **Confidence**: Make changes knowing tests will catch regressions
- **Documentation**: Tests serve as examples of how the system should work
- **Debugging**: Failed tests pinpoint exactly what broke and where

### **For Code Reviews**
- **Verification**: Reviewers can see that new features are properly tested
- **Understanding**: Tests help reviewers understand the intended behavior
- **Quality**: Well-tested code is generally higher quality

### **For Maintenance**
- **Refactoring Safety**: Tests ensure that code changes don't break existing functionality
- **Bug Prevention**: Tests catch issues before they reach production
- **Regression Testing**: Automated tests run consistently, unlike manual testing

## 🚨 Common Issues and Solutions

### **Tests Pass Locally but Fail in CI**
- **File Paths**: Use `Path.Combine()` for cross-platform compatibility
- **Time Zones**: Use UTC for date/time comparisons
- **Dependencies**: Ensure all required packages are properly referenced

### **Flaky Tests**
- **Timing Issues**: Avoid tests that depend on specific timing
- **Shared State**: Ensure tests don't interfere with each other
- **External Dependencies**: Mock external services rather than calling them

### **Slow Tests**
- **Integration Tests**: These are inherently slower than unit tests
- **File I/O**: Use in-memory alternatives when possible
- **Database**: Use in-memory databases for testing

---

**Remember**: Good tests are an investment in code quality and developer productivity. They catch bugs early, serve as documentation, and give you confidence to make changes safely.