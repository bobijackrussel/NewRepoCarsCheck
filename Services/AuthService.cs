using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CarRentalManagment.Models;
using CarRentalManagment.Utilities.Security;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace CarRentalManagment.Services
{
    public class AuthService : IAuthService
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IDatabaseService databaseService, ILogger<AuthService> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        public async Task<User?> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            const string query = @"SELECT id, username, full_name, email, password_hash, created_at
                                     FROM users
                                     WHERE email = @email
                                     LIMIT 1";

            var parameters = new List<MySqlParameter>
            {
                new("@email", email)
            };

            try
            {
                _logger.LogInformation("Attempting to login user with email: {Email}", email);
                var rows = await _databaseService.ExecuteQueryAsync(query, parameters, cancellationToken).ConfigureAwait(false);
                var record = rows.FirstOrDefault();

                if (record == null)
                {
                    _logger.LogWarning("No user found with email: {Email}", email);
                    return null;
                }

                _logger.LogInformation("User found in database: {Email}", email);

                if (!record.TryGetValue("password_hash", out var passwordHashObj) || passwordHashObj is DBNull)
                {
                    _logger.LogWarning("User record missing password hash for {Email}", email);
                    return null;
                }

                var storedHash = Convert.ToString(passwordHashObj) ?? string.Empty;
                _logger.LogInformation("Verifying password for user: {Email}", email);
                _logger.LogDebug("Stored hash length: {Length} characters", storedHash.Length);
                _logger.LogDebug("Stored hash format check - contains colon: {HasColon}", storedHash.Contains(':'));

                if (storedHash.Contains(':'))
                {
                    var parts = storedHash.Split(':');
                    _logger.LogDebug("Hash parts - Salt length: {SaltLen}, Key length: {KeyLen}", parts[0].Length, parts.Length > 1 ? parts[1].Length : 0);
                }
                else
                {
                    _logger.LogWarning("Password hash for {Email} is malformed - missing colon separator", email);
                }

                _logger.LogDebug("Input password length: {Length} characters", password.Length);

                bool verificationResult = PasswordHasher.VerifyPassword(password, storedHash);
                _logger.LogInformation("Password verification result for {Email}: {Result}", email, verificationResult);

                if (!verificationResult)
                {
                    _logger.LogWarning("Password verification failed for {Email}", email);
                    return null;
                }

                _logger.LogInformation("Login successful for {Email}", email);
                return MapUser(record);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for {Email}", email);
                throw;
            }
        }

        public async Task<bool> SignUpAsync(User user, string password, CancellationToken cancellationToken = default)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password must not be empty", nameof(password));
            }

            if (await EmailExistsAsync(user.Email, cancellationToken).ConfigureAwait(false))
            {
                return false;
            }

            const string command = @"INSERT INTO users (username, full_name, email, password_hash, created_at)
                                     VALUES (@username, @fullName, @email, @passwordHash, @createdAt)";

            var passwordHash = PasswordHasher.HashPassword(password);
            _logger.LogInformation("Generated password hash for {Email}", user.Email);
            _logger.LogDebug("Hash length: {Length} characters, Contains colon: {HasColon}",
                passwordHash.Length, passwordHash.Contains(':'));

            // Use email as username (extract the part before @ for uniqueness)
            var username = user.Email.Split('@')[0];
            // Combine first and last name into full_name
            var fullName = $"{user.FirstName} {user.LastName}".Trim();

            var parameters = new List<MySqlParameter>
            {
                new("@username", username),
                new("@fullName", fullName),
                new("@email", user.Email),
                new("@passwordHash", passwordHash),
                new("@createdAt", DateTime.UtcNow)
            };

            try
            {
                var rowsAffected = await _databaseService.ExecuteNonQueryAsync(command, parameters, cancellationToken).ConfigureAwait(false);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sign up user {Email}", user.Email);
                throw;
            }
        }

        public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            const string query = "SELECT COUNT(1) FROM users WHERE email = @email";
            var parameters = new List<MySqlParameter>
            {
                new("@email", email)
            };

            var count = await _databaseService.ExecuteScalarAsync<long>(query, parameters, cancellationToken).ConfigureAwait(false);
            return count > 0;
        }

        private static User MapUser(IDictionary<string, object> record)
        {
            var user = new User();

            if (record.TryGetValue("id", out var idObj) && idObj is not DBNull)
            {
                user.Id = Convert.ToInt32(idObj);
            }

            if (record.TryGetValue("username", out var usernameObj) && usernameObj is not DBNull)
            {
                user.Username = Convert.ToString(usernameObj) ?? string.Empty;
            }

            // Parse full_name into FirstName and LastName
            if (record.TryGetValue("full_name", out var fullNameObj) && fullNameObj is not DBNull)
            {
                var fullName = Convert.ToString(fullNameObj) ?? string.Empty;
                var nameParts = fullName.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

                if (nameParts.Length > 0)
                {
                    user.FirstName = nameParts[0];
                }

                if (nameParts.Length > 1)
                {
                    user.LastName = nameParts[1];
                }
            }

            if (record.TryGetValue("email", out var emailObj) && emailObj is not DBNull)
            {
                user.Email = Convert.ToString(emailObj) ?? string.Empty;
            }

            if (record.TryGetValue("created_at", out var createdAtObj) && createdAtObj is not DBNull)
            {
                user.CreatedAt = Convert.ToDateTime(createdAtObj);
            }

            return user;
        }
    }
}
