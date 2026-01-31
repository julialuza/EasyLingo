using System.Windows.Controls;

namespace EasyLingo.Views
{
    public partial class SetsView : UserControl
    {
        public SetsView()
        {
            InitializeComponent();
        }

        private void OnSetRightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;

            var btn = sender as System.Windows.Controls.Button;
            if (btn?.DataContext == null) return;

            var setIdProp = btn.DataContext.GetType().GetProperty("SetId");
            if (setIdProp == null) return;

            var setIdObj = setIdProp.GetValue(btn.DataContext, null);
            int setId;
            if (setIdObj == null || !int.TryParse(setIdObj.ToString(), out setId)) return;

            var itemsControl = FindAncestor<System.Windows.Controls.ItemsControl>(btn);
            var vm = itemsControl?.DataContext as EasyLingo.ViewModels.SetsViewModel;
            if (vm == null) return;

            if (vm.SelectSetCommand.CanExecute(setId))
                vm.SelectSetCommand.Execute(setId);
        }

        private T? FindAncestor<T>(System.Windows.DependencyObject current) where T : System.Windows.DependencyObject
        {
            while (current != null)
            {
                if (current is T match) return match;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return null;
        }

    }
}
