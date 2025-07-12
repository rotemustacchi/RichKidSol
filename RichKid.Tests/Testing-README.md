# 🧪 RichKid Testing Guide

Complete guide to the testing infrastructure for the RichKid User Management System. This guide covers everything you need to know about running, understanding, and maintaining the test suite.

## 🛡️ **DATA SAFETY GUARANTEE**

**YOUR `Users.json` FILE IS 100% SAFE!** 

The testing infrastructure is designed to **never touch your development data**:
- ✅ Tests use isolated temporary files with unique names
- ✅ Each test run creates its own data environment  
- ✅ Automatic cleanup removes all test files after completion
- ✅ Your real `Users.json` remains completely untouched
- ✅ You can run tests as many times as you want without worry

## 📋 Table of Contents

1. [Quick Start](#-quick-start)
2. [Test Architecture](#-test-architecture)
3. [Running Tests](#-running-tests)
4. [Test Categories](#-test-categories)
5. [Data Isolation](#-data-isolation)
6. [Test Configuration](#-test-configuration)
7. [Understanding Test Output](#-understanding-test-output)
8. [Troubleshooting](#-troubleshooting)
9. [Adding New Tests](#-adding-new-tests)
10. [Best Practices](#-best-practices)

## 🚀 Quick Start

### **Run All Tests (Recommended)**
```bash
# Navigate to solution root
cd RichKidSol

# Run complete test suite
dotnet test

# Expected output: 50+ tests passing in under 2 seconds
```

### **What You'll See:**
```
Starting test execution, please wait...
✅ Passed!  - RichKid.Tests.Unit.AuthControllerTests.Login_WithValidCredentials_ShouldReturnJwtToken
✅ Passed!  - RichKid.Tests.Integration.ApiIntegrationTests.GetUsers_WithValidToken_ShouldReturnUserList
...
Test Run Successful.
Total tests: 56
     Passed: 53
     Failed: 0
     Skipped: 3
 Total time: 1.8s
```

## 🏗️ Test Architecture

### **Project Structure**
```
RichKid.Tests/
├── Unit/                           # Fast, isolated unit tests
│   ├── AuthControllerTests.cs      # Authentication logic testing
│   ├── DataServiceTests.cs         # File operations testing  
│   ├── UserServiceTests.cs         # Business logic testing
│   └── SimpleIntegrationTests.cs   # Basic infrastructure tests
├── Integration/                    # Full application testing
│   └── ApiIntegrationTests.cs      # End-to-end API testing
├── RichKid.Tests.csproj            # Test project configuration
├── xunit.runner.json               # Test execution settings
└── appsettings.Testing.json        # Test environment config
```

### **Testing Framework Stack**
- **xUnit** - Primary testing framework (.NET standard)
- **Moq** - Mocking framework for unit test isolation
- **ASP.NET Core Testing** - Integration testing infrastructure
- **Custom Test Services** - Data isolation and cleanup

## 🎯 Running Tests

### **Basic Commands**

```bash
# Run all tests (comprehensive)
dotnet test

# Run with detailed output (for debugging)
dotnet test --logger "console;verbosity=detailed"

# Run quietly (minimal output)
dotnet test --verbosity quiet

# Run and collect code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### **Filtered Test Execution**

```bash
# Run only unit tests (fast execution)
dotnet test --filter "FullyQualifiedName~Unit"

# Run only integration tests (full application testing)
dotnet test --filter "FullyQualifiedName~Integration"

# Run specific test class
dotnet test --filter "ClassName=AuthControllerTests"

# Run specific test method
dotnet test --filter "FullyQualifiedName~Login_WithValidCredentials"

# Run tests containing specific text
dotnet test --filter "DisplayName~Authentication"
```

### **Performance Testing**

```bash
# Time test execution
Measure-Command { dotnet test }

# Run tests multiple times to check consistency
for ($i=1; $i -le 5; $i++) { 
    Write-Host "Run $i"; 
    dotnet test --verbosity quiet 
}
```

## 📚 Test Categories

### **1. Unit Tests** (`/Unit/`)

Fast, isolated tests that verify individual components work correctly.

#### **AuthControllerTests.cs**
- **Purpose**: Tests JWT authentication and authorization logic
- **Coverage**: Login flows, token generation, permission assignment
- **Key Tests**:
  - Valid login credentials → JWT token with correct claims
  - Invalid credentials → appropriate error messages
  - Different user roles → correct permission assignments
  - Token structure validation → issuer, audience, expiration

```csharp
// Example: Testing admin user login
[Fact]
public void Login_WithValidAdminCredentials_ShouldReturnTokenWithAdminPermissions()
{
    // Arranges admin user data
    // Acts by calling login endpoint
    // Asserts JWT contains admin permissions
}
```

#### **DataServiceTests.cs**
- **Purpose**: Tests file I/O operations and JSON handling
- **Coverage**: Loading/saving users, file creation, error handling
- **Key Tests**:
  - Load users from valid JSON file
  - Handle missing or corrupted files gracefully
  - Save users with proper JSON formatting
  - Preserve special characters and unicode
  - Create directories when needed

```csharp
// Example: Testing round-trip data preservation
[Fact]
public void SaveAndLoadUsers_ShouldPreserveAllUserData()
{
    // Arranges complex user data
    // Acts by saving then loading
    // Asserts all data is preserved exactly
}
```

#### **UserServiceTests.cs**
- **Purpose**: Tests business logic for user management
- **Coverage**: CRUD operations, validation, conflict detection
- **Key Tests**:
  - Add user with automatic ID assignment
  - Prevent duplicate usernames
  - Update user data correctly
  - Delete users and maintain data integrity
  - Search functionality with case-insensitive matching

```csharp
// Example: Testing username conflict prevention
[Fact]
public void AddUser_WithDuplicateUsername_ShouldThrowException()
{
    // Arranges existing user data
    // Acts by trying to add duplicate
    // Asserts exception is thrown with appropriate message
}
```

### **2. Integration Tests** (`/Integration/`)

Complete application tests that verify the entire system works together.

#### **ApiIntegrationTests.cs**
- **Purpose**: End-to-end testing of the complete API
- **Coverage**: Authentication flow, authorization policies, CRUD operations
- **Key Features**:
  - **Isolated Test Data**: Each test uses unique temporary files
  - **Complete HTTP Testing**: Real HTTP requests to test API
  - **Authentication Flow**: JWT token generation and validation
  - **Permission Testing**: Role-based access control verification

**Test Scenarios:**
```csharp
// Authentication Testing
✅ Valid credentials → successful login with JWT
✅ Invalid credentials → proper error messages  
✅ Inactive users → access denied

// Authorization Testing  
✅ Admin users → full CRUD access
✅ Regular users → limited permissions
✅ No authentication → access denied

// CRUD Operations
✅ Create user → proper validation and storage
✅ Read users → correct data retrieval
✅ Update user → data modification and persistence
✅ Delete user → proper removal and cleanup

// Edge Cases
✅ Duplicate usernames → conflict detection
✅ Invalid data → validation error messages
✅ Non-existent users → not found responses
```

## 🔒 Data Isolation

### **How Test Data Isolation Works**

1. **Unique File Creation**: Each test run creates files with unique GUIDs
   ```
   TestUsers_a1b2c3d4e5f6.json  # Test run 1
   TestUsers_f6e5d4c3b2a1.json  # Test run 2
   ```

2. **Temporary Storage**: All test files are created in the system temp directory
   ```
   C:\Users\[user]\AppData\Local\Temp\TestUsers_[guid].json
   ```

3. **Automatic Cleanup**: Files are deleted immediately after each test
   ```csharp
   public void Dispose()
   {
       if (File.Exists(_testFilePath))
           File.Delete(_testFilePath);
   }
   ```

4. **No Real Data Access**: Tests never read from or write to `Users.json`

### **Data Isolation Architecture**

```csharp
// Real DataService (used by application)
public class DataService : IDataService
{
    private readonly string _filePath = "Users.json"; // Real file
    // Production file operations...
}

// Test DataService (used by tests only)  
public class TestDataService : IDataService
{
    private readonly string _filePath; // Temporary test file
    // Same logic, different file location
}
```

### **Verification of Data Safety**

You can verify your data is safe by:

1. **Before running tests**: Note your `Users.json` file size/modification date
2. **Run tests**: `dotnet test`
3. **After tests**: Check that `Users.json` is unchanged
4. **Check temp folder**: Verify no test files remain in temp directory

## ⚙️ Test Configuration

### **Test Project Configuration** (`RichKid.Tests.csproj`)

```xml
<!-- Core testing packages -->
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
<PackageReference Include="xunit" Version="2.6.2" />
<PackageReference Include="Moq" Version="4.20.69" />

<!-- Integration testing for ASP.NET Core -->
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.7" />

<!-- JWT token handling for auth tests -->
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.12.1" />
```

### **Test Runner Configuration** (`xunit.runner.json`)

```json
{
  "parallelizeTestCollections": true,  // Run test classes in parallel
  "maxParallelThreads": 4,            // Use 4 threads for speed
  "methodDisplay": "method",          // Show full method names
  "diagnosticMessages": false,        // Reduce noise in output
  "stopOnFail": false                 // Continue running after failures
}
```

### **Test Environment Settings** (`appsettings.Testing.json`)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",           // Reduce log noise during tests
      "RichKid": "Information"        // Keep our app logs visible
    }
  },
  "Jwt": {
    "DurationInMinutes": 15           // Shorter token duration for tests
  },
  "ApiSettings": {
    "Timeouts": {
      "DefaultTimeoutSeconds": 10,    // Faster timeouts for tests
      "LoginTimeoutSeconds": 5
    }
  }
}
```

## 📊 Understanding Test Output

### **Successful Test Run**
```
Test run for RichKid.Tests.dll (.NETCoreApp,Version=v9.0)
Microsoft (R) Test Execution Command Line Tool Version 17.8.0

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

✅ Passed!  - RichKid.Tests.Unit.AuthControllerTests.Login_WithValidCredentials_ShouldReturnJwtToken [34ms]
✅ Passed!  - RichKid.Tests.Unit.DataServiceTests.LoadUsers_WhenFileExists_ShouldReturnUserList [12ms]
✅ Passed!  - RichKid.Tests.Integration.ApiIntegrationTests.GetUsers_WithValidToken_ShouldReturnUserList [156ms]

Test Run Successful.
Total tests: 56
     Passed: 53  ✅
     Failed: 0   
     Skipped: 3  ⚠️
 Total time: 1.8436 Seconds
```

### **Test Failure Example**
```
❌ Failed!  - RichKid.Tests.Integration.ApiIntegrationTests.CreateUser_WithDuplicateUsername_ShouldReturnBadRequest [45ms]
  Error Message:
   Assert.Contains() Failure: Sub-string not found
   String:    "{"type":"https://tools.ietf.org/html/rfc9"···
   Not found: "already exists"
   
  Stack Trace:
     at ApiIntegrationTests.CreateUser_WithDuplicateUsername_ShouldReturnBadRequest() 
     in C:\...\ApiIntegrationTests.cs:line 532
```

### **Performance Indicators**
- **Fast Tests** (< 50ms): Unit tests with mocked dependencies
- **Medium Tests** (50-200ms): Integration tests with HTTP calls
- **Slow Tests** (> 200ms): Complex integration scenarios

### **Log Output During Tests**
```
[2025-07-12 21:47:08] info: Program[0] - RichKid API starting up in Testing mode
[2025-07-12 21:47:08] dbug: TestDataService[0] - Loading test users from: C:\...\TestUsers_1c5f.json
[2025-07-12 21:47:08] info: TestDataService[0] - Successfully loaded 4 test users
[2025-07-12 21:47:08] info: AuthController[0] - Login successful for user: Rotem (ID: 1)
```

## 🔧 Troubleshooting

### **Common Issues and Solutions**

#### **Issue: "Users.json file is locked"**
```
Error: The process cannot access the file 'Users.json' because it is being used by another process.
```
**Solution**: This should never happen with the test suite since tests use isolated files. If you see this:
1. Make sure you're running `dotnet test` from the solution root
2. Check that no other application is using the `Users.json` file
3. Restart Visual Studio/VS Code if file locks persist

#### **Issue: "TestDataService not found"**
```
Error: Unable to resolve service for type 'TestDataService'
```
**Solution**: The test infrastructure automatically registers `TestDataService`. If you see this:
1. Verify you're running tests with `dotnet test` command
2. Check that test project references are correct
3. Rebuild the solution: `dotnet build`

#### **Issue: Tests are slow**
```
Total time: 30+ seconds for test run
```
**Solution**: Tests should complete in under 3 seconds normally. If slow:
1. Check system resources (CPU/memory usage)
2. Run tests without parallel execution: modify `xunit.runner.json`
3. Run unit tests only: `dotnet test --filter "FullyQualifiedName~Unit"`

#### **Issue: "Port already in use"**
```
Error: Failed to bind to address http://localhost:5270: address already in use
```
**Solution**: Integration tests use temporary test servers. If you see this:
1. Close any running instances of the API
2. Kill any orphaned processes: `taskkill /f /im dotnet.exe` (Windows)
3. Use different port in test configuration

#### **Issue: Test data not cleaned up**
```
Warning: Temporary test files found in temp directory
```
**Solution**: Test cleanup should be automatic. If files remain:
1. Check for failed test runs that didn't complete
2. Manually clean temp directory if needed
3. Restart the test run - cleanup happens at the start

### **Debugging Test Failures**

#### **1. Enable Detailed Logging**
```bash
dotnet test --logger "console;verbosity=detailed"
```

#### **2. Run Specific Failing Test**
```bash
dotnet test --filter "FullyQualifiedName~CreateUser_WithDuplicateUsername"
```

#### **3. Check Test Data**
Add temporary logging to see what test data is being used:
```csharp
[Fact]
public void MyTest()
{
    // Add this line to see the test file location
    Console.WriteLine($"Test file: {_testFilePath}");
    
    // Your test code...
}
```

#### **4. Verify Test Isolation**
Make sure each test starts with clean data:
```csharp
[Fact]
public void MyTest()
{
    // Verify test file doesn't exist initially
    Assert.False(File.Exists(_testFilePath));
    
    // Your test code...
}
```

## ➕ Adding New Tests

### **Adding a Unit Test**

1. **Choose the appropriate test class** based on what you're testing:
   - `AuthControllerTests.cs` - Authentication and JWT logic
   - `DataServiceTests.cs` - File operations and JSON handling
   - `UserServiceTests.cs` - Business logic and user management

2. **Create a new test method**:
```csharp
[Fact]
public void YourNewTest_WithSpecificCondition_ShouldExpectedBehavior()
{
    // Arrange - Set up test data and mocks
    var mockService = new Mock<IDataService>();
    mockService.Setup(x => x.LoadUsers()).Returns(testUsers);
    
    // Act - Execute the method being tested
    var result = userService.YourMethodBeingTested(input);
    
    // Assert - Verify the expected outcome
    Assert.Equal(expectedValue, result);
    Assert.True(result.SomeProperty);
}
```

3. **Follow naming conventions**:
   - `MethodName_WithCondition_ShouldExpectedResult`
   - Example: `AddUser_WithValidData_ShouldAssignIdAndSaveUser`

### **Adding an Integration Test**

1. **Add to `ApiIntegrationTests.cs`**:
```csharp
[Fact]
public async Task YourNewApiTest_WithCondition_ShouldExpectedBehavior()
{
    // Arrange - Get authentication token and prepare data
    var token = await GetValidJwtTokenAsync();
    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
    
    var testData = new YourDataModel { /* test data */ };
    var content = new StringContent(
        JsonSerializer.Serialize(testData), 
        Encoding.UTF8, 
        "application/json"
    );
    
    // Act - Make HTTP request to API
    var response = await _client.PostAsync("/api/your-endpoint", content);
    
    // Assert - Verify HTTP response and data
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    
    var responseContent = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<YourResponseType>(responseContent);
    Assert.NotNull(result);
}
```

2. **Test data is automatically isolated** - each test gets its own temporary file

### **Testing Best Practices**

#### **1. Test Naming**
```csharp
// ✅ Good: Descriptive and follows pattern
[Fact]
public void Login_WithInvalidPassword_ShouldReturnUnauthorized()

// ❌ Bad: Vague and doesn't describe condition/outcome
[Fact]
public void TestLogin()
```

#### **2. Arrange-Act-Assert Pattern**
```csharp
[Fact]
public void AddUser_WithValidData_ShouldAssignIdAndSaveUser()
{
    // Arrange - Set up test data and dependencies
    var newUser = new User { UserName = "TestUser", Password = "test123" };
    var existingUsers = new List<User>();
    _mockDataService.Setup(x => x.LoadUsers()).Returns(existingUsers);
    
    // Act - Execute the method being tested
    _userService.AddUser(newUser);
    
    // Assert - Verify expected outcomes
    Assert.Equal(1, newUser.UserID); // ID should be assigned
    _mockDataService.Verify(x => x.SaveUsers(It.IsAny<List<User>>()), Times.Once);
}
```

#### **3. Test One Thing**
```csharp
// ✅ Good: Tests one specific behavior
[Fact]
public void AddUser_WithDuplicateUsername_ShouldThrowException()
{
    // Test only duplicate username detection
}

[Fact]
public void AddUser_WithValidData_ShouldAssignCorrectId()
{
    // Test only ID assignment logic
}

// ❌ Bad: Tests multiple behaviors in one test
[Fact]
public void AddUser_ShouldDoEverything()
{
    // Tests validation, ID assignment, saving, etc.
}
```

#### **4. Independent Tests**
```csharp
// ✅ Good: Each test sets up its own data
[Fact]
public void TestA()
{
    var testData = CreateTestData(); // Fresh data for this test
    // Test logic...
}

[Fact]
public void TestB()
{
    var testData = CreateDifferentTestData(); // Different fresh data
    // Test logic...
}

// ❌ Bad: Tests depend on shared state
private static List<User> _sharedTestData = new();

[Fact]
public void TestA()
{
    _sharedTestData.Add(user); // Modifies shared state
}

[Fact]
public void TestB()
{
    // This test depends on TestA running first
}
```

## 📋 Best Practices

### **Test Organization**

#### **File Structure**
```
RichKid.Tests/
├── Unit/                          # Fast, isolated tests
│   ├── AuthControllerTests.cs     # Group related tests by class
│   ├── DataServiceTests.cs        
│   └── UserServiceTests.cs        
├── Integration/                   # Full application tests  
│   └── ApiIntegrationTests.cs     # Group by functionality
├── Helpers/                       # Test utilities (if needed)
│   └── TestDataBuilder.cs         # Common test data creation
└── Fixtures/                      # Shared test setup (if needed)
    └── DatabaseFixture.cs         # Shared resources
```

#### **Test Method Organization**
```csharp
public class UserServiceTests
{
    #region Constructor and Setup
    // Setup code here
    #endregion

    #region GetAllUsers Tests
    [Fact]
    public void GetAllUsers_WhenUsersExist_ShouldReturnAllUsers() { }
    
    [Fact]
    public void GetAllUsers_WhenNoUsers_ShouldReturnEmptyList() { }
    #endregion

    #region AddUser Tests
    [Fact]
    public void AddUser_WithValidData_ShouldSucceed() { }
    
    [Fact]
    public void AddUser_WithDuplicateUsername_ShouldThrowException() { }
    #endregion

    #region Helper Methods
    private User CreateTestUser() { /* helper logic */ }
    #endregion
}
```

### **Data Management**

#### **Test Data Creation**
```csharp
// ✅ Good: Create test data in each test
[Fact]
public void MyTest()
{
    var testUser = new User 
    {
        UserName = "TestUser_" + Guid.NewGuid().ToString("N")[..8], // Unique
        Password = "testpass",
        Active = true,
        Data = new UserData 
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com"
        }
    };
    // Use testUser in test...
}

// ✅ Good: Use helper methods for complex data
private User CreateValidTestUser(string username = null)
{
    return new User
    {
        UserName = username ?? "TestUser_" + Guid.NewGuid().ToString("N")[..8],
        Password = "testpass123",
        Active = true,
        Data = new UserData
        {
            FirstName = "Test",
            LastName = "User", 
            Email = "test@example.com",
            Phone = "555-1234"
        }
    };
}
```

#### **Assertions**
```csharp
// ✅ Good: Specific assertions with clear error messages
Assert.Equal(expectedUserId, actualUser.UserID);
Assert.True(actualUser.Active, "User should be active after creation");
Assert.Contains("already exists", exception.Message);

// ✅ Good: Test important properties
Assert.NotNull(result);
Assert.Equal(3, result.Count);
Assert.All(result, user => Assert.True(user.UserID > 0));

// ❌ Bad: Vague assertions
Assert.True(result != null);
Assert.True(result.Count > 0);
```

### **Performance Considerations**

#### **Fast Test Guidelines**
```csharp
// ✅ Fast: Unit tests with mocks
[Fact]
public void FastUnitTest()
{
    var mockService = new Mock<IDataService>();
    // No file I/O, no HTTP calls
}

// ⚠️ Slower: Integration tests (but still needed)
[Fact]
public async Task IntegrationTest()
{
    var response = await _client.GetAsync("/api/users");
    // HTTP calls are slower but test real behavior
}

// ❌ Avoid: Unnecessary delays in tests
[Fact]
public void SlowTest()
{
    Thread.Sleep(1000); // Don't add artificial delays
}
```

#### **Resource Management**
```csharp
// ✅ Good: Proper cleanup
public class MyTests : IDisposable
{
    private readonly string _testFile;
    
    public MyTests()
    {
        _testFile = Path.GetTempFileName();
    }
    
    public void Dispose()
    {
        if (File.Exists(_testFile))
            File.Delete(_testFile);
    }
}

// ✅ Good: Use using statements for resources
[Fact]
public void TestWithResource()
{
    using var stream = new MemoryStream();
    // Stream is automatically disposed
}
```

### **Error Testing**

#### **Exception Testing**
```csharp
// ✅ Good: Test specific exceptions and messages
[Fact]
public void AddUser_WithDuplicateUsername_ShouldThrowSpecificException()
{
    // Arrange - Create duplicate scenario
    var existingUser = new User { UserName = "duplicate" };
    var users = new List<User> { existingUser };
    _mockDataService.Setup(x => x.LoadUsers()).Returns(users);
    
    var newUser = new User { UserName = "duplicate" };
    
    // Act & Assert - Verify specific exception and message
    var exception = Assert.Throws<Exception>(() => _userService.AddUser(newUser));
    Assert.Contains("Username already exists", exception.Message);
}

// ✅ Good: Test error conditions
[Fact]
public void LoadUsers_WithCorruptedFile_ShouldThrowInvalidOperationException()
{
    // Test how system handles corrupt data
}
```

### **Maintenance**

#### **Keeping Tests Updated**
1. **Run tests frequently** during development
2. **Update tests when changing business logic**
3. **Add tests for new features immediately**
4. **Remove tests for deleted functionality**
5. **Refactor tests when refactoring code**

#### **Test Documentation**
```csharp
/// <summary>
/// Tests that user creation assigns the next available ID correctly.
/// This ensures no ID conflicts occur when adding multiple users.
/// </summary>
[Fact]
public void AddUser_WithMultipleUsers_ShouldAssignSequentialIds()
{
    // Test implementation...
}
```

## 🎯 Continuous Integration

### **Running Tests in CI/CD**

```yaml
# Example GitHub Actions workflow
name: Run Tests
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    - name: Restore dependencies
      run: dotnet restore
    - name: Build
      run: dotnet build --no-restore
    - name: Test
      run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"
```

### **Test Metrics to Monitor**
- **Test Count**: Should increase with new features
- **Test Duration**: Should remain under 3 seconds for full suite
- **Code Coverage**: Aim for >80% coverage on business logic
- **Test Stability**: Tests should pass consistently

## 🔍 Advanced Topics

### **Custom Test Attributes**
```csharp
// Create custom attributes for test categorization
public class IntegrationTestAttribute : FactAttribute
{
    public IntegrationTestAttribute()
    {
        if (!IsIntegrationTestEnvironment())
        {
            Skip = "Integration tests require test environment";
        }
    }
    
    private static bool IsIntegrationTestEnvironment()
    {
        return Environment.GetEnvironmentVariable("RUN_INTEGRATION_TESTS") == "true";
    }
}

// Usage
[IntegrationTest]
public async Task MyIntegrationTest() { }
```

### **Test Data Builders**
```csharp
// Create fluent builders for complex test data
public class UserBuilder
{
    private User _user = new User { Data = new UserData() };
    
    public UserBuilder WithUsername(string username)
    {
        _user.UserName = username;
        return this;
    }
    
    public UserBuilder WithGroup(int groupId)
    {
        _user.UserGroupID = groupId;
        return this;
    }
    
    public UserBuilder Active(bool active = true)
    {
        _user.Active = active;
        return this;
    }
    
    public User Build() => _user;
}

// Usage in tests
[Fact]
public void TestWithBuilder()
{
    var user = new UserBuilder()
        .WithUsername("testuser")
        .WithGroup(UserGroups.ADMIN)
        .Active()
        .Build();
    
    // Use user in test...
}
```

## 📚 Additional Resources

### **Useful Commands Reference**
```bash
# Test execution
dotnet test                                    # Run all tests
dotnet test --filter "Category=Unit"          # Run by category
dotnet test --filter "Priority=1"             # Run by priority
dotnet test --list-tests                      # List all tests
dotnet test --help                            # Show all options

# Coverage and reporting
dotnet test --collect:"XPlat Code Coverage"   # Collect coverage
dotnet test --logger trx                      # Generate test report
dotnet test --results-directory ./TestResults # Specify output directory

# Debugging
dotnet test --logger console --verbosity detailed  # Detailed output
dotnet test --filter "FullyQualifiedName~MyTest" --logger console --verbosity detailed
```

### **Learning Resources**
- [xUnit Documentation](https://xunit.net/docs/getting-started/netcore/cmdline)
- [ASP.NET Core Testing](https://docs.microsoft.com/en-us/aspnet/core/test/)
- [Moq Documentation](https://github.com/moq/moq4)
- [.NET Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/best-practices)

---

## 🎉 Summary

You now have a complete testing infrastructure that:

- ✅ **Protects your data** - Never touches `Users.json`
- ✅ **Runs fast** - Complete suite in under 2 seconds  
- ✅ **Provides comprehensive coverage** - Unit and integration tests
- ✅ **Is easy to extend** - Clear patterns for adding new tests
- ✅ **Gives clear feedback** - Detailed output and error messages
- ✅ **Supports debugging** - Verbose logging and isolation
- ✅ **Scales with your project** - Parallel execution and cleanup

**Happy Testing!** 🧪✨

Your development data is always safe, your tests run reliably, and you can develop with confidence knowing that your changes won't break existing functionality.