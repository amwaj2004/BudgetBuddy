using System;

namespace BudgetBuddy
{
    // Base class
    public class Transaction
    {
        public string Title { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
    }

    // Expense inherits from Transaction
    public class Expense : Transaction
    {
        public string Category { get; set; } = "";
        public string AmountDisplay => $"-{Amount:C}";
        public string AmountColor => "#C97070";
        public string Meta => $"{Date:MMM d} · {Category}";
    }

    // Income inherits from Transaction
    public class Income : Transaction
    {
        public string Source { get; set; } = "";
        public string AmountDisplay => $"+{Amount:C}";
        public string AmountColor => "#6BA580";
        public string Meta => $"{Date:MMM d} · Income";
    }

    // SavingsGoal inherits from Transaction
    public class SavingsGoal : Transaction
    {
        public decimal TargetAmount { get; set; }
        public decimal InitialAmount { get; set; } = 0;  // starting amount, doesn't affect balance
        public decimal AllocatedAmount { get; set; } = 0; // money added from balance
        public decimal SavedAmount => InitialAmount + AllocatedAmount;
        public double Percentage => TargetAmount == 0 ? 0
            : (double)(SavedAmount / TargetAmount) * 100;
        public string ProgressLabel => $"{SavedAmount:C} / {TargetAmount:C}";
    }
    public class SaveData
    {
        public List<Expense> Expenses { get; set; } = new List<Expense>();
        public List<Income> Incomes { get; set; } = new List<Income>();
        public List<SavingsGoal> Goals { get; set; } = new List<SavingsGoal>();
        public List<string> Categories { get; set; } = new List<string>();
        public decimal TotalSavings { get; set; } = 0;
        public string CurrentMonth { get; set; } = "";
        public string CurrentYear { get; set; } = "";
    }
}