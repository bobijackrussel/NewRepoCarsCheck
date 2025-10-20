using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CarRentalManagment.Models;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace CarRentalManagment.Services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<FeedbackService> _logger;

        public FeedbackService(IDatabaseService databaseService, ILogger<FeedbackService> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        public async Task<bool> SubmitAsync(Feedback feedback, CancellationToken cancellationToken = default)
        {
            if (feedback == null)
            {
                throw new ArgumentNullException(nameof(feedback));
            }

            const string command = @"INSERT INTO feedbacks (user_id, reservation_id, rating, comment)
                                     VALUES (@userId, @reservationId, @rating, @comment)";

            var parameters = new List<MySqlParameter>
            {
                new("@userId", feedback.UserId),
                new("@reservationId", feedback.ReservationId.HasValue ? feedback.ReservationId.Value : DBNull.Value),
                new("@rating", feedback.Rating),
                new("@comment", string.IsNullOrWhiteSpace(feedback.Comment) ? DBNull.Value : feedback.Comment!)
            };

            try
            {
                var rowsAffected = await _databaseService.ExecuteNonQueryAsync(command, parameters, cancellationToken).ConfigureAwait(false);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to submit feedback for user {UserId}", feedback.UserId);
                throw;
            }
        }

        public async Task<IReadOnlyList<Feedback>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            const string query = @"SELECT id, user_id, reservation_id, rating, comment, created_at
                                  FROM feedbacks
                                  ORDER BY created_at DESC";

            try
            {
                var rows = await _databaseService.ExecuteQueryAsync(query, cancellationToken: cancellationToken).ConfigureAwait(false);
                return rows.Select(MapFeedback).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load feedback entries");
                throw;
            }
        }

        public async Task<IReadOnlyList<Feedback>> GetByUserAsync(long userId, CancellationToken cancellationToken = default)
        {
            const string query = @"SELECT id, user_id, reservation_id, rating, comment, created_at
                                  FROM feedbacks
                                  WHERE user_id = @userId
                                  ORDER BY created_at DESC";

            var parameters = new List<MySqlParameter>
            {
                new("@userId", userId)
            };

            try
            {
                var rows = await _databaseService.ExecuteQueryAsync(query, parameters, cancellationToken).ConfigureAwait(false);
                return rows.Select(MapFeedback).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load feedback entries for user {UserId}", userId);
                throw;
            }
        }

        private static Feedback MapFeedback(IDictionary<string, object> record)
        {
            var feedback = new Feedback();

            if (record.TryGetValue("id", out var idObj) && idObj is not DBNull)
            {
                feedback.Id = Convert.ToInt64(idObj);
            }

            if (record.TryGetValue("user_id", out var userObj) && userObj is not DBNull)
            {
                feedback.UserId = Convert.ToInt64(userObj);
            }

            if (record.TryGetValue("reservation_id", out var reservationObj) && reservationObj is not DBNull)
            {
                feedback.ReservationId = Convert.ToInt64(reservationObj);
            }

            if (record.TryGetValue("rating", out var ratingObj) && ratingObj is not DBNull)
            {
                feedback.Rating = Convert.ToByte(ratingObj);
            }

            if (record.TryGetValue("comment", out var commentObj) && commentObj is not DBNull)
            {
                feedback.Comment = Convert.ToString(commentObj);
            }

            if (record.TryGetValue("created_at", out var createdObj) && createdObj is not DBNull)
            {
                feedback.CreatedAt = Convert.ToDateTime(createdObj);
            }

            return feedback;
        }
    }
}
