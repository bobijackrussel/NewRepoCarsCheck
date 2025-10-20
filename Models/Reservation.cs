using System;
using System.ComponentModel.DataAnnotations;

namespace CarRentalManagment.Models
{
    public class Reservation : BaseModel
    {
        private long _id;
        private long _userId;
        private long _vehicleId;
        private DateTime _startDate;
        private DateTime _endDate;
        private ReservationStatus _status = ReservationStatus.Pending;
        private decimal _totalAmount;
        private string? _notes;
        private DateTime _createdAt;
        private DateTime _updatedAt;
        private DateTime? _cancelledAt;
        private string? _cancellationReason;

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

        [Range(1, long.MaxValue)]
        public long VehicleId
        {
            get => _vehicleId;
            set => SetProperty(ref _vehicleId, value);
        }

        [Required]
        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        [Required]
        public DateTime EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        [Required]
        public ReservationStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        [Range(0, double.MaxValue)]
        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        [StringLength(500)]
        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set => SetProperty(ref _updatedAt, value);
        }

        public DateTime? CancelledAt
        {
            get => _cancelledAt;
            set => SetProperty(ref _cancelledAt, value);
        }

        [StringLength(255)]
        public string? CancellationReason
        {
            get => _cancellationReason;
            set => SetProperty(ref _cancellationReason, value);
        }
    }

    public enum ReservationStatus
    {
        Pending,
        Confirmed,
        Cancelled,
        Completed
    }
}
