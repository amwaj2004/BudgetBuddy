using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json;
using System.IO;

namespace BudgetBuddy
{
    public partial class MainWindow : Window
    {
        private List<Expense> _expenses = new List<Expense>();
        private List<Income> _incomes = new List<Income>();
        private List<SavingsGoal> _goals = new List<SavingsGoal>();
        private List<string> _categories = new List<string>
            {
                "Housing", "Food", "Transport", "Health", "Entertainment"
            };
        private decimal _totalSavings = 0;

        // for simplicity, we'll save data in a single JSON file in Documents\BudgetBuddy\savedata.json
        private readonly string _saveFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BudgetBuddy");
        private readonly string _saveFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "BudgetBuddy", "savedata.json");


        public MainWindow()
        {
            InitializeComponent();
            YearPicker.Text = DateTime.Now.Year.ToString();
            LoadData(); // ← load saved data on startup
        }

        private void UpdateSummaryCards()
        {
            decimal totalSpent = _expenses.Any() ? _expenses.Sum(e => e.Amount) : 0;
            decimal totalIncome = _incomes.Any() ? _incomes.Sum(i => i.Amount) : 0;
            decimal totalAllocated = _goals.Any() ? _goals.Sum(g => g.AllocatedAmount) : 0; // only deduct allocated
            decimal balance = totalIncome - totalSpent - totalAllocated;

            SpentLabel.Text = totalSpent.ToString("C");
            SpentSubLabel.Text = $"from {_expenses.Count} expense(s)";

            IncomeDisplay.Text = totalIncome.ToString("C");

            BalanceLabel.Text = balance.ToString("C");
            BalanceChangeLabel.Text = balance >= 0 ? "You are within budget" : "You are over budget";

            BalanceLabel.Foreground = balance >= 0
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6BA580"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C97070"));
        }
        private void AddExpense_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MonthYearLabel.Text))
            {
                MessageBox.Show("Please select a month and year first.", "Missing Info");
                return;
            }

            var dialog = new AddExpenseWindow(_categories);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                if (!_categories.Contains(dialog.Category))
                    _categories.Add(dialog.Category);

                var expense = new Expense
                {
                    Title = dialog.ExpenseTitle,
                    Category = dialog.Category,
                    Amount = dialog.Amount,
                    Date = DateTime.Now
                };

                _expenses.Add(expense);
                AddExpenseToPanel(expense);
                UpdateBreakdown();
                UpdateSummaryCards();
                SaveData(); // ← save data after editing expense   
            }
        }

        private void AddExpenseToPanel(Expense expense)
        {
            Grid row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Border iconBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3E5EE")),
                CornerRadius = new CornerRadius(10),
                Width = 36,
                Height = 36
            };
            iconBorder.Child = new TextBlock
            {
                Text = "💸",
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(iconBorder, 0);

            StackPanel info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            info.Children.Add(new TextBlock
            {
                Text = expense.Title,
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3D2534"))
            });
            info.Children.Add(new TextBlock
            {
                Text = expense.Meta,
                FontSize = 10,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B09AA8"))
            });
            Grid.SetColumn(info, 2);

            TextBlock amount = new TextBlock
            {
                Text = expense.AmountDisplay,
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(expense.AmountColor)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(amount, 3);

            Button editBtn = new Button
            {
                Content = "Edit",
                FontSize = 11,
                Padding = new Thickness(10, 4, 10, 4),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D3D3D3")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333")),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            editBtn.Click += (s, e) => EditExpense(expense, row);
            Grid.SetColumn(editBtn, 5);

            Button deleteBtn = new Button
            {
                Content = "Delete",
                FontSize = 11,
                Padding = new Thickness(10, 4, 10, 4),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C97070")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            deleteBtn.Click += (s, e) => DeleteExpense(expense, row);
            Grid.SetColumn(deleteBtn, 7);

            row.Children.Add(iconBorder);
            row.Children.Add(info);
            row.Children.Add(amount);
            row.Children.Add(editBtn);
            row.Children.Add(deleteBtn);

            EntriesPanel.Children.Insert(0, row);
        }

        private void UpdateBreakdown()
        {
            BreakdownPanel.Children.Clear();

            var categoryTotals = _expenses
                .GroupBy(e => e.Category)
                .Select(g => new { Category = g.Key, Total = (double)g.Sum(e => e.Amount) })
                .OrderByDescending(c => c.Total)
                .ToList();

            if (categoryTotals.Count == 0) return;

            double max = categoryTotals.Max(c => c.Total);

            foreach (var cat in categoryTotals)
            {
                Grid row = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                row.Children.Add(new TextBlock
                {
                    Text = cat.Category,
                    FontSize = 12,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5C3458")),
                    HorizontalAlignment = HorizontalAlignment.Left
                });
                row.Children.Add(new TextBlock
                {
                    Text = $"${cat.Total:F2}",
                    FontSize = 11,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B09AA8")),
                    HorizontalAlignment = HorizontalAlignment.Right
                });
                BreakdownPanel.Children.Add(row);

                Border track = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3E5EE")),
                    CornerRadius = new CornerRadius(99),
                    Height = 6,
                    Margin = new Thickness(0, 4, 0, 8)
                };
                Border fill = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C9A0BC")),
                    CornerRadius = new CornerRadius(99),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = (cat.Total / max) * 200
                };
                track.Child = fill;
                BreakdownPanel.Children.Add(track);
            }

            TotalSpentText.Text = $"${categoryTotals.Sum(c => c.Total):F2}";
        }

        private void AddIncome_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MonthYearLabel.Text))
            {
                MessageBox.Show("Please select a month and year first.", "Missing Info");
                return;
            }

            if (decimal.TryParse(IncomeInput.Text, System.Globalization.NumberStyles.Currency,
                System.Globalization.CultureInfo.GetCultureInfo("en-US"), out decimal result))
            {
                _incomes.Clear();

                var income = new Income
                {
                    Title = "Income",
                    Source = "Manual Entry",
                    Amount = result,
                    Date = DateTime.Now
                };
                _incomes.Add(income);

                IncomeInput.Text = "";
                UpdateSummaryCards();
                SaveData(); // ← save data after editing expense   
            }
            else
            {
                MessageBox.Show("Please enter a valid amount.", "Invalid Input");
            }
        }

        private void AddGoal_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MonthYearLabel.Text))
            {
                MessageBox.Show("Please select a month and year first.", "Missing Info");
                return;
            }

            var dialog = new SavingsGoalWindow();
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                var goal = new SavingsGoal
                {
                    Title = dialog.GoalName,
                    TargetAmount = dialog.TargetAmount,
                    InitialAmount = dialog.SavedAmount, // starting amount only
                    AllocatedAmount = 0,                // nothing deducted from balance yet
                    Date = DateTime.Now
                };

                _goals.Add(goal);
                AddGoalToPanel(goal);
                UpdateSummaryCards();
                SaveData(); // ← save data after editing expense   
            }
        }

        private void AddGoalToPanel(SavingsGoal goal)
        {
            Grid row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Goal name label — stored so EditGoal can update it
            TextBlock nameLabel = new TextBlock
            {
                Text = goal.Title,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3D2534")),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            row.Children.Add(nameLabel);

            // Saved / Target amount label — stored so it can be updated
            TextBlock amounts = new TextBlock
            {
                Text = goal.ProgressLabel,
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B09AA8")),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(amounts, 1);
            row.Children.Add(amounts);

            // Add Money button
            Button addMoneyBtn = new Button
            {
                Content = "+ Add Money",
                FontSize = 11,
                Padding = new Thickness(10, 4, 10, 4),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#756e91")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            addMoneyBtn.Click += (s, e) => AddMoneyToGoal(goal, amounts);
            Grid.SetColumn(addMoneyBtn, 3);
            row.Children.Add(addMoneyBtn);

            // Edit button
            Button editBtn = new Button
            {
                Content = "Edit",
                FontSize = 11,
                Padding = new Thickness(10, 4, 10, 4),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D3D3D3")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333")),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            editBtn.Click += (s, e) => EditGoal(goal, nameLabel, amounts);
            Grid.SetColumn(editBtn, 5);
            row.Children.Add(editBtn);

            // Delete button
            Button deleteBtn = new Button
            {
                Content = "Delete",
                FontSize = 11,
                Padding = new Thickness(10, 4, 10, 4),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C97070")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            deleteBtn.Click += (s, e) => DeleteGoal(goal, row);
            Grid.SetColumn(deleteBtn, 7);
            row.Children.Add(deleteBtn);

            GoalsPanel.Children.Add(row);
        }

        private void AddMoneyToGoal(SavingsGoal goal, TextBlock amountsLabel)
        {
            decimal totalIncome = _incomes.Any() ? _incomes.Sum(i => i.Amount) : 0;
            decimal totalSpent = _expenses.Any() ? _expenses.Sum(e => e.Amount) : 0;
            decimal totalAllocated = _goals.Sum(g => g.SavedAmount);
            decimal availableBalance = totalIncome - totalSpent - totalAllocated;

            if (availableBalance <= 0)
            {
                MessageBox.Show("You don't have enough balance to add money to this goal.", "Insufficient Balance");
                return;
            }

            var dialog = new AddMoneyWindow(availableBalance);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                decimal amount = dialog.Amount;

                if (amount > availableBalance)
                {
                    MessageBox.Show($"You only have {availableBalance:C} available.", "Insufficient Balance");
                    return;
                }
                if (goal.SavedAmount + amount > goal.TargetAmount)
                {
                    MessageBox.Show($"This would exceed your goal target of {goal.TargetAmount:C}.", "Exceeds Target");
                    return;
                }

                goal.AllocatedAmount += amount;
                amountsLabel.Text = goal.ProgressLabel;

                if (goal.SavedAmount >= goal.TargetAmount)
                {
                    MessageBox.Show($"🎉 Congratulations! You have reached your '{goal.Title}' goal!", "Goal Reached!");
                }

                UpdateSummaryCards();
                SaveData();
            }
        }
        private void EditGoal(SavingsGoal goal, TextBlock nameLabel, TextBlock amountsLabel)
        {
            var dialog = new SavingsGoalWindow();
            dialog.Owner = this;

            dialog.GoalNameInput.Text = goal.Title;
            dialog.TargetAmountInput.Text = goal.TargetAmount.ToString();

            // Show allocated amount only — this is what they can edit
            dialog.SavedAmountInput.Text = goal.AllocatedAmount.ToString();

            // Lock the label so they know what it means
            // Find the label above SavedAmountInput and update it
            dialog.SavedAmountLabel.Text = "Allocated Amount ($) — editable";

            if (dialog.ShowDialog() == true)
            {
                // Validate new allocated amount
                if (dialog.SavedAmount > goal.AllocatedAmount)
                {
                    MessageBox.Show("You can only reduce the allocated amount here. Use '+ Add Money' to add more.", "Invalid Input");
                    return;
                }

                // Only update name, target and allocated amount
                goal.Title = dialog.GoalName;
                goal.TargetAmount = dialog.TargetAmount;
                goal.AllocatedAmount = dialog.SavedAmount;

                nameLabel.Text = goal.Title;
                amountsLabel.Text = goal.ProgressLabel;

                if (goal.SavedAmount >= goal.TargetAmount)
                {
                    MessageBox.Show($"🎉 Congratulations! You have reached your '{goal.Title}' goal!", "Goal Reached!");
                }

                UpdateSummaryCards();
                SaveData(); // ← save data after editing expense   
            }
        }
        private void DeleteGoal(SavingsGoal goal, Grid row)
        {
            _goals.Remove(goal);
            GoalsPanel.Children.Remove(row);
            UpdateSummaryCards();
            SaveData(); // ← save data after editing expense   
        }

        private void DeleteExpense(Expense expense, Grid row)
        {
            _expenses.Remove(expense);
            EntriesPanel.Children.Remove(row);

            if (_expenses.Count == 0)
            {
                SpentLabel.Text = "$0.00";
                SpentSubLabel.Text = "from 0 expense(s)";
                BreakdownPanel.Children.Clear();
                TotalSpentText.Text = "$0.00";
            }

            UpdateBreakdown();
            UpdateSummaryCards();
            SaveData(); // ← save data after editing expense   
        }

        private void EditExpense(Expense expense, Grid row)
        {
            var dialog = new AddExpenseWindow(_categories);
            dialog.Owner = this;

            dialog.TitleInput.Text = expense.Title;
            dialog.AmountInput.Text = expense.Amount.ToString();

            if (dialog.ShowDialog() == true)
            {
                if (!_categories.Contains(dialog.Category))
                    _categories.Add(dialog.Category);

                expense.Title = dialog.ExpenseTitle;
                expense.Category = dialog.Category;
                expense.Amount = dialog.Amount;

                EntriesPanel.Children.Remove(row);
                AddExpenseToPanel(expense);
                UpdateBreakdown();
                UpdateSummaryCards();
                SaveData(); // ← save data after editing expense    
            }
        }
        private void pickMonth_Click(object sender, RoutedEventArgs e)
        {
            if (MonthPicker.SelectedItem == null)
            {
                MessageBox.Show("Please select a month.", "Missing Info");
                return;
            }
            if (!int.TryParse(YearPicker.Text, out int year) || year < 2000 || year > 2100)
            {
                MessageBox.Show("Please enter a valid year.", "Missing Info");
                return;
            }

            string month = (MonthPicker.SelectedItem as ComboBoxItem)?.Content.ToString()!;
            MonthYearLabel.Text = $"{month} {YearPicker.Text}";
            SaveData(); // ← save data after editing expense   
        }


        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MonthYearLabel.Text))
            {
                MessageBox.Show("Please select a month first.", "Missing Info");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to complete this month?\nYour leftover balance will be added to savings.",
                "Complete This Month", MessageBoxButton.YesNo);

            if (result != MessageBoxResult.Yes) return;

            // Calculate leftover balance
            decimal totalSpent = _expenses.Any() ? _expenses.Sum(ex => ex.Amount) : 0;
            decimal totalIncome = _incomes.Any() ? _incomes.Sum(i => i.Amount) : 0;
            decimal totalAllocated = _goals.Any() ? _goals.Sum(g => g.AllocatedAmount) : 0;
            decimal leftover = totalIncome - totalSpent - totalAllocated;

            // Save summary to txt file
            SaveMonthSummary(leftover);

            // Add leftover to savings
            if (leftover > 0)
                _totalSavings += leftover;

            // Update savings label
            SavingsLabel.Text = _totalSavings.ToString("C");
            SavingsSubLabel.Text = "accumulated from previous months";

            // Clear expenses and income for new month
            _expenses.Clear();
            _incomes.Clear();
            EntriesPanel.Children.Clear();
            BreakdownPanel.Children.Clear();
            TotalSpentText.Text = "";

            // Move to next month automatically
            string[] months = { "January", "February", "March", "April", "May", "June",
                        "July", "August", "September", "October", "November", "December" };

            if (MonthPicker.SelectedItem != null)
            {
                string currentMonthName = (MonthPicker.SelectedItem as ComboBoxItem)?.Content.ToString()!;
                int monthIndex = Array.IndexOf(months, currentMonthName);

                if (monthIndex == 11)
                {
                    MonthPicker.SelectedIndex = 0;
                    YearPicker.Text = (int.Parse(YearPicker.Text) + 1).ToString();
                }
                else
                {
                    MonthPicker.SelectedIndex = monthIndex + 1;
                }

                string newMonth = (MonthPicker.SelectedItem as ComboBoxItem)?.Content.ToString()!;
                MonthYearLabel.Text = $"{newMonth} {YearPicker.Text}";
            }

            // Reset summary cards
            SpentLabel.Text = "$0.00";
            SpentSubLabel.Text = "from 0 expense(s)";
            IncomeDisplay.Text = "Enter Income";
            BalanceLabel.Text = "$0.00";
            BalanceChangeLabel.Text = "";

            MessageBox.Show("Month completed! Your summary has been saved.", "Done");
            SaveData(); // ← save data after completing month
        }

        private void SaveMonthSummary(decimal leftover)
        {
            try
            {
                string month = MonthYearLabel.Text;
                string fileName = $"Summary_{month.Replace(" ", "_")}.txt";
                string folderPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "BudgetBuddy");

                System.IO.Directory.CreateDirectory(folderPath);
                string filePath = System.IO.Path.Combine(folderPath, fileName);

                decimal totalIncome = _incomes.Any() ? _incomes.Sum(i => i.Amount) : 0;
                decimal totalSpent = _expenses.Any() ? _expenses.Sum(e => e.Amount) : 0;
                decimal totalAllocated = _goals.Any() ? _goals.Sum(g => g.AllocatedAmount) : 0;

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine($"===== Budget Buddy Summary — {month} =====");
                sb.AppendLine($"Date Generated: {DateTime.Now:MMMM d, yyyy}");
                sb.AppendLine();
                sb.AppendLine("--- Income ---");
                sb.AppendLine($"Total Income: {totalIncome:C}");
                sb.AppendLine();
                sb.AppendLine("--- Expenses ---");

                foreach (var expense in _expenses)
                    sb.AppendLine($"  {expense.Title} ({expense.Category}): {expense.AmountDisplay}  [{expense.Meta}]");

                sb.AppendLine($"Total Spent: {totalSpent:C}");
                sb.AppendLine();
                sb.AppendLine("--- Savings Goals ---");

                foreach (var goal in _goals)
                    sb.AppendLine($"  {goal.Title}: {goal.ProgressLabel}");

                sb.AppendLine($"Total Allocated to Goals: {totalAllocated:C}");
                sb.AppendLine();
                sb.AppendLine("--- Summary ---");
                sb.AppendLine($"Leftover Balance: {leftover:C}");
                sb.AppendLine($"Added to Savings: {(leftover > 0 ? leftover.ToString("C") : "$0.00")}");
                sb.AppendLine($"Total Accumulated Savings: {(_totalSavings + (leftover > 0 ? leftover : 0)):C}");

                System.IO.File.WriteAllText(filePath, sb.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save summary: {ex.Message}", "Error");
            }
          
        }

        private void ViewSummary_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "BudgetBuddy");

            if (!System.IO.Directory.Exists(folderPath))
            {
                MessageBox.Show("No summaries found yet. Complete a month first!", "No Summaries");
                return;
            }

            string[] files = System.IO.Directory.GetFiles(folderPath, "*.txt");

            if (files.Length == 0)
            {
                MessageBox.Show("No summaries found yet. Complete a month first!", "No Summaries");
                return;
            }

            var dialog = new ViewSummaryWindow(files);
            dialog.Owner = this;
            dialog.ShowDialog();
        }


        private async void SaveData()
        {
            try
            {
                await Task.Run(() =>
                {
                    Directory.CreateDirectory(_saveFolder);

                    var data = new SaveData
                    {
                        Expenses = _expenses,
                        Incomes = _incomes,
                        Goals = _goals,
                        Categories = _categories,
                        TotalSavings = _totalSavings,
                        CurrentMonth = (MonthPicker.Dispatcher.Invoke(() =>
                            (MonthPicker.SelectedItem as ComboBoxItem)?.Content.ToString()) ?? ""),
                        CurrentYear = Dispatcher.Invoke(() => YearPicker.Text)
                    };

                    string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                    File.WriteAllText(_saveFile, json);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save data: {ex.Message}", "Error");
            }
        }

        private async void LoadData()
        {
            try
            {
                if (!File.Exists(_saveFile)) return;

                await Task.Run(() =>
                {
                    string json = File.ReadAllText(_saveFile);
                    var data = JsonConvert.DeserializeObject<SaveData>(json);

                    if (data == null) return;

                    Dispatcher.Invoke(() =>
                    {
                        _expenses = data.Expenses ?? new List<Expense>();
                        _incomes = data.Incomes ?? new List<Income>();
                        _goals = data.Goals ?? new List<SavingsGoal>();
                        _categories = data.Categories ?? new List<string>();
                        _totalSavings = data.TotalSavings;

                        // Restore month and year
                        if (!string.IsNullOrWhiteSpace(data.CurrentMonth))
                        {
                            string[] months = { "January", "February", "March", "April",
                                       "May", "June", "July", "August", "September",
                                       "October", "November", "December" };
                            int index = Array.IndexOf(months, data.CurrentMonth);
                            if (index >= 0) MonthPicker.SelectedIndex = index;
                            YearPicker.Text = data.CurrentYear;
                            MonthYearLabel.Text = $"{data.CurrentMonth} {data.CurrentYear}";
                        }

                        // Restore savings label
                        SavingsLabel.Text = _totalSavings.ToString("C");

                        // Rebuild expense rows
                        foreach (var expense in _expenses)
                            AddExpenseToPanel(expense);

                        // Rebuild goal rows
                        foreach (var goal in _goals)
                            AddGoalToPanel(goal);

                        // Update cards
                        UpdateBreakdown();
                        UpdateSummaryCards();
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load data: {ex.Message}", "Error");
            }
        }

        private void AddToSavings_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MonthYearLabel.Text))
            {
                MessageBox.Show("Please select a month first.", "Missing Info");
                return;
            }

            decimal totalSpent = _expenses.Any() ? _expenses.Sum(ex => ex.Amount) : 0;
            decimal totalIncome = _incomes.Any() ? _incomes.Sum(i => i.Amount) : 0;
            decimal totalAllocated = _goals.Any() ? _goals.Sum(g => g.AllocatedAmount) : 0;
            decimal availableBalance = totalIncome - totalSpent - totalAllocated;

            if (availableBalance <= 0)
            {
                MessageBox.Show("You don't have enough balance to add to savings.", "Insufficient Balance");
                return;
            }

            var dialog = new SavingsWindow(
                "Add to Savings",
                $"Available balance: {availableBalance:C}",
                "Add to Savings",
                "#6BA580");
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                decimal amount = dialog.Amount;

                if (amount > availableBalance)
                {
                    MessageBox.Show($"You only have {availableBalance:C} available.", "Insufficient Balance");
                    return;
                }

                _totalSavings += amount;

                var savingsExpense = new Expense
                {
                    Title = "Savings Deposit",
                    Category = "Savings",
                    Amount = amount,
                    Date = DateTime.Now
                };
                _expenses.Add(savingsExpense);
                AddExpenseToPanel(savingsExpense);

                SavingsLabel.Text = _totalSavings.ToString("C");
                SavingsSubLabel.Text = "accumulated savings";

                UpdateBreakdown();
                UpdateSummaryCards();
                SaveData();
            }
        }

        private void WithdrawSavings_Click(object sender, RoutedEventArgs e)
        {
            if (_totalSavings <= 0)
            {
                MessageBox.Show("You have no savings to withdraw.", "No Savings");
                return;
            }

            var dialog = new SavingsWindow(
                "Withdraw from Savings",
                $"Available savings: {_totalSavings:C}",
                "Withdraw",
                "#C97070");
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                decimal amount = dialog.Amount;

                if (amount > _totalSavings)
                {
                    MessageBox.Show($"You only have {_totalSavings:C} in savings.", "Insufficient Savings");
                    return;
                }

                _totalSavings -= amount;

                var withdrawalIncome = new Income
                {
                    Title = "Savings Withdrawal",
                    Source = "Savings",
                    Amount = amount,
                    Date = DateTime.Now
                };
                _incomes.Add(withdrawalIncome);

                SavingsLabel.Text = _totalSavings.ToString("C");
                SavingsSubLabel.Text = "accumulated savings";

                UpdateSummaryCards();
                SaveData();

                MessageBox.Show($"Successfully withdrew {amount:C} from savings!", "Withdrawal Complete");
            }
        }
    }
}