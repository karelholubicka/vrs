using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    static class Program
    {
        /// <summary>
        /// Hlavní vstupní bod aplikace.
        /// </summary>
        [MTAThread]
        static void Main()
        {
//            for (int i = 0; i < 4; i++)
            {
                        var mainWindow = new MainWindow();
                        Application.Run(mainWindow);
            }

         //   var mainGameWindow = new MainGameWindow();
          // mainGameWindow.Run(60);

        }
    }
}
