using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestableProject
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int Copies { get; set; }

        public Book(int id, string title, int copies)
        {
            Id = id;
            Title = title;
            Copies = copies;
        }

        public bool IsAvailable() => Copies > 0;
    }

    public class Library
    {
        public List<Book> Books { get; set; } = new List<Book>();

        public void AddBook(Book book)
        {
            Books.Add(book);
        }

        public bool LendBook(int bookId)
        {
            var book = Books.FirstOrDefault(b => b.Id == bookId);
            if (book == null || !book.IsAvailable())
                return false;

            book.Copies--;
            return true;
        }

        public async Task<bool> LendBookAsync(int bookId)
        {
            await Task.Delay(100);
            return LendBook(bookId);
        }
    }
}
