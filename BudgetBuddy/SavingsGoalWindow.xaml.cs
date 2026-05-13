using System;
using System.Windows;

namespace BudgetBuddy
{
    public partial class SavingsGoalWindow : Window
    {
        public string GoalName { get; private set; } = "";
        public decimal TargetAmount { get; private set; }
        public decimal SavedAmount { get; private set; }

        public SavingsGoalWindow()
        {
            InitializeComponent();
        }

        private void SaveGoal_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(GoalNameInput.Text))
            {
                MessageBox.Show("Please enter a goal name.", "Missing Info");
                return;
            }
            if (!decimal.TryParse(TargetAmountInput.Text, System.Globalization.NumberStyles.Currency,
                System.Globalization.CultureInfo.GetCultureInfo("en-US"), out decimal target) || target <= 0)
            {
                MessageBox.Show("Please enter a valid target amount.", "Missing Info");
                return;
            }
            if (!decimal.TryParse(SavedAmountInput.Text, System.Globalization.NumberStyles.Currency,
                System.Globalization.CultureInfo.GetCultureInfo("en-US"), out decimal saved))
            {
                saved = 0;
            }
            if (saved > target)
            {
                MessageBox.Show("Amount saved cannot exceed target amount.", "Invalid Input");
                return;
            }

            GoalName = GoalNameInput.Text.Trim();
            TargetAmount = target;
            SavedAmount = saved;

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