using System;
using System.ComponentModel.DataAnnotations;

namespace CarRentalManagment.Models
{
    public class ViolationReport : BaseModel
    {
        private long _id;
        private long _reservationId;
        private long _userId;
        private ViolationType _type = ViolationType.Other;
        private ViolationSeverity _severity = ViolationSeverity.Low;
        private string? _description;
        private ViolationStatus _status = ViolationStatus.Open;
        private DateTime _reportedAt;
        private DateTime? _resolvedAt;
        private string? _resolutionNotes;

        [Range(0, long.MaxValue)]
        public long Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        [Range(1, long.MaxValue)]
        public long ReservationId
        {
            get => _reservationId;
            set => SetProperty(ref _reservationId, value);
        }

        [Range(1, long.MaxValue)]
        public long UserId
        {
            get => _userId;
            set => SetProperty(ref _userId, value);
        }

        [Required]
        public ViolationType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        [Required]
        public ViolationSeverity Severity
        {
            get => _severity;
            set => SetProperty(ref _severity, value);
        }

        [StringLength(1500)]
        public string? Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        [Required]
        public ViolationStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public DateTime ReportedAt
        {
            get => _reportedAt;
            set => SetProperty(ref _reportedAt, value);
        }

        public DateTime? ResolvedAt
        {
            get => _resolvedAt;
            set => SetProperty(ref _resolvedAt, value);
        }

        [StringLength(1000)]
        public string? ResolutionNotes
        {
            get => _resolutionNotes;
            set => SetProperty(ref _resolutionNotes, value);
        }
    }

    public enum ViolationType
    {
        LateReturn,
        Damage,
        Cleanliness,
        Other
    }

    public enum ViolationSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum ViolationStatus
    {
        Open,
        UnderReview,
        Resolved,
        Dismissed
    }
}
