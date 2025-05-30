using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace RapPhim
{
    public partial class MainWindow : Window
    {
        ObservableCollection<Movie> movieList = new ObservableCollection<Movie>();
        ObservableCollection<Showtime> showtimeList = new ObservableCollection<Showtime>();

        public MainWindow()
        {
            InitializeComponent();
            LoadDataFromDatabase();
        }

        private void LoadDataFromDatabase()
        {
            var movies = DatabaseHelper.GetAllMovies();
            movieList.Clear();

            foreach (var m in movies)
            {
                // Load showtimes cho từng phim
                m.Showtimes = DatabaseHelper.GetShowtimesByMovieId(m.MovieID);
                movieList.Add(m);
            }

            dgMovie.ItemsSource = movieList;
        }



        private void dgPhim_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedMovie = dgMovie.SelectedItem as Movie;
            if (selectedMovie != null)
            {
                showtimeList.Clear();
                foreach (var s in selectedMovie.Showtimes)
                {
                    showtimeList.Add(s);
                }
                dgShowtime.ItemsSource = showtimeList;
            }
        }

        // ========================= MOVIE =========================

        private void ThemPhim_Click(object sender, RoutedEventArgs e)
        {
            var window = new MovieWindow(movieList.ToList());
            window.Owner = this;

            if (window.ShowDialog() == true)
            {
                movieList.Add(window.Movie);
                dgMovie.ItemsSource = null;
                dgMovie.ItemsSource = movieList;
            }
        }


        private void SuaPhim_Click(object sender, RoutedEventArgs e)
        {
            var selected = dgMovie.SelectedItem as Movie;
            if (selected != null)
            {
                var window = new MovieWindow(movieList.ToList(), selected);
                window.Owner = this;

                if (window.ShowDialog() == true)
                {
                    // Cập nhật thông tin trong DB
                    bool success = DatabaseHelper.UpdateMovie(window.Movie);
                    if (success)
                    {
                        // Cập nhật lại giao diện
                        selected.MovieName = window.Movie.MovieName;
                        selected.Genre = window.Movie.Genre;
                        selected.Director = window.Movie.Director;
                        selected.Runtime = window.Movie.Runtime;

                        dgMovie.Items.Refresh();
                    }
                }
            }
        }


        private void XoaPhim_Click(object sender, RoutedEventArgs e)
        {
            var selected = dgMovie.SelectedItem as Movie;
            if (selected != null)
            {
                if (MessageBox.Show($"Are you sure you want to delete movie '{selected.MovieName}'?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try
                    {
                        bool success = DatabaseHelper.DeleteMovie(selected.MovieID);
                        if (success)
                        {
                            movieList.Remove(selected);
                            showtimeList.Clear();
                            dgShowtime.ItemsSource = null;
                        }
                        else
                        {
                            MessageBox.Show("Delete failed in database!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error while deleting: " + ex.Message, "System error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }



        // ========================= SHOWTIME =========================

        private void AddShowtime_Click(object sender, RoutedEventArgs e)
        {
            var selected = dgMovie.SelectedItem as Movie;
            if (selected == null)
            {
                MessageBox.Show("Please select movie before adding a showtime", "Haven't selected a movie yet", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var window = new ShowtimeWindow(selected.MovieID);
            window.Owner = this;

            if (window.ShowDialog() == true)
            {
                selected.Showtimes.Add(window.Showtime);
                dgShowtime.ItemsSource = null;
                dgShowtime.ItemsSource = selected.Showtimes;
            }
        }




        private void EditShowtime_Click(object sender, RoutedEventArgs e)
        {
            var selectedMovie = dgMovie.SelectedItem as Movie;
            var selectedShowtime = dgShowtime.SelectedItem as Showtime;

            if (selectedMovie != null && selectedShowtime != null)
            {
                var window = new ShowtimeWindow(selectedMovie.MovieID, selectedShowtime);
                window.Owner = this;

                if (window.ShowDialog() == true)
                {
                    bool success = DatabaseHelper.UpdateShowtime(window.Showtime);
                    if (success)
                    {
                        // update in UI
                        selectedShowtime.Date = window.Showtime.Date;
                        selectedShowtime.Time = window.Showtime.Time;
                        selectedShowtime.Room = window.Showtime.Room;

                        dgShowtime.Items.Refresh();
                    }
                }
            }
        }


        private void DeleteShowtime_Click(object sender, RoutedEventArgs e)
        {
            var selectedMovie = dgMovie.SelectedItem as Movie;
            var selectedShowtime = dgShowtime.SelectedItem as Showtime;

            if (selectedMovie != null && selectedShowtime != null)
            {
                if (MessageBox.Show($"Are you sure you want to delete showtime {selectedShowtime.ShowtimeID}?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    bool success = DatabaseHelper.DeleteShowtime(selectedShowtime.ShowtimeID);
                    if (success)
                    {
                        selectedMovie.Showtimes.Remove(selectedShowtime);
                        dgShowtime.ItemsSource = null;
                        dgShowtime.ItemsSource = selectedMovie.Showtimes;
                    }
                    else
                    {
                        MessageBox.Show("Delete failed in database!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

    }

    // ====== MODEL CLASSES ======

    public class Movie
    {
        public string MovieID { get; set; }
        public string MovieName { get; set; }
        public string Genre { get; set; }
        public string Director { get; set; }
        public string Runtime { get; set; }
        public List<Showtime> Showtimes { get; set; } = new List<Showtime>();
    }

    public class Showtime
    {
        public string ShowtimeID { get; set; }
        public string MovieID { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Room { get; set; }
    }

}

