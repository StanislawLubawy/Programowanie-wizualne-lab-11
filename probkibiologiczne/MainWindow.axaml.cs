using System;
using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using probkibiologiczne.Models;
using probkibiologiczne.Services;

namespace probkibiologiczne
{
    public partial class MainWindow : Window
    {
        private readonly RepozytoriumProbek _repo;
        private readonly QrSerwis _qr;
        private ObservableCollection<Probka> _probki = new();
        private Probka? _aktualna;

        private ListBox? _listaProbekCtrl;
        private TextBox? _szukajBox;
        private ComboBox? _filtrTypu;
        private TextBox? _nazwaBox;
        private ComboBox? _typBox;
        private DatePicker? _dataBox;
        private TextBox? _opisBox;
        private Image? _podgladQr;

        public MainWindow()
        {
            InitializeComponent();

            _repo = new RepozytoriumProbek();
            _qr = new QrSerwis();

            var btnSzukaj = this.FindControl<Button>("BtnSzukaj");
            var btnNowy = this.FindControl<Button>("BtnNowy");
            _listaProbekCtrl = this.FindControl<ListBox>("ListaProbek");
            var btnZapisz = this.FindControl<Button>("BtnZapisz");
            var btnUsun = this.FindControl<Button>("BtnUsun");
            var btnGeneruj = this.FindControl<Button>("BtnGeneruj");
            var btnEksportuj = this.FindControl<Button>("BtnEksportuj");
            var btnDrukuj = this.FindControl<Button>("BtnDrukuj");

            _szukajBox = this.FindControl<TextBox>("SzukajBox");
            _filtrTypu = this.FindControl<ComboBox>("FiltrTypu");
            _nazwaBox = this.FindControl<TextBox>("NazwaBox");
            _typBox = this.FindControl<ComboBox>("TypBox");
            _dataBox = this.FindControl<DatePicker>("DataBox");
            _opisBox = this.FindControl<TextBox>("OpisBox");
            _podgladQr = this.FindControl<Image>("PodgladQr");

            if (btnSzukaj != null) btnSzukaj.Click += BtnSzukaj_Click;
            if (btnNowy != null) btnNowy.Click += BtnNowy_Click;
            if (_listaProbekCtrl != null) _listaProbekCtrl.SelectionChanged += ListaProbek_ZmianaWyboru;
            if (btnZapisz != null) btnZapisz.Click += BtnZapisz_Click;
            if (btnUsun != null) btnUsun.Click += BtnUsun_Click;
            if (btnGeneruj != null) btnGeneruj.Click += BtnGeneruj_Click;
            if (btnEksportuj != null) btnEksportuj.Click += BtnEksportuj_Click;
            if (btnDrukuj != null) btnDrukuj.Click += BtnDrukuj_Click;

            LoadAll();
        }

        private void LoadAll()
        {
            _probki = new ObservableCollection<Probka>(_repo.GetAll());
            if (_listaProbekCtrl != null)
            {
                var items = _listaProbekCtrl.Items as System.Collections.IList;
                if (items != null)
                {
                    items.Clear();
                    foreach (var it in _probki) items.Add(it);
                }
            }
        }

        private void BtnSzukaj_Click(object? sender, RoutedEventArgs e)
        {
            TypProbki? filter = null;
            if (_filtrTypu != null && _filtrTypu.SelectedIndex > 0) filter = (TypProbki)(_filtrTypu.SelectedIndex - 1);
            var q = _szukajBox?.Text;
            _probki = new ObservableCollection<Probka>(_repo.Search(q, filter));
            if (_listaProbekCtrl != null)
            {
                var items = _listaProbekCtrl.Items as System.Collections.IList;
                if (items != null)
                {
                    items.Clear();
                    foreach (var it in _probki) items.Add(it);
                }
            }
        }

        private void BtnNowy_Click(object? sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ListaProbek_ZmianaWyboru(object? sender, SelectionChangedEventArgs e)
        {
            if (_listaProbekCtrl?.SelectedItem is Probka p)
            {
                _aktualna = p;
                if (_nazwaBox != null) _nazwaBox.Text = p.Nazwa;
                if (_typBox != null) _typBox.SelectedIndex = (int)p.Typ;
                if (_dataBox != null) _dataBox.SelectedDate = new DateTimeOffset(p.DataPobrania);
                if (_opisBox != null) _opisBox.Text = p.Opis;
                if (_podgladQr != null) _podgladQr.Source = null;
            }
        }

        private void BtnZapisz_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_nazwaBox?.Text)) return;
            if (_aktualna == null)
            {
                var p = new Probka
                {
                    Nazwa = _nazwaBox?.Text ?? string.Empty,
                    Typ = (TypProbki)(_typBox != null && _typBox.SelectedIndex >= 0 ? _typBox.SelectedIndex : 3),
                    DataPobrania = _dataBox?.SelectedDate?.DateTime ?? DateTime.Now,
                    Opis = _opisBox?.Text ?? string.Empty
                };
                _repo.Add(p);
            }
            else
            {
                _aktualna.Nazwa = _nazwaBox?.Text ?? _aktualna.Nazwa;
                _aktualna.Typ = (TypProbki)(_typBox != null && _typBox.SelectedIndex >= 0 ? _typBox.SelectedIndex : (int)_aktualna.Typ);
                _aktualna.DataPobrania = _dataBox?.SelectedDate?.DateTime ?? _aktualna.DataPobrania;
                _aktualna.Opis = _opisBox?.Text ?? _aktualna.Opis;
                _repo.Update(_aktualna);
            }
            LoadAll();
            ClearForm();
        }

        private void BtnUsun_Click(object? sender, RoutedEventArgs e)
        {
            if (_aktualna == null) return;
            _repo.Delete(_aktualna.Id);
            LoadAll();
            ClearForm();
        }

        private void BtnGeneruj_Click(object? sender, RoutedEventArgs e)
        {
            var probka = _aktualna;
            if (probka == null) return;
            var png = _qr.GenerujPngDlaProbki(probka.Id);
            using var ms = new MemoryStream(png);
            if (_podgladQr != null) _podgladQr.Source = Bitmap.DecodeToWidth(ms, 400);
        }

        private async void BtnEksportuj_Click(object? sender, RoutedEventArgs e)
        {
            var probka = _aktualna;
            if (probka == null) return;
            var png = _qr.GenerujPngDlaProbki(probka.Id);
            var filename = Path.Combine(Environment.CurrentDirectory, probka.Nazwa + "_qr.png");
            await File.WriteAllBytesAsync(filename, png);
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo(filename) { UseShellExecute = true };
                System.Diagnostics.Process.Start(psi);
            }
            catch { }
        }

        private void BtnDrukuj_Click(object? sender, RoutedEventArgs e)
        {
            var probka = _aktualna;
            if (probka == null) return;
            var png = _qr.GenerujPngDlaProbki(probka.Id);

            try
            {
                using var ms = new MemoryStream(png);
                using var img = System.Drawing.Image.FromStream(ms);
                using var pd = new System.Drawing.Printing.PrintDocument();
                pd.PrintPage += (s, ev) =>
                {
                    var g = ev.Graphics;
                    var bounds = ev.MarginBounds;
                    var w = img.Width;
                    var h = img.Height;
                    var ratio = Math.Min(bounds.Width / (double)w, bounds.Height / (double)h);
                    var drawW = (int)(w * ratio);
                    var drawH = (int)(h * ratio);
                    g.DrawImage(img, bounds.Left, bounds.Top, drawW, drawH);
                    using var font = new System.Drawing.Font("Arial", 12);
                    g.DrawString(probka.Nazwa, font, System.Drawing.Brushes.Black, bounds.Left, bounds.Top + drawH + 10);
                };
                pd.Print();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Drukowanie nie powiod³o siê: " + ex.Message);
            }
        }

        private void ClearForm()
        {
            _aktualna = null;
            if (_nazwaBox != null) _nazwaBox.Text = string.Empty;
            if (_typBox != null) _typBox.SelectedIndex = 3;
            if (_dataBox != null) _dataBox.SelectedDate = DateTime.Now;
            if (_opisBox != null) _opisBox.Text = string.Empty;
            if (_podgladQr != null) _podgladQr.Source = null;
            if (_listaProbekCtrl != null) _listaProbekCtrl.SelectedItem = null;
        }
    }
}
