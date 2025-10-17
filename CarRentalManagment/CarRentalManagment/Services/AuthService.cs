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

            const string query = @"SELECT id, first_name, last_name, email, password_hash, created_at
                                     FROM users
                                     WHERE email = @email
                                     LIMIT 1";

            var parameters = new List<MySqlParameter>
            {
                new("@email", email)
            };

            try
            {
                var rows = await _databaseService.ExecuteQueryAsync(query, parameters, cancellationToken).ConfigureAwait(false);
                var record = rows.FirstOrDefault();
                if (record == null)
                {
                    return null;
                }

                if (!record.TryGetValue("password_hash", out var passwordHashObj) || passwordHashObj is DBNull)
                {
                    _logger.LogWarning("User record missing password hash for {Email}", email);
                    return null;
                }

                var storedHash = Convert.ToString(passwordHashObj) ?? string.Empty;
                if (!PasswordHasher.VerifyPassword(password, storedHash))
                {
                    return null;
                }

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

            const string command = @"INSERT INTO users (first_name, last_name, email, password_hash, created_at)
                                     VALUES (@firstName, @lastName, @email, @passwordHash, @createdAt)";

            var passwordHash = PasswordHasher.HashPassword(password);
            var parameters = new List<MySqlParameter>
            {
                new("@firstName", user.FirstName),
                new("@lastName", user.LastName),
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

            if (record.TryGetValue("first_name", out var firstNameObj) && firstNameObj is not DBNull)
            {
                user.FirstName = Convert.ToString(firstNameObj) ?? string.Empty;
            }

            if (record.TryGetValue("last_name", out var lastNameObj) && lastNameObj is not DBNull)
            {
                user.LastName = Convert.ToString(lastNameObj) ?? string.Empty;
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
