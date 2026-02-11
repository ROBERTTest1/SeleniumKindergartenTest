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
    public void Kindergarten_Create_InvalidChildrenCount_ShowsError()
    {
        Assert.That(_driver, Is.Not.Null);

        NavigateTo($"{BaseUrl}/Kindergarten");
        var initialRowCount = CountElements(By.CssSelector("table tbody tr"));

        NavigateTo($"{BaseUrl}/Kindergarten/Create");

        var groupName = $"INVALID_CREATE_GROUP_{DateTime.UtcNow:HHmmss}";

        FillInput(By.Id("GroupName"), groupName);
        FillInput(By.Id("KindergartenName"), "Sunshine KG");
        SetInputValue(By.Id("ChildrenCount"), "A");
        FillInput(By.Id("TeacherName"), "Ms. Smith");

        var imagePath = ResolveAssetPath("TestAssets/pilt.jpg");
        WaitForElement(By.Id("imageFiles")).SendKeys(imagePath);

        Click(By.CssSelector("button[type='submit'].btn-success"));

        WaitUntil(driver => driver.Url.Contains("/Kindergarten/Create", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(10));
        var childrenInput = WaitForElement(By.Id("ChildrenCount"));
        var currentValue = childrenInput.GetDomProperty("value") ?? string.Empty;
        Assert.That(currentValue, Is.Not.EqualTo("A"), "ChildrenCount input accepted invalid letter");

        NavigateTo($"{BaseUrl}/Kindergarten");
        WaitUntil(_ => CountElements(By.CssSelector("table tbody tr")) <= initialRowCount, TimeSpan.FromSeconds(10));
        Assert.That(RowExistsInTable(groupName), Is.False);
    }

    [Test]
    public void Spaceships_Create_InvalidCrew_ShowsError()
    {
        Assert.That(_driver, Is.Not.Null);

        NavigateTo($"{BaseUrl}/Spaceships");
        var initialRowCount = CountElements(By.CssSelector("table tbody tr"));

        NavigateTo($"{BaseUrl}/Spaceships/Create");
        WaitForElement(By.Id("Name"));

        var spaceshipName = $"INVALID_CREATE_SHIP_{DateTime.UtcNow:HHmmssfff}";

        FillInput(By.Id("Name"), spaceshipName);
        FillInput(By.Id("Classification"), "Explorer");
        SetDateTimeLocal(By.Id("BuiltDate"), new DateTime(2025, 2, 1, 12, 30, 0));
        SetInputValue(By.Id("Crew"), "A");
        FillInput(By.Id("EnginePower"), "5000");

        Click(By.CssSelector("input[type='submit'][value='Create']"));

        WaitUntil(driver => driver.Url.Contains("/Spaceships/Create", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(10));
        var crewInput = WaitForElement(By.Id("Crew"));
        var currentValue = crewInput.GetDomProperty("value") ?? string.Empty;
        Assert.That(currentValue, Is.Not.EqualTo("A"), "Crew input accepted invalid letter");

        NavigateTo($"{BaseUrl}/Spaceships");
        WaitUntil(_ => CountElements(By.CssSelector("table tbody tr")) <= initialRowCount, TimeSpan.FromSeconds(10));
        Assert.That(RowExistsInTable(spaceshipName), Is.False);
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
    public void Spaceships_Delete_RemovesItem()
    {
        Assert.That(_driver, Is.Not.Null);

        NavigateTo($"{BaseUrl}/Spaceships");
        var initialRowCount = CountElements(By.CssSelector("table tbody tr"));

        NavigateTo($"{BaseUrl}/Spaceships/Create");
        WaitForElement(By.Id("Name"));

        var spaceshipName = $"DELETE_SHIP_{DateTime.UtcNow:HHmmssfff}";

        FillInput(By.Id("Name"), spaceshipName);
        FillInput(By.Id("Classification"), "Explorer");
        SetDateTimeLocal(By.Id("BuiltDate"), new DateTime(2025, 2, 1, 12, 30, 0));
        FillInput(By.Id("Crew"), "5");
        FillInput(By.Id("EnginePower"), "5000");

        Click(By.CssSelector("input[type='submit'][value='Create']"));

        WaitUntil(_ => RowExistsInTable(spaceshipName) && CountElements(By.CssSelector("table tbody tr")) == initialRowCount + 1, TimeSpan.FromSeconds(10));

        var createdRow = WaitForElement(By.XPath($"//table//tr[td[normalize-space()='{spaceshipName}']]"), TimeSpan.FromSeconds(10));
        ClickElement(createdRow.FindElement(By.LinkText("Delete")));

        WaitForPageLoad();
        ClickElement(WaitForElement(By.CssSelector("form input.btn-danger"), TimeSpan.FromSeconds(10)));

        WaitUntil(_ => !RowExistsInTable(spaceshipName) && CountElements(By.CssSelector("table tbody tr")) == initialRowCount, TimeSpan.FromSeconds(10));
    }

    [Test]
    public void Spaceships_Update_ChangesCrew()
    {
        Assert.That(_driver, Is.Not.Null);

        NavigateTo($"{BaseUrl}/Spaceships");

        NavigateTo($"{BaseUrl}/Spaceships/Create");
        WaitForElement(By.Id("Name"));

        var spaceshipName = $"UPDATE_SHIP_{DateTime.UtcNow:HHmmssfff}";

        FillInput(By.Id("Name"), spaceshipName);
        FillInput(By.Id("Classification"), "Explorer");
        SetDateTimeLocal(By.Id("BuiltDate"), new DateTime(2025, 2, 1, 12, 30, 0));
        FillInput(By.Id("Crew"), "5");
        FillInput(By.Id("EnginePower"), "5000");
        Click(By.CssSelector("input[type='submit'][value='Create']"));

        WaitUntil(_ => RowExistsInTable(spaceshipName), TimeSpan.FromSeconds(10));
        var createdRow = FindRowByCellText(spaceshipName);
        var originalCrewValue = GetCellText(createdRow, 5);

        ClickElement(createdRow.FindElement(By.LinkText("Update")));
        WaitUntil(driver => driver.Url.Contains("/Spaceships/update", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(10));
        WaitForPageLoad();

        var newCrewValue = "8";
        FillInput(By.Id("Crew"), newCrewValue);
        Click(By.CssSelector("input[type='submit'].btn-success"));

        WaitUntil(driver => driver.Url.EndsWith("/Spaceships", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(10));
        WaitUntil(_ => RowExistsInTable(spaceshipName), TimeSpan.FromSeconds(10));
        var updatedRow = FindRowByCellText(spaceshipName);
        var crewCell = GetCellText(updatedRow, 5);
        Assert.That(crewCell, Is.EqualTo(newCrewValue));
        Assert.That(crewCell, Is.Not.EqualTo(originalCrewValue));
    }

    [Test]
    public void Spaceships_Update_InvalidCrew_ShowsError()
    {
        Assert.That(_driver, Is.Not.Null);

        NavigateTo($"{BaseUrl}/Spaceships/Create");
        WaitForElement(By.Id("Name"));

        var spaceshipName = $"INVALID_SHIP_{DateTime.UtcNow:HHmmssfff}";

        FillInput(By.Id("Name"), spaceshipName);
        FillInput(By.Id("Classification"), "Explorer");
        SetDateTimeLocal(By.Id("BuiltDate"), new DateTime(2025, 2, 1, 12, 30, 0));
        FillInput(By.Id("Crew"), "5");
        FillInput(By.Id("EnginePower"), "5000");
        Click(By.CssSelector("input[type='submit'][value='Create']"));

        WaitUntil(_ => RowExistsInTable(spaceshipName), TimeSpan.FromSeconds(10));
        var createdRow = FindRowByCellText(spaceshipName);
        var originalCrewValue = GetCellText(createdRow, 5);

        ClickElement(createdRow.FindElement(By.LinkText("Update")));
        WaitUntil(driver => driver.Url.Contains("/Spaceships/update", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(10));
        WaitForPageLoad();

        SetInputValue(By.Id("Crew"), "A");
        Click(By.CssSelector("input[type='submit'].btn-success"));

        WaitUntil(driver => driver.Url.Contains("/Spaceships/update", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(10));
        var crewInput = WaitForElement(By.Id("Crew"));
        var currentValue = crewInput.GetDomProperty("value") ?? string.Empty;
        Assert.That(currentValue, Is.Not.EqualTo("A"), "Crew input accepted invalid letter");

        NavigateTo($"{BaseUrl}/Spaceships");
        var row = FindRowByCellText(spaceshipName);
        var crewCell = GetCellText(row, 5);
        Assert.That(
            crewCell,
            Is.EqualTo(originalCrewValue).Or.EqualTo(string.Empty),
            "Crew value changed despite invalid input");
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
    public void Kindergarten_Delete_RemovesItem()
    {
        Assert.That(_driver, Is.Not.Null);

        NavigateTo($"{BaseUrl}/Kindergarten");
        var initialRowCount = CountElements(By.CssSelector("table tbody tr"));

        NavigateTo($"{BaseUrl}/Kindergarten/Create");

        var groupName = $"DELETE_GROUP_{DateTime.UtcNow:HHmmss}";

        FillInput(By.Id("GroupName"), groupName);
        FillInput(By.Id("KindergartenName"), "Sunshine KG");
        FillInput(By.Id("ChildrenCount"), "18");
        FillInput(By.Id("TeacherName"), "Ms. Smith");

        var imagePath = ResolveAssetPath("TestAssets/pilt.jpg");
        WaitForElement(By.Id("imageFiles")).SendKeys(imagePath);

        Click(By.CssSelector("button[type='submit'].btn-success"));

        WaitUntil(_ => RowExistsInTable(groupName) && CountElements(By.CssSelector("table tbody tr")) == initialRowCount + 1, TimeSpan.FromSeconds(10));

        var createdRow = WaitForElement(By.XPath($"//table//tr[td[normalize-space()='{groupName}']]"), TimeSpan.FromSeconds(10));
        ClickElement(createdRow.FindElement(By.LinkText("Delete")));

        WaitForPageLoad();
        ClickElement(WaitForElement(By.CssSelector("form input.btn-danger"), TimeSpan.FromSeconds(10)));

        WaitUntil(_ => !RowExistsInTable(groupName) && CountElements(By.CssSelector("table tbody tr")) == initialRowCount, TimeSpan.FromSeconds(10));
    }

    [Test]
    public void Kindergarten_Update_ChangesChildrenCount()
    {
        Assert.That(_driver, Is.Not.Null);

        NavigateTo($"{BaseUrl}/Kindergarten/Create");

        var groupName = $"UPDATE_GROUP_{DateTime.UtcNow:HHmmss}";

        FillInput(By.Id("GroupName"), groupName);
        FillInput(By.Id("KindergartenName"), "Sunshine KG");
        FillInput(By.Id("ChildrenCount"), "5");
        FillInput(By.Id("TeacherName"), "Ms. Smith");

        var imagePath = ResolveAssetPath("TestAssets/pilt.jpg");
        WaitForElement(By.Id("imageFiles")).SendKeys(imagePath);
        Click(By.CssSelector("button[type='submit'].btn-success"));

        WaitUntil(_ => RowExistsInTable(groupName), TimeSpan.FromSeconds(10));
        var createdRow = FindRowByCellText(groupName);

        ClickElement(createdRow.FindElement(By.LinkText("Update")));
        WaitUntil(driver => driver.Url.Contains("/Kindergarten/update", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(10));
        WaitForPageLoad();

        FillInput(By.Id("ChildrenCount"), "12");
        Click(By.CssSelector("button[type='submit'].btn-success"));

        WaitUntil(driver => driver.Url.EndsWith("/Kindergarten", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(10));
        WaitUntil(_ => RowExistsInTable(groupName), TimeSpan.FromSeconds(10));
        var updatedRow = FindRowByCellText(groupName);
        var childrenCountCell = GetCellText(updatedRow, 4);
        Assert.That(childrenCountCell, Is.EqualTo("12"));
    }

    [Test]
    public void Kindergarten_Update_InvalidChildrenCount_ShowsError()
    {
        Assert.That(_driver, Is.Not.Null);

        NavigateTo($"{BaseUrl}/Kindergarten/Create");

        var groupName = $"INVALID_GROUP_{DateTime.UtcNow:HHmmss}";

        FillInput(By.Id("GroupName"), groupName);
        FillInput(By.Id("KindergartenName"), "Sunshine KG");
        FillInput(By.Id("ChildrenCount"), "7");
        FillInput(By.Id("TeacherName"), "Ms. Smith");

        var imagePath = ResolveAssetPath("TestAssets/pilt.jpg");
        WaitForElement(By.Id("imageFiles")).SendKeys(imagePath);
        Click(By.CssSelector("button[type='submit'].btn-success"));

        WaitUntil(_ => RowExistsInTable(groupName), TimeSpan.FromSeconds(10));
        var createdRow = FindRowByCellText(groupName);
        var originalChildrenValue = GetCellText(createdRow, 4);

        ClickElement(createdRow.FindElement(By.LinkText("Update")));
        WaitForPageLoad();

        SetInputValue(By.Id("ChildrenCount"), "A");
        Click(By.CssSelector("button[type='submit'].btn-success"));

        WaitUntil(driver => driver.Url.Contains("/Kindergarten/update", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(10));
        var childrenInput = WaitForElement(By.Id("ChildrenCount"));
        var currentValue = childrenInput.GetDomProperty("value") ?? string.Empty;
        Assert.That(currentValue, Is.Not.EqualTo("A"), "ChildrenCount input accepted invalid letter");

        NavigateTo($"{BaseUrl}/Kindergarten");
        var row = FindRowByCellText(groupName);
        var childrenCountCell = GetCellText(row, 4);
        Assert.That(
            childrenCountCell,
            Is.EqualTo(originalChildrenValue).Or.EqualTo(string.Empty),
            "Children count changed despite invalid input");
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

    private void SetInputValue(By by, string value)
    {
        var input = WaitForElement(by);
        var executor = (IJavaScriptExecutor)_driver!;
        executor.ExecuteScript(@"
            arguments[0].value = arguments[1];
            arguments[0].dispatchEvent(new Event('input', { bubbles: true }));
            arguments[0].dispatchEvent(new Event('change', { bubbles: true }));
        ", input, value);
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

    private void WaitUntil(Func<IWebDriver, bool> condition, TimeSpan? timeout = null)
    {
        var wait = new WebDriverWait(_driver!, timeout ?? TimeSpan.FromSeconds(10));
        wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
        wait.Until(condition);
    }

    private bool RowExistsInTable(string cellText)
    {
        return _driver!.FindElements(By.XPath($"//table//tr[td[normalize-space()='{cellText}']]")).Count > 0;
    }

    private IWebElement FindRowByCellText(string cellText)
    {
        var locator = By.XPath($"//table//tr[td[normalize-space()='{cellText}']]");
        WaitUntil(_ => _driver!.FindElements(locator).Count > 0, TimeSpan.FromSeconds(20));
        return _driver!.FindElements(locator).First();
    }

    private string GetCellText(IWebElement row, int columnIndex)
    {
        return row.FindElement(By.CssSelector($"td:nth-child({columnIndex})")).Text.Trim();
    }
}
