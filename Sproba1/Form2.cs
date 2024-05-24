using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace AutoSave
{
    public partial class Form2 : Form
    {
        private List<string> selectedFiles = new List<string>();
        public event Action<List<string>> FilesAdded;

        public Form2()
        {
            InitializeComponent();
            LoadSelectedFiles();
        }

        private async void LoadSelectedFiles()
        {
            string filePath = "selected_files.json";
            if (File.Exists(filePath))
            {
                string json;
                using (var reader = new StreamReader(filePath))
                {
                    json = await reader.ReadToEndAsync();
                }

                selectedFiles = JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
                UpdateListBox();
            }
        }

        private async void SaveSelectedFiles()
        {
            string filePath = "selected_files.json";

            // Save the unique files list to the JSON file
            string json = JsonConvert.SerializeObject(selectedFiles, Formatting.Indented);
            using (var writer = new StreamWriter(filePath))
            {
                await writer.WriteAsync(json);
            }
            Console.WriteLine("Selected files saved to disk.");
        }

        private void label1_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Label 1 clicked.");
        }

        private void label2_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Label 2 clicked.");
        }

        private void buttonGoBack_Click(object sender, EventArgs e)
        {
            if (this.Owner is Form1 form1)
            {
                // Передача списку вибраних файлів до Form1
                form1.SelectedFiles = selectedFiles ?? new List<string>();
                this.Close();
                Console.WriteLine("Go Back button clicked.");
            }
        }

        private void UpdateListBox()
        {
            listBoxFiles.Items.Clear();
            if (selectedFiles != null)
            {
                foreach (string fileName in selectedFiles)
                {
                    listBoxFiles.Items.Add(fileName);
                }
            }
        }

        private void buttonAddFiles_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                foreach (string fileName in openFileDialog.FileNames)
                {
                    if (!selectedFiles.Contains(fileName))
                    {
                        selectedFiles.Add(fileName);
                    }
                }

                UpdateListBox();
                SaveSelectedFiles();

                FilesAdded?.Invoke(selectedFiles.ToList());

                Console.WriteLine("Files added: " + string.Join(", ", openFileDialog.FileNames));
            }
        }

        private void buttonRemoveFiles_Click(object sender, EventArgs e)
        {
            if (listBoxFiles.SelectedItems.Count > 0)
            {
                // Create a list to store the selected items for removal
                List<string> itemsToRemove = new List<string>();

                // Iterate over selected items and add them to the removal list
                foreach (var selectedItem in listBoxFiles.SelectedItems)
                {
                    itemsToRemove.Add(selectedItem.ToString());
                }

                // Remove each item from the selectedFiles list
                foreach (var item in itemsToRemove)
                {
                    selectedFiles.Remove(item);
                }

                // Update the list box to reflect the changes
                UpdateListBox();

                // Save the updated list to the JSON file
                SaveSelectedFiles();

                Console.WriteLine("Selected files updated after removal.");
            }
            else
            {
                MessageBox.Show("Please select files to remove.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Console.WriteLine("Attempted to remove files but none were selected.");
            }
        }
    }
}
