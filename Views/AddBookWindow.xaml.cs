using System;
using System.Windows;
using System.Linq;
namespace BookPlatformWPF
{
    public partial class AddBookWindow : Window
    {
        private readonly Users _author;
        private readonly Books _bookToEdit;

        public AddBookWindow(Users author, Books bookToEdit = null)
        {
            InitializeComponent();
            LoadGenres();
            _author = author;
            _bookToEdit = bookToEdit;

            if (_bookToEdit != null)
            {
                Title = "Редактирование книги";
                txtTitle.Text = _bookToEdit.Title;
                txtDescription.Text = _bookToEdit.Description;
                txtCoverPath.Text = _bookToEdit.CoverPath;
                txtContent.Text = _bookToEdit.Content;
            }
        }
        private void LoadGenres()
        {
            var genres = Core.Context.Genres.ToList();

            lbGenres.ItemsSource = genres;

            if (_bookToEdit != null)
            {
                foreach (var genre in genres)
                {
                    bool hasGenre = _bookToEdit.Genres
                        .Any(g => g.GenreID == genre.GenreID);

                    if (hasGenre)
                    {
                        lbGenres.SelectedItems.Add(genre);
                    }
                }
            }
        }


        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageBox.Show("Название книги обязательно!");
                return;
            }

            Books currentBook;

            if (_bookToEdit != null)
            {
                currentBook = _bookToEdit;

                currentBook.Title = txtTitle.Text;
                currentBook.Description = txtDescription.Text;
                currentBook.CoverPath = txtCoverPath.Text;
                currentBook.Content = txtContent.Text;

                currentBook.Genres.Clear();
            }
            else
            {
                currentBook = new Books
                {
                    Title = txtTitle.Text,
                    Description = txtDescription.Text,
                    CoverPath = txtCoverPath.Text,
                    Content = txtContent.Text,
                    AuthorID = _author.UserID,
                    IsFrozen = false,
                    CreatedAt = DateTime.Now
                };

                Core.Context.Books.Add(currentBook);
            }

            var selectedGenres = lbGenres.SelectedItems
                .Cast<Genres>()
                .ToList();

            foreach (var genre in selectedGenres)
            {
                currentBook.Genres.Add(genre);
            }

            Core.Context.SaveChanges();

            MessageBox.Show("Книга успешно сохранена!", "Успех");

            DialogResult = true;
            Close();
        }
    }
}