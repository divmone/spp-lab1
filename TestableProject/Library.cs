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
        public int Copies { get; set; }
        public decimal Price { get; set; }
        public BookCategory Category { get; set; }
        public DateTime PublishedDate { get; set; }
        public bool IsReserved { get; set; }

        public Book(int id, string title, string author, int copies, decimal price, BookCategory category)
        {
            if (id <= 0) throw new ArgumentException("Id must be positive", nameof(id));
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title cannot be empty", nameof(title));
            if (copies < 0) throw new ArgumentException("Copies cannot be negative", nameof(copies));
            if (price < 0) throw new ArgumentException("Price cannot be negative", nameof(price));

            Id = id;
            Title = title;
            Author = author;
            Copies = copies;
            Price = price;
            Category = category;
            PublishedDate = DateTime.Now;
            IsReserved = false;
        }

        public bool IsAvailable() => Copies > 0 && !IsReserved;

        public decimal CalculateDiscount(int quantity)
        {
            if (quantity <= 0) return 0;
            if (quantity >= 10) return Price * quantity * 0.20m;
            if (quantity >= 5) return Price * quantity * 0.10m;
            return 0;
        }

        public string GetAgeCategory()
        {
            var age = (DateTime.Now - PublishedDate).Days / 365;
            if (age < 1) return "New";
            if (age < 5) return "Recent";
            return "Old";
        }
    }

    public enum BookCategory
    {
        Fiction,
        NonFiction,
        Science,
        History,
        Children
    }

    public class Member
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public MembershipType Type { get; set; }
        public List<int> BorrowedBooks { get; set; } = new List<int>();

        public Member(int id, string name, string email, MembershipType type)
        {
            if (id <= 0) throw new ArgumentException("Id must be positive");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty");

            Id = id;
            Name = name;
            Email = email;
            Type = type;
        }

        public int MaxBorrowLimit()
        {
            return Type switch
            {
                MembershipType.Basic => 3,
                MembershipType.Premium => 10,
                MembershipType.VIP => 20,
                _ => 0
            };
        }

        public bool CanBorrowMore() => BorrowedBooks.Count < MaxBorrowLimit();
    }

    public enum MembershipType
    {
        Basic,
        Premium,
        VIP
    }

    public class Library
    {
        public List<Book> Books { get; set; } = new List<Book>();
        public List<Member> Members { get; set; } = new List<Member>();
        public string Name { get; set; }
        public int Capacity { get; set; }

        public Library(string name, int capacity)
        {
            Name = name;
            Capacity = capacity;
        }

        public void AddBook(Book book)
        {
            if (book == null) throw new ArgumentNullException(nameof(book));
            if (Books.Count >= Capacity) throw new InvalidOperationException("Library is at full capacity");
            Books.Add(book);
        }

        public void AddMember(Member member)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));
            if (Members.Any(m => m.Email == member.Email))
                throw new InvalidOperationException("Member with this email already exists");
            Members.Add(member);
        }

        public bool LendBook(int bookId, int memberId)
        {
            var book = Books.FirstOrDefault(b => b.Id == bookId);
            var member = Members.FirstOrDefault(m => m.Id == memberId);

            if (book == null || member == null) return false;
            if (!book.IsAvailable() || !member.CanBorrowMore()) return false;

            book.Copies--;
            member.BorrowedBooks.Add(bookId);
            return true;
        }

        public bool ReturnBook(int bookId, int memberId)
        {
            var book = Books.FirstOrDefault(b => b.Id == bookId);
            var member = Members.FirstOrDefault(m => m.Id == memberId);

            if (book == null || member == null) return false;
            if (!member.BorrowedBooks.Contains(bookId)) return false;

            book.Copies++;
            member.BorrowedBooks.Remove(bookId);
            return true;
        }

        public async Task<bool> LendBookAsync(int bookId, int memberId)
        {
            await Task.Delay(50);
            return LendBook(bookId, memberId);
        }

        public async Task<List<Book>> SearchBooksByAuthorAsync(string author)
        {
            await Task.Delay(100);
            return Books.Where(b => b.Author.Contains(author, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<Book> GetBooksByCategory(BookCategory category)
        {
            return Books.Where(b => b.Category == category).ToList();
        }

        public decimal CalculateTotalValue()
        {
            return Books.Sum(b => b.Price * b.Copies);
        }

        public int GetAvailableBooksCount()
        {
            return Books.Where(b => b.IsAvailable()).Sum(b => b.Copies);
        }
    }
}
