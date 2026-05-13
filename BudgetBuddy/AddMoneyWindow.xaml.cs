using System.Windows;

namespace BudgetBuddy
{
    public partial class AddMoneyWindow : Window
    {
        public decimal Amount { get; private set; }

        public AddMoneyWindow(decimal availableBalance)
        {
            InitializeComponent();
            AvailableBalanceLabel.Text = $"Available balance: {availableBalance:C}";
        }

        private void AddMoney_Click(object sender, RoutedEventArgs e)
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