using System;
using System.ComponentModel.DataAnnotations;

namespace CarRentalManagment.Models
{
    public class Branch : BaseModel
    {
        private long _id;
        private string _name = string.Empty;
        private string? _addressLine;
        private string? _city;
        private string? _stateRegion;
        private string? _postalCode;
        private string? _countryCode;
        private string? _phone;
        private DateTime _createdAt;

        [Range(0, long.MaxValue)]
        public long Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        [Required]
        [StringLength(100)]
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        [StringLength(200)]
        public string? AddressLine
        {
            get => _addressLine;
            set => SetProperty(ref _addressLine, value);
        }

        [StringLength(80)]
        public string? City
        {
            get => _city;
            set => SetProperty(ref _city, value);
        }

        [StringLength(80)]
        public string? StateRegion
        {
            get => _stateRegion;
            set => SetProperty(ref _stateRegion, value);
        }

        [StringLength(20)]
        public string? PostalCode
        {
            get => _postalCode;
            set => SetProperty(ref _postalCode, value);
        }

        [StringLength(2, MinimumLength = 2)]
        public string? CountryCode
        {
            get => _countryCode;
            set => SetProperty(ref _countryCode, value);
        }

        [StringLength(30)]
        public string? Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }
    }
}
