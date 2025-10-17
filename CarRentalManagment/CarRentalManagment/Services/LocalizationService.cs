using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace CarRentalManagment.Services
{
    public class LocalizationService : ILocalizationService
    {
        private CultureInfo _currentCulture = CultureInfo.GetCultureInfo("en-US");

        public CultureInfo CurrentCulture => _currentCulture;

        public event EventHandler<CultureInfo>? LanguageChanged;

        public void ApplyLanguage(string cultureName)
        {
            if (string.IsNullOrWhiteSpace(cultureName))
            {
                throw new ArgumentException("A culture name is required.", nameof(cultureName));
            }

            var culture = CultureInfo.GetCultureInfo(cultureName);

            void Apply()
            {
                _currentCulture = culture;
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
                FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
                    new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));
                LanguageChanged?.Invoke(this, _currentCulture);
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.Invoke(Apply);
            }
            else
            {
                Apply();
            }
        }
    }
}
