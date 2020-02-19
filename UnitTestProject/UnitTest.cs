using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using todo.Controllers;
using todo.Services;
using todo.Models;
using Microsoft.AspNetCore.Mvc;

namespace UnitTestProject
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void TestMethod1()
        {
        }

        [TestMethod]
        public void TestDetailsView()
        {
            var controller = new ItemController(null);
            var result = controller.Create() as ViewResult;//Task(IActionResult);
            Assert.AreEqual(null, result.ViewName);

        }
        [TestMethod]
        public void TestDetailsViewCount()
        {
            var controller = new ItemController(null);
            var result = controller.Create() as ViewResult;//Task(IActionResult);
            Assert.AreEqual(0, result.ViewData.Count);

        }
    }
}
