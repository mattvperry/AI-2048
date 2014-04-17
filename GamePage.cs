using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.PageObjects;
using System.Text.RegularExpressions;

namespace AI_2048
{
    /// <summary>
    /// Selenium Page Object for the game itself
    /// </summary>
    public class GamePage : IDisposable
    {
        #region Constants
        /// <summary>
        /// URL of the game
        /// </summary>
        public static string GAME_SITE_URL = "http://gabrielecirulli.github.io/2048/";

        /// <summary>
        /// Relative path to chromedriver
        /// </summary>
        private static string CHROME_DRIVER_PATH = @"packages\Selenium.WebDriver.ChromeDriver.2.9.0.1\content";

        /// <summary>
        /// Regular express matching the CSS class of tile elements, used to extract tile information
        /// </summary>
        private string TileInfoRegex = @"tile-(\d+) tile-position-(\d)-(\d)";
        #endregion

        #region Page Object
        // <summary>
        /// Selenium web driver
        /// </summary>
        private IWebDriver Driver { get; set; }

        /// <summary>
        /// Page body, used for keypress
        /// </summary>
        [FindsBy(How = How.TagName, Using = "body")]
        private IWebElement PageBody { get; set; }

        /// <summary>
        /// Element that contains the game grid
        /// </summary>
        [FindsBy(How = How.ClassName, Using = "tile")]
        private IList<IWebElement> Tiles { get; set; }

        /// <summary>
        /// Element that contains the current score
        /// </summary>
        [FindsBy(How = How.ClassName, Using = "score-container")]
        private IWebElement Score { get; set; }
        #endregion

        #region Public Interface
        /// <summary>
        /// Default GamePage Constructor
        /// </summary>
        /// <param name="driver">Web driver to use</param>
        public GamePage()
        {
            Driver = new ChromeDriver(CHROME_DRIVER_PATH);
            Driver.Navigate().GoToUrl(GAME_SITE_URL);
            PageFactory.InitElements(Driver, this);
        }

        /// <summary>
        /// Gets the current game score
        /// </summary>
        /// <returns>Current score</returns>
        public int GetScore()
        {
            return Int32.Parse(Score.Text);
        }

        /// <summary>
        /// Read the current game state
        /// </summary>
        public int[,] GetGameState()
        {
            int[,] grid = new int[4, 4];
            foreach(var tile in Tiles)
            {
                string klass = tile.GetAttribute("class");
                Match matches = Regex.Match(klass, TileInfoRegex);
                int value = Int32.Parse(matches.Groups[1].Value);
                int xpos = Int32.Parse(matches.Groups[2].Value) - 1;
                int ypos = Int32.Parse(matches.Groups[3].Value) - 1;
                grid[xpos, ypos] = value;
            }
            return grid;
        }

        /// <summary>
        /// Make a move in the game
        /// </summary>
        /// <param name="key">Key to press</param>
        public void MakeMove(Moves move)
        {
            string key = "";
            switch (move)
            {
                case Moves.Up:
                    key = Keys.ArrowUp;
                    break;
                case Moves.Down:
                    key = Keys.ArrowDown;
                    break;
                case Moves.Left:
                    key = Keys.ArrowLeft;
                    break;
                case Moves.Right:
                    key = Keys.ArrowRight;
                    break;
            }
            PageBody.SendKeys(key);
        }

        /// <summary>
        /// Dispose of game page
        /// </summary>
        public void Dispose()
        {
            Driver.Quit();
        }
        #endregion
    }

    /// <summary>
    /// Enumeration of possible moves
    /// </summary>
    public enum Moves
    {
        Up,
        Down,
        Left,
        Right
    }
}
