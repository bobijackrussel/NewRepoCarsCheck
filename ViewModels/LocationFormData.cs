using System;

namespace CarRentalManagment.ViewModels
{
    public class LocationFormData
    {
        public Guid? Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Subtitle { get; init; } = string.Empty;

        public int FleetCount { get; init; }

        public int UtilizationPercent { get; init; }

        public bool IsActive { get; init; }
    }
}
