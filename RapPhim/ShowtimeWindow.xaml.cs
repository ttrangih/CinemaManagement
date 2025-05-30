using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows;

namespace RapPhim
{
    public partial class ShowtimeWindow : Window
    {
        private bool isEditing;
            

        public Showtime Showtime { get; private set; }

        public ShowtimeWindow(string movieId, Showtime existing = null)
        {
            InitializeComponent();

            // Load danh sách phòng
            var rooms = DatabaseHelper.GetAllRooms();
            cbRoom.ItemsSource = rooms;

            txtMovieID.Text = movieId;

            if (existing != null)
            {
                isEditing = true;
                Showtime = new Showtime
                {
                    ShowtimeID = existing.ShowtimeID,
                    MovieID = existing.MovieID,
                    Date = existing.Date,
                    Time = existing.Time,
                    Room = existing.Room
                };

                txtShowtimeID.Text = Showtime.ShowtimeID;
                if (DateTime.TryParseExact(Showtime.Date, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                {
                    datePicker.SelectedDate = parsedDate;
                }

                txtTime.Text = Showtime.Time;
                cbRoom.SelectedItem = Showtime.Room;
                txtShowtimeID.IsEnabled = false;
            }
            else
            {
                isEditing = false;
                string nextId = DatabaseHelper.GenerateNextShowtimeID();
                Showtime = new Showtime { ShowtimeID = nextId, MovieID = movieId };
                txtShowtimeID.Text = nextId;
                txtShowtimeID.IsEnabled = false;
            }


            if (existing != null)
            {
                Showtime = new Showtime
                {
                    ShowtimeID = existing.ShowtimeID,
                    MovieID = existing.MovieID,
                    Date = existing.Date,
                    Time = existing.Time,
                    Room = existing.Room
                };

                txtShowtimeID.Text = Showtime.ShowtimeID;
                if (DateTime.TryParseExact(Showtime.Date, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                {
                    datePicker.SelectedDate = parsedDate;
                }

                txtTime.Text = Showtime.Time;
                cbRoom.SelectedItem = Showtime.Room;

                txtShowtimeID.IsEnabled = false;
            }
            else
            {
                // ShowtimeID tự động
                string nextId = DatabaseHelper.GenerateNextShowtimeID();
                Showtime = new Showtime { ShowtimeID = nextId, MovieID = movieId };
                txtShowtimeID.Text = nextId;
                txtShowtimeID.IsEnabled = false;
            }
        }


        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTime.Text) ||
                cbRoom.SelectedItem == null||
                datePicker.SelectedDate == null)
            {
                MessageBox.Show("Please enter complete information!", "Missing information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(txtTime.Text, @"^(?:[01]\d|2[0-3]):[0-5]\d$"))
            {
                MessageBox.Show("Showtime must be in HH:mm format (e.g. 14:30)", "Wrong format", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Showtime.ShowtimeID = txtShowtimeID.Text;
            Showtime.Date = datePicker.SelectedDate?.ToString("dd-MM-yyyy");            // Dạng "2025-05-22"
            Showtime.Time = txtTime.Text;
            Showtime.Room = cbRoom.SelectedItem?.ToString();

            bool success;

            if (isEditing)
            {
                success = DatabaseHelper.UpdateShowtime(Showtime);
            }
            else
            {
                success = DatabaseHelper.InsertShowtime(Showtime);
            }

            if (!success)
            {
                MessageBox.Show(isEditing ? "Cannot update showtime." : "Cannot add showtime.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            DialogResult = true;
            Close();
        }


        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
