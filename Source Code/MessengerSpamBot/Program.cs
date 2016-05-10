using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using System.IO;

/*
Amazing Chat Bot for messenger.com
Created by Simon Duchastel 
*/

namespace MessengerSpamBot
{
    class Program
    {
        static IWebDriver driver;
        static Thread inputThread;
        const string defaultChatRoom = "530610663768168";
        const string signInFailedURL = "https://www.messenger.com/login/password/";

        static TrollSpecific troll;
        static UniformResponse uniformResponse;
        static Detect detect;

        static void Main(string[] args)
        {
            try
            {
                troll = new TrollSpecific();
                uniformResponse = new UniformResponse();
                detect = new Detect();

                Console.Title = "Messenger Spam Bot";

                SpamAction.WriteLineClean("Starting ChromeDriver...");
                driver = new ChromeDriver(Directory.GetCurrentDirectory());
                SpamAction.WriteLineClean("ChromeDriver is running");

                SpamAction.WriteLineClean("Navigating to https://www.messenger.com...");
                driver.Navigate().GoToUrl("https://www.messenger.com");
                SpamAction.WriteLineClean("Navigation successful");

                SpamAction.WriteLineClean("Starting sign-in...");
                SignIn();
                SpamAction.WriteLineClean("Sign-in complete");

                inputThread = new Thread(new ThreadStart(CheckForInput));

                Thread.Sleep(5000);
                while (true)
                {
                    try
                    {
                        Respond();
                    }
                    catch(Exception ex)
                    {
                        SpamAction.WriteLineClean("Error: " + ex.Message);
                    }
                }
            }
            catch(Exception ex)
            {
                SpamAction.WriteLineClean("Error: " + ex.Message);
                SpamAction.WriteLineClean("Exiting, press any key to continue...: ");
                Console.ReadKey();
                Environment.Exit(0);
            }
        }

        static void Respond()
        {
            if (!inputThread.IsAlive)
            {
                inputThread = new Thread(new ThreadStart(CheckForInput));
                inputThread.Start();
            }

            troll.Check(driver);
            uniformResponse.Check(driver);
            detect.Check(driver);
            
        }

        static void SignIn()
        {
            IWebElement emailEntry = driver.FindElement(By.Name("email"));
            IWebElement passwordEntry = driver.FindElement(By.Name("pass"));
            IWebElement submitButton = driver.FindElement(By.Name("login"));

            Console.Write("Username: ");
            string email = Console.ReadLine();
            Console.Write("Password: ");
            string password = string.Empty;
            char typedKey = Console.ReadKey(true).KeyChar;
            while(typedKey != '\r')
            {
                password += typedKey;
                typedKey = Console.ReadKey(true).KeyChar;
            }
            Console.WriteLine();

            emailEntry.SendKeys(email);
            passwordEntry.SendKeys(password);

            submitButton.Click();
            if(driver.Url == signInFailedURL) { throw new Exception("Sign-in failed"); }
            GoToChat(defaultChatRoom);
        }

        static void CheckForInput()
        {
            try
            {
                    Console.Write(">");
                string[] consoleInput = Console.ReadLine().Split('~');
                switch (consoleInput[0])
                {
                    case "write":
                        SpamAction.WriteToChat(consoleInput[1], driver);
                        break;
                    case "moveto":
                        GoToChat(consoleInput[1]);
                        break;
                    case "quit":
                        Exit();
                        break;
                    case "exit":
                        Exit();
                        break;
                    case "trollDisable":
                        troll.isActive = false;
                        SpamAction.WriteLineClean("troll specific person is now disabled");
                        break;
                    case "troll":
                        troll.personToTroll = consoleInput[1];
                        troll.message = consoleInput[2];
                        if(consoleInput.Length >= 4) { troll.timesToRepeat = int.Parse(consoleInput[3]); }
                        else { troll.timesToRepeat = 1; }
                        troll.isActive = true;
                        SpamAction.WriteLineClean("troll specific person is now active");
                        break;
                    case "printTroll":
                        troll.PrintStatus();
                        break;
                    case "uniformResponseDisable":
                        uniformResponse.isActive = false;
                        SpamAction.WriteLineClean("uniform response is now disabled");
                        break;
                    case "uniformResponse":
                        uniformResponse.message = consoleInput[1];
                        uniformResponse.isActive = true;
                        if (consoleInput.Length >= 3) { uniformResponse.timesToRepeat = int.Parse(consoleInput[2]); }
                        else { uniformResponse.timesToRepeat = 1; }
                        SpamAction.WriteLineClean("uniform response is now active");
                        break;
                    case "printUniformResponse":
                        uniformResponse.PrintStatus();
                        break;
                    case "detectDisable":
                        detect.isActive = false;
                        SpamAction.WriteLineClean("detection is now disabled");
                        break;
                    case "detect":
                        detect.stringToDetect = consoleInput[1];
                        detect.message = consoleInput[2];
                        detect.isActive = true;
                        if (consoleInput.Length >= 4) { detect.timesToRepeat = int.Parse(consoleInput[3]); }
                        else { detect.timesToRepeat = 1; }
                        SpamAction.WriteLineClean("detection is now active");
                        break;
                    case "printDetect":
                        detect.PrintStatus();
                        break;
                    case "printStatus":
                        SpamAction.WriteLineClean("Troll status:");
                        troll.PrintStatus();
                        SpamAction.WriteLineClean("Uniform response status:");
                        uniformResponse.PrintStatus();
                        SpamAction.WriteLineClean("Detect status:");
                        detect.PrintStatus();
                        break;
                    case "clear":
                        Console.Clear();
                        break;
                    case "help":
                        StreamReader reader = new StreamReader(Directory.GetCurrentDirectory() + "\\help.txt");
                        string nextLine = reader.ReadLine();
                        while(nextLine != null)
                        {
                            SpamAction.WriteLineClean(nextLine);
                            nextLine = reader.ReadLine();
                        }
                        reader.Close();
                        break;
                    default:
                        SpamAction.WriteLineClean("Error: unknown command");
                        break;
                 }
            }
            catch (Exception ex)
            {
                SpamAction.WriteLineClean("Error: " + ex.Message);
            }
        }

        static void Exit()
        {
            SpamAction.WriteLineClean("Shutting down ChromeDriver...");
            driver.Quit();
            SpamAction.WriteLineClean("Exiting...");
            Environment.Exit(0);
        }

        static void GoToChat(string chatID)
        {
            SpamAction.WriteLineClean("Moving to chat room " + defaultChatRoom + "...");
            driver.Navigate().GoToUrl("https://www.messenger.com/t/" + chatID);
            SpamAction.WriteLineClean("Move successful");
        }
    }
}
 
class SpamAction
{
    public SpamAction()
    {

    }

    protected void SendEmojiToChat(IWebDriver driver)
    {
        IWebElement emojiButton = driver.FindElement(By.ClassName("_5j_u"));
        emojiButton.Click();
    }

    public static void WriteToChat(string write, IWebDriver driver)
    {
        IWebElement textInput = driver.FindElement(By.ClassName("_1mf"));
        WriteLineClean("Writing to chat: \"" + write + "\"...");
        new Actions(driver).MoveToElement(textInput).Click().Perform();
        new Actions(driver).SendKeys(write + "\n").Perform();
        WriteLineClean("Write successful");
    }

    public static void WriteLineClean(string output)
    {
        int oldTopPosition = Console.CursorTop;
        int oldLeftPosition = Console.CursorLeft;
        Console.MoveBufferArea(0, oldTopPosition, oldTopPosition, 1, 0, oldTopPosition + 1);
        Console.SetCursorPosition(0, oldTopPosition);
        Console.Write(output);
        int numLines = 1;
        for (int i = 0; i < output.Length; i++)
        {
            if (output[i] == '\n')
            {
                numLines++;
            }
        }
        Console.SetCursorPosition(oldLeftPosition, oldTopPosition + numLines);
    }
}

class TrollSpecific : SpamAction
{
    public TrollSpecific()
    {
        alreadyResponded = false;
        message = string.Empty;
        personToTroll = string.Empty;
        isActive = false;
        timesToRepeat = 1;
    }

    private bool alreadyResponded;
    public string message;
    public string personToTroll;
    public bool isActive;
    public int timesToRepeat;

    public void PrintStatus()
    {
        WriteLineClean("\tactive: " + isActive.ToString());
        WriteLineClean("\tmessage: " + message);
        WriteLineClean("\ttimes: " + timesToRepeat.ToString());
        WriteLineClean("\tusername: " + personToTroll);
    }

    public void Check(IWebDriver driver)
    {
        if (isActive)
        {
            IWebElement lastMessageAuthor = driver.FindElement(By.Id("js_2")).FindElements(By.TagName("h5")).Last();
            if (!alreadyResponded && lastMessageAuthor.Text == personToTroll)
            {
                string response = message;
                if (response.Contains("[USERNAME]"))
                {
                    response = response.Replace("[USERNAME]", lastMessageAuthor.Text);
                }
                if (response.Contains("[EMOJI]"))
                {
                    response = response.Replace("[EMOJI]", string.Empty);
                    for (int i = 0; i < timesToRepeat; i++)
                    {
                        SendEmojiToChat(driver);
                    }
                }
                alreadyResponded = true;
                for (int i = 0; i < timesToRepeat; i++)
                {
                    WriteToChat(response, driver);
                }
            }
            if (lastMessageAuthor.Text != personToTroll)
            {
                alreadyResponded = false;
            }
        }
    }
}

class UniformResponse : SpamAction
{
    public UniformResponse()
    {
        lastMessageOld = string.Empty;
        message = string.Empty;
        isActive = false;
        timesToRepeat = 1;
    }

    private string lastMessageOld;
    public string message;
    public bool isActive;
    public int timesToRepeat;

    public void PrintStatus()
    {
        WriteLineClean("\tactive: " + isActive.ToString());
        WriteLineClean("\tmessage: " + message);
        WriteLineClean("\ttimes: " + timesToRepeat.ToString());
    }

    public void Check(IWebDriver driver)
    {
        if (isActive)
        {

            IWebElement lastMessage = driver.FindElement(By.Id("js_2")).FindElements(By.TagName("div")).Last();
            IWebElement lastMessageAuthor = driver.FindElement(By.Id("js_2")).FindElements(By.TagName("h5")).Last();
            string response = message;
            if (response.Contains("[USERNAME]"))
            {
                response = response.Replace("[USERNAME]", lastMessageAuthor.Text);
            }
            if (response.Contains("[EMOJI]"))
            {
                response = response.Replace("[EMOJI]", string.Empty);
                if (lastMessage.Text != lastMessageOld && lastMessage.Text != response)
                {
                    for (int i = 0; i < timesToRepeat; i++)
                    {
                        SendEmojiToChat(driver);
                    }
                }
            }
            if (lastMessage.Text != lastMessageOld && lastMessage.Text != response)
            {
                lastMessageOld = lastMessage.Text;
                for (int i = 0; i < timesToRepeat; i++)
                {
                    WriteToChat(response, driver);
                }
            }
        }
    }
}

class Detect : SpamAction
{
    public Detect()
    {
        lastMessageOld = string.Empty;
        message = string.Empty;
        stringToDetect = string.Empty;
        isActive = false;
        timesToRepeat = 1;
    }

    private string lastMessageOld;
    public string message;
    public string stringToDetect;
    public bool isActive;
    public int timesToRepeat;

    public void PrintStatus()
    {
        WriteLineClean("\tactive: " + isActive.ToString());
        WriteLineClean("\tmessage: " + message);
        WriteLineClean("\tdetect string: " + stringToDetect);
        WriteLineClean("\ttimes: " + timesToRepeat.ToString());
    }

    public void Check(IWebDriver driver)
    {
        if (isActive)
        {
            IWebElement lastMessage = driver.FindElement(By.Id("js_2")).FindElements(By.TagName("div")).Last();
            IWebElement lastMessageAuthor = driver.FindElement(By.Id("js_2")).FindElements(By.TagName("h5")).Last();
            string response = message;
            if (response.Contains("[USERNAME]"))
            {
                response = response.Replace("[USERNAME]", lastMessageAuthor.Text);
            }
            if (response.Contains("[EMOJI]"))
            {
                response = response.Replace("[EMOJI]", string.Empty);
                if (lastMessage.Text != lastMessageOld && lastMessage.Text != response && lastMessage.Text.Contains(stringToDetect))
                {
                    for (int i = 0; i < timesToRepeat; i++)
                    {
                        SendEmojiToChat(driver);
                    }
                }
            }
            if (lastMessage.Text != lastMessageOld && lastMessage.Text != response && lastMessage.Text.Contains(stringToDetect))
            {
                lastMessageOld = lastMessage.Text;
                for (int i = 0; i < timesToRepeat; i++)
                {
                    WriteToChat(response, driver);
                }
            }
        }
    }
}