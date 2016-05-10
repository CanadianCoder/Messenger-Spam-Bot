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
        static SpamBot bot;

        static void Main(string[] args)
        {
            bot = new SpamBot();
            bot.MainProgram();
        }
    }

    class SpamBot
    {
        static IWebDriver driver;
        static Thread inputThread;
        const string defaultChatRoom = "530610663768168";
        const string signInFailedURL = "https://www.messenger.com/login/password/";

        static TrollSpecific troll;
        static UniformResponse uniformResponse;
        static Detect detect;

        public void MainProgram()
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
                        IWebElement lastMessage = driver.FindElement(By.Id("js_2")).FindElements(By.TagName("div")).Last();
                        IWebElement lastMessageAuthor = driver.FindElement(By.Id("js_2")).FindElements(By.TagName("h5")).Last();
                        Respond(lastMessage, lastMessageAuthor);
                    }
                    catch (Exception ex)
                    {
                        SpamAction.WriteLineClean("Error: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                SpamAction.WriteLineClean("Error: " + ex.Message);
                SpamAction.WriteLineClean("Exiting, press any key to continue...: ");
                Console.ReadKey();
                Environment.Exit(0);
            }
        }

        void CheckForInput()
        {
            Console.Write(">");
            string command = Console.ReadLine();
            SpamAction.ExecuteCommand(command, troll, uniformResponse, detect, driver);
        }

        void SignIn()
        {
            IWebElement emailEntry = driver.FindElement(By.Name("email"));
            IWebElement passwordEntry = driver.FindElement(By.Name("pass"));
            IWebElement submitButton = driver.FindElement(By.Name("login"));

            Console.Write("Username: ");
            string email = Console.ReadLine();
            Console.Write("Password: ");
            string password = string.Empty;
            char typedKey = Console.ReadKey(true).KeyChar;
            while (typedKey != '\r')
            {
                password += typedKey;
                typedKey = Console.ReadKey(true).KeyChar;
            }
            Console.WriteLine();

            emailEntry.SendKeys(email);
            passwordEntry.SendKeys(password);

            submitButton.Click();
            if (driver.Url == signInFailedURL) { throw new Exception("Sign-in failed"); }
            SpamAction.GoToChat(defaultChatRoom, driver);
        }

        void Respond(IWebElement lastMessage, IWebElement lastMessageAuthor)
        {
            if (!inputThread.IsAlive)
            {
                inputThread = new Thread(new ThreadStart(CheckForInput));
                inputThread.Start();
            }

            SpamAction.CheckRemote(troll, uniformResponse, detect, driver);
            troll.Check(driver, lastMessageAuthor);
            uniformResponse.Check(driver, lastMessage, lastMessageAuthor);
            detect.Check(driver, lastMessage, lastMessageAuthor);
        }
    }

    class SpamAction
    {
        public SpamAction()
        {
            commandAlreadyExecuted = false;
            lastExecutedCommand = string.Empty;
        }

        protected void SendEmojiToChat(IWebDriver driver)
        {
            IWebElement emojiButton = driver.FindElement(By.ClassName("_5j_u"));
            emojiButton.Click();
        }

        private static bool commandAlreadyExecuted;
        private static string lastExecutedCommand;
        private const char commandChar = '~';
        public static string remoteCommandString = "[EXECUTE]";


        public static void CheckRemote(TrollSpecific troll, UniformResponse uniformResponse, Detect detect, IWebDriver driver)
        {
            IWebElement lastMessage = driver.FindElement(By.Id("js_2")).FindElements(By.TagName("div")).Last();
            if (lastMessage.Text.StartsWith(remoteCommandString))
            {
                string message = lastMessage.Text.Replace(remoteCommandString, string.Empty);
                if (!commandAlreadyExecuted)
                {
                    ExecuteCommand(message, troll, uniformResponse, detect, driver);
                    commandAlreadyExecuted = true;
                    lastExecutedCommand = message;
                }
                else if (message != lastExecutedCommand)
                {
                    commandAlreadyExecuted = false;
                }
            }
        }

        public static void ExecuteCommand(string command, TrollSpecific troll, UniformResponse uniformResponse,
                                          Detect detect, IWebDriver driver)
        {
            try
            {
                string[] consoleInput = command.Split(commandChar);
                switch (consoleInput[0])
                {
                    case "write":
                        SpamAction.WriteToChat(consoleInput[1], driver);
                        break;
                    case "moveto":
                        GoToChat(consoleInput[1], driver);
                        break;
                    case "quit":
                        Exit(driver);
                        break;
                    case "exit":
                        Exit(driver);
                        break;
                    case "trollDisable":
                        troll.isActive = false;
                        SpamAction.WriteLineClean("troll specific person is now disabled");
                        break;
                    case "troll":
                        troll.personToTroll = consoleInput[1];
                        troll.message = consoleInput[2];
                        if (consoleInput.Length >= 4) { troll.timesToRepeat = int.Parse(consoleInput[3]); }
                        else { troll.timesToRepeat = 1; }
                        troll.isActive = true;
                        SpamAction.WriteLineClean("troll specific person is now active");
                        break;
                    case "printTroll":
                        troll.PrintStatus(false, driver);
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
                        uniformResponse.PrintStatus(false, driver);
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
                        detect.PrintStatus(false, driver);
                        break;
                    case "printStatus":
                        SpamAction.WriteLineClean("Troll status:");
                        troll.PrintStatus(false, driver);
                        SpamAction.WriteLineClean("Uniform response status:");
                        uniformResponse.PrintStatus(false, driver);
                        SpamAction.WriteLineClean("Detect status:");
                        detect.PrintStatus(false, driver);
                        break;
                    case "printStatusToChat":
                        SpamAction.WriteLineClean("Writing status to chat...");
                        SpamAction.WriteToChat("Troll status:", driver);
                        troll.PrintStatus(true, driver);
                        SpamAction.WriteToChat("Uniform response status:", driver);
                        uniformResponse.PrintStatus(true, driver);
                        SpamAction.WriteToChat("Detect status:", driver);
                        detect.PrintStatus(true, driver);
                        SpamAction.WriteLineClean("Write successful");
                        break;
                    case "changeRemoteCommand":
                        remoteCommandString = consoleInput[1];
                        break;
                    case "clear":
                        Console.Clear();
                        break;
                    case "help":
                        StreamReader reader = new StreamReader(Directory.GetCurrentDirectory() + "\\help.txt");
                        string nextLine = reader.ReadLine();
                        while (nextLine != null)
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

        public static void Exit(IWebDriver driver)
        {
            SpamAction.WriteLineClean("Shutting down ChromeDriver...");
            driver.Quit();
            SpamAction.WriteLineClean("Exiting...");
            Environment.Exit(0);
        }

        public static void GoToChat(string chatID, IWebDriver driver)
        {
            SpamAction.WriteLineClean("Moving to chat room " + chatID + "...");
            driver.Navigate().GoToUrl("https://www.messenger.com/t/" + chatID);
            SpamAction.WriteLineClean("Move successful");
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

        public void PrintStatus(bool printToChat, IWebDriver driver)
        {
            if (printToChat)
            {
                WriteToChat("active: " + isActive.ToString(), driver);
                WriteToChat("tmessage: " + message, driver);
                WriteToChat("times: " + timesToRepeat.ToString(), driver);
                WriteToChat("username: " + personToTroll.ToString(), driver);
            }
            else
            {
                WriteLineClean("\tactive: " + isActive.ToString());
                WriteLineClean("\tmessage: " + message);
                WriteLineClean("\ttimes: " + timesToRepeat.ToString());
                WriteLineClean("\tusername: " + personToTroll.ToString());
            }
        }

        public void Check(IWebDriver driver, IWebElement lastMessageAuthor)
        {
            if (isActive)
            {
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

        public void PrintStatus(bool printToChat, IWebDriver driver)
        {
            if (printToChat)
            {
                WriteToChat("active: " + isActive.ToString(), driver);
                WriteToChat("message: " + message, driver);
                WriteToChat("times: " + timesToRepeat.ToString(), driver);
            }
            else
            {
                WriteLineClean("\tactive: " + isActive.ToString());
                WriteLineClean("\tmessage: " + message);
                WriteLineClean("\ttimes: " + timesToRepeat.ToString());
            }
        }

        public void Check(IWebDriver driver, IWebElement lastMessage, IWebElement lastMessageAuthor)
        {
            if (isActive)
            {
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

        public void PrintStatus(bool printToChat, IWebDriver driver)
        {
            if (printToChat)
            {
                WriteToChat("active: " + isActive.ToString(), driver);
                WriteToChat("message: " + message, driver);
                WriteToChat("detect string: " + stringToDetect, driver);
                WriteToChat("times: " + timesToRepeat.ToString(), driver);
            }
            else
            {
                WriteLineClean("\tactive: " + isActive.ToString());
                WriteLineClean("\tmessage: " + message);
                WriteLineClean("\tdetect string: " + stringToDetect);
                WriteLineClean("\ttimes: " + timesToRepeat.ToString());
            }
       }

        public void Check(IWebDriver driver, IWebElement lastMessage, IWebElement lastMessageAuthor)
        {
            if (isActive)
            {
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
}