using NUnit.Framework;
using SOLITUDE.Editor;

namespace SOLITUDE.Tests
{
    public sealed class UnityMetaValidatorTests
    {
        [TestCase("Assets/_Project.zip", true)]
        [TestCase("Assets/_Project.zip.meta", true)]
        [TestCase("Assets/_Project 2.zip", true)]
        [TestCase("Assets/_Project 3.zip.meta", true)]
        [TestCase("Assets/_Project 123.zip", true)]
        [TestCase("Assets/_Project/Exports/_Project.zip", false)]
        [TestCase("Assets/_Project/Exports/_Project 2.zip.meta", false)]
        [TestCase("Assets/Other.zip", false)]
        [TestCase("Assets/_Project backup.zip", false)]
        [TestCase("Assets/_Project 2backup.zip", false)]
        [TestCase("Assets/_Project 2.zip.asset", false)]
        [TestCase("Assets/_Project 2.zip.meta\n", false)]
        [TestCase("Assets/_Project/Domain/Item.cs", false)]
        [TestCase("Assets/_Project/Domain/Item.cs.meta", false)]
        [TestCase("Assets/_Project.zip/meta", false)]
        [TestCase(null, false)]
        [TestCase("", false)]
        public void ReviewArchiveExceptionIsLimitedToRootReviewExports(string path, bool expected)
        {
            Assert.That(UnityMetaValidator.IsReviewArchivePath(path), Is.EqualTo(expected));
        }
    }
}
