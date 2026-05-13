using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace BudgetBuddy
{
    public partial class ViewSummaryWindow : Window
    {
        private string[] _files;

        public ViewSummaryWindow(string[] files)
        {
            InitializeComponent();
            _files = files;

            foreach (string file in files)
            {
                FileList.Items.Add(Path.GetFileNameWithoutExtension(file));
            }
        }

        private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = FileList.SelectedIndex;
            if (index < 0) return;

            try
            {
                string content = File.ReadAllText(_files[index]);
                FileContent.Text = content;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not read file: {ex.Message}", "Error");
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}