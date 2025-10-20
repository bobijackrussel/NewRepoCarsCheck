using System;
using System.ComponentModel.DataAnnotations;

namespace CarRentalManagment.Models
{
    public class Vehicle : BaseModel
    {
        private long _id;
        private long? _branchId;
        private string? _vin;
        private string _plateNumber = string.Empty;
        private string _make = string.Empty;
        private string _model = string.Empty;
        private short _modelYear;
        private VehicleCategory _category;
        private TransmissionType _transmission;
        private FuelType _fuel;
        private byte _seats;
        private byte _doors;
        private string? _color;
        private string? _description;
        private decimal _dailyRate;
        private VehicleStatus _status = VehicleStatus.Active;
        private DateTime _createdAt;
        private DateTime _updatedAt;

        [Range(0, long.MaxValue)]
        public long Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public long? BranchId
        {
            get => _branchId;
            set => SetProperty(ref _branchId, value);
        }

        [StringLength(17, MinimumLength = 11)]
        public string? Vin
        {
            get => _vin;
            set => SetProperty(ref _vin, value);
        }

        [Required]
        [StringLength(20)]
        public string PlateNumber
        {
            get => _plateNumber;
            set => SetProperty(ref _plateNumber, value);
        }

        [Required]
        [StringLength(50)]
        public string Make
        {
            get => _make;
            set => SetProperty(ref _make, value);
        }

        [Required]
        [StringLength(50)]
        public string Model
        {
            get => _model;
            set => SetProperty(ref _model, value);
        }

        [Range(1980, 2100)]
        public short ModelYear
        {
            get => _modelYear;
            set => SetProperty(ref _modelYear, value);
        }

        [Required]
        public VehicleCategory Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        [Required]
        public TransmissionType Transmission
        {
            get => _transmission;
            set => SetProperty(ref _transmission, value);
        }

        [Required]
        public FuelType Fuel
        {
            get => _fuel;
            set => SetProperty(ref _fuel, value);
        }

        [Range(1, 12)]
        public byte Seats
        {
            get => _seats;
            set => SetProperty(ref _seats, value);
        }

        [Range(1, 6)]
        public byte Doors
        {
            get => _doors;
            set => SetProperty(ref _doors, value);
        }

        [StringLength(30)]
        public string? Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        public string? Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        [Range(0, double.MaxValue)]
        public decimal DailyRate
        {
            get => _dailyRate;
            set => SetProperty(ref _dailyRate, value);
        }

        [Required]
        public VehicleStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
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
    }

    public enum VehicleCategory
    {
        Economy,
        Compact,
        Midsize,
        Suv,
        Luxury,
        Van,
        Truck
    }

    public enum TransmissionType
    {
        Manual,
        Automatic
    }

    public enum FuelType
    {
        Gasoline,
        Diesel,
        Hybrid,
        Electric
    }

    public enum VehicleStatus
    {
        Active,
        Maintenance,
        Retired
    }
}
