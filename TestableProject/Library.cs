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
        public string Author { get; set; }
        public string Genre { get; set; }
        public int Copies { get; set; }
        public int Year { get; set; }

        public Book(int id, string title, int copies, string author = "Unknown", string genre = "General", int year = 2000)
        {
            Id = id; Title = title; Copies = copies;
            Author = author; Genre = genre; Year = year;
        }

        public bool IsAvailable() => Copies > 0;
        public override string ToString() => $"[{Id}] \"{Title}\" by {Author} ({Copies} copies)";
    }

    public class Member
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<int> BorrowedBookIds { get; set; } = new();
        public bool IsActive { get; set; } = true;

        public Member(int id, string name) { Id = id; Name = name; }

        public int BorrowedCount => BorrowedBookIds.Count;
        public bool CanBorrow(int limit = 5) => IsActive && BorrowedCount < limit;
    }

    public class LendRecord
    {
        public int BookId { get; set; }
        public int MemberId { get; set; }
        public DateTime LentAt { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public bool IsReturned => ReturnedAt.HasValue;
    }

    public class Library
    {
        public List<Book> Books { get; set; } = new();
        public List<Member> Members { get; set; } = new();
        private List<LendRecord> _history = new();

        public void AddBook(Book book)
        {
            if (book == null) throw new ArgumentNullException(nameof(book));
            if (Books.Any(b => b.Id == book.Id)) throw new InvalidOperationException($"Book {book.Id} exists.");
            Books.Add(book);
        }

        public bool RemoveBook(int id) { var b = Books.FirstOrDefault(x => x.Id == id); if (b == null) return false; Books.Remove(b); return true; }
        public Book FindBookById(int id) => Books.FirstOrDefault(b => b.Id == id);
        public List<Book> FindByAuthor(string a) => Books.Where(b => b.Author.Equals(a, StringComparison.OrdinalIgnoreCase)).ToList();
        public List<Book> FindByGenre(string g) => Books.Where(b => b.Genre.Equals(g, StringComparison.OrdinalIgnoreCase)).ToList();
        public List<Book> GetAvailable() => Books.Where(b => b.IsAvailable()).ToList();
        public int TotalCopies() => Books.Sum(b => b.Copies);
        public List<Book> SortedByTitle() => Books.OrderBy(b => b.Title).ToList();
        public List<Book> SortedByYear() => Books.OrderBy(b => b.Year).ToList();

        public void RegisterMember(Member m)
        {
            if (m == null) throw new ArgumentNullException(nameof(m));
            if (Members.Any(x => x.Id == m.Id)) throw new InvalidOperationException($"Member {m.Id} exists.");
            Members.Add(m);
        }

        public bool DeactivateMember(int id) { var m = Members.FirstOrDefault(x => x.Id == id); if (m == null) return false; m.IsActive = false; return true; }
        public Member FindMemberById(int id) => Members.FirstOrDefault(m => m.Id == id);

        public bool LendBook(int bookId)
        {
            var b = Books.FirstOrDefault(x => x.Id == bookId);
            if (b == null || !b.IsAvailable()) return false;
            b.Copies--;
            _history.Add(new LendRecord { BookId = bookId, MemberId = -1, LentAt = DateTime.Now });
            return true;
        }

        public bool LendToMember(int bookId, int memberId)
        {
            var b = Books.FirstOrDefault(x => x.Id == bookId);
            var m = Members.FirstOrDefault(x => x.Id == memberId);
            if (b == null || !b.IsAvailable() || m == null || !m.CanBorrow()) return false;
            b.Copies--; m.BorrowedBookIds.Add(bookId);
            _history.Add(new LendRecord { BookId = bookId, MemberId = memberId, LentAt = DateTime.Now });
            return true;
        }

        public bool ReturnBook(int bookId, int memberId)
        {
            var b = Books.FirstOrDefault(x => x.Id == bookId);
            var m = Members.FirstOrDefault(x => x.Id == memberId);
            if (b == null || m == null || !m.BorrowedBookIds.Contains(bookId)) return false;
            b.Copies++; m.BorrowedBookIds.Remove(bookId);
            var r = _history.LastOrDefault(x => x.BookId == bookId && x.MemberId == memberId && !x.IsReturned);
            if (r != null) r.ReturnedAt = DateTime.Now;
            return true;
        }

        public async Task<bool> LendBookAsync(int id) { await Task.Delay(100); return LendBook(id); }
        public async Task<bool> LendToMemberAsync(int bId, int mId) { await Task.Delay(50); return LendToMember(bId, mId); }

        public List<LendRecord> GetHistory() => _history.ToList();
        public int GetLendCount(int bookId) => _history.Count(r => r.BookId == bookId);
    }
}