using System;

namespace CarRentalManagment.Models
{
    public class User : BaseModel
    {
        private int _id;
        private string _username = string.Empty;
        private string _firstName = string.Empty;
        private string _lastName = string.Empty;
        private string _email = string.Empty;
        private DateTime _createdAt;

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string FirstName
        {
            get => _firstName;
            set => SetProperty(ref _firstName, value);
        }

        public string LastName
        {
            get => _lastName;
            set => SetProperty(ref _lastName, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        public string FullName => $"{FirstName} {LastName}".Trim();

        public bool IsAdministrator =>
            (!string.IsNullOrWhiteSpace(Username) && Username.Equals("admin", StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(Email) && Email.Contains("admin", StringComparison.OrdinalIgnoreCase));
    }
}
