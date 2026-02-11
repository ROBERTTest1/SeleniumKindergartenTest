using System;
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

        _driver!.Navigate().GoToUrl(BaseUrl);

        var spaceshipNav = WaitForElement(By.XPath("//a[normalize-space()='Spaceship']"));
        spaceshipNav.Click();

        var createButton = WaitForElement(By.XPath("//a[normalize-space()='Create']"), TimeSpan.FromSeconds(15));
        Assert.That(createButton.Displayed, Is.True);
    }

    [Test]
    public void Spaceships_Create_WithValidData()
    {
        Assert.That(_driver, Is.Not.Null);

        _driver!.Navigate().GoToUrl(BaseUrl);

        WaitForElement(By.XPath("//a[normalize-space()='Spaceship']")).Click();
        WaitForElement(By.XPath("//a[normalize-space()='Create']")).Click();

        WaitForElement(By.Id("Name"));

        FillInput(By.Id("Name"), "TEST_SHIP_01");
        FillInput(By.Id("Classification"), "Explorer");
        SetDateTimeLocal(By.Id("BuiltDate"), new DateTime(2025, 2, 1, 12, 30, 0));
        FillInput(By.Id("Crew"), "5");
        FillInput(By.Id("EnginePower"), "5000");

        WaitForElement(By.CssSelector("form input[type='submit'][value='Create']")).Click();

        var successRow = WaitForElement(By.XPath("//td[normalize-space()='TEST_SHIP_01']"));
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

    private IWebElement WaitForElement(By by, TimeSpan? timeout = null)
    {
        var wait = new WebDriverWait(_driver!, timeout ?? TimeSpan.FromSeconds(10));
        wait.IgnoreExceptionTypes(typeof(NoSuchElementException));
        return wait.Until(driver => driver.FindElement(by));
    }
}
