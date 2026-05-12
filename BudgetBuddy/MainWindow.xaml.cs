using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BudgetBuddy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private decimal MoneyIncome = 0; 
        public MainWindow()
        {
             

            InitializeComponent();
        }

        private void AddExpense_Click(object sender, RoutedEventArgs e)
        {


        }

        private void AddGoal_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AddIncome_Click(object sender, RoutedEventArgs e)
        {

            if (decimal.TryParse(IncomeInput.Text, System.Globalization.NumberStyles.Currency, System.Globalization.CultureInfo.GetCultureInfo("en-US"), out decimal result))
            {
                MoneyIncome = result;
                IncomeDisplay.Text = MoneyIncome.ToString();
            }
            else
            {
                IncomeDisplay.Text = "invalid, please try again";

            }
        }

        private void ViewSummary_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}