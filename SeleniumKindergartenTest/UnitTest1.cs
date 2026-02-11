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

        var heading = WaitForElement(By.CssSelector("div.h1"), TimeSpan.FromSeconds(10));
        Assert.That(heading.Text, Does.Contain("Spaceships"));
        var table = WaitForElement(By.CssSelector("table"));

        Assert.That(table.Displayed, Is.True);
    }

    [Test]
    public void Spaceships_Create_WithValidData()
    {
        Assert.That(_driver, Is.Not.Null);

        NavigateTo($"{BaseUrl}/Spaceships");
        var initialRowCount = CountElements(By.CssSelector("table tbody tr"));

        NavigateTo($"{BaseUrl}/Spaceships/Create");
        WaitForElement(By.Id("Name"));

        var spaceshipName = $"TEST_SHIP_{DateTime.UtcNow:HHmmssfff}";

        FillInput(By.Id("Name"), spaceshipName);
        FillInput(By.Id("Classification"), "Explorer");
        SetDateTimeLocal(By.Id("BuiltDate"), new DateTime(2025, 2, 1, 12, 30, 0));
        FillInput(By.Id("Crew"), "5");
        FillInput(By.Id("EnginePower"), "5000");

        Click(By.CssSelector("input[type='submit'][value='Create']"));

        var listHeading = WaitForElement(By.CssSelector("div.h1"), TimeSpan.FromSeconds(10));
        Assert.That(listHeading.Text, Does.Contain("Spaceships"));
        var successRow = WaitForElement(By.XPath($"//td[normalize-space()='{spaceshipName}']"), TimeSpan.FromSeconds(20));

        Assert.That(successRow.Displayed, Is.True);
        var finalRowCount = CountElements(By.CssSelector("table tbody tr"));
        Assert.That(finalRowCount, Is.GreaterThan(initialRowCount));
    }

    [Test]
    public void Spaceships_View_Details()
    {
        Assert.That(_driver, Is.Not.Null);

        NavigateTo($"{BaseUrl}/Spaceships");

        var firstRow = WaitForElement(By.CssSelector("table tbody tr"), TimeSpan.FromSeconds(10));
        var spaceshipName = firstRow.FindElement(By.CssSelector("td:nth-child(2)")).Text.Trim();

        var detailsLink = firstRow.FindElement(By.LinkText("Details"));
        ClickElement(detailsLink);

        WaitForPageLoad();

        var detailHeading = WaitForElement(By.CssSelector("h1"), TimeSpan.FromSeconds(10));
        Assert.That(detailHeading.Text, Does.Contain("Details"));

        var pageText = WaitForElement(By.TagName("body")).Text;
        Assert.That(pageText, Does.Contain(spaceshipName));
    }

    [Test]
    public void Kindergarten_Create_WithValidData()
    {
        Assert.That(_driver, Is.Not.Null);

        NavigateTo($"{BaseUrl}/Kindergarten");
        var initialRowCount = CountElements(By.CssSelector("table tbody tr"));

        NavigateTo($"{BaseUrl}/Kindergarten/Create");

        var groupName = $"Group {DateTime.UtcNow:HHmmss}";

        FillInput(By.Id("GroupName"), groupName);
        FillInput(By.Id("KindergartenName"), "Sunshine KG");
        FillInput(By.Id("ChildrenCount"), "18");
        FillInput(By.Id("TeacherName"), "Ms. Smith");

        var imagePath = ResolveAssetPath("TestAssets/pilt.jpg");
        WaitForElement(By.Id("imageFiles")).SendKeys(imagePath);

        Click(By.CssSelector("button[type='submit'].btn-success"));

        var kindergartenHeading = WaitForElement(By.CssSelector("div.h1"), TimeSpan.FromSeconds(10));
        Assert.That(kindergartenHeading.Text, Does.Contain("Kindergarten"));

        var successRow = WaitForElement(By.XPath($"//td[normalize-space()='{groupName}']"), TimeSpan.FromSeconds(20));
        Assert.That(successRow.Displayed, Is.True);

        var finalRowCount = CountElements(By.CssSelector("table tbody tr"));
        Assert.That(finalRowCount, Is.GreaterThan(initialRowCount));
    }

    [Test]
    public void Kindergarten_View_Details()
    {
        Assert.That(_driver, Is.Not.Null);

        NavigateTo($"{BaseUrl}/Kindergarten");

        var firstRow = WaitForElement(By.CssSelector("table tbody tr"), TimeSpan.FromSeconds(10));
        var groupName = firstRow.FindElement(By.CssSelector("td:nth-child(2)")).Text.Trim();

        var detailsLink = firstRow.FindElement(By.LinkText("Details"));
        ClickElement(detailsLink);

        WaitForPageLoad();

        var heading = WaitForElement(By.CssSelector("h1"), TimeSpan.FromSeconds(10));
        Assert.That(heading.Text, Does.Contain("Details"));

        var pageText = WaitForElement(By.TagName("body")).Text;
        Assert.That(pageText, Does.Contain(groupName));
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

    private void ClickElement(IWebElement element)
    {
        var executor = (IJavaScriptExecutor)_driver!;
        executor.ExecuteScript("arguments[0].scrollIntoView({ block: 'center' });", element);
        executor.ExecuteScript("arguments[0].click();", element);
    }

    private int CountElements(By by) => _driver!.FindElements(by).Count;

    private IWebElement WaitForElement(By by, TimeSpan? timeout = null)
    {
        var wait = new WebDriverWait(_driver!, timeout ?? TimeSpan.FromSeconds(20));
        wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
        return wait.Until(driver => driver.FindElement(by));
    }
}
