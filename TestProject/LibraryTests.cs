using System;
using System.Threading;
using System.Threading.Tasks;
using TestFramework;
using TestableProject;

namespace TestProject
{
    [TestClass]
    public class LibraryTests
    {
        private Library _lib;

        [TestMethodInit]
        public void Setup()
        {
            _lib = new Library();
            _lib.AddBook(new Book(1, "Clean Code", 3, "Robert Martin", "Programming", 2008));
            _lib.AddBook(new Book(2, "Design Patterns", 1, "Gang of Four", "Architecture", 1994));
            _lib.RegisterMember(new Member(1, "Alice"));
        }

        [TestMethodCleanup]
        public void Teardown() => _lib = null;


        [TestMethod]
        [TestPriority(10)]
        public void AddBook_IncreasesCount()
        {
            _lib.AddBook(new Book(3, "SICP", 2));
            Assert.areEqual(3, _lib.Books.Count);
        }

        [TestMethod]
        [TestPriority(9)]
        public void AddBook_DuplicateId_Throws()
        {
            bool threw = false;
            try { _lib.AddBook(new Book(1, "Dup", 1)); }
            catch (InvalidOperationException) { threw = true; }
            Assert.isTrue(threw);
        }

        [TestMethod]
        [TestData(1, true)]
        [TestData(99, false)]
        [TestPriority(8)]
        public void RemoveBook_Parametrized(int id, bool expected)
            => Assert.areEqual(expected, _lib.RemoveBook(id));

        [TestMethod]
        [TestPriority(7)]
        public void FindBookById_NotFound_ReturnsNull()
            => Assert.isNull(_lib.FindBookById(9999));

        [TestMethod]
        [TestData("Robert Martin", 1)]
        [TestData("Nobody", 0)]
        [TestPriority(6)]
        public void FindByAuthor_Parametrized(string author, int count)
            => Assert.areEqual(count, _lib.FindByAuthor(author).Count);

        [TestMethod]
        [TestData("Programming", 1)]
        [TestData("Fantasy", 0)]
        [TestPriority(6)]
        public void FindByGenre_Parametrized(string genre, int count)
            => Assert.areEqual(count, _lib.FindByGenre(genre).Count);

        [TestMethod]
        [TestPriority(5)]
        public void GetAvailable_AllAvailableInitially()
            => Assert.areEqual(2, _lib.GetAvailable().Count);

        [TestMethod]
        [TestPriority(5)]
        public void TotalCopies_IsCorrect()
            => Assert.areEqual(4, _lib.TotalCopies());

        [TestMethod]
        [TestPriority(4)]
        public void SortedByTitle_IsOrdered()
        {
            var sorted = _lib.SortedByTitle();
            Assert.isLessThanOrEqualTo(sorted[0].Title, sorted[1].Title);
        }

        [TestMethod]
        [TestPriority(4)]
        public void SortedByYear_IsOrdered()
        {
            var sorted = _lib.SortedByYear();
            Assert.isLessThanOrEqualTo(sorted[0].Year, sorted[1].Year);
        }


        [TestMethod]
        [TestData(1, true)]
        [TestData(99, false)]
        [TestPriority(9)]
        public void LendBook_Parametrized(int id, bool expected)
            => Assert.areEqual(expected, _lib.LendBook(id));

        [TestMethod]
        [TestPriority(8)]
        public void LendBook_ReducesCopies()
        {
            int before = _lib.FindBookById(1).Copies;
            _lib.LendBook(1);
            Assert.areEqual(before - 1, _lib.FindBookById(1).Copies);
        }

        [TestMethod]
        [TestPriority(7)]
        public void LendBook_NoCopies_ReturnsFalse()
        {
            _lib.LendBook(2);
            Assert.isFalse(_lib.LendBook(2));
        }

        [TestMethod]
        [TestAsync]
        [TestPriority(6)]
        public async Task LendBookAsync_ReturnsTrue()
            => Assert.isTrue(await _lib.LendBookAsync(1));

        [TestMethod]
        [TestAsync]
        [TestPriority(6)]
        public async Task LendBookAsync_Unknown_ReturnsFalse()
            => Assert.isFalse(await _lib.LendBookAsync(9999));


        [TestMethod]
        [TestPriority(9)]
        public void RegisterMember_DuplicateThrows()
        {
            bool threw = false;
            try { _lib.RegisterMember(new Member(1, "Bob")); }
            catch (InvalidOperationException) { threw = true; }
            Assert.isTrue(threw);
        }

        [TestMethod]
        [TestPriority(8)]
        public void FindMember_NotFound_ReturnsNull()
            => Assert.isNull(_lib.FindMemberById(9999));

        [TestMethod]
        [TestData(1, 1, true)]
        [TestData(99, 1, false)]
        [TestPriority(7)]
        public void LendToMember_Parametrized(int bookId, int memberId, bool expected)
            => Assert.areEqual(expected, _lib.LendToMember(bookId, memberId));

        [TestMethod]
        [TestPriority(7)]
        public void LendToMember_TracksBorrowedCount()
        {
            _lib.LendToMember(1, 1);
            Assert.areEqual(1, _lib.FindMemberById(1).BorrowedCount);
        }

        [TestMethod]
        [TestPriority(6)]
        public void ReturnBook_Success()
        {
            _lib.LendToMember(1, 1);
            Assert.isTrue(_lib.ReturnBook(1, 1));
            Assert.areEqual(0, _lib.FindMemberById(1).BorrowedCount);
        }

        [TestMethod]
        [TestPriority(6)]
        public void ReturnBook_NotBorrowed_ReturnsFalse()
            => Assert.isFalse(_lib.ReturnBook(1, 1));

        [TestMethod]
        [TestPriority(5)]
        public void DeactivateMember_BlocksBorrowing()
        {
            _lib.DeactivateMember(1);
            Assert.isFalse(_lib.LendToMember(1, 1));
        }

        [TestMethod]
        [TestAsync]
        [TestPriority(5)]
        public async Task LendToMemberAsync_Success()
            => Assert.isTrue(await _lib.LendToMemberAsync(1, 1));

        [TestMethod]
        [TestAsync]
        [TestPriority(4)]
        public async Task LendToMemberAsync_Inactive_Fails()
        {
            _lib.DeactivateMember(1);
            Assert.isFalse(await _lib.LendToMemberAsync(1, 1));
        }


        [TestMethod]
        [TestPriority(4)]
        public void History_EmptyInitially()
            => Assert.isEmpty(_lib.GetHistory());

        [TestMethod]
        [TestPriority(3)]
        public void History_NotEmptyAfterLend()
        {
            _lib.LendBook(1);
            Assert.isNotEmpty(_lib.GetHistory());
        }

        [TestMethod]
        [TestPriority(3)]
        public void GetLendCount_IsCorrect()
        {
            _lib.LendBook(1);
            _lib.LendBook(1);
            Assert.areEqual(2, _lib.GetLendCount(1));
        }


        [TestMethod]
        [TestPriority(2)]
        public void Book_CopiesIsPositive()
            => Assert.isPositive(_lib.FindBookById(1).Copies);

        [TestMethod]
        [TestPriority(2)]
        public void Book_YearGreaterThan1900()
            => Assert.isGreaterThan(_lib.FindBookById(1).Year, 1900);

        [TestMethod]
        [TestPriority(2)]
        public void Book_SameReference()
        {
            var b = _lib.FindBookById(1);
            Assert.areSame(b, b);
        }

        [TestMethod]
        [TestPriority(2)]
        public void Book_DifferentReference()
            => Assert.areNotSame(_lib.FindBookById(1), _lib.FindBookById(2));

        [TestMethod]
        [TestPriority(1)]
        public void Book_ToString_ContainsTitle()
            => Assert.isTrue(_lib.FindBookById(1).ToString().Contains("Clean Code"));

        [TestMethod]
        [TestPriority(1)]
        public void Member_IsActive_Initially()
            => Assert.isTrue(_lib.FindMemberById(1).IsActive);

        [TestMethod]
        [TestIgnore]
        public void Ignored_ShouldNotRun()
            => throw new Exception("Не должен выполняться");


        [TestMethod]
        [TestTimeout(500)]
        [TestPriority(3)]
        public void FastOperation_WithinTimeout_Passes()
        {
            Thread.Sleep(50);
            Assert.isTrue(true);
        }

        [TestMethod]
        [TestTimeout(200)]
        [TestPriority(3)]
        public void SlowOperation_ExceedsTimeout_IsMarkedTimeout()
        {
            Thread.Sleep(1000);
            Assert.isTrue(true);
        }
    }
    [TestClass]
    public class BookTests
    {
        private Book _book;

        [TestMethodInit]
        public void Setup() => _book = new Book(1, "The Pragmatic Programmer", 5, "David Thomas", "Programming", 1999);

        [TestMethodCleanup]
        public void Teardown() => _book = null;

        [TestMethod]
        [TestPriority(10)]
        public void Book_IsAvailable_WhenCopiesPositive()
            => Assert.isTrue(_book.IsAvailable());

        [TestMethod]
        [TestPriority(9)]
        public void Book_IsNotAvailable_WhenNoCopies()
        {
            _book.Copies = 0;
            Assert.isFalse(_book.IsAvailable());
        }

        [TestMethod]
        [TestPriority(8)]
        public void Book_ToString_ContainsAuthor()
            => Assert.isTrue(_book.ToString().Contains("David Thomas"));

        [TestMethod]
        [TestPriority(7)]
        public void Book_ToString_ContainsId()
            => Assert.isTrue(_book.ToString().Contains("1"));

        [TestMethod]
        [TestPriority(6)]
        public void Book_Year_IsPositive()
            => Assert.isPositive(_book.Year);

        [TestMethod]
        [TestPriority(5)]
        public void Book_Copies_IsCorrect()
            => Assert.areEqual(5, _book.Copies);

        [TestMethod]
        [TestPriority(4)]
        public void Book_Genre_IsCorrect()
            => Assert.areEqual("Programming", _book.Genre);

        [TestMethod]
        [TestPriority(3)]
        public void Book_Title_IsCorrect()
            => Assert.areEqual("The Pragmatic Programmer", _book.Title);

        [TestMethod]
        [TestAsync]
        [TestPriority(2)]
        public async Task Book_AsyncDelay_Completes()
        {
            await Task.Delay(80);
            Assert.isTrue(_book.IsAvailable());
        }

        [TestMethod]
        [TestTimeout(300)]
        [TestPriority(2)]
        public void Book_FastCheck_WithinTimeout()
        {
            Thread.Sleep(50);
            Assert.areEqual("The Pragmatic Programmer", _book.Title);
        }
    }
}