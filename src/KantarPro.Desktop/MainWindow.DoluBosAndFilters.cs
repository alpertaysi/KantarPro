using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Windows.Controls;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using KantarPro.Application.Services;
using KantarPro.Domain;
using KantarPro.Domain.Entities;
using KantarPro.Infrastructure.Data;
using System.Data.Entity;

namespace KantarPro.Desktop
{
    public partial class MainWindow : Window
    {
        private void DoluBosBekleyenBul_Click(object sender, RoutedEventArgs e)
        {
            var plaka = NormalizePlaka(DoluBosPlakaTextBox.Text);
            if (!SelectPendingWeighingRow(plaka))
            {
                MessageBox.Show("Bu plaka icin bekleyen ilk tartim bulunamadi. Yeni ilk tartim olarak acilabilir.", "Dolu-Bos Tartim");
                DoluBosYeniIlkRadio.IsChecked = true;
                return;
            }
        }

        private bool SelectPendingWeighingRow(string plaka)
        {
            var normalizedPlaka = NormalizePlaka(plaka);
            var match = PendingWeighings.FirstOrDefault(x => NormalizePlaka(x.Plaka) == normalizedPlaka);
            if (match == null)
            {
                return false;
            }

            DoluBosBekleyenGrid.SelectedItem = match;
            DoluBosBekleyenGrid.ScrollIntoView(match);
            ApplyPendingWeighingSelection(match);
            return true;
        }

        private void DoluBosBekleyenGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var row = DoluBosBekleyenGrid.SelectedItem as PendingWeighingPrototypeRow;
            if (row != null)
            {
                ApplyPendingWeighingSelection(row);
            }
        }

        private void ApplyPendingWeighingSelection(PendingWeighingPrototypeRow row)
        {
            DoluBosEskiTartimRadio.IsChecked = true;
            DoluBosSeciliPlakaTextBox.Text = row.Plaka;
            DoluBosSeciliFirmaTextBox.Text = row.FirmaAdi;
            DoluBosIlkTartimTextBox.Text = row.IlkTartimTarihi + " " + row.IlkTartimSaati;
            DoluBosIlkKgTextBox.Text = row.IlkAgirlik;
            CalculateDoluBosNet();
        }

        private void DoluBosKiloAl_Click(object sender, RoutedEventArgs e)
        {
            DoluBosIkinciKgTextBox.Text = AgirlikTextBox != null && !string.IsNullOrWhiteSpace(AgirlikTextBox.Text)
                ? AgirlikTextBox.Text
                : "0";
            CalculateDoluBosNet();
        }

        private void DoluBosNetHesapla_Click(object sender, RoutedEventArgs e)
        {
            CalculateDoluBosNet();
        }

        private void DoluBosPrototypeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var row = DoluBosBekleyenGrid.SelectedItem as PendingWeighingPrototypeRow;
                if (DoluBosEskiTartimRadio.IsChecked == true && row == null)
                {
                    throw new InvalidOperationException("Once bekleyen ilk tartim satirini secin.");
                }

                var plaka = DoluBosEskiTartimRadio.IsChecked == true ? row.Plaka : DoluBosPlakaTextBox.Text;
                var firma = DoluBosEskiTartimRadio.IsChecked == true ? row.FirmaAdi : DoluBosFirmaTextBox.Text;
                var gelisTuru = row != null && row.YukDurumu == KantarSabitleri.YukDurumu.Bos
                    ? KantarSabitleri.GelisTuru.Dolu
                    : KantarSabitleri.GelisTuru.Bos;
                var ikinciKg = ParseAgirlik(DoluBosIkinciKgTextBox.Text);

                CreateEntry(plaka, firma, DoluBosAciklamaTextBox.Text, true, ikinciKg, DateTime.Now, gelisTuru);
                LoadDashboardData();
                ShowEntryPage();
                MessageBox.Show("Dolu-bos tartim kaydi acildi. Arac cikis yaptiginda tahsilati tamamlanip kesin cikisa aktarilacak.", "Dolu-Bos Tartim");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Dolu-bos tartim kaydedilemedi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CalculateDoluBosNet()
        {
            decimal ilkKg;
            decimal ikinciKg;
            if (!decimal.TryParse((DoluBosIlkKgTextBox.Text ?? string.Empty).Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out ilkKg) ||
                !decimal.TryParse((DoluBosIkinciKgTextBox.Text ?? string.Empty).Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out ikinciKg))
            {
                DoluBosNetKgTextBox.Text = "0";
                return;
            }

            DoluBosNetKgTextBox.Text = Math.Abs(ilkKg - ikinciKg).ToString("N0");
            DoluBosTahakkukTextBox.Text = FormatPara(732m);
        }

        private void ColumnFilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            var tag = textBox != null ? textBox.Tag as string : null;
            var value = textBox != null ? textBox.Text : string.Empty;

            switch (tag)
            {
                case "EntryPlaka":
                    _entryPlakaFilter = NormalizePlaka(value);
                    EntryVehiclesView.Refresh();
                    break;
                case "EntryFirma":
                    _entryFirmaFilter = NormalizeText(value);
                    EntryVehiclesView.Refresh();
                    break;
                case "ExitPlaka":
                    _exitPlakaFilter = NormalizePlaka(value);
                    ExitVehiclesView.Refresh();
                    break;
                case "ExitFirma":
                    _exitFirmaFilter = NormalizeText(value);
                    ExitVehiclesView.Refresh();
                    break;
                case "RevenuePlaka":
                    _revenuePlakaFilter = NormalizePlaka(value);
                    DailyRevenueView.Refresh();
                    break;
                case "RevenueFirma":
                    _revenueFirmaFilter = NormalizeText(value);
                    DailyRevenueView.Refresh();
                    break;
            }
        }

        private void EntryVehiclesGrid_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.OriginalSource is TextBox)
            {
                return;
            }

            if (AppendEntryGridSearch(e.Text, sender as DataGrid))
            {
                e.Handled = true;
            }
        }

        private void ExitVehiclesGrid_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.OriginalSource is TextBox)
            {
                return;
            }

            if (AppendExitGridSearch(e.Text, sender as DataGrid))
            {
                e.Handled = true;
            }
        }

        private void EntryVehiclesGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var keyText = GetSearchTextFromKey(e.Key);
            if (!string.IsNullOrEmpty(keyText) && AppendEntryGridSearch(keyText, sender as DataGrid))
            {
                e.Handled = true;
                return;
            }

            if (HandleEntryGridSearchKey(e.Key, sender as DataGrid))
            {
                e.Handled = true;
            }
        }

        private void ExitVehiclesGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var keyText = GetSearchTextFromKey(e.Key);
            if (!string.IsNullOrEmpty(keyText) && AppendExitGridSearch(keyText, sender as DataGrid))
            {
                e.Handled = true;
                return;
            }

            if (HandleExitGridSearchKey(e.Key, sender as DataGrid))
            {
                e.Handled = true;
            }
        }

        private bool AppendEntryGridSearch(string text, DataGrid sourceGrid)
        {
            return false;
        }

        private bool AppendExitGridSearch(string text, DataGrid sourceGrid)
        {
            return false;
        }

        private bool HandleEntryGridSearchKey(Key key, DataGrid sourceGrid)
        {
            if (key == Key.Escape)
            {
                _entryPlakaFilter = string.Empty;
                _entryFirmaFilter = string.Empty;
                EntryVehiclesView.Refresh();
                SelectFirstVisibleVehicle(EntryVehiclesView, sourceGrid);
                return true;
            }

            return false;
        }

        private bool HandleExitGridSearchKey(Key key, DataGrid sourceGrid)
        {
            if (key == Key.Escape)
            {
                _exitPlakaFilter = string.Empty;
                _exitFirmaFilter = string.Empty;
                ExitVehiclesView.Refresh();
                SelectFirstVisibleVehicle(ExitVehiclesView, sourceGrid);
                return true;
            }

            return false;
        }

        private bool FilterEntryVehicle(object item)
        {
            return FilterVehicle(item, _entryPlakaFilter, _entryFirmaFilter);
        }

        private bool FilterExitVehicle(object item)
        {
            return FilterVehicle(item, _exitPlakaFilter, _exitFirmaFilter);
        }

        private bool FilterDailyRevenue(object item)
        {
            var row = item as DailyRevenueRow;
            if (row == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(_revenuePlakaFilter) &&
                !NormalizePlaka(row.Plaka).StartsWith(_revenuePlakaFilter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(_revenueFirmaFilter) &&
                !NormalizeText(row.FirmaAdi).Contains(_revenueFirmaFilter))
            {
                return false;
            }

            return true;
        }

        private static bool FilterVehicle(object item, string plakaFilter, string firmaFilter)
        {
            var row = item as VehicleMovementRow;
            if (row == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(plakaFilter) &&
                !NormalizePlaka(row.Plaka).StartsWith(plakaFilter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(firmaFilter) &&
                !NormalizeText(row.FirmaAdi).Contains(firmaFilter))
            {
                return false;
            }

            return true;
        }

        private static string GetSearchTextFromKey(Key key)
        {
            if (key >= Key.A && key <= Key.Z)
            {
                return key.ToString();
            }

            if (key >= Key.D0 && key <= Key.D9)
            {
                return ((int)(key - Key.D0)).ToString(CultureInfo.InvariantCulture);
            }

            if (key >= Key.NumPad0 && key <= Key.NumPad9)
            {
                return ((int)(key - Key.NumPad0)).ToString(CultureInfo.InvariantCulture);
            }

            return null;
        }

        private static void SelectFirstVisibleVehicle(ICollectionView view, DataGrid grid)
        {
            if (view == null || grid == null)
            {
                return;
            }

            var first = view.Cast<object>().FirstOrDefault();
            if (first == null)
            {
                grid.SelectedItem = null;
                return;
            }

            grid.SelectedItem = first;
            grid.ScrollIntoView(first);
            if (grid.Columns.Count > 0)
            {
                grid.CurrentCell = new DataGridCellInfo(first, grid.Columns[0]);
            }
            grid.Focus();
        }

        private void ClearVehicleFilters()
        {
            _entryPlakaFilter = string.Empty;
            _entryFirmaFilter = string.Empty;
            _exitPlakaFilter = string.Empty;
            _exitFirmaFilter = string.Empty;
        }
    }
}
