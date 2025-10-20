using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using CarRentalManagment;

namespace CarRentalManagment.Utilities
{
    public static class AppServices
    {
        public static T GetRequiredService<T>() where T : notnull
        {
            if (Application.Current is not App app)
            {
                throw new InvalidOperationException("The application has not been initialized.");
            }

            return app.Services.GetRequiredService<T>();
        }
    }
}
