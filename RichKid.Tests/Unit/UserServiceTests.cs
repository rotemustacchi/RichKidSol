using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using RichKid.API.Services;
using RichKid.Shared.Models;
using RichKid.Shared.Services;
using System.Collections.Generic;
using System.Linq;

namespace RichKid.Tests.Unit
{
    /// <summary>
    /// Unit tests for the UserService class
    /// These tests verify that the business logic works correctly in isolation
    /// by mocking external dependencies like file operations
    /// </summary>
    public class UserServiceTests
    {
        private readonly Mock<IDataService> _mockDataService;
        private readonly Mock<ILogger<UserService>> _mockLogger;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            // Set up mock objects before each test
            // This ensures each test runs with fresh, controlled dependencies
            _mockDataService = new Mock<IDataService>();
            _mockLogger = new Mock<ILogger<UserService>>();
            
            // Create the service we're testing with mocked dependencies
            _userService = new UserService(_mockDataService.Object, _mockLogger.Object);
        }

        #region GetAllUsers Tests

        [Fact]
        public void GetAllUsers_WhenDataServiceReturnsUsers_ShouldReturnAllUsers()
        {
            // Arrange - Set up test data and mock behavior
            var expectedUsers = new List<User>
            {
                new User { UserID = 1, UserName = "TestUser1", Active = true },
                new User { UserID = 2, UserName = "TestUser2", Active = false }
            };
            
            // Tell the mock what to return when LoadUsers is called
            _mockDataService.Setup(x => x.LoadUsers()).Returns(expectedUsers);

            // Act - Call the method we're testing
            var result = _userService.GetAllUsers();

            // Assert - Verify the results match our expectations
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("TestUser1", result[0].UserName);
            Assert.Equal("TestUser2", result[1].UserName);
            
            // Verify that our mock was called exactly once
            _mockDataService.Verify(x => x.LoadUsers(), Times.Once);
        }

        [Fact]
        public void GetAllUsers_WhenDataServiceReturnsEmptyList_ShouldReturnEmptyList()
        {
            // Arrange - Test the edge case of no users in the system
            var emptyUserList = new List<User>();
            _mockDataService.Setup(x => x.LoadUsers()).Returns(emptyUserList);

            // Act
            var result = _userService.GetAllUsers();

            // Assert - Should handle empty list gracefully
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockDataService.Verify(x => x.LoadUsers(), Times.Once);
        }

        #endregion

        #region AddUser Tests

        [Fact]
        public void AddUser_WithValidUser_ShouldAssignIdAndSaveUser()
        {
            // Arrange - Create a valid user and existing user list
            var newUser = new User 
            { 
                UserName = "NewUser", 
                Password = "password123",
                Active = true,
                Data = new UserData 
                { 
                    FirstName = "John", 
                    LastName = "Doe",
                    Email = "john@example.com",
                    Phone = "123-456-7890"
                }
            };
            
            var existingUsers = new List<User>
            {
                new User { UserID = 1, UserName = "ExistingUser" }
            };

            // Set up mock to return existing users first, then accept the save
            _mockDataService.Setup(x => x.LoadUsers()).Returns(existingUsers);
            _mockDataService.Setup(x => x.SaveUsers(It.IsAny<List<User>>()));

            // Act
            _userService.AddUser(newUser);

            // Assert - Verify the user was processed correctly
            Assert.Equal(2, newUser.UserID); // Should get next available ID
            Assert.NotNull(newUser.Data.CreationDate);
            Assert.NotEmpty(newUser.Data.CreationDate);
            
            // Verify data service methods were called appropriately
            _mockDataService.Verify(x => x.LoadUsers(), Times.Once);
            _mockDataService.Verify(x => x.SaveUsers(It.Is<List<User>>(list => 
                list.Count == 2 && list.Any(u => u.UserName == "NewUser"))), Times.Once);
        }

        [Fact]
        public void AddUser_WithDuplicateUsername_ShouldThrowException()
        {
            // Arrange - Create scenario where username already exists
            var newUser = new User { UserName = "ExistingUser", Password = "password" };
            var existingUsers = new List<User>
            {
                new User { UserID = 1, UserName = "ExistingUser" }
            };

            _mockDataService.Setup(x => x.LoadUsers()).Returns(existingUsers);

            // Act & Assert - Verify that duplicate usernames are rejected
            var exception = Assert.Throws<Exception>(() => _userService.AddUser(newUser));
            Assert.Contains("Username already exists", exception.Message);
            
            // Verify that save was never called since validation failed
            _mockDataService.Verify(x => x.SaveUsers(It.IsAny<List<User>>()), Times.Never);
        }

        [Fact]
        public void AddUser_WithFirstUser_ShouldAssignIdOne()
        {
            // Arrange - Test the case where this is the very first user
            var firstUser = new User 
            { 
                UserName = "FirstUser", 
                Password = "password",
                Data = new UserData { FirstName = "First", LastName = "User" }
            };
            
            var emptyUserList = new List<User>(); // No existing users

            _mockDataService.Setup(x => x.LoadUsers()).Returns(emptyUserList);
            _mockDataService.Setup(x => x.SaveUsers(It.IsAny<List<User>>()));

            // Act
            _userService.AddUser(firstUser);

            // Assert - First user should get ID 1
            Assert.Equal(1, firstUser.UserID);
        }

        #endregion

        #region GetUserById Tests

        [Fact]
        public void GetUserById_WithValidId_ShouldReturnCorrectUser()
        {
            // Arrange
            var users = new List<User>
            {
                new User { UserID = 1, UserName = "User1" },
                new User { UserID = 2, UserName = "User2" },
                new User { UserID = 3, UserName = "User3" }
            };

            _mockDataService.Setup(x => x.LoadUsers()).Returns(users);

            // Act
            var result = _userService.GetUserById(2);

            // Assert - Should return the specific user requested
            Assert.NotNull(result);
            Assert.Equal(2, result.UserID);
            Assert.Equal("User2", result.UserName);
        }

        [Fact]
        public void GetUserById_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var users = new List<User>
            {
                new User { UserID = 1, UserName = "User1" }
            };

            _mockDataService.Setup(x => x.LoadUsers()).Returns(users);

            // Act
            var result = _userService.GetUserById(999); // Non-existent ID

            // Assert - Should handle missing users gracefully
            Assert.Null(result);
        }

        #endregion

        #region UpdateUser Tests

        [Fact]
        public void UpdateUser_WithValidUser_ShouldUpdateExistingUser()
        {
            // Arrange
            var existingUsers = new List<User>
            {
                new User 
                { 
                    UserID = 1, 
                    UserName = "OriginalName", 
                    Password = "oldpass",
                    Active = false,
                    Data = new UserData { FirstName = "Old", LastName = "Name" }
                },
                new User { UserID = 2, UserName = "OtherUser" }
            };

            var updatedUser = new User
            {
                UserID = 1,
                UserName = "UpdatedName",
                Password = "newpass",
                Active = true,
                Data = new UserData { FirstName = "New", LastName = "Name" }
            };

            _mockDataService.Setup(x => x.LoadUsers()).Returns(existingUsers);
            _mockDataService.Setup(x => x.SaveUsers(It.IsAny<List<User>>()));

            // Act
            _userService.UpdateUser(updatedUser);

            // Assert - Verify the user was updated in the list
            _mockDataService.Verify(x => x.SaveUsers(It.Is<List<User>>(list =>
                list.First(u => u.UserID == 1).UserName == "UpdatedName" &&
                list.First(u => u.UserID == 1).Active == true)), Times.Once);
        }

        [Fact]
        public void UpdateUser_WithDuplicateUsername_ShouldThrowException()
        {
            // Arrange - Create scenario where update would create duplicate username
            var existingUsers = new List<User>
            {
                new User { UserID = 1, UserName = "User1" },
                new User { UserID = 2, UserName = "User2" }
            };

            var updatedUser = new User
            {
                UserID = 1,
                UserName = "User2" // Trying to use User2's name
            };

            _mockDataService.Setup(x => x.LoadUsers()).Returns(existingUsers);

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => _userService.UpdateUser(updatedUser));
            Assert.Contains("Username already exists", exception.Message);
        }

        #endregion

        #region DeleteUser Tests

        [Fact]
        public void DeleteUser_WithValidId_ShouldRemoveUserFromList()
        {
            // Arrange
            var users = new List<User>
            {
                new User { UserID = 1, UserName = "User1" },
                new User { UserID = 2, UserName = "User2" },
                new User { UserID = 3, UserName = "User3" }
            };

            _mockDataService.Setup(x => x.LoadUsers()).Returns(users);
            _mockDataService.Setup(x => x.SaveUsers(It.IsAny<List<User>>()));

            // Act
            _userService.DeleteUser(2);

            // Assert - Verify the specific user was removed
            _mockDataService.Verify(x => x.SaveUsers(It.Is<List<User>>(list =>
                list.Count == 2 && 
                !list.Any(u => u.UserID == 2) &&
                list.Any(u => u.UserID == 1) &&
                list.Any(u => u.UserID == 3))), Times.Once);
        }

        #endregion

        #region SearchByFullName Tests

        [Fact]
        public void SearchByFullName_WithMatchingNames_ShouldReturnFilteredResults()
        {
            // Arrange
            var users = new List<User>
            {
                new User 
                { 
                    UserID = 1, 
                    UserName = "user1",
                    Data = new UserData { FirstName = "John", LastName = "Smith" }
                },
                new User 
                { 
                    UserID = 2, 
                    UserName = "user2",
                    Data = new UserData { FirstName = "Jane", LastName = "Johnson" }
                },
                new User 
                { 
                    UserID = 3, 
                    UserName = "user3",
                    Data = new UserData { FirstName = "Johnny", LastName = "Smith" }
                }
            };

            _mockDataService.Setup(x => x.LoadUsers()).Returns(users);

            // Act - Search for users with first name containing "John"
            var result = _userService.SearchByFullName("John", "");

            // Assert - Should find both "John" and "Johnny"
            Assert.Equal(2, result.Count);
            Assert.Contains(result, u => u.Data.FirstName == "John");
            Assert.Contains(result, u => u.Data.FirstName == "Johnny");
        }

        [Fact]
        public void SearchByFullName_WithCaseInsensitiveSearch_ShouldReturnResults()
        {
            // Arrange
            var users = new List<User>
            {
                new User 
                { 
                    UserID = 1,
                    Data = new UserData { FirstName = "JOHN", LastName = "SMITH" }
                }
            };

            _mockDataService.Setup(x => x.LoadUsers()).Returns(users);

            // Act - Search with different case
            var result = _userService.SearchByFullName("john", "smith");

            // Assert - Should find the user regardless of case
            Assert.Single(result);
            Assert.Equal("JOHN", result[0].Data.FirstName);
        }

        [Fact]
        public void SearchByFullName_WithNoMatches_ShouldReturnEmptyList()
        {
            // Arrange
            var users = new List<User>
            {
                new User 
                { 
                    UserID = 1,
                    Data = new UserData { FirstName = "John", LastName = "Smith" }
                }
            };

            _mockDataService.Setup(x => x.LoadUsers()).Returns(users);

            // Act - Search for non-existent name
            var result = _userService.SearchByFullName("NonExistent", "Name");

            // Assert - Should return empty list, not null
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion
    }
}