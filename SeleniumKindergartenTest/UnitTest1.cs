using System;
using System.IO;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumKindergartenTest.Drivers;

namespace SeleniumKindergartenTest;

public class Tests
{
    private const string BaseUrl = "http://localhost:5138";
    private IWebDriver? _driver;

    [SetUp]
    public void Setup()
    {
        _driver = SafariDriverFactory.Create();
    }

    [TearDown]
    public void TearDown()
    {
        _driver?.Quit();
        _driver?.Dispose();
    }

    [Test]
    public void Smoke_LandingPageLoads()
    {
        Assert.That(_driver, Is.Not.Null);

        _driver!.Navigate().GoToUrl(BaseUrl);

        Assert.That(_driver.Title, Is.Not.Empty);
    }

    [Test]
    public void Spaceships_NavigateToList()
    {
        Assert.That(_driver, Is.Not.Null);

        NavigateTo($"{BaseUrl}/Spaceships");
        WaitForElement(By.XPath("//h1[contains(normalize-space(),'Spaceships')]"));

        var createButton = WaitForElement(By.CssSelector("a[href='/Spaceships/Create']"), TimeSpan.FromSeconds(20));
        Assert.That(createButton.Displayed, Is.True);
    }

    [Test]
    public void Spaceships_Create_WithValidData()
    {
        Assert.That(_driver, Is.Not.Null);

        NavigateTo($"{BaseUrl}/Spaceships/Create");
        WaitForElement(By.Id("Name"));

        FillInput(By.Id("Name"), "TEST_SHIP_11");
        FillInput(By.Id("Classification"), "Explorer");
        SetDateTimeLocal(By.Id("BuiltDate"), new DateTime(2025, 2, 1, 12, 30, 0));
        FillInput(By.Id("Crew"), "5");
        FillInput(By.Id("EnginePower"), "5000");

        Click(By.CssSelector("input[type='submit'][value='Create']"));

        var successRow = WaitForElement(By.XPath("//td[normalize-space()='TEST_SHIP_11']"), TimeSpan.FromSeconds(20));
        Assert.That(successRow.Displayed, Is.True);
    }

    [Test]
    public void Kindergarten_Create_WithValidData()
    {
        Assert.That(_driver, Is.Not.Null);

        NavigateTo($"{BaseUrl}/Kindergarten/Create");

        FillInput(By.Id("GroupName"), "Group Alpha");
        FillInput(By.Id("KindergartenName"), "Sunshine KG");
        FillInput(By.Id("ChildrenCount"), "18");
        FillInput(By.Id("TeacherName"), "Ms. Smith");

        var imagePath = ResolveAssetPath("TestAssets/pilt.jpg");
        WaitForElement(By.Id("imageFiles")).SendKeys(imagePath);

        Click(By.CssSelector("button[type='submit'].btn-success"));

        WaitForElement(By.XPath("//h1[contains(normalize-space(),'Kindergarten')]"), TimeSpan.FromSeconds(10));

        var successRow = WaitForElement(By.XPath("//td[normalize-space()='Group Alpha']"), TimeSpan.FromSeconds(20));
        Assert.That(successRow.Displayed, Is.True);
    }

    private void FillInput(By by, string value)
    {
        var input = WaitForElement(by);
        input.Clear();
        input.SendKeys(value);
    }

    private void SetDateTimeLocal(By by, DateTime value)
    {
        var input = WaitForElement(by);
        var executor = (IJavaScriptExecutor)_driver!;
        executor.ExecuteScript("arguments[0].value = arguments[1];", input, value.ToString("yyyy-MM-ddTHH:mm"));
    }

    private static string ResolveAssetPath(string relativePath)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
        return Path.Combine(projectRoot, relativePath);
    }

    private void NavigateTo(string url)
    {
        _driver!.Navigate().GoToUrl(url);
        WaitForPageLoad();
    }

    private void WaitForPageLoad(TimeSpan? timeout = null)
    {
        var wait = new WebDriverWait(_driver!, timeout ?? TimeSpan.FromSeconds(15));
        wait.Until(driver =>
            ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete"));
    }

    private void Click(By by)
    {
        var element = WaitForElement(by);
        var executor = (IJavaScriptExecutor)_driver!;
        executor.ExecuteScript("arguments[0].scrollIntoView({ block: 'center' });", element);
        executor.ExecuteScript("arguments[0].click();", element);
    }

    private IWebElement WaitForElement(By by, TimeSpan? timeout = null)
    {
        var wait = new WebDriverWait(_driver!, timeout ?? TimeSpan.FromSeconds(20));
        wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
        return wait.Until(driver => driver.FindElement(by));
    }
}
