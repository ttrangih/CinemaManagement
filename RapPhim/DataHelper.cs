using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows;
using Oracle.ManagedDataAccess.Client;

namespace RapPhim
{
    public class DatabaseHelper
    {
        private static readonly string connStr = ConfigurationManager.ConnectionStrings["OracleConn"].ConnectionString;

        // Lấy danh sách phim từ Oracle
        public static List<Movie> GetAllMovies()
        {
            var list = new List<Movie>();
            using (var conn = new OracleConnection(connStr))
            {
                conn.Open();
                var cmd = new OracleCommand("SELECT * FROM Movie", conn);
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new Movie
                    {
                        MovieID = reader["MovieID"].ToString(),
                        MovieName = reader["MovieName"].ToString(),
                        Genre = reader["Genre"].ToString(),
                        Director = reader["Director"].ToString(),
                        Runtime = reader["Runtime"].ToString()
                    });
                }
            }
            return list;
        }

        // lấy suất chiếu từ oracle
        public static List<Showtime> GetShowtimesByMovieId(string movieId)
        {
            var list = new List<Showtime>();
            using (var conn = new OracleConnection(connStr))
            {
                conn.Open();
                var cmd = new OracleCommand(@"
            SELECT ShowtimeID, MovieID, TO_CHAR(ShowDate, 'DD-MM-YYYY') AS ShowDate, ShowTime, Room
            FROM Showtime
            WHERE MovieID = :movieId
        ", conn);

                cmd.Parameters.Add(":movieId", movieId);
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new Showtime
                    {
                        ShowtimeID = reader["ShowtimeID"].ToString(),
                        MovieID = reader["MovieID"].ToString(),
                        Date = reader["ShowDate"].ToString(),
                        Time = reader["ShowTime"].ToString(),
                        Room = reader["Room"].ToString()
                    });
                }
            }
            return list;
        }


        //--------insert movie
        public static bool InsertMovie(Movie movie)
        {
            try
            {
                using (var conn = new OracleConnection(connStr))
                {
                    conn.Open();
                    var cmd = new OracleCommand(@"
                INSERT INTO Movie (MovieID, MovieName, Genre, Director, Runtime)
                VALUES (:id, :name, :genre, :director, :runtime)", conn);

                    cmd.Parameters.Add(":id", movie.MovieID);
                    cmd.Parameters.Add(":name", movie.MovieName);
                    cmd.Parameters.Add(":genre", movie.Genre);
                    cmd.Parameters.Add(":director", movie.Director);
                    cmd.Parameters.Add(":runtime", int.Parse(movie.Runtime));

                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0;
                }
            }
            catch (OracleException ex)
            {
                // Nếu lỗi là trùng khóa chính hoặc khóa unique
                if (ex.Number == 1) // ORA-00001
                {
                    MessageBox.Show("Movie already exited (duplicate MovieID or MovieName).", "Duplicate data", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show("Oracle error: " + ex.Message, "Database error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                return false;
            }
        }

        //----------Insert showtime--------
        public static bool InsertShowtime(Showtime showtime)
        {
            using (var conn = new OracleConnection(connStr))
            {
                conn.Open();

                //Kiểm tra trùng suất chiếu trong cùng phòng
                var checkCmd = new OracleCommand(@"
    SELECT 1
    FROM Showtime s
    JOIN Movie m ON s.MovieID = m.MovieID
    JOIN Movie m_new ON m_new.MovieID = :pMovieId
    WHERE s.Room = :pRoom
      AND s.ShowDate = TO_DATE(:pDate, 'DD-MM-YYYY')
      AND (
        TO_TIMESTAMP(s.ShowTime, 'HH24:MI') < TO_TIMESTAMP(:pTime, 'HH24:MI') + NUMTODSINTERVAL(m_new.Runtime, 'MINUTE')
        AND
        TO_TIMESTAMP(:pTime, 'HH24:MI') < TO_TIMESTAMP(s.ShowTime, 'HH24:MI') + NUMTODSINTERVAL(m.Runtime, 'MINUTE')
      )
", conn);

                checkCmd.Parameters.Add(":pMovieId", showtime.MovieID);
                checkCmd.Parameters.Add(":pRoom", showtime.Room);
                checkCmd.Parameters.Add(":pDate", showtime.Date);
                checkCmd.Parameters.Add(":pTime", showtime.Time);


                var conflict = checkCmd.ExecuteScalar();
                if (conflict != null)
                {
                    MessageBox.Show("Reference room set for this time slot!", "Duplicate time", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                //Nếu không trùng thì chèn suất chiếu như bình thường
                var insertCmd = new OracleCommand(@"
    INSERT INTO Showtime (ShowtimeID, MovieID, ShowDate, ShowTime, Room)
    VALUES (:pId, :pMovieId, TO_DATE(:pDate, 'DD-MM-YYYY'), :pTime, :pRoom)
", conn);

                insertCmd.Parameters.Add(":pId", showtime.ShowtimeID);
                insertCmd.Parameters.Add(":pMovieId", showtime.MovieID);
                insertCmd.Parameters.Add(":pDate", showtime.Date);
                insertCmd.Parameters.Add(":pTime", showtime.Time);
                insertCmd.Parameters.Add(":pRoom", showtime.Room);


                int rows = insertCmd.ExecuteNonQuery();
                return rows > 0;
            }
        }

        //edit movie
        public static bool UpdateMovie(Movie movie)
        {
            try
            {
                using (var conn = new OracleConnection(connStr))
                {
                    conn.Open();

                    // Kiểm tra trùng tên với các phim khác
                    using (var checkCmd = new OracleCommand("SELECT COUNT(*) FROM Movie WHERE MovieName = :name AND MovieID != :id", conn))
                    {
                        checkCmd.Parameters.Add(":name", movie.MovieName);
                        checkCmd.Parameters.Add(":id", movie.MovieID);

                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (count > 0)
                        {
                            MessageBox.Show("Movie name already exists.", "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return false;
                        }
                    }

                    // Nếu không trùng thì update
                    using (var cmd = new OracleCommand(@"
                UPDATE Movie
                SET MovieName = :name,
                    Genre = :genre,
                    Director = :director,
                    Runtime = :runtime
                WHERE MovieID = :id", conn))
                    {
                        cmd.Parameters.Add(":name", movie.MovieName);
                        cmd.Parameters.Add(":genre", movie.Genre);
                        cmd.Parameters.Add(":director", movie.Director);
                        cmd.Parameters.Add(":runtime", int.Parse(movie.Runtime));
                        cmd.Parameters.Add(":id", movie.MovieID);

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating movie: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }


        //edit showtime
        public static bool UpdateShowtime(Showtime showtime)
        {
            try
            {
                using (var conn = new OracleConnection(connStr))
                {
                    conn.Open();

                    using (var cmd = new OracleCommand(@"
                UPDATE Showtime
                SET ShowDate = TO_DATE(:pDate, 'DD-MM-YYYY'),
                    ShowTime = :pTime,
                    Room = :pRoom
                WHERE ShowtimeID = :pId", conn))
                    {
                        cmd.Parameters.Add(":pDate", showtime.Date);
                        cmd.Parameters.Add(":pTime", showtime.Time);
                        cmd.Parameters.Add(":pRoom", showtime.Room);
                        cmd.Parameters.Add(":pId", showtime.ShowtimeID);

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating showtime: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }




        public static List<string> GetAllRooms()
        {
            var list = new List<string>();
            using (var conn = new OracleConnection(connStr))
            {
                conn.Open();
                var cmd = new OracleCommand("SELECT RoomID FROM Room", conn);
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(reader["RoomID"].ToString());
                }
            }
            return list;
        }

        //ShowtimeID automatic
        public static string GenerateNextShowtimeID()
        {
            using (var conn = new OracleConnection(connStr))
            {
                conn.Open();
                var cmd = new OracleCommand("SELECT MAX(ShowtimeID) FROM Showtime", conn);
                var result = cmd.ExecuteScalar()?.ToString();

                if (string.IsNullOrWhiteSpace(result))
                    return "S001";

                if (int.TryParse(result.Substring(1), out int num))
                    return $"S{(num + 1).ToString("D3")}";

                return "S001";
            }
        }


        //delete movie
        public static bool DeleteMovie(string movieId)
        {
            using (var conn = new OracleConnection(connStr))
            {
                conn.Open();
                var cmd = new OracleCommand("DELETE FROM Movie WHERE MovieID = :id", conn);
                cmd.Parameters.Add(":id", movieId);
                int rows = cmd.ExecuteNonQuery();
                return rows > 0;
            }
        }

        //delete showtime
        public static bool DeleteShowtime(string showtimeId)
        {
            using (var conn = new OracleConnection(connStr))
            {
                conn.Open();
                var cmd = new OracleCommand("DELETE FROM Showtime WHERE ShowtimeID = :id", conn);
                cmd.Parameters.Add(":id", showtimeId);
                int rows = cmd.ExecuteNonQuery();
                return rows > 0;
            }
        }



    }
}
