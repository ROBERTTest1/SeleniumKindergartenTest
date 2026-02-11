using OpenQA.Selenium;
using OpenQA.Selenium.Safari;

namespace SeleniumKindergartenTest.Drivers;

public static class SafariDriverFactory
{
    public static IWebDriver Create()
    {
        return new SafariDriver(new SafariOptions());
    }
}
