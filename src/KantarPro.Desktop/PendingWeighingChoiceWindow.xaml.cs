using System.Collections.ObjectModel;
using System.Windows;

namespace KantarPro.Desktop
{
    public partial class PendingWeighingChoiceWindow : Window
    {
        public PendingWeighingChoiceWindow(string plaka, ObservableCollection<PendingWeighingChoiceRow> rows)
        {
            PendingRows = rows ?? new ObservableCollection<PendingWeighingChoiceRow>();
            DataContext = this;
            InitializeComponent();
            TitleText.Text = plaka + " plakasina ait tamamlanmamis ilk tartim bulundu.";
            if (PendingRows.Count > 0)
            {
                PendingGrid.SelectedIndex = 0;
            }
        }

        public ObservableCollection<PendingWeighingChoiceRow> PendingRows { get; private set; }
        public PendingWeighingChoiceResult Result { get; private set; }
        public int? SelectedBekleyenTartimId { get; private set; }

        private void UseExisting_Click(object sender, RoutedEventArgs e)
        {
            var row = PendingGrid.SelectedItem as PendingWeighingChoiceRow;
            if (row == null)
            {
                MessageBox.Show("Kullanilacak eski tartim satirini secin.", "Bekleyen Ilk Tartim", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedBekleyenTartimId = row.BekleyenTartimId;
            Result = PendingWeighingChoiceResult.UseExisting;
            DialogResult = true;
        }

        private void CreateNew_Click(object sender, RoutedEventArgs e)
        {
            Result = PendingWeighingChoiceResult.CreateNewFirst;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Result = PendingWeighingChoiceResult.Cancel;
            DialogResult = false;
        }
    }

    public enum PendingWeighingChoiceResult
    {
        Cancel = 0,
        UseExisting = 1,
        CreateNewFirst = 2
    }

    public class PendingWeighingChoiceRow
    {
        public int BekleyenTartimId { get; set; }
        public string Plaka { get; set; }
        public string FirmaAdi { get; set; }
        public string IlkTartimTarihi { get; set; }
        public string IlkTartimSaati { get; set; }
        public string IlkAgirlik { get; set; }
        public string Aciklama { get; set; }
    }
}
