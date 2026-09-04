namespace MiniSmartstoreMvc.ViewModels
{
    public class CategoryMenuItemViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Alias { get; set; }

        public int ProductCount { get; set; }

        public List<CategoryMenuItemViewModel> Children { get; set; } = new();
    }
}