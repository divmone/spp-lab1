using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        public Book(int id, string title, int copies,
                    string author = "Unknown", string genre = "General", int year = 2000)
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
            if (Books.Any(b => b.Id == book.Id))
                throw new InvalidOperationException($"Книга с Id={book.Id} уже существует.");
            Books.Add(book);
        }

        public bool RemoveBook(int id)
        {
            var book = Books.FirstOrDefault(b => b.Id == id);
            if (book == null) return false;
            Books.Remove(book);
            return true;
        }

        public Book FindBookById(int id) => Books.FirstOrDefault(b => b.Id == id);
        public List<Book> FindByAuthor(string a) => Books.Where(b => b.Author.Equals(a, StringComparison.OrdinalIgnoreCase)).ToList();
        public List<Book> FindByGenre(string g) => Books.Where(b => b.Genre.Equals(g, StringComparison.OrdinalIgnoreCase)).ToList();
        public List<Book> GetAvailable() => Books.Where(b => b.IsAvailable()).ToList();
        public int TotalCopies() => Books.Sum(b => b.Copies);
        public List<Book> SortedByTitle() => Books.OrderBy(b => b.Title).ToList();
        public List<Book> SortedByYear() => Books.OrderBy(b => b.Year).ToList();

        public void RegisterMember(Member member)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));
            if (Members.Any(m => m.Id == member.Id))
                throw new InvalidOperationException($"Member with Id={member.Id} already exist.");
            Members.Add(member);
        }

        public bool DeactivateMember(int id) { var m = Members.FirstOrDefault(x => x.Id == id); if (m == null) return false; m.IsActive = false; return true; }
        public Member FindMemberById(int id) => Members.FirstOrDefault(m => m.Id == id);
        public bool LendBook(int bookId)
        {
            var book = Books.FirstOrDefault(b => b.Id == bookId);
            if (book == null || !book.IsAvailable()) return false;
            book.Copies--;
            _history.Add(new LendRecord { BookId = bookId, MemberId = -1, LentAt = DateTime.Now });
            return true;
        }

        public bool LendToMember(int bookId, int memberId)
        {
            var book = Books.FirstOrDefault(b => b.Id == bookId);
            var member = Members.FirstOrDefault(m => m.Id == memberId);
            if (book == null || !book.IsAvailable() || member == null || !member.CanBorrow())
                return false;
            book.Copies--;
            member.BorrowedBookIds.Add(bookId);
            _history.Add(new LendRecord { BookId = bookId, MemberId = memberId, LentAt = DateTime.Now });
            return true;
        }

        public bool ReturnBook(int bookId, int memberId)
        {
            var book = Books.FirstOrDefault(b => b.Id == bookId);
            var member = Members.FirstOrDefault(m => m.Id == memberId);
            if (book == null || member == null || !member.BorrowedBookIds.Contains(bookId))
                return false;
            book.Copies++;
            member.BorrowedBookIds.Remove(bookId);
            var record = _history.LastOrDefault(r => r.BookId == bookId && r.MemberId == memberId && !r.IsReturned);
            if (record != null) record.ReturnedAt = DateTime.Now;
            return true;
        }

        public async Task<bool> LendBookAsync(int id)
        {
            await Task.Delay(100);
            return LendBook(id);
        }

        public async Task<bool> LendToMemberAsync(int bookId, int memberId)
        {
            await Task.Delay(50);
            return LendToMember(bookId, memberId);
        }

        public List<LendRecord> GetHistory() => _history.ToList();
        public int GetLendCount(int id) => _history.Count(r => r.BookId == id);
    }
}