using System.Windows;
using System.Xml.Linq;

namespace RapPhim
{
    public partial class MovieWindow : Window
    {
        public Movie Movie { get; private set; }
        private bool isEditing = false;


        public MovieWindow(List<Movie> existingMovies, Movie movieToEdit = null)
        {
            InitializeComponent();

            if (movieToEdit != null)
            {
                // Chế độ chỉnh sửa
                isEditing = true;
                Movie = new Movie
                {
                    MovieID = movieToEdit.MovieID,
                    MovieName = movieToEdit.MovieName,
                    Genre = movieToEdit.Genre,
                    Director = movieToEdit.Director,
                    Runtime = movieToEdit.Runtime
                };

                txtMovieID.Text = Movie.MovieID;
                txtName.Text = Movie.MovieName;
                txtGenre.Text = Movie.Genre;
                txtDirector.Text = Movie.Director;
                txtRuntime.Text = Movie.Runtime;

                txtMovieID.IsEnabled = false;
            }
            else
            {
                // Chế độ thêm mới
                isEditing = false;

                string nextId = "P001";
                if (existingMovies.Count > 0)
                {
                    var lastId = existingMovies
                        .Select(m => m.MovieID)
                        .Where(id => id.StartsWith("P"))
                        .Select(id => int.TryParse(id.Substring(1), out int n) ? n : 0)
                        .DefaultIfEmpty(0)
                        .Max();

                    nextId = $"P{(lastId + 1).ToString("D3")}";
                }

                Movie = new Movie { MovieID = nextId };
                txtMovieID.Text = nextId;
                txtMovieID.IsEnabled = false;
            }
        }


        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) ||
                string.IsNullOrWhiteSpace(txtGenre.Text) ||
                string.IsNullOrWhiteSpace(txtDirector.Text) ||
                string.IsNullOrWhiteSpace(txtRuntime.Text))
            {
                MessageBox.Show("Please enter complete information!", "Missing information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtRuntime.Text, out int runtime) || runtime <= 0)
            {
                MessageBox.Show("Runtime must be a positive integer!", "Invalid data", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Movie.MovieName = txtName.Text;
            Movie.Genre = txtGenre.Text;
            Movie.Director = txtDirector.Text;
            Movie.Runtime = txtRuntime.Text;

            bool success;
            if (isEditing)
            {
                success = DatabaseHelper.UpdateMovie(Movie);
            }
            else
            {
                success = DatabaseHelper.InsertMovie(Movie);
            }

            if (!success)
            {
                MessageBox.Show(isEditing ? "Cannot update movie." : "Cannot add movie.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
