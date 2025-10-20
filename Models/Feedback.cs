using System;
using System.ComponentModel.DataAnnotations;

namespace CarRentalManagment.Models
{
    public class Feedback : BaseModel
    {
        private long _id;
        private long _userId;
        private long? _reservationId;
        private byte _rating;
        private string? _comment;
        private DateTime _createdAt;

        [Range(0, long.MaxValue)]
        public long Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        [Range(1, long.MaxValue)]
        public long UserId
        {
            get => _userId;
            set => SetProperty(ref _userId, value);
        }

        public long? ReservationId
        {
            get => _reservationId;
            set => SetProperty(ref _reservationId, value);
        }

        [Range(1, 5)]
        public byte Rating
        {
            get => _rating;
            set => SetProperty(ref _rating, value);
        }

        [StringLength(1000)]
        public string? Comment
        {
            get => _comment;
            set => SetProperty(ref _comment, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }
    }
}
