using System;
using System.Threading.Tasks;
using TestFramework;
using TestableProject;

public class LibraryFixture : ISharedFixture
{
    public Library SharedLibrary { get; private set; }

    public void Initialize()
    {
        Console.WriteLine("LibraryFixture: Initializing shared library...");
        SharedLibrary = new Library("Central Library", 1000);
        SharedLibrary.AddBook(new Book(1, "1984", "George Orwell", 5, 15.99m, BookCategory.Fiction));
        SharedLibrary.AddBook(new Book(2, "Sapiens", "Yuval Harari", 3, 20.50m, BookCategory.History));
    }

    public void Dispose()
    {
        Console.WriteLine("LibraryFixture: Disposing shared library...");
        SharedLibrary = null;
    }
}

[CollectionDefinition("LibraryCollection", typeof(LibraryFixture))]
public class LibraryCollection { }


[TestClass]
public class BookTests
{
    private Book _testBook;

    [TestClassInit]
    public static void ClassSetup()
    {
        Console.WriteLine("BookTests: Class initialization");
    }

    [TestMethodInit]
    public void Setup()
    {
        _testBook = new Book(1, "Test Book", "Test Author", 5, 10.99m, BookCategory.Fiction);
    }

    [TestMethodCleanup]
    public void Cleanup()
    {
        _testBook = null;
    }

    [TestMethod]
    [TestPriority(10)]
    public void TestBookCreation()
    {
        Assert.areEqual(1, _testBook.Id);
        Assert.areEqual("Test Book", _testBook.Title);
        Assert.areEqual(5, _testBook.Copies);
        Assert.areEqual("Test Author", _testBook.Author);
    }

    [TestMethod]
    [TestData(1, "Book1", "Author1", 3, 9.99)]
    [TestData(2, "Book2", "Author2", 10, 15.50)]
    [TestData(3, "Book3", "Author3", 0, 25.00)]
    public void TestBookCreationWithData(int id, string title, string author, int copies, double price)
    {
        var book = new Book(id, title, author, copies, (decimal)price, BookCategory.Fiction);
        Assert.areEqual(id, book.Id);
        Assert.areEqual(title, book.Title);
        Assert.areEqual(author, book.Author);
        Assert.isPositive(book.Id);
    }

    [TestMethod]
    public void TestBookIsAvailable()
    {
        Assert.isTrue(_testBook.IsAvailable());
        _testBook.Copies = 0;
        Assert.isFalse(_testBook.IsAvailable());
    }

    [TestMethod]
    public void TestBookReservation()
    {
        _testBook.IsReserved = true;
        Assert.isFalse(_testBook.IsAvailable());
        Assert.isTrue(_testBook.IsReserved);
    }

    [TestMethod]
    [TestData(5, 5.495)]
    [TestData(10, 21.98)]
    [TestData(3, 0)]
    public void TestCalculateDiscount(int quantity, double expectedDiscount)
    {
        var discount = _testBook.CalculateDiscount(quantity);
        Assert.isGreaterThanOrEqualTo(discount, 0m);
    }

    [TestMethod]
    public void TestDiscountForLargeQuantity()
    {
        var discount = _testBook.CalculateDiscount(10);
        Assert.isPositive(discount);
        Assert.isGreaterThan(discount, 20m);
    }

    [TestMethod]
    public void TestInvalidBookId()
    {
        try
        {
            var book = new Book(0, "Title", "Author", 5, 10m, BookCategory.Fiction);
            Assert.isTrue(false);
        }
        catch (ArgumentException ex)
        {
            Assert.isNotNull(ex);
            Assert.isTrue(ex.Message.Contains("Id"));
        }
    }

    [TestMethod]
    public void TestInvalidBookTitle()
    {
        try
        {
            var book = new Book(1, "", "Author", 5, 10m, BookCategory.Fiction);
            Assert.isTrue(false);
        }
        catch (ArgumentException ex)
        {
            Assert.isNotNull(ex);
        }
    }

    [TestMethod]
    public void TestNegativeCopies()
    {
        try
        {
            var book = new Book(1, "Title", "Author", -5, 10m, BookCategory.Fiction);
            Assert.isTrue(false);
        }
        catch (ArgumentException ex)
        {
            Assert.isNotNull(ex);
        }
    }

    [TestMethod]
    public void TestBookPrice()
    {
        Assert.isPositive(_testBook.Price);
        Assert.isGreaterThan(_testBook.Price, 0m);
    }

    [TestMethod]
    public void TestBookCopies()
    {
        Assert.isPositive(_testBook.Copies);
        Assert.isGreaterThanOrEqualTo(_testBook.Copies, 1);
    }

    [TestMethod]
    public void TestBookCategory()
    {
        Assert.areEqual(BookCategory.Fiction, _testBook.Category);
        Assert.areNotEqual(BookCategory.Science, _testBook.Category);
    }

    [TestMethod]
    [TestIgnore]
    public void TestIgnoredMethod()
    {
        Assert.isTrue(false);
    }

    [TestMethod]
    public void TestAreSameReference()
    {
        var book1 = _testBook;
        var book2 = _testBook;
        Assert.areSame(book1, book2);
    }

    [TestMethod]
    public void TestAreNotSameReference()
    {
        var book1 = new Book(1, "Book1", "Author", 1, 10m, BookCategory.Fiction);
        var book2 = new Book(2, "Book2", "Author", 1, 10m, BookCategory.Fiction);
        Assert.areNotSame(book1, book2);
    }

    [TestClassCleanup]
    public static void ClassCleanup()
    {
        Console.WriteLine("BookTests: Class cleanup");
    }
}


[TestClass]
public class MemberTests
{
    [TestMethod]
    [TestData(1, "John Doe", "john@test.com", MembershipType.Basic, 3)]
    [TestData(2, "Jane Smith", "jane@test.com", MembershipType.Premium, 10)]
    [TestData(3, "Bob Wilson", "bob@test.com", MembershipType.VIP, 20)]
    public void TestMaxBorrowLimit(int id, string name, string email, MembershipType type, int expectedLimit)
    {
        var member = new Member(id, name, email, type);
        Assert.areEqual(expectedLimit, member.MaxBorrowLimit());
        Assert.isPositive(member.MaxBorrowLimit());
    }

    [TestMethod]
    public void TestCanBorrowMore()
    {
        var member = new Member(1, "Test", "test@test.com", MembershipType.Basic);
        Assert.isTrue(member.CanBorrowMore());
        Assert.isEmpty(member.BorrowedBooks);

        member.BorrowedBooks.Add(1);
        member.BorrowedBooks.Add(2);
        member.BorrowedBooks.Add(3);

        Assert.isFalse(member.CanBorrowMore());
        Assert.isNotEmpty(member.BorrowedBooks);
        Assert.areEqual(3, member.BorrowedBooks.Count);
    }

    [TestMethod]
    public void TestInvalidMemberId()
    {
        try
        {
            var member = new Member(-1, "Name", "email@test.com", MembershipType.Basic);
            Assert.isTrue(false);
        }
        catch (ArgumentException ex)
        {
            Assert.isNotNull(ex);
        }
    }

    [TestMethod]
    public void TestInvalidMemberIdNegative()
    {
        try
        {
            var member = new Member(-5, "Name", "email@test.com", MembershipType.Basic);
            Assert.isTrue(false);
        }
        catch (ArgumentException)
        {
            Assert.isTrue(true);
        }
    }

    [TestMethod]
    public void TestMemberBorrowedBooks()
    {
        var member = new Member(1, "Test", "test@test.com", MembershipType.Premium);
        Assert.isEmpty(member.BorrowedBooks);

        member.BorrowedBooks.Add(1);
        Assert.isNotEmpty(member.BorrowedBooks);
        Assert.areEqual(1, member.BorrowedBooks.Count);
    }

    [TestMethod]
    public void TestMembershipComparison()
    {
        var basic = new Member(1, "User1", "u1@test.com", MembershipType.Basic);
        var premium = new Member(2, "User2", "u2@test.com", MembershipType.Premium);

        Assert.isLessThan(basic.MaxBorrowLimit(), premium.MaxBorrowLimit());
        Assert.isGreaterThan(premium.MaxBorrowLimit(), basic.MaxBorrowLimit());
    }
}


[TestClass]
[TestCollection("LibraryCollection")]
public class LibraryBorrowingTests
{
    private LibraryFixture _fixture;

    public LibraryBorrowingTests(LibraryFixture fixture)
    {
        _fixture = fixture;
    }

    [TestMethod]
    public void TestSharedLibraryExists()
    {
        Assert.isNotNull(_fixture.SharedLibrary);
        Assert.areEqual("Central Library", _fixture.SharedLibrary.Name);
    }

    [TestMethod]
    public void TestSharedLibraryHasBooks()
    {
        Assert.isNotEmpty(_fixture.SharedLibrary.Books);
        Assert.isGreaterThan(_fixture.SharedLibrary.Books.Count, 0);
    }

    [TestMethod]
    public void TestSharedLibraryCapacity()
    {
        Assert.isPositive(_fixture.SharedLibrary.Capacity);
        Assert.areEqual(1000, _fixture.SharedLibrary.Capacity);
    }
}

[TestClass]
[TestCollection("LibraryCollection")]
public class LibrarySearchTests
{
    private LibraryFixture _fixture;

    public LibrarySearchTests(LibraryFixture fixture)
    {
        _fixture = fixture;
    }

    [TestMethod]
    public void TestGetBooksByCategory()
    {
        var fictionBooks = _fixture.SharedLibrary.GetBooksByCategory(BookCategory.Fiction);
        Assert.isNotNull(fictionBooks);
        Assert.isNotEmpty(fictionBooks);
    }

    [TestAsync]
    public async Task TestSearchBooksByAuthorAsync()
    {
        var books = await _fixture.SharedLibrary.SearchBooksByAuthorAsync("Orwell");
        Assert.isNotNull(books);
        Assert.isGreaterThan(books.Count, 0);
    }

    [TestAsync]
    public async Task TestSearchBooksByAuthorAsyncNotFound()
    {
        var books = await _fixture.SharedLibrary.SearchBooksByAuthorAsync("NonExistentAuthor");
        Assert.isNotNull(books);
        Assert.isEmpty(books);
    }
}

[TestClass]
public class LibraryOperationsTests
{
    private Library _library;
    private Book _book;
    private Member _member;

    [TestMethodInit]
    public void Setup()
    {
        _library = new Library("Test Library", 100);
        _book = new Book(1, "Test", "Author", 5, 10m, BookCategory.Fiction);
        _member = new Member(1, "User", "user@test.com", MembershipType.Basic);
        _library.AddBook(_book);
        _library.AddMember(_member);
    }

    [TestMethod]
    [TestPriority(10)]
    public void TestLendBook()
    {
        var initialCopies = _book.Copies;
        var result = _library.LendBook(1, 1);

        Assert.isTrue(result);
        Assert.areEqual(4, _book.Copies);
        Assert.isLessThan(_book.Copies, initialCopies);
        Assert.isNotEmpty(_member.BorrowedBooks);
    }

    [TestMethod]
    public void TestReturnBook()
    {
        _library.LendBook(1, 1);
        var result = _library.ReturnBook(1, 1);

        Assert.isTrue(result);
        Assert.areEqual(5, _book.Copies);
        Assert.isEmpty(_member.BorrowedBooks);
    }

    [TestAsync]
    [TestPriority(8)]
    public async Task TestLendBookAsync()
    {
        var result = await _library.LendBookAsync(1, 1);
        Assert.isTrue(result);
        Assert.isLessThan(_book.Copies, 5);
    }

    [TestMethod]
    public void TestAddBookAtCapacity()
    {
        var smallLibrary = new Library("Small", 1);
        smallLibrary.AddBook(new Book(1, "Book1", "Author", 1, 10m, BookCategory.Fiction));

        try
        {
            smallLibrary.AddBook(new Book(2, "Book2", "Author", 1, 10m, BookCategory.Fiction));
            Assert.isTrue(false);
        }
        catch (InvalidOperationException ex)
        {
            Assert.isNotNull(ex);
        }
    }

    [TestMethod]
    public void TestAddDuplicateMember()
    {
        try
        {
            _library.AddMember(new Member(2, "Another", "user@test.com", MembershipType.Basic));
            Assert.isTrue(false);
        }
        catch (InvalidOperationException ex)
        {
            Assert.isNotNull(ex);
        }
    }

    [TestMethod]
    public void TestCalculateTotalValue()
    {
        var total = _library.CalculateTotalValue();
        Assert.isPositive(total);
        Assert.areEqual(50m, total);
        Assert.isGreaterThan(total, 0m);
    }

    [TestMethod]
    public void TestGetAvailableBooksCount()
    {
        var count = _library.GetAvailableBooksCount();
        Assert.areEqual(5, count);
        Assert.isPositive(count);
    }

    [TestMethod]
    public void TestAddNullBook()
    {
        try
        {
            _library.AddBook(null);
            Assert.isTrue(false);
        }
        catch (ArgumentNullException ex)
        {
            Assert.isNotNull(ex);
        }
    }

    [TestMethod]
    public void TestAddNullMember()
    {
        try
        {
            _library.AddMember(null);
            Assert.isTrue(false);
        }
        catch (ArgumentNullException ex)
        {
            Assert.isNotNull(ex);
        }
    }

    [TestMethod]
    public void TestLendNonExistentBook()
    {
        var result = _library.LendBook(999, 1);
        Assert.isFalse(result);
    }

    [TestMethod]
    public void TestReturnNonBorrowedBook()
    {
        var result = _library.ReturnBook(1, 1);
        Assert.isFalse(result);
    }

    [TestMethodCleanup]
    public void Cleanup()
    {
        _library = null;
        _book = null;
        _member = null;
    }
}


[TestClass]
public class ComparisonTests
{
    [TestMethod]
    public void TestGreaterThanComparisons()
    {
        Assert.isGreaterThan(10, 5);
        Assert.isGreaterThan(100.5, 50.2);
        Assert.isGreaterThanOrEqualTo(10, 10);
        Assert.isGreaterThanOrEqualTo(10, 5);
    }


    [TestMethod]
    public void TestLessThanComparisons()
    {
        Assert.isLessThan(5, 10);
        Assert.isLessThan(-5, 0);
        Assert.isLessThanOrEqualTo(5, 5);
        Assert.isLessThanOrEqualTo(5, 10);
    }

    [TestMethod]
    public void TestPositiveNegativeNumbers()
    {
        Assert.isPositive(5);
        Assert.isPositive(0.1m);
        Assert.isNegative(-5);
        Assert.isNegative(-0.1m);
    }

    [TestMethod]
    public void TestCollectionOperations()
    {
        var emptyList = new List<int>();
        var filledList = new List<int> { 1, 2, 3 };

        Assert.isEmpty(emptyList);
        Assert.isNotEmpty(filledList);
    }
}
