using ParticleConverter.util;
using System.Windows;
using System.Windows.Threading;

namespace ParticleConverter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public App()
        {
        }

        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.WriteExceptionLog(e.Exception);
            MessageBox.Show(
                e.Exception.ToString(),
                "Particle Converter",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            // Marking it handled keeps the window open. Without this the app reported the error
            // and then exited, losing whatever the user had set up.
            e.Handled = true;
        }
    }
}
