using System;
using System.Threading.Tasks;
using TestFramework;
using TestableProject;

namespace TestProject
{
    [TestClass]
    public class LibraryTests
    {
        private Library _library;

        [TestMethodInit]
        public void Setup()
        {
            _library = new Library();
            _library.AddBook(new Book(1, "Test Book", 2));
        }

        [TestMethodCleanup]
        public void Teardown()
        {
            _library = null;
        }

        [TestMethod]
        [TestPriority(3)]
        public void TestAddBook()
        {
            var book = new Book(2, "New Book", 1);
            _library.AddBook(book);

            Assert.areEqual(2, _library.Books.Count);
        }

        [TestMethod]
        [TestData(1, true)]
        [TestData(99, false)]
        [TestPriority(2)]
        public void TestLendBook(int bookId, bool expectedResult)
        {
            bool result = _library.LendBook(bookId);
            Assert.areEqual(expectedResult, result);
        }

        [TestMethod]
        [TestAsync]
        [TestPriority(1)]
        public async Task TestLendBookAsync()
        {
            bool result = await _library.LendBookAsync(1);

            Assert.isTrue(result);
            Assert.areEqual(1, _library.Books[0].Copies);
        }

        [TestMethod]
        [TestIgnore]
        public void TestRemoveBook()
        {
            throw new Exception("Этот код не должен выполняться");
        }
    }
}
