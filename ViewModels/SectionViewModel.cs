namespace CarRentalManagment.ViewModels
{
    public abstract class SectionViewModel : BaseViewModel
    {
        private string _title = string.Empty;
        private string _description = string.Empty;

        protected SectionViewModel(string title, string description)
        {
            _title = title;
            _description = description;
        }

        public string Title
        {
            get => _title;
            protected set => SetProperty(ref _title, value);
        }

        public string Description
        {
            get => _description;
            protected set => SetProperty(ref _description, value);
        }
    }
}
