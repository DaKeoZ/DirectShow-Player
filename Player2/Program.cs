using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace fr.ipmfrance.webcam
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main( )
        {
            /*for (int i = 0; i < Assembly.GetExecutingAssembly().GetTypes().Length; i++)
            {
                Debug.WriteLine(Assembly.GetExecutingAssembly().GetTypes()[i]);
            }*/

            Debug.WriteLine(Assembly.GetExecutingAssembly().GetTypes().Length);
            Application.EnableVisualStyles( );
            Application.SetCompatibleTextRenderingDefault( false );
            Application.Run( new gui.MainForm( ) );
        }
    }
}
