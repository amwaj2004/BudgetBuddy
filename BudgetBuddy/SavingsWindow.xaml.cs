using System.Windows;

namespace BudgetBuddy
{
    public partial class SavingsWindow : Window
    {
        public decimal Amount { get; private set; }

        public SavingsWindow(string title, string availableLabel, string confirmButtonText, string confirmButtonColor)
        {
            InitializeComponent();

            TitleLabel.Text = title;
            AvailableLabel.Text = availableLabel;
            ConfirmBtn.Content = confirmButtonText;
            ConfirmBtn.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(confirmButtonColor));
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(AmountInput.Text,
                System.Globalization.NumberStyles.Currency,
                System.Globalization.CultureInfo.GetCultureInfo("en-US"),
                out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Please enter a valid amount.", "Invalid Input");
                return;
            }

            Amount = amount;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}