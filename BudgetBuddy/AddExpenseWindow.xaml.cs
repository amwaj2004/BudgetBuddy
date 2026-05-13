using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BudgetBuddy
{
    /// <summary>
    /// Interaction logic for AddExpenseWindow.xaml
    /// </summary>
    public partial class AddExpenseWindow : Window
    {
        public string ExpenseTitle { get; private set; } = "";
        public string Category { get; private set; } = "";
        public decimal Amount { get; private set; }
        public AddExpenseWindow(List<string> categories)
        {
            InitializeComponent();

            CategoryInput.Items.Clear();
            foreach (string category in categories)
            {
                CategoryInput.Items.Add(category);
            }
        }

        private void SaveExpense_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleInput.Text))
            {
                MessageBox.Show("Please enter a valid title.", "Missing Info");
                return;
            }
            if (string.IsNullOrWhiteSpace(CategoryInput.Text))
            {
                MessageBox.Show("Please select or type a category.", "Missing Info");
                return;
            }
            if (!decimal.TryParse(AmountInput.Text, System.Globalization.NumberStyles.Currency, System.Globalization.CultureInfo.GetCultureInfo("en-US"), out decimal amount) || amount <= 0) 
            {
                MessageBox.Show("Please enter a valid amount.", "Missing Info");
                return;
            }

            ExpenseTitle = TitleInput.Text.Trim();
            Category = CategoryInput.Text.Trim();
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
