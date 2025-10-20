using System;
using System.ComponentModel.DataAnnotations;

namespace CarRentalManagment.Models
{
    public class VehiclePhoto : BaseModel
    {
        private long _id;
        private long _vehicleId;
        private string _photoUrl = string.Empty;
        private string? _caption;
        private bool _isPrimary;
        private DateTime _createdAt;

        [Range(0, long.MaxValue)]
        public long Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        [Range(1, long.MaxValue)]
        public long VehicleId
        {
            get => _vehicleId;
            set => SetProperty(ref _vehicleId, value);
        }

        [Required]
        [Url]
        [StringLength(500)]
        public string PhotoUrl
        {
            get => _photoUrl;
            set => SetProperty(ref _photoUrl, value);
        }

        [StringLength(140)]
        public string? Caption
        {
            get => _caption;
            set => SetProperty(ref _caption, value);
        }

        public bool IsPrimary
        {
            get => _isPrimary;
            set => SetProperty(ref _isPrimary, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }
    }
}
