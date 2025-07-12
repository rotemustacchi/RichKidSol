using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using RichKid.API.Controllers;
using RichKid.Shared.Models;
using RichKid.Shared.Services;
using RichKid.Shared.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;

namespace RichKid.Tests.Unit
{
    /// <summary>
    /// Unit tests for the Authentication Controller
    /// These tests verify that the JWT authentication logic works correctly
    /// including user validation, token generation, and permission assignment
    /// </summary>
    public class AuthControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly IConfiguration _configuration;
        private readonly AuthController _authController;

        public AuthControllerTests()
        {
            // Set up the required dependencies for testing authentication
            _mockUserService = new Mock<IUserService>();
            _mockLogger = new Mock<ILogger<AuthController>>();
            
            // Create a test configuration with JWT settings
            // This simulates the appsettings.json configuration
            var configData = new Dictionary<string, string>
            {
                {"Jwt:Key", "ThisIsAReallyStrongSecretKey1234!"},
                {"Jwt:Issuer", "RichKidAPI"},
                {"Jwt:Audience", "RichKidClients"},
                {"Jwt:DurationInMinutes", "60"}
            };
            
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(configData);
            _configuration = configBuilder.Build();

            // Create the controller with mocked dependencies
            _authController = new AuthController(_configuration, _mockUserService.Object, _mockLogger.Object);
        }

        #region Successful Login Tests

        [Fact]
        public void Login_WithValidAdminCredentials_ShouldReturnTokenWithAdminPermissions()
        {
            // Arrange - Create a test admin user
            var adminUser = new User
            {
                UserID = 1,
                UserName = "admin",
                Password = "admin123",
                Active = true,
                UserGroupID = UserGroups.ADMIN, // Admin group
                Data = new UserData { FirstName = "Admin", LastName = "User" }
            };

            var users = new List<User> { adminUser };
            _mockUserService.Setup(x => x.GetAllUsers()).Returns(users);

            var loginRequest = new LoginRequest
            {
                UserName = "admin",
                Password = "admin123"
            };

            // Act
            var result = _authController.Login(loginRequest);

            // Assert - Should return OK with a JWT token
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<LoginResponse>(okResult.Value);
            Assert.NotEmpty(response.Token);

            // Verify the JWT token contains admin permissions
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.ReadJwtToken(response.Token);
            
            Assert.Contains(token.Claims, c => c.Type == "UserID" && c.Value == "1");
            Assert.Contains(token.Claims, c => c.Type == "UserGroupID" && c.Value == "1");
            Assert.Contains(token.Claims, c => c.Type == "CanCreate" && c.Value == "true");
            Assert.Contains(token.Claims, c => c.Type == "CanEdit" && c.Value == "true");
            Assert.Contains(token.Claims, c => c.Type == "CanDelete" && c.Value == "true");
            Assert.Contains(token.Claims, c => c.Type == "CanView" && c.Value == "true");
        }

        [Fact]
        public void Login_WithValidEditorCredentials_ShouldReturnTokenWithEditorPermissions()
        {
            // Arrange - Create a test editor user
            var editorUser = new User
            {
                UserID = 2,
                UserName = "editor",
                Password = "editor123",
                Active = true,
                UserGroupID = UserGroups.EDITOR, // Editor group
                Data = new UserData { FirstName = "Editor", LastName = "User" }
            };

            var users = new List<User> { editorUser };
            _mockUserService.Setup(x => x.GetAllUsers()).Returns(users);

            var loginRequest = new LoginRequest
            {
                UserName = "editor",
                Password = "editor123"
            };

            // Act
            var result = _authController.Login(loginRequest);

            // Assert - Should return OK with appropriate editor permissions
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<LoginResponse>(okResult.Value);
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.ReadJwtToken(response.Token);
            
            // Editors can create and edit but not delete
            Assert.Contains(token.Claims, c => c.Type == "CanCreate" && c.Value == "true");
            Assert.Contains(token.Claims, c => c.Type == "CanEdit" && c.Value == "true");
            Assert.Contains(token.Claims, c => c.Type == "CanDelete" && c.Value == "false");
            Assert.Contains(token.Claims, c => c.Type == "CanView" && c.Value == "true");
        }

        [Fact]
        public void Login_WithValidRegularUserCredentials_ShouldReturnTokenWithLimitedPermissions()
        {
            // Arrange - Create a test regular user
            var regularUser = new User
            {
                UserID = 3,
                UserName = "user",
                Password = "user123",
                Active = true,
                UserGroupID = UserGroups.REGULAR_USER, // Regular user group
                Data = new UserData { FirstName = "Regular", LastName = "User" }
            };

            var users = new List<User> { regularUser };
            _mockUserService.Setup(x => x.GetAllUsers()).Returns(users);

            var loginRequest = new LoginRequest
            {
                UserName = "user",
                Password = "user123"
            };

            // Act
            var result = _authController.Login(loginRequest);

            // Assert - Regular users can only edit themselves
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<LoginResponse>(okResult.Value);
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.ReadJwtToken(response.Token);
            
            Assert.Contains(token.Claims, c => c.Type == "CanCreate" && c.Value == "false");
            Assert.Contains(token.Claims, c => c.Type == "CanEdit" && c.Value == "self"); // Can only edit self
            Assert.Contains(token.Claims, c => c.Type == "CanDelete" && c.Value == "false");
            Assert.Contains(token.Claims, c => c.Type == "CanView" && c.Value == "true");
        }

        #endregion

        #region Failed Login Tests

        [Fact]
        public void Login_WithNonExistentUsername_ShouldReturnUnauthorized()
        {
            // Arrange - No users in the system
            var emptyUserList = new List<User>();
            _mockUserService.Setup(x => x.GetAllUsers()).Returns(emptyUserList);

            var loginRequest = new LoginRequest
            {
                UserName = "nonexistent",
                Password = "password"
            };

            // Act
            var result = _authController.Login(loginRequest);

            // Assert - Should return 401 with specific error message
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Username not found", unauthorizedResult.Value);
        }

        [Fact]
        public void Login_WithWrongPassword_ShouldReturnUnauthorized()
        {
            // Arrange - User exists but password is wrong
            var user = new User
            {
                UserID = 1,
                UserName = "testuser",
                Password = "correctpassword",
                Active = true,
                UserGroupID = UserGroups.REGULAR_USER
            };

            var users = new List<User> { user };
            _mockUserService.Setup(x => x.GetAllUsers()).Returns(users);

            var loginRequest = new LoginRequest
            {
                UserName = "testuser",
                Password = "wrongpassword" // Incorrect password
            };

            // Act
            var result = _authController.Login(loginRequest);

            // Assert - Should return 401 with password error message
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Incorrect password", unauthorizedResult.Value);
        }

        [Fact]
        public void Login_WithInactiveAccount_ShouldReturnUnauthorized()
        {
            // Arrange - User exists but account is inactive
            var inactiveUser = new User
            {
                UserID = 1,
                UserName = "inactive",
                Password = "password",
                Active = false, // Account is disabled
                UserGroupID = UserGroups.REGULAR_USER
            };

            var users = new List<User> { inactiveUser };
            _mockUserService.Setup(x => x.GetAllUsers()).Returns(users);

            var loginRequest = new LoginRequest
            {
                UserName = "inactive",
                Password = "password"
            };

            // Act
            var result = _authController.Login(loginRequest);

            // Assert - Should reject inactive accounts
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Contains("Account is inactive", unauthorizedResult.Value.ToString());
        }

        #endregion

        #region Permission Assignment Tests

        [Fact]
        public void Login_WithViewOnlyUser_ShouldAssignCorrectPermissions()
        {
            // Arrange - Create a view-only user
            var viewOnlyUser = new User
            {
                UserID = 4,
                UserName = "viewer",
                Password = "viewer123",
                Active = true,
                UserGroupID = UserGroups.VIEW_ONLY // View-only group
            };

            var users = new List<User> { viewOnlyUser };
            _mockUserService.Setup(x => x.GetAllUsers()).Returns(users);

            var loginRequest = new LoginRequest
            {
                UserName = "viewer",
                Password = "viewer123"
            };

            // Act
            var result = _authController.Login(loginRequest);

            // Assert - View-only users should have very limited permissions
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<LoginResponse>(okResult.Value);
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.ReadJwtToken(response.Token);
            
            Assert.Contains(token.Claims, c => c.Type == "CanCreate" && c.Value == "false");
            Assert.Contains(token.Claims, c => c.Type == "CanEdit" && c.Value == "self"); // Can edit own profile
            Assert.Contains(token.Claims, c => c.Type == "CanDelete" && c.Value == "false");
            Assert.Contains(token.Claims, c => c.Type == "CanView" && c.Value == "true");
        }

        [Fact]
        public void Login_WithUnassignedUser_ShouldAssignMinimalPermissions()
        {
            // Arrange - Create a user without a group assignment
            var unassignedUser = new User
            {
                UserID = 5,
                UserName = "unassigned",
                Password = "password",
                Active = true,
                UserGroupID = null // No group assigned
            };

            var users = new List<User> { unassignedUser };
            _mockUserService.Setup(x => x.GetAllUsers()).Returns(users);

            var loginRequest = new LoginRequest
            {
                UserName = "unassigned",
                Password = "password"
            };

            // Act
            var result = _authController.Login(loginRequest);

            // Assert - Unassigned users should have no permissions
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<LoginResponse>(okResult.Value);
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.ReadJwtToken(response.Token);
            
            // Should have no permissions at all
            Assert.Contains(token.Claims, c => c.Type == "CanCreate" && c.Value == "false");
            Assert.Contains(token.Claims, c => c.Type == "CanEdit" && c.Value == "false");
            Assert.Contains(token.Claims, c => c.Type == "CanDelete" && c.Value == "false");
            Assert.Contains(token.Claims, c => c.Type == "CanView" && c.Value == "false");
        }

        #endregion

        #region JWT Token Validation Tests

        [Fact]
        public void Login_ShouldGenerateTokenWithCorrectIssuerAndAudience()
        {
            // Arrange
            var user = new User
            {
                UserID = 1,
                UserName = "testuser",
                Password = "password",
                Active = true,
                UserGroupID = UserGroups.REGULAR_USER
            };

            var users = new List<User> { user };
            _mockUserService.Setup(x => x.GetAllUsers()).Returns(users);

            var loginRequest = new LoginRequest
            {
                UserName = "testuser",
                Password = "password"
            };

            // Act
            var result = _authController.Login(loginRequest);

            // Assert - Verify JWT token structure
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<LoginResponse>(okResult.Value);
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.ReadJwtToken(response.Token);
            
            // Check that token has correct issuer and audience from config
            Assert.Equal("RichKidAPI", token.Issuer);
            Assert.Contains("RichKidClients", token.Audiences);
            
            // Verify required claims are present
            Assert.Contains(token.Claims, c => c.Type == "sub" && c.Value == "testuser");
            Assert.Contains(token.Claims, c => c.Type == "UserID" && c.Value == "1");
        }

        [Fact]
        public void Login_ShouldGenerateTokenWithExpirationTime()
        {
            // Arrange
            var user = new User
            {
                UserID = 1,
                UserName = "testuser",
                Password = "password",
                Active = true,
                UserGroupID = UserGroups.REGULAR_USER
            };

            var users = new List<User> { user };
            _mockUserService.Setup(x => x.GetAllUsers()).Returns(users);

            var loginRequest = new LoginRequest
            {
                UserName = "testuser",
                Password = "password"
            };

            // Act
            var result = _authController.Login(loginRequest);

            // Assert - Token should have proper expiration
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<LoginResponse>(okResult.Value);
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.ReadJwtToken(response.Token);
            
            // Token should expire in the future (within the configured time)
            Assert.True(token.ValidTo > DateTime.UtcNow);
            Assert.True(token.ValidTo <= DateTime.UtcNow.AddMinutes(61)); // Allow 1 minute buffer
        }

        #endregion
    }
}