using System;
using System.Collections.Generic;
using CarRentalManagment.Models;

namespace CarRentalManagment.Utilities.SampleData
{
    public static class VehicleSampleFactory
    {
        public static (List<Vehicle> Vehicles, IDictionary<long, VehiclePhoto?> PrimaryPhotos) CreateSample()
        {
            var sampleVehicles = new List<Vehicle>
            {
                new Vehicle
                {
                    Id = 1001,
                    PlateNumber = "BG-320D",
                    Make = "BMW",
                    Model = "320d",
                    ModelYear = 2022,
                    Category = VehicleCategory.Midsize,
                    Transmission = TransmissionType.Automatic,
                    Fuel = FuelType.Diesel,
                    Seats = 5,
                    Doors = 4,
                    Color = "Carbon Black",
                    DailyRate = 75,
                    Status = VehicleStatus.Active,
                    CreatedAt = DateTime.UtcNow.AddDays(-14),
                    UpdatedAt = DateTime.UtcNow.AddDays(-3),
                    Description = "Luxury sedan with M Sport package and adaptive cruise control."
                },
                new Vehicle
                {
                    Id = 1002,
                    PlateNumber = "BG-E200",
                    Make = "Mercedes-Benz",
                    Model = "EQC 400",
                    ModelYear = 2023,
                    Category = VehicleCategory.Suv,
                    Transmission = TransmissionType.Automatic,
                    Fuel = FuelType.Electric,
                    Seats = 5,
                    Doors = 4,
                    Color = "Polar White",
                    DailyRate = 98,
                    Status = VehicleStatus.Active,
                    CreatedAt = DateTime.UtcNow.AddDays(-21),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5),
                    Description = "Premium electric SUV with 380 km range and fast charging."
                },
                new Vehicle
                {
                    Id = 1003,
                    PlateNumber = "NS-45UP",
                    Make = "Skoda",
                    Model = "Octavia",
                    ModelYear = 2021,
                    Category = VehicleCategory.Midsize,
                    Transmission = TransmissionType.Automatic,
                    Fuel = FuelType.Gasoline,
                    Seats = 5,
                    Doors = 5,
                    Color = "Moon White",
                    DailyRate = 52,
                    Status = VehicleStatus.Active,
                    CreatedAt = DateTime.UtcNow.AddDays(-45),
                    UpdatedAt = DateTime.UtcNow.AddDays(-12),
                    Description = "Business favorite with roomy interior and integrated navigation."
                },
                new Vehicle
                {
                    Id = 1004,
                    PlateNumber = "NI-CL35",
                    Make = "Renault",
                    Model = "Clio",
                    ModelYear = 2020,
                    Category = VehicleCategory.Economy,
                    Transmission = TransmissionType.Manual,
                    Fuel = FuelType.Gasoline,
                    Seats = 5,
                    Doors = 5,
                    Color = "Flame Red",
                    DailyRate = 32,
                    Status = VehicleStatus.Active,
                    CreatedAt = DateTime.UtcNow.AddDays(-60),
                    UpdatedAt = DateTime.UtcNow.AddDays(-20),
                    Description = "Agile city hatchback with low consumption and CarPlay support."
                }
            };

            var primaryPhotos = new Dictionary<long, VehiclePhoto?>
            {
                {
                    1001,
                    new VehiclePhoto
                    {
                        Id = 5001,
                        VehicleId = 1001,
                        PhotoUrl = "https://images.pexels.com/photos/210019/pexels-photo-210019.jpeg",
                        Caption = "BMW 320d M Sport",
                        IsPrimary = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-3)
                    }
                },
                {
                    1002,
                    new VehiclePhoto
                    {
                        Id = 5002,
                        VehicleId = 1002,
                        PhotoUrl = "https://images.pexels.com/photos/1402787/pexels-photo-1402787.jpeg",
                        Caption = "Mercedes EQC",
                        IsPrimary = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-5)
                    }
                },
                {
                    1003,
                    new VehiclePhoto
                    {
                        Id = 5003,
                        VehicleId = 1003,
                        PhotoUrl = "https://images.pexels.com/photos/358070/pexels-photo-358070.jpeg",
                        Caption = "Skoda Octavia",
                        IsPrimary = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-12)
                    }
                },
                {
                    1004,
                    new VehiclePhoto
                    {
                        Id = 5004,
                        VehicleId = 1004,
                        PhotoUrl = "https://images.pexels.com/photos/1149831/pexels-photo-1149831.jpeg",
                        Caption = "Renault Clio",
                        IsPrimary = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-20)
                    }
                }
            };

            return (sampleVehicles, primaryPhotos);
        }
    }
}
